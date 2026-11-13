using GSBC.ChurchApp.Features.News.Contracts;

namespace GSBC.ChurchApp.Features.News.Api.Data;

public sealed class PostRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "{}";
    public string Audience { get; set; } = "";
    public NewsState State { get; set; }
    public DateTime? PublishAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime ChangedAt { get; set; }
    public string LinkPath { get; set; } = "";
    public string LinkLabel { get; set; } = "";
    public bool Tell { get; set; }
    public string TellTitle { get; set; } = "";
    public string TellBody { get; set; } = "";
    public bool Urgent { get; set; }
    public DateTime? ToldAt { get; set; }
    public string? Source { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? FromId { get; set; }
    public string FromName { get; set; } = "";
    public string FromLink { get; set; } = "";
    public Guid? AskedById { get; set; }
    public string AskedByName { get; set; } = "";
    public Guid? AnsweredById { get; set; }
    public string AnsweredByName { get; set; } = "";
}
