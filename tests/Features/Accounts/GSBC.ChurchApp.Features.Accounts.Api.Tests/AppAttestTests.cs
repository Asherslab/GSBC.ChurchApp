using System.Formats.Asn1;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using GSBC.ChurchApp.Features.Accounts.Api.Installs;
using GSBC.ChurchApp.Features.Accounts.Data;

using GSBC.ChurchApp.Testing;
namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

public sealed class AppAttestTests : IDisposable
{
    private const string AppId = "YNT9A4LGVA.au.com.baptist.app.dev";
    private static readonly DateTime Now = TestClock.Start;

    private readonly ECDsa _rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
    private readonly ECDsa _intermediateKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
    private readonly TestDevice _device = new(AppId);
    private readonly X509Certificate2 _root;
    private readonly X509Certificate2 _intermediate;
    private readonly byte[] _challenge = RandomNumberGenerator.GetBytes(32);

    public AppAttestTests()
    {
        _root = Authority("CN=Test App Attestation Root", _rootKey, issuer: null, issuerKey: _rootKey);
        _intermediate = Authority("CN=Test App Attestation CA", _intermediateKey, _root, _rootKey);
    }

    [Fact]
    public void AGenuineAttestationGivesTheKeyAndItsEnvironment()
    {
        AttestedKey? key = Verifier().VerifyAttestation(_device.KeyId, _challenge, Attestation(_challenge, AppId), Now);

        Assert.NotNull(key);
        Assert.Equal(AppId, key.AppId);
        Assert.Equal(AttestEnvironment.Development, key.Environment);
    }

    [Fact]
    public void AnAttestationIsRefusedForAnotherChallengeAnotherAppOrAnotherRoot()
    {
        byte[] attestation = Attestation(_challenge, AppId);

        Assert.Null(Verifier().VerifyAttestation(_device.KeyId, RandomNumberGenerator.GetBytes(32), attestation, Now));
        Assert.Null(Verifier().VerifyAttestation(_device.KeyId, _challenge, Attestation(_challenge, "YNT9A4LGVA.someone.else"), Now));
        using ECDsa otherRootKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        Assert.Null(new AppAttest(Authority("CN=Other", otherRootKey, null, otherRootKey), [AppId]).VerifyAttestation(_device.KeyId, _challenge, attestation, Now));
    }

    [Fact]
    public void AnAssertionIsAcceptedOnlyForItsRequestAndOnlyOnce()
    {
        AttestedKey key = Verifier().VerifyAttestation(_device.KeyId, _challenge, Attestation(_challenge, AppId), Now)!;
        byte[] request = "/gsbc.notifications.Devices/Register\nbody"u8.ToArray();
        byte[] assertion = _device.Assertion(request, counter: 1);

        Assert.Equal(1, AppAttest.VerifyAssertion(key.PublicKey, AppId, 0, assertion, request));
        Assert.Null(AppAttest.VerifyAssertion(key.PublicKey, AppId, 1, assertion, request));
        Assert.Null(AppAttest.VerifyAssertion(key.PublicKey, AppId, 0, assertion, "/gsbc.notifications.Devices/Register\nother"u8.ToArray()));
    }

    public void Dispose()
    {
        _root.Dispose();
        _intermediate.Dispose();
        _rootKey.Dispose();
        _intermediateKey.Dispose();
        _device.Dispose();
    }

    private AppAttest Verifier() => new(_root, [AppId]);

    private byte[] Attestation(byte[] challenge, string appId)
    {
        byte[] keyId = SHA256.HashData(_device.Point);
        byte[] authData =
        [
            .. SHA256.HashData(Encoding.UTF8.GetBytes(appId)), 0x40, 0, 0, 0, 0,
            .. "appattestdevelop"u8, (byte)(keyId.Length >> 8), (byte)keyId.Length, .. keyId,
        ];
        byte[] nonce = SHA256.HashData([.. authData, .. SHA256.HashData(challenge)]);

        CertificateRequest request = new("CN=device", _device.Key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509Extension("1.2.840.113635.100.8.2", NonceExtension(nonce), critical: false));
        using X509Certificate2 leaf = request.Create(_intermediate.SubjectName, X509SignatureGenerator.CreateForECDsa(_intermediateKey),
            Now.AddMinutes(-5), Now.AddDays(1), RandomNumberGenerator.GetBytes(8));

        CborWriter writer = new();
        writer.WriteStartMap(3);
        writer.WriteTextString("fmt");
        writer.WriteTextString("apple-appattest");
        writer.WriteTextString("attStmt");
        writer.WriteStartMap(2);
        writer.WriteTextString("x5c");
        writer.WriteStartArray(2);
        writer.WriteByteString(leaf.RawData);
        writer.WriteByteString(_intermediate.RawData);
        writer.WriteEndArray();
        writer.WriteTextString("receipt");
        writer.WriteByteString([]);
        writer.WriteEndMap();
        writer.WriteTextString("authData");
        writer.WriteByteString(authData);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static byte[] NonceExtension(byte[] nonce)
    {
        AsnWriter writer = new(AsnEncodingRules.DER);
        using (writer.PushSequence())
        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
            writer.WriteOctetString(nonce);
        return writer.Encode();
    }

    private static X509Certificate2 Authority(string name, ECDsa key, X509Certificate2? issuer, ECDsa issuerKey)
    {
        CertificateRequest request = new(name, key, HashAlgorithmName.SHA384);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        return issuer is null
            ? request.CreateSelfSigned(Now.AddYears(-1), Now.AddYears(10))
            : request.Create(issuer.SubjectName, X509SignatureGenerator.CreateForECDsa(issuerKey), Now.AddYears(-1), Now.AddYears(5), RandomNumberGenerator.GetBytes(8));
    }
}
