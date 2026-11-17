using System.IO.Compression;
using System.Text;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

internal sealed record GnafAddress(string Id, string Number, string Street, string Type, string Locality, double Latitude, double Longitude,
    string Unit = "", string Building = "", string Suffix = "", string Retired = "");

internal sealed class GnafFiles
{
    private const string Folder = "G-NAF/G-NAF NOVEMBER 2026/";
    private readonly List<GnafAddress> _addresses = [];

    public GnafFiles Add(GnafAddress address)
    {
        _addresses.Add(address);
        return this;
    }

    public string Write(string directory)
    {
        string path = Path.Combine(directory, $"gnaf-{Guid.NewGuid():N}.zip");
        using ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
        Entry(zip, "Authority Code/Authority_Code_STREET_TYPE_AUT_psv.psv", "CODE|NAME|DESCRIPTION", ["STREET|ST|STREET", "ROAD|RD|ROAD"]);
        Entry(zip, "Authority Code/Authority_Code_STREET_SUFFIX_AUT_psv.psv", "CODE|NAME|DESCRIPTION", ["E|EAST|EAST", "W|WEST|WEST"]);
        foreach (IGrouping<string, GnafAddress> state in _addresses.GroupBy(address => State(address.Locality)))
        {
            string prefix = $"Standard/{state.Key}_";
            string[] localities = [.. state.Select(address => address.Locality).Distinct()];
            string[] streets = [.. state.Select(StreetId).Distinct()];
            Entry(zip, $"{prefix}STATE_psv.psv", "STATE_PID|DATE_CREATED|DATE_RETIRED|STATE_NAME|STATE_ABBREVIATION", [$"{state.Key}1||| |{state.Key}"]);
            Entry(zip, $"{prefix}LOCALITY_psv.psv", "LOCALITY_PID|DATE_CREATED|DATE_RETIRED|LOCALITY_NAME|PRIMARY_POSTCODE|LOCALITY_CLASS_CODE|STATE_PID|GNAF_LOCALITY_PID|GNAF_RELIABILITY_CODE",
                [.. localities.Select(locality => $"{LocalityId(locality)}|||{locality.ToUpperInvariant()}||G|{state.Key}1||5")]);
            Entry(zip, $"{prefix}LOCALITY_POINT_psv.psv", "LOCALITY_POINT_PID|DATE_CREATED|DATE_RETIRED|LOCALITY_PID|PLANIMETRIC_ACCURACY|LONGITUDE|LATITUDE",
                [.. localities.Select(locality => state.First(address => address.Locality == locality))
                    .Select(address => $"P{LocalityId(address.Locality)}|||{LocalityId(address.Locality)}||{address.Longitude}|{address.Latitude}")]);
            Entry(zip, $"{prefix}STREET_LOCALITY_psv.psv", "STREET_LOCALITY_PID|DATE_CREATED|DATE_RETIRED|STREET_CLASS_CODE|STREET_NAME|STREET_TYPE_CODE|STREET_SUFFIX_CODE|LOCALITY_PID|GNAF_STREET_PID|GNAF_STREET_CONFIDENCE|GNAF_RELIABILITY_CODE",
                [.. streets.Select(id => state.First(address => StreetId(address) == id))
                    .Select(address => $"{StreetId(address)}|||C|{address.Street.ToUpperInvariant()}|{address.Type}|{address.Suffix}|{LocalityId(address.Locality)}||2|4")]);
            Entry(zip, $"{prefix}ADDRESS_DETAIL_psv.psv", AddressDetailHeader, [.. state.Select(address => Row(AddressDetailHeader, new Dictionary<string, string>
            {
                ["ADDRESS_DETAIL_PID"] = address.Id, ["DATE_RETIRED"] = address.Retired, ["BUILDING_NAME"] = address.Building.ToUpperInvariant(),
                ["FLAT_TYPE_CODE"] = address.Unit.Length > 0 ? "UNIT" : "", ["FLAT_NUMBER"] = address.Unit, ["NUMBER_FIRST"] = address.Number,
                ["STREET_LOCALITY_PID"] = StreetId(address), ["LOCALITY_PID"] = LocalityId(address.Locality), ["ALIAS_PRINCIPAL"] = "P",
                ["POSTCODE"] = Postcode(address.Locality), ["CONFIDENCE"] = "2", ["PRIMARY_SECONDARY"] = "P",
            }))]);
            Entry(zip, $"{prefix}ADDRESS_DEFAULT_GEOCODE_psv.psv", "ADDRESS_DEFAULT_GEOCODE_PID|DATE_CREATED|DATE_RETIRED|ADDRESS_DETAIL_PID|GEOCODE_TYPE_CODE|LONGITUDE|LATITUDE",
                [.. state.Select(address => $"G{address.Id}|||{address.Id}|PC|{address.Longitude}|{address.Latitude}")]);
        }
        return path;
    }

    private const string AddressDetailHeader =
        "ADDRESS_DETAIL_PID|DATE_CREATED|DATE_LAST_MODIFIED|DATE_RETIRED|BUILDING_NAME|LOT_NUMBER_PREFIX|LOT_NUMBER|LOT_NUMBER_SUFFIX|FLAT_TYPE_CODE|FLAT_NUMBER_PREFIX|FLAT_NUMBER|FLAT_NUMBER_SUFFIX|LEVEL_TYPE_CODE|LEVEL_NUMBER_PREFIX|LEVEL_NUMBER|LEVEL_NUMBER_SUFFIX|NUMBER_FIRST_PREFIX|NUMBER_FIRST|NUMBER_FIRST_SUFFIX|NUMBER_LAST_PREFIX|NUMBER_LAST|NUMBER_LAST_SUFFIX|STREET_LOCALITY_PID|LOCATION_DESCRIPTION|LOCALITY_PID|ALIAS_PRINCIPAL|POSTCODE|PRIVATE_STREET|LEGAL_PARCEL_ID|CONFIDENCE|ADDRESS_SITE_PID|LEVEL_GEOCODED_CODE|PROPERTY_PID|GNAF_PROPERTY_PID|PRIMARY_SECONDARY";

    private static string Row(string header, Dictionary<string, string> values) =>
        string.Join('|', header.Split('|').Select(column => values.GetValueOrDefault(column, "")));

    private static string State(string locality) => locality == "Tweed Heads" ? "NSW" : "QLD";

    private static string Postcode(string locality) => locality == "Tweed Heads" ? "2485" : "4035";

    private static string LocalityId(string locality) => $"LOC{locality.Replace(" ", "").ToUpperInvariant()}";

    private static string StreetId(GnafAddress address) => $"ST{address.Street.Replace(" ", "").ToUpperInvariant()}{address.Type}{LocalityId(address.Locality)}";

    private static void Entry(ZipArchive zip, string name, string header, IEnumerable<string> rows)
    {
        using StreamWriter writer = new(zip.CreateEntry(Folder + name).Open(), new UTF8Encoding(false));
        writer.Write(header + "\r\n");
        foreach (string row in rows)
            writer.Write(row + "\r\n");
    }
}
