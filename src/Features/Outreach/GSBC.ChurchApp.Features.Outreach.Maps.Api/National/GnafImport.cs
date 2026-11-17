using System.IO.Compression;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.National;

internal sealed class GnafImport(OutreachMapsDbContext db, IGnafSource source, NationalAddresses addresses, TimeProvider time)
{
    private const string Psv = "_psv.psv";
    private const string AddressDetail = "ADDRESS_DETAIL";
    private static readonly string[] StateTables = [AddressDetail, "STREET_LOCALITY", "LOCALITY", "LOCALITY_POINT", "ADDRESS_DEFAULT_GEOCODE", "STATE"];
    private static readonly string[] Authorities = ["STREET_TYPE_AUT", "STREET_SUFFIX_AUT"];
    private const string Schema = OutreachMapsDbContext.SchemaName;

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async Task<bool> DueAsync(CancellationToken cancellationToken) =>
        await addresses.CurrentAsync(cancellationToken) is not { } current
        || (await source.LatestAsync(cancellationToken)).Name != current.Name;

    public async Task ImportLatestAsync(CancellationToken cancellationToken)
    {
        GnafFile latest = await source.LatestAsync(cancellationToken);
        string file = Path.Combine(Path.GetTempPath(), $"gsbc-gnaf-{Guid.NewGuid():N}.zip");
        try
        {
            await source.DownloadAsync(latest, file, cancellationToken);
            await ImportAsync(latest.Name, latest.Url.ToString(), file, cancellationToken);
        }
        finally
        {
            File.Delete(file);
        }
    }

    public async Task ImportAsync(string name, string from, string zipPath, CancellationToken cancellationToken)
    {
        await addresses.ForgetUnfinishedAsync(cancellationToken);
        NationalReleaseRecord release = new() { Name = name, Source = from, StartedAt = Now };
        db.NationalReleases.Add(release);
        await db.SaveChangesAsync(cancellationToken);

        using ZipArchive zip = ZipFile.OpenRead(zipPath);
        NpgsqlConnection connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (string authority in Authorities)
                await StageAsync(connection, Staged(authority), Entry(zip, $"Authority_Code_{authority}{Psv}"), cancellationToken);
            foreach (string state in States(zip))
            {
                foreach (string table in StateTables)
                    await StageAsync(connection, Staged(table), Entry(zip, $"{state}_{table}{Psv}"), cancellationToken);
                await ExecuteAsync(connection, AddressesSql(release.Id), cancellationToken);
                await ExecuteAsync(connection, LocalitiesSql(release.Id), cancellationToken);
                foreach (string table in StateTables)
                    await ExecuteAsync(connection, $"DROP TABLE IF EXISTS {Staged(table)}", cancellationToken);
            }
        }
        finally
        {
            foreach (string table in StateTables.Concat(Authorities))
                await ExecuteAsync(connection, $"DROP TABLE IF EXISTS {Staged(table)}", CancellationToken.None);
            await db.Database.CloseConnectionAsync();
        }

