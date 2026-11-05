using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Media.Contracts;

[ProtoContract]
public sealed class BeginUploadRequest
{
    [ProtoMember(1)] public string FileName { get; set; } = "";
    [ProtoMember(2)] public string ContentType { get; set; } = "";
    [ProtoMember(3)] public long Bytes { get; set; }
    [ProtoMember(6)] public string AltText { get; set; } = "";
    [ProtoMember(7)] public MediaAudience? PrivateTo { get; set; }
    [ProtoMember(8)] public string? FolderId { get; set; }
}

[ProtoContract]
public sealed class MediaAudience
{
    [ProtoMember(1)] public string Permission { get; set; } = "";
    [ProtoMember(2)] public string? ScopeId { get; set; }
}

[ProtoContract]
public sealed class UploadTicket
{
    [ProtoMember(1)] public string UploadId { get; set; } = "";
    [ProtoMember(2)] public string Url { get; set; } = "";
    [ProtoMember(3)] public string ContentType { get; set; } = "";
    [ProtoMember(4)] public DateTime ExpiresAt { get; set; }
}

[ProtoContract]
public sealed class CompleteUploadRequest
{
    [ProtoMember(1)] public string UploadId { get; set; } = "";
    [ProtoMember(2)] public bool AddAgain { get; set; }
    [ProtoMember(3)] public string? PutBackInto { get; set; }
}

[ProtoContract]
public sealed class UploadedMedia
{
    [ProtoMember(1)] public MediaItemInfo? Item { get; set; }
    [ProtoMember(2)] public bool AlreadyInLibrary { get; set; }
    [ProtoMember(3)] public bool PutBack { get; set; }
}

[ProtoContract]
public sealed class FindByHashRequest
{
    [ProtoMember(1)] public string Sha256 { get; set; } = "";
    [ProtoMember(2)] public string ContentType { get; set; } = "";
    [ProtoMember(3)] public MediaAudience? PrivateTo { get; set; }
}

[ProtoContract]
public sealed class MediaMatches
{
    [ProtoMember(1)] public List<MediaItemInfo> Items { get; set; } = [];
    [ProtoMember(2)] public List<MediaItemInfo> RemovedOriginals { get; set; } = [];
}

[ProtoContract]
public sealed class LinkRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Key { get; set; } = "";
}

[ProtoContract]
public sealed class MediaLink
{
    [ProtoMember(1)] public string Url { get; set; } = "";
    [ProtoMember(2)] public DateTime ExpiresAt { get; set; }
}

[ProtoContract]
public sealed class MediaQuery
{
    [ProtoMember(1)] public int Skip { get; set; }
    [ProtoMember(2)] public int Take { get; set; }
    [ProtoMember(3)] public string? FolderId { get; set; }
    [ProtoMember(4)] public string Search { get; set; } = "";
    [ProtoMember(5)] public MediaFilter Filter { get; set; }
}

public enum MediaFilter
{
    All = 0,
    Pictures = 1,
    Documents = 2,
    Unused = 3,
    LargeOriginals = 4,
}

[ProtoContract]
public sealed class MediaList
{
    [ProtoMember(1)] public List<MediaItemInfo> Items { get; set; } = [];
    [ProtoMember(2)] public bool HasMore { get; set; }
    [ProtoMember(3)] public MediaCounts? Counts { get; set; }
}

[ProtoContract]
public sealed class MediaCounts
{
    [ProtoMember(1)] public int All { get; set; }
    [ProtoMember(2)] public int Pictures { get; set; }
    [ProtoMember(3)] public int Documents { get; set; }
    [ProtoMember(4)] public int Unused { get; set; }
    [ProtoMember(5)] public int LargeOriginals { get; set; }
}

[ProtoContract]
public sealed class MediaItemInfo
{
    [ProtoMember(1)] public MediaRef? Media { get; set; }
    [ProtoMember(2)] public DateTime UploadedAt { get; set; }
    [ProtoMember(3)] public string UploadedBy { get; set; } = "";
    [ProtoMember(4)] public MediaAudience? PrivateTo { get; set; }
    [ProtoMember(5)] public int Uses { get; set; }
    [ProtoMember(6)] public string? FolderId { get; set; }
    [ProtoMember(7)] public MediaOriginal? Original { get; set; }
    [ProtoMember(8)] public PictureSizing Sizing { get; set; }

    public bool HasLargeOriginal => Original is { Removed: false } original && Media is { Sizes.Count: > 0 } && MediaTypes.IsLargeOriginal(original.Bytes);
}

[ProtoContract]
public sealed class MediaOriginal
{
    [ProtoMember(1)] public string ContentType { get; set; } = "";
    [ProtoMember(2)] public long Bytes { get; set; }
    [ProtoMember(3)] public int Width { get; set; }
    [ProtoMember(4)] public int Height { get; set; }
    [ProtoMember(5)] public bool Removed { get; set; }
}

public enum PictureSizing
{
    NotAPicture = 0,
    Waiting = 1,
    Made = 2,
    KeptAsIs = 3,
}

[ProtoContract]
public sealed class RemoveOriginalRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class DeleteMediaRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class FontSearch
{
    [ProtoMember(1)] public string Query { get; set; } = "";
}

[ProtoContract]
public sealed class FontFamilies
{
    [ProtoMember(1)] public List<FontFamilyChoice> Families { get; set; } = [];
}

[ProtoContract]
public sealed class ImportFontRequest
{
    [ProtoMember(1)] public string Family { get; set; } = "";
}
