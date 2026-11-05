namespace GSBC.ChurchApp.Platform.Api.Media;

public sealed record StoredFile(string Key, string Sha256, long Bytes, string ContentType, string FileName);