        release.Addresses = await db.NationalAddresses.LongCountAsync(address => address.Release == release.Id, cancellationToken);
        release.Localities = await db.NationalLocalities.LongCountAsync(locality => locality.Release == release.Id, cancellationToken);
        if (release.Addresses == 0)
            throw new InvalidOperationException($"The G-NAF release {name} held no addresses");
        release.FinishedAt = Now;
        await db.SaveChangesAsync(cancellationToken);
        await addresses.ForgetOlderThanAsync(release.Id, cancellationToken);
    }

    public static string KeySql(string text) =>
        $"trim(regexp_replace(regexp_replace(regexp_replace(regexp_replace(lower({text}), '[''’.]', '', 'g'), '[^a-z0-9/-]+', ' ', 'g'), '(^| )-( |$)', ' ', 'g'), ' +', ' ', 'g'))";

    private static string Staged(string table) => $"gnaf_{table.ToLowerInvariant()}";

    private static IEnumerable<string> States(ZipArchive zip) =>
        zip.Entries.Select(entry => entry.Name)
            .Where(entryName => entryName.EndsWith($"_{AddressDetail}{Psv}", StringComparison.OrdinalIgnoreCase))
            .Select(entryName => entryName[..^($"_{AddressDetail}{Psv}".Length)])
            .Order(StringComparer.Ordinal);

    private static ZipArchiveEntry Entry(ZipArchive zip, string name) =>
        zip.Entries.FirstOrDefault(entry => entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"The G-NAF download has no {name}");

    private static async Task StageAsync(NpgsqlConnection connection, string table, ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using Stream stream = entry.Open();
        using StreamReader reader = new(stream);
        string header = await reader.ReadLineAsync(cancellationToken) ?? throw new InvalidOperationException($"{entry.Name} is empty");
        string[] columns = [.. header.Split('|').Select(column => column.Trim().ToLowerInvariant())];
        await ExecuteAsync(connection, $"DROP TABLE IF EXISTS {table}", cancellationToken);
        await ExecuteAsync(connection, $"CREATE TEMP TABLE {table} ({string.Join(", ", columns.Select(column => $"\"{column}\" text"))})", cancellationToken);
        await using (TextWriter import = await connection.BeginTextImportAsync(
                         $"COPY {table} FROM STDIN (FORMAT csv, DELIMITER '|', QUOTE E'\\x01', NULL E'\\x02')", cancellationToken))
        {
            char[] buffer = new char[1 << 16];
            int read;
            while ((read = await reader.ReadAsync(buffer, cancellationToken)) > 0)
                await import.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        await ExecuteAsync(connection, $"ANALYZE {table}", cancellationToken);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = new(sql, connection);
        command.CommandTimeout = 0;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string FullWord(string alias, string code) =>
        $"COALESCE(CASE WHEN length({alias}.name) > length({alias}.code) THEN {alias}.name ELSE {alias}.code END, {code})";

    private static string AddressesSql(int release)
    {
        string street = "initcap(sl.street_name)"
                        + $" || CASE WHEN sl.street_type_code <> '' THEN ' ' || initcap({FullWord("tt", "sl.street_type_code")}) ELSE '' END"
                        + $" || CASE WHEN sl.street_suffix_code <> '' THEN ' ' || initcap({FullWord("sx", "sl.street_suffix_code")}) ELSE '' END";
        return $"""
            INSERT INTO {Schema}.national_addresses (id, release, number, unit, street, locality, state, postcode, building, building_key, key, latitude, longitude)
            SELECT ad.address_detail_pid, {release},
                lower(ad.number_first_prefix || ad.number_first || ad.number_first_suffix
                    || CASE WHEN ad.number_last <> '' THEN '-' || ad.number_last_prefix || ad.number_last || ad.number_last_suffix ELSE '' END),
                lower(ad.flat_number_prefix || ad.flat_number || ad.flat_number_suffix),
                {street}, initcap(l.locality_name), st.state_abbreviation, ad.postcode,
                initcap(ad.building_name), {KeySql("ad.building_name")}, {KeySql($"{street} || ' ' || l.locality_name")},
                g.latitude::double precision, g.longitude::double precision
            FROM gnaf_address_detail ad
            JOIN gnaf_street_locality sl ON sl.street_locality_pid = ad.street_locality_pid
            JOIN gnaf_locality l ON l.locality_pid = ad.locality_pid
            JOIN gnaf_state st ON st.state_pid = l.state_pid
            JOIN gnaf_address_default_geocode g ON g.address_detail_pid = ad.address_detail_pid
            LEFT JOIN gnaf_street_type_aut tt ON tt.code = sl.street_type_code
            LEFT JOIN gnaf_street_suffix_aut sx ON sx.code = sl.street_suffix_code
            WHERE ad.date_retired = '' AND ad.alias_principal = 'P' AND g.latitude <> '' AND g.longitude <> ''
            ON CONFLICT DO NOTHING
            """;
    }

    private static string LocalitiesSql(int release) => $"""
        INSERT INTO {Schema}.national_localities (id, release, name, state, key, latitude, longitude)
        SELECT l.locality_pid, {release}, initcap(l.locality_name), st.state_abbreviation, {KeySql("l.locality_name")},
            lp.latitude::double precision, lp.longitude::double precision
        FROM gnaf_locality l
        JOIN gnaf_state st ON st.state_pid = l.state_pid
        JOIN gnaf_locality_point lp ON lp.locality_pid = l.locality_pid
        WHERE l.date_retired = '' AND lp.latitude <> '' AND lp.longitude <> ''
        ON CONFLICT DO NOTHING
        """;
}
