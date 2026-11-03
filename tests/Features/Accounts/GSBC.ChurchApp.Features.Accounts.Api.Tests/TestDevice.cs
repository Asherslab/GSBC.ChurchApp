using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

internal sealed class TestDevice(string appId) : IDisposable
{
    public ECDsa Key { get; } = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public byte[] Point
    {
        get
        {
            ECParameters parameters = Key.ExportParameters(false);
            return [0x04, .. parameters.Q.X!, .. parameters.Q.Y!];
        }
    }

    public string KeyId => Convert.ToBase64String(SHA256.HashData(Point));

    public byte[] Assertion(byte[] clientData, uint counter)
    {
        byte[] authenticatorData =
        [
            .. SHA256.HashData(Encoding.UTF8.GetBytes(appId)), 0x40,
            (byte)(counter >> 24), (byte)(counter >> 16), (byte)(counter >> 8), (byte)counter,
        ];
        byte[] nonce = SHA256.HashData([.. authenticatorData, .. SHA256.HashData(clientData)]);
        CborWriter writer = new();
        writer.WriteStartMap(2);
        writer.WriteTextString("signature");
        writer.WriteByteString(Key.SignData(nonce, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
        writer.WriteTextString("authenticatorData");
        writer.WriteByteString(authenticatorData);
        writer.WriteEndMap();
        return writer.Encode();
    }

    public void Dispose() => Key.Dispose();
}
