namespace GSBC.ChurchApp.Testing;

public sealed class TestBuckets
{
    private TestBuckets(string serviceUrl, string id)
    {
        ServiceUrl = serviceUrl;
        Public = SharedS3.PublicBucketPrefix + id;
        Private = SharedS3.PrivateBucketPrefix + id;
    }

    public string ServiceUrl { get; }

    public string Public { get; }

    public string Private { get; }

    public string AccessKey => SharedS3.AccessKey;

    public string SecretKey => SharedS3.SecretKey;

    public static async Task<TestBuckets> CreateAsync()
    {
        SharedS3 server = await SharedS3.InstanceAsync();
        return new TestBuckets(server.ServiceUrl, Guid.NewGuid().ToString("N"));
    }
}
