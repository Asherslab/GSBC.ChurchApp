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
}

[ProtoContract]
public sealed class LinkRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
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
}

[ProtoContract]
public sealed class MediaList
{
    [ProtoMember(1)] public List<MediaItemInfo> Items { get; set; } = [];
    [ProtoMember(2)] public bool HasMore { get; set; }
}

[ProtoContract]
public sealed class MediaItemInfo
{
    [ProtoMember(1)] public MediaRef? Media { get; set; }
    [ProtoMember(2)] public DateTime UploadedAt { get; set; }
    [ProtoMember(3)] public string UploadedBy { get; set; } = "";
    [ProtoMember(4)] public MediaAudience? PrivateTo { get; set; }
    [ProtoMember(5)] public int Uses { get; set; }
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
