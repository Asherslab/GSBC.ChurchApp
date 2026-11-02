using Amazon.S3;

namespace GSBC.ChurchApp.Platform.Storage;

internal sealed record S3Presigner(IAmazonS3 Client);
