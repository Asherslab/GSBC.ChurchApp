using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed record FetchedCalendar(bool Changed, string Text, string? ETag, string? LastModified);

internal sealed class ICalFetcher(HttpClient http)
{
    public const string UserAgent = "GSBC-ChurchApp/1.0 (+https://app.baptist.com.au)";
    private const int LargestCalendarBytes = 8 * 1024 * 1024;

    public static Uri? AddressOf(string source)
    {
        string text = source.Trim();
        if (text.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase))
            text = "https://" + text["webcal://".Length..];
        return Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) ? uri : null;
    }

    public async Task<FetchedCalendar> FetchAsync(Uri address, string? etag, string? lastModified, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, address);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.ParseAdd("text/calendar");
        if (etag is not null && EntityTagHeaderValue.TryParse(etag, out EntityTagHeaderValue? tag))
            request.Headers.IfNoneMatch.Add(tag);
        if (lastModified is not null && DateTimeOffset.TryParse(lastModified, out DateTimeOffset modified))
            request.Headers.IfModifiedSince = modified;

        using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotModified)
            return new FetchedCalendar(false, "", etag, lastModified);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > LargestCalendarBytes)
            throw new InvalidDataException("The calendar is too big to copy.");

        await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using MemoryStream copy = new();
        byte[] buffer = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (copy.Length + read > LargestCalendarBytes)
                throw new InvalidDataException("The calendar is too big to copy.");
            copy.Write(buffer, 0, read);
        }
        return new FetchedCalendar(true, Encoding.UTF8.GetString(copy.GetBuffer(), 0, (int)copy.Length),
            response.Headers.ETag?.ToString(), response.Content.Headers.LastModified?.ToString("R"));
    }

    public static SocketsHttpHandler PublicInternetOnly() => new()
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
        ConnectCallback = async (context, cancellationToken) =>
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            IPAddress address = addresses.FirstOrDefault(IsPublic)
                ?? throw new HttpRequestException($"{context.DnsEndPoint.Host} isn't on the public internet.");
            Socket socket = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return false;
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal || address.IsIPv6Multicast);
        byte[] bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 or 127 or 0 => false,
            100 when bytes[1] is >= 64 and <= 127 => false,
            169 when bytes[1] == 254 => false,
            172 when bytes[1] is >= 16 and <= 31 => false,
            192 when bytes[1] == 168 => false,
            >= 224 => false,
            _ => true,
        };
    }
}
