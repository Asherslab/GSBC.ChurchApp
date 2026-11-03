using System.Formats.Asn1;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using GSBC.ChurchApp.Features.Accounts.Data;

namespace GSBC.ChurchApp.Features.Accounts.Api.Installs;

public sealed record AttestedKey(byte[] PublicKey, string AppId, AttestEnvironment Environment);

public sealed class AppAttest(X509Certificate2 root, IReadOnlyList<string> appIds)
{
    private const string NonceExtension = "1.2.840.113635.100.8.2";
    private const int RpIdHashLength = 32;
    private const int CounterOffset = 33;
    private const int AaguidOffset = 37;
    private const int AaguidLength = 16;
    private const int CredentialIdLengthOffset = AaguidOffset + AaguidLength;
    private static readonly byte[] DevelopmentAaguid = "appattestdevelop"u8.ToArray();
    private static readonly byte[] ProductionAaguid = [.. "appattest"u8, 0, 0, 0, 0, 0, 0, 0];

    public static X509Certificate2 AppleRoot()
    {
        using Stream pem = typeof(AppAttest).Assembly.GetManifestResourceStream("AppleAppAttestationRootCA.pem")!;
        using StreamReader reader = new(pem);
        return X509Certificate2.CreateFromPem(reader.ReadToEnd());
    }

    public AttestedKey? VerifyAttestation(string keyId, byte[] challenge, byte[] attestation, DateTime now)
    {
        try
        {
            (byte[][] chain, byte[] authData) = ReadAttestation(attestation);
            using X509Certificate2 leaf = X509CertificateLoader.LoadCertificate(chain[0]);
            if (!ChainsToRoot(leaf, chain[1..], now))
                return null;

            byte[] nonce = SHA256.HashData([.. authData, .. SHA256.HashData(challenge)]);
            if (!NonceIn(leaf).SequenceEqual(nonce))
                return null;

            using ECDsa key = leaf.GetECDsaPublicKey()!;
            byte[] point = UncompressedPoint(key);
            byte[] keyIdBytes = SHA256.HashData(point);
            if (Convert.ToBase64String(keyIdBytes) != keyId)
                return null;

            if (AppIdFor(authData) is not { } appId || Counter(authData) != 0)
                return null;
            ReadOnlySpan<byte> aaguid = authData.AsSpan(AaguidOffset, AaguidLength);
            AttestEnvironment? environment = aaguid.SequenceEqual(DevelopmentAaguid) ? AttestEnvironment.Development
                : aaguid.SequenceEqual(ProductionAaguid) ? AttestEnvironment.Production
                : null;
            int credentialLength = authData[CredentialIdLengthOffset] << 8 | authData[CredentialIdLengthOffset + 1];
            ReadOnlySpan<byte> credentialId = authData.AsSpan(CredentialIdLengthOffset + 2, credentialLength);
            if (environment is null || !credentialId.SequenceEqual(keyIdBytes))
                return null;

            return new AttestedKey(key.ExportSubjectPublicKeyInfo(), appId, environment.Value);
        }
        catch (Exception exception) when (exception is CborContentException or InvalidOperationException or CryptographicException or AsnContentException or ArgumentException or IndexOutOfRangeException)
        {
            return null;
        }
    }

