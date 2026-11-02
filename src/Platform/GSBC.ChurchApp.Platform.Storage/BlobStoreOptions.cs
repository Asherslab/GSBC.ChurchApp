namespace GSBC.ChurchApp.Platform.Storage;

public sealed class BlobStoreOptions
{
    public const string Section = "Storage:Blobs";

    public required string ServiceUrl { get; set; }
    public string? PublicServiceUrl { get; set; }
    public string Region { get; set; } = "auto";
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
    public required string PublicBucket { get; set; }
    public required string PrivateBucket { get; set; }
    public required string PublicBaseUrl { get; set; }
    public bool CreateBuckets { get; set; }
}
