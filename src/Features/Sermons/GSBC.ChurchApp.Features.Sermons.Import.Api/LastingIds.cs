using System.Security.Cryptography;
using System.Text;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api;

internal static class LastingIds
{
    private static readonly byte[] Namespace = new Guid("af284872-6d77-44e3-aaa3-f3eccadf212c").ToByteArray(bigEndian: true);

    public static Guid Sermon(string websiteBase, string listedPath) => NameBased($"sermon:{websiteBase.TrimEnd('/')}{listedPath}");

    public static Guid Series(string slug) => NameBased($"series:{slug.Trim().ToLowerInvariant()}");

    public static Guid Speaker(string key) => NameBased($"speaker:{key}");

    private static Guid NameBased(string name)
    {
        byte[] hash = SHA1.HashData([.. Namespace, .. Encoding.UTF8.GetBytes(name)]);
        hash[6] = (byte)(hash[6] & 0x0F | 0x50);
        hash[8] = (byte)(hash[8] & 0x3F | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
