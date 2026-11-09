using System.ComponentModel;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Sermons.Import.Contracts;

public static class ImportStates
{
    public const string NotStarted = "not-started";
    public const string Waiting = "waiting";
    public const string Finding = "finding";
    public const string Importing = "importing";
    public const string Finished = "finished";
}

[ProtoContract]
public sealed class ImportRequest;

[ProtoContract]
public sealed class ImportRetry
{
    [ProtoMember(1)] public string WebsitePath { get; set; } = "";
}

[ProtoContract]
public sealed class ImportProgress
{
    [ProtoMember(1), DefaultValue(ImportStates.NotStarted)] public string State { get; set; } = ImportStates.NotStarted;
    [ProtoMember(2)] public DateTime? StartedAt { get; set; }
    [ProtoMember(3)] public DateTime? FinishedAt { get; set; }
    [ProtoMember(4)] public int Found { get; set; }
    [ProtoMember(5)] public int Copied { get; set; }
    [ProtoMember(6)] public int Skipped { get; set; }
    [ProtoMember(7)] public int Failed { get; set; }
    [ProtoMember(8)] public int Waiting { get; set; }
    [ProtoMember(9)] public ImportItem? Current { get; set; }
    [ProtoMember(10)] public List<ImportItem> Failures { get; set; } = [];
    [ProtoMember(11)] public string? Problem { get; set; }
    [ProtoMember(12)] public int ListingPages { get; set; }
    [ProtoMember(13)] public int WebsiteCount { get; set; }
}

[ProtoContract]
public sealed class ImportItem
{
    [ProtoMember(1)] public string WebsitePath { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public DateTime? PreachedOn { get; set; }
    [ProtoMember(4)] public string? Problem { get; set; }
    [ProtoMember(5)] public long Bytes { get; set; }
    [ProtoMember(6)] public long CopiedBytes { get; set; }
}
