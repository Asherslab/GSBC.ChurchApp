using System.Diagnostics.CodeAnalysis;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Platform.Contracts;

public static class Fingerprint
{
    public static string Of<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(TypeModel model, T message)
    {
        using MemoryStream stream = new();
        model.Serialize(stream, message);
        return Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
    }
}
