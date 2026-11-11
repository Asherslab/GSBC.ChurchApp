using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class QueuedFileTests
{
    [Fact]
    public void TheMediaIdThePhoneAddsToAQueuedFileBecomesTheFilesDocumentAndKeepsTheRest()
    {
        FileSave save = new() { MinistryId = "ministry-1", File = new FileItem { Id = "file-1", New = true, Title = "Helpers' guide", Audience = Audiences.Team, Kind = FileKinds.Document } };
        string mediaId = Guid.CreateVersion7().ToString();
        using MemoryStream queued = new();
        MinistriesContractsModel.Instance.Serialize(queued, save);

        FileSave sent = MinistriesContractsModel.Instance.Deserialize<FileSave>(new MemoryStream([.. queued.ToArray(), .. Spliced(mediaId, MinistryFileArea.MediaIdInFileSave)]));

        Assert.Equal(mediaId, sent.File.MediaId);
        Assert.Equal(("ministry-1", "file-1", "Helpers' guide", Audiences.Team), (sent.MinistryId, sent.File.Id, sent.File.Title, sent.File.Audience));
    }

    private static byte[] Spliced(string value, IReadOnlyList<int> path)
    {
        byte[] inner = [.. Field(path[^1], System.Text.Encoding.UTF8.GetBytes(value))];
        for (int depth = path.Count - 2; depth >= 0; depth--)
            inner = [.. Field(path[depth], inner)];
        return inner;
    }

    private static IEnumerable<byte> Field(int number, byte[] value) => [(byte)(number << 3 | 2), (byte)value.Length, .. value];
}
