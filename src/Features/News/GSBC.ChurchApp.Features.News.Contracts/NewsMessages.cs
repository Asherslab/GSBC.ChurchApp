using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.News.Contracts;

public enum NewsState
{
    Draft = 0,
    WaitingToPublish = 1,
    Scheduled = 2,
    Published = 3,
    Asked = 4,
    Declined = 5,
    Archived = 6,
}

[ProtoContract]
public sealed class NewsPost
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3, IsRequired = true)] public RichText Body { get; set; } = new();
    [ProtoMember(4)] public string Audience { get; set; } = "";
    [ProtoMember(5)] public NewsState State { get; set; }
    [ProtoMember(6)] public DateTime? PublishAt { get; set; }
    [ProtoMember(7)] public DateTime? PublishedAt { get; set; }
    [ProtoMember(8)] public string Author { get; set; } = "";
    [ProtoMember(9)] public DateTime? EditedAt { get; set; }
    [ProtoMember(10)] public string LinkPath { get; set; } = "";
    [ProtoMember(11)] public string LinkLabel { get; set; } = "";
    [ProtoMember(12)] public bool Tell { get; set; }
    [ProtoMember(13)] public string TellTitle { get; set; } = "";
    [ProtoMember(14)] public string TellBody { get; set; } = "";
    [ProtoMember(15)] public bool Urgent { get; set; }
    [ProtoMember(16)] public string From { get; set; } = "";
    [ProtoMember(17)] public string FromLink { get; set; } = "";
    [ProtoMember(18)] public string AskedBy { get; set; } = "";
    [ProtoMember(19)] public bool New { get; set; }
    [ProtoMember(20)] public MediaRef? Picture { get; set; }
    [ProtoMember(21)] public DateTime? ToldAt { get; set; }
    [ProtoMember(22)] public DateTime CreatedAt { get; set; }
    [ProtoMember(23)] public bool Shared { get; set; }
    [ProtoMember(24)] public string AnsweredBy { get; set; } = "";
}

[ProtoContract]
public sealed class NewsSave
{
    [ProtoMember(1, IsRequired = true)] public NewsPost Post { get; set; } = new();
    [ProtoMember(2)] public List<string> Uploaded { get; set; } = [];
}

[ProtoContract]
public sealed class NewsRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class PublishRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public DateTime? At { get; set; }
}

[ProtoContract]
public sealed class AskAnswer
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public bool Yes { get; set; }
    [ProtoMember(3)] public bool Tell { get; set; }
}

[ProtoContract]
public sealed class ReachRequest
{
    [ProtoMember(1)] public string Audience { get; set; } = "";
}

[ProtoContract]
public sealed class NewsReach
{
    [ProtoMember(1)] public int People { get; set; }
    [ProtoMember(2)] public int Phones { get; set; }
}

[ProtoContract]
public sealed class DeskRequest;

[ProtoContract]
public sealed class NewsList
{
    [ProtoMember(1)] public List<NewsPost> Posts { get; set; } = [];
}

[ProtoContract]
public sealed class Done;