    public static long? VerifyAssertion(byte[] publicKey, string appId, long counter, byte[] assertion, byte[] clientData)
    {
        try
        {
            (byte[] signature, byte[] authenticatorData) = ReadAssertion(assertion);
            using ECDsa key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(publicKey, out _);
            byte[] nonce = SHA256.HashData([.. authenticatorData, .. SHA256.HashData(clientData)]);
            if (!key.VerifyData(nonce, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
                return null;
            if (!authenticatorData.AsSpan(0, RpIdHashLength).SequenceEqual(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(appId))))
                return null;
            long next = Counter(authenticatorData);
            return next > counter ? next : null;
        }
        catch (Exception exception) when (exception is CborContentException or InvalidOperationException or CryptographicException or ArgumentException or IndexOutOfRangeException)
        {
            return null;
        }
    }

    private bool ChainsToRoot(X509Certificate2 leaf, byte[][] intermediates, DateTime now)
    {
        using X509Chain chain = new();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(root);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationTime = now;
        foreach (byte[] intermediate in intermediates)
            chain.ChainPolicy.ExtraStore.Add(X509CertificateLoader.LoadCertificate(intermediate));
        return chain.Build(leaf);
    }

    private string? AppIdFor(byte[] authData)
    {
        ReadOnlySpan<byte> rpIdHash = authData.AsSpan(0, RpIdHashLength);
        foreach (string appId in appIds)
        {
            if (rpIdHash.SequenceEqual(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(appId))))
                return appId;
        }
        return null;
    }

    private static long Counter(byte[] authData) =>
        (long)authData[CounterOffset] << 24 | (long)authData[CounterOffset + 1] << 16 | (long)authData[CounterOffset + 2] << 8 | authData[CounterOffset + 3];

    private static byte[] NonceIn(X509Certificate2 leaf)
    {
        X509Extension extension = leaf.Extensions[NonceExtension] ?? throw new InvalidOperationException("No nonce extension.");
        AsnReader sequence = new AsnReader(extension.RawData, AsnEncodingRules.DER).ReadSequence();
        AsnReader tagged = sequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
        return tagged.ReadOctetString();
    }

    private static byte[] UncompressedPoint(ECDsa key)
    {
        ECParameters parameters = key.ExportParameters(includePrivateParameters: false);
        return [0x04, .. parameters.Q.X!, .. parameters.Q.Y!];
    }

    private static (byte[][] Chain, byte[] AuthData) ReadAttestation(byte[] attestation)
    {
        CborReader reader = new(attestation);
        string? format = null;
        List<byte[]> chain = [];
        byte[]? authData = null;
        int? entries = reader.ReadStartMap();
        for (int index = 0; entries is null ? reader.PeekState() != CborReaderState.EndMap : index < entries; index++)
        {
            switch (reader.ReadTextString())
            {
                case "fmt":
                    format = reader.ReadTextString();
                    break;
                case "authData":
                    authData = reader.ReadByteString();
                    break;
                case "attStmt":
                    chain = ReadStatement(reader);
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();

        if (format != "apple-appattest" || authData is null || chain.Count < 2)
            throw new InvalidOperationException("Not an App Attest attestation.");
        return ([.. chain], authData);
    }

    private static List<byte[]> ReadStatement(CborReader reader)
    {
        List<byte[]> chain = [];
        int? entries = reader.ReadStartMap();
        for (int index = 0; entries is null ? reader.PeekState() != CborReaderState.EndMap : index < entries; index++)
        {
            if (reader.ReadTextString() == "x5c")
            {
                int? certificates = reader.ReadStartArray();
                for (int certificate = 0; certificates is null ? reader.PeekState() != CborReaderState.EndArray : certificate < certificates; certificate++)
                    chain.Add(reader.ReadByteString());
                reader.ReadEndArray();
            }
            else
            {
                reader.SkipValue();
            }
        }
        reader.ReadEndMap();
        return chain;
    }

    private static (byte[] Signature, byte[] AuthenticatorData) ReadAssertion(byte[] assertion)
    {
        CborReader reader = new(assertion);
        byte[]? signature = null;
        byte[]? authenticatorData = null;
        int? entries = reader.ReadStartMap();
        for (int index = 0; entries is null ? reader.PeekState() != CborReaderState.EndMap : index < entries; index++)
        {
            switch (reader.ReadTextString())
            {
                case "signature":
                    signature = reader.ReadByteString();
                    break;
                case "authenticatorData":
                    authenticatorData = reader.ReadByteString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        return (signature ?? throw new InvalidOperationException("No signature."), authenticatorData ?? throw new InvalidOperationException("No authenticator data."));
    }
}
