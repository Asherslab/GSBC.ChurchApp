using Npgsql;

namespace GSBC.ChurchApp.Testing;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SharedPostgres _server;
    private readonly string _name;

    private TestDatabase(SharedPostgres server, string name)
    {
        _server = server;
        _name = name;
        ConnectionString = server.ConnectionStringFor(name);
    }

    public string ConnectionString { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        SharedPostgres server = await SharedPostgres.InstanceAsync();
        string name = $"test_{Guid.NewGuid():N}";
        await server.ExecuteAsync($"CREATE DATABASE {name}");
        return new TestDatabase(server, name);
    }

    public async Task DropAsync()
    {
        await using (NpgsqlConnection pooled = new(ConnectionString))
            NpgsqlConnection.ClearPool(pooled);
        await _server.ExecuteAsync($"DROP DATABASE IF EXISTS {_name} WITH (FORCE)");
    }

    public async ValueTask DisposeAsync() => await DropAsync();
}
