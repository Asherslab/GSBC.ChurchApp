using System.Security.Cryptography;
using System.Text;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal static class SourceIds
{
    private static readonly byte[] Namespace = new Guid("af284872-6d77-44e3-aaa3-f3eccadf212c").ToByteArray(bigEndian: true);

    public static Guid Sermon(string feedGuid) => NameBased($"sermon:{feedGuid.Trim()}");

    public static Guid Series(string slug) => NameBased($"series:{slug.Trim().ToLowerInvariant()}");

    public static Guid Speaker(string alias) => NameBased($"speaker:{alias}");

    private static Guid NameBased(string name)
    {
        byte[] hash = SHA1.HashData([.. Namespace, .. Encoding.UTF8.GetBytes(name)]);
        hash[6] = (byte)(hash[6] & 0x0F | 0x50);
        hash[8] = (byte)(hash[8] & 0x3F | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
