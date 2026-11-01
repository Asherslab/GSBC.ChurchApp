namespace GSBC.ChurchApp.Platform.Storage;

public sealed class BlobStoreOptions
{
    public const string Section = "Storage:Blobs";

    public required string ServiceUrl { get; set; }
    public required string Bucket { get; set; }
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
}
