namespace GSBC.ChurchApp.Features.Outreach.Client.Planning;

public static class RouteColours
{
    private static readonly string[] All = ["#5275b2", "#008378", "#b75b1d", "#8466b5", "#33853f", "#c34c65", "#b54e9c", "#867416", "#906e4e"];
    private const string Quiet = "#8a8f99";

    public static int Count => All.Length;

    public static string Of(int colour) => All[Math.Abs(colour) % All.Length];

    public static string Faded(int colour) => Mix(Of(colour), Quiet, 0.45);

    private static string Mix(string hex, string with, double share)
    {
        int value = Convert.ToInt32(hex[1..], 16), other = Convert.ToInt32(with[1..], 16);
        int Channel(int shift) => (int)Math.Round(((value >> shift) & 0xFF) * share + ((other >> shift) & 0xFF) * (1 - share));
        return $"#{Channel(16):x2}{Channel(8):x2}{Channel(0):x2}";
    }
}
