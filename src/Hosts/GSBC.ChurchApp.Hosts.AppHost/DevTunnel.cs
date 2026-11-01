namespace GSBC.ChurchApp.Hosts.AppHost;

internal sealed record DevTunnel(string Command, string WorkingDirectory, string Name)
{
    private const string TunnelName = "gsbc-dev";

    public static DevTunnel? Find()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string config = Path.Combine(home, ".cloudflared", "config.yml");
        if (!File.Exists(config) || !File.ReadAllText(config).Contains($"tunnel: {TunnelName}"))
            return null;

        string? command = new[] { "/opt/homebrew/bin/cloudflared", "/usr/local/bin/cloudflared" }
            .Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(':').Select(folder => Path.Combine(folder, "cloudflared")))
            .FirstOrDefault(File.Exists);

        return command is null ? null : new DevTunnel(command, home, TunnelName);
    }
}
