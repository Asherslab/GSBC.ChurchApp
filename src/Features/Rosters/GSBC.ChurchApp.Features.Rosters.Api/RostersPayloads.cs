using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Rosters.Contracts;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal static class RostersPayloads
{
    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        RostersContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    public static T Decode<T>(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        return RostersContractsModel.Instance.Deserialize<T>(stream);
    }

    public static string Hash(byte[] payload) => Convert.ToHexString(SHA256.HashData(payload));
}
