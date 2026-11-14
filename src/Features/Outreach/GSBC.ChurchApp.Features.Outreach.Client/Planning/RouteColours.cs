namespace GSBC.ChurchApp.Features.Outreach.Client.Planning;

public static class RouteColours
{
    private static readonly string[] All = ["#2a3d5e", "#00776d", "#a6531a", "#6b4e9b", "#2f7a3a", "#a8384f", "#1f6a9e", "#6d5f12", "#4d5b70"];

    public static int Count => All.Length;

    public static string Of(int colour) => All[Math.Abs(colour) % All.Length];

    public static string Faded(int colour) => Mix(Of(colour), 0.32);

    private static string Mix(string hex, double share)
    {
        int value = Convert.ToInt32(hex[1..], 16);
        int Channel(int shift) => (int)Math.Round(((value >> shift) & 0xFF) * share + 255 * (1 - share));
        return $"#{Channel(16):x2}{Channel(8):x2}{Channel(0):x2}";
    }
}
