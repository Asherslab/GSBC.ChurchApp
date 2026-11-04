namespace GSBC.ChurchApp.Platform.Api.Media;

public sealed record KeptFile(byte[] Bytes, string ContentType, string Extension, string FileName, int Width = 0, int Height = 0);
