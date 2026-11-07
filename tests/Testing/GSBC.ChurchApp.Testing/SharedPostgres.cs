using Npgsql;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Testing;

internal sealed class SharedPostgres
{
    private const string Image = "postgres:17";
    private const string ClusterName = "gsbc-churchapp-tests";
    private const int Attempts = 5;
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LockPoll = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan OneIdleCheck = TimeSpan.FromSeconds(1.5);
    private static readonly string ServerFile = Path.Combine(Path.GetTempPath(), "gsbc-churchapp-tests-postgres");
    private static readonly Lazy<Task<SharedPostgres>> Shared = new(StartAsync);

    private const string ServerThatStopsWhenUnused = $"""
        docker-entrypoint.sh postgres -c fsync=off -c synchronous_commit=off -c full_page_writes=off -c max_connections=1000 -c cluster_name={ClusterName} &
        server=$!
        idle=0
        while sleep 1 && kill -0 $server 2>/dev/null; do
          clients=$(psql -U postgres -tAc "select count(*) from pg_stat_activity where backend_type = 'client backend'" 2>/dev/null)
          if [ "$clients" = 1 ]; then idle=$((idle + 1)); else idle=0; fi
          if [ $idle -ge 30 ]; then kill -INT $server; wait $server; exit 0; fi
        done
        """;

    private static NpgsqlConnection? _keepsTheServerRunning;
    private readonly NpgsqlDataSource _admin;
    private readonly string _connectionString;

    private SharedPostgres(string connectionString)
    {
        _connectionString = connectionString;
        _admin = NpgsqlDataSource.Create(connectionString);
    }

    public static Task<SharedPostgres> InstanceAsync() => Shared.Value;

    public string ConnectionStringFor(string database) =>
        new NpgsqlConnectionStringBuilder(_connectionString) { Database = database }.ConnectionString;

    public async Task ExecuteAsync(string sql)
    {
        await using NpgsqlCommand command = _admin.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<SharedPostgres> StartAsync()
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                (string connectionString, bool joined) = await JoinOrStartAsync();
                if (joined && !await SomeoneElseHoldsItAsync())
                    await OutlastAnIdleCheckAsync();
                return new SharedPostgres(connectionString);
            }
            catch (Exception) when (attempt < Attempts)
            {
                await Task.Delay(OneIdleCheck);
            }
        }
    }

    private static async Task<(string ConnectionString, bool Joined)> JoinOrStartAsync()
    {
        await using FileStream state = await CrossProcessLockAsync();
        string running = await new StreamReader(state).ReadToEndAsync();
        if (running.Length > 0 && await TryKeepRunningAsync(running))
            return (running, true);

        string started = await StartServerAsync();
        await KeepRunningAsync(started);
        state.SetLength(0);
        await using StreamWriter writer = new(state);
        await writer.WriteAsync(started);
        return (started, false);
    }

    private static async Task<string> StartServerAsync()
    {
        using CancellationTokenSource timeout = new(StartTimeout);
        PostgreSqlContainer container = new PostgreSqlBuilder(Image)
            .WithCleanUp(false)
            .WithAutoRemove(true)
            .WithLabel("gsbc.churchapp", "tests")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithEntrypoint("sh", "-c", ServerThatStopsWhenUnused)
            .Build();
        await container.StartAsync(timeout.Token);
        return container.GetConnectionString();
    }

    private static async Task<bool> TryKeepRunningAsync(string connectionString)
    {
        try
        {
            await KeepRunningAsync(connectionString);
            if (await IsOursAsync())
                return true;
            await _keepsTheServerRunning!.DisposeAsync();
            _keepsTheServerRunning = null;
            return false;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    private static async Task KeepRunningAsync(string connectionString)
    {
        NpgsqlConnection connection = new(new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString);
        await connection.OpenAsync();
        _keepsTheServerRunning = connection;
    }

    private static async Task<bool> IsOursAsync()
    {
        await using NpgsqlCommand show = new("SHOW cluster_name", _keepsTheServerRunning);
        return await show.ExecuteScalarAsync() is ClusterName;
    }

    private static async Task<bool> SomeoneElseHoldsItAsync()
    {
        await using NpgsqlCommand others = new("""
            SELECT count(*) FROM pg_stat_activity
            WHERE backend_type = 'client backend' AND pid <> pg_backend_pid() AND application_name <> 'psql'
            """, _keepsTheServerRunning);
        return (long)(await others.ExecuteScalarAsync())! > 0;
    }

    private static async Task OutlastAnIdleCheckAsync()
    {
        await Task.Delay(OneIdleCheck);
        await using NpgsqlCommand ping = new("SELECT 1", _keepsTheServerRunning);
        await ping.ExecuteScalarAsync();
    }

    private static async Task<FileStream> CrossProcessLockAsync()
    {
        while (true)
        {
            try
            {
                return new FileStream(ServerFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(LockPoll);
            }
        }
    }
}
