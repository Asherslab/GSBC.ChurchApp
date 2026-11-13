namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistryFileArea
{
    public const string Key = "ministry-files";
    public const string PostsKey = "ministry-posts";

    private const int SaveFile = 2;
    private const int FileMediaId = 6;

    public static readonly int[] MediaIdInFileSave = [SaveFile, FileMediaId];

    private const int SaveUploaded = 3;

    public static readonly int[] UploadedInPostSave = [SaveUploaded];
}
