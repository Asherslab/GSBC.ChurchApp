using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;

namespace GSBC.ChurchApp.Features.Accounts.Gateway.Tests;

internal sealed class SoftwareAuthenticator(string origin) : IDisposable
{
    private const byte UserPresent = 0x01;
    private const byte UserVerified = 0x04;
    private const byte AttestedCredentialData = 0x40;

    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly byte[] _credentialId = RandomNumberGenerator.GetBytes(16);
    private string _userHandle = "";
    private uint _signCount;

    public string Create(string creationOptionsJson)
    {
        JsonNode options = JsonNode.Parse(creationOptionsJson)!;
        _userHandle = options["user"]!["id"]!.GetValue<string>();
        string rpId = options["rp"]!["id"]!.GetValue<string>();

        byte[] authData = [.. AuthenticatorDataPrefix(rpId, UserPresent | UserVerified | AttestedCredentialData), .. new byte[16],
            (byte)(_credentialId.Length >> 8), (byte)_credentialId.Length, .. _credentialId, .. CosePublicKey()];

        CborWriter attestation = new(CborConformanceMode.Ctap2Canonical);
        attestation.WriteStartMap(3);
        attestation.WriteTextString("fmt");
        attestation.WriteTextString("none");
        attestation.WriteTextString("attStmt");
        attestation.WriteStartMap(0);
        attestation.WriteEndMap();
        attestation.WriteTextString("authData");
        attestation.WriteByteString(authData);
        attestation.WriteEndMap();

        return Credential(new JsonObject
        {
            ["clientDataJSON"] = ClientData("webauthn.create", options),
            ["attestationObject"] = WebEncoders.Base64UrlEncode(attestation.Encode()),
            ["transports"] = new JsonArray("internal"),
        });
    }

    public string Assert(string requestOptionsJson)
    {
        JsonNode options = JsonNode.Parse(requestOptionsJson)!;
        string rpId = options["rpId"]!.GetValue<string>();
        _signCount++;

        byte[] authenticatorData = AuthenticatorDataPrefix(rpId, UserPresent | UserVerified);
        string clientData = ClientData("webauthn.get", options);
        byte[] signed = [.. authenticatorData, .. SHA256.HashData(WebEncoders.Base64UrlDecode(clientData))];

        return Credential(new JsonObject
        {
            ["clientDataJSON"] = clientData,
            ["authenticatorData"] = WebEncoders.Base64UrlEncode(authenticatorData),
            ["signature"] = WebEncoders.Base64UrlEncode(_key.SignData(signed, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)),
            ["userHandle"] = _userHandle,
        });
    }

    private byte[] AuthenticatorDataPrefix(string rpId, int flags) =>
        [.. SHA256.HashData(Encoding.UTF8.GetBytes(rpId)), (byte)flags,
            (byte)(_signCount >> 24), (byte)(_signCount >> 16), (byte)(_signCount >> 8), (byte)_signCount];

    private byte[] CosePublicKey()
    {
        ECParameters parameters = _key.ExportParameters(includePrivateParameters: false);
        CborWriter cose = new(CborConformanceMode.Ctap2Canonical);
        cose.WriteStartMap(5);
        cose.WriteInt32(1);
        cose.WriteInt32(2);
        cose.WriteInt32(3);
        cose.WriteInt32(-7);
        cose.WriteInt32(-1);
        cose.WriteInt32(1);
        cose.WriteInt32(-2);
        cose.WriteByteString(parameters.Q.X!);
        cose.WriteInt32(-3);
        cose.WriteByteString(parameters.Q.Y!);
        cose.WriteEndMap();
        return cose.Encode();
    }

    private string ClientData(string type, JsonNode options) =>
        WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new JsonObject
        {
            ["type"] = type,
            ["challenge"] = options["challenge"]!.GetValue<string>(),
            ["origin"] = origin,
            ["crossOrigin"] = false,
        }));

    private string Credential(JsonObject response)
    {
        string id = WebEncoders.Base64UrlEncode(_credentialId);
        return new JsonObject
        {
            ["id"] = id,
            ["rawId"] = id,
            ["type"] = "public-key",
            ["response"] = response,
            ["clientExtensionResults"] = new JsonObject(),
            ["authenticatorAttachment"] = "platform",
        }.ToJsonString();
    }

    public void Dispose() => _key.Dispose();
}
