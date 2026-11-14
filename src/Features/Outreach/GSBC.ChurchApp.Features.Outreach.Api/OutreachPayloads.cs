using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal static class OutreachPayloads
{
    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        OutreachContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    public static T Decode<T>(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        return OutreachContractsModel.Instance.Deserialize<T>(stream);
    }
}
