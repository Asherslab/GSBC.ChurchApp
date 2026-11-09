using System.ComponentModel;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.People.Contracts;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class MineRequest;

[ProtoContract]
public sealed class MyPeople
{
    [ProtoMember(1, IsRequired = true)] public PersonFamily Me { get; set; } = new();
}

[ProtoContract]
public sealed class PersonFamily
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public DateTime? BirthDate { get; set; }
    [ProtoMember(4)] public bool SaysAdult { get; set; }
    [ProtoMember(5)] public bool CanSignIn { get; set; }
    [ProtoMember(6)] public bool AddedByFamily { get; set; }
    [ProtoMember(7)] public List<PersonCard> Parents { get; set; } = [];
    [ProtoMember(8)] public List<PersonCard> Children { get; set; } = [];
    [ProtoMember(9)] public List<LinkAsk> Asks { get; set; } = [];
    [ProtoMember(10)] public SafeguardingRecord? Safeguarding { get; set; }
    [ProtoMember(11)] public bool SafeguardingShown { get; set; }
    [ProtoMember(12)] public List<LinkAsk> AskedOfThem { get; set; } = [];
}

[ProtoContract]
public sealed class PersonCard
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public DateTime? BirthDate { get; set; }
    [ProtoMember(4)] public bool CanSignIn { get; set; }
    [ProtoMember(5)] public bool AddedByFamily { get; set; }
    [ProtoMember(6)] public DateTime LinkedAt { get; set; }
    [ProtoMember(7), DefaultValue(LinkHow.Office)] public string How { get; set; } = LinkHow.Office;
    [ProtoMember(8)] public string? LinkedBy { get; set; }
}

public static class LinkHow
{
    public const string Added = "added";
    public const string Approved = "approved";
    public const string Office = "office";
}

[ProtoContract]
public sealed class LinkAsk
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string ChildId { get; set; } = "";
    [ProtoMember(3)] public string ChildName { get; set; } = "";
    [ProtoMember(4)] public DateTime? ChildBirthDate { get; set; }
    [ProtoMember(5)] public string ParentEmail { get; set; } = "";
    [ProtoMember(6)] public string AskedByName { get; set; } = "";
    [ProtoMember(7)] public DateTime AskedAt { get; set; }
    [ProtoMember(8)] public bool AskedByChild { get; set; }
}

[ProtoContract]
public sealed class SafeguardingRecord
{
    [ProtoMember(1)] public string CardNumber { get; set; } = "";
    [ProtoMember(2)] public string CardStatus { get; set; } = "";
    [ProtoMember(3)] public DateTime? CardExpires { get; set; }
    [ProtoMember(4)] public DateTime? CardChecked { get; set; }
    [ProtoMember(5)] public DateTime? TrainingDone { get; set; }
    [ProtoMember(6)] public DateTime? TrainingDue { get; set; }
    [ProtoMember(7), DefaultValue(SafeguardingSources.ByHand)] public string Source { get; set; } = SafeguardingSources.ByHand;
    [ProtoMember(8)] public DateTime? UpdatedAt { get; set; }
    [ProtoMember(9)] public string? UpdatedBy { get; set; }

    public bool HasCard => CardNumber.Length > 0 || CardStatus.Length > 0 || CardExpires is not null;

    public bool HasTraining => TrainingDone is not null || TrainingDue is not null;
}

public static class SafeguardingSources
{
    public const string ByHand = "hand";
    public const string SafeMinistryCheck = "safe-ministry-check";
    public const string Elvanto = "elvanto";
}

[ProtoContract]
public sealed class BirthDateRequest
{
    [ProtoMember(1)] public DateTime? BirthDate { get; set; }
    [ProtoMember(2)] public bool Adult { get; set; }
}

[ProtoContract]
public sealed class AddChildRequest
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public DateTime BirthDate { get; set; }
    [ProtoMember(3)] public string ParentId { get; set; } = "";
}

[ProtoContract]
public sealed class AskRequest
{
    [ProtoMember(1)] public string ChildId { get; set; } = "";
    [ProtoMember(2)] public string ParentEmail { get; set; } = "";
}

[ProtoContract]
public sealed class AnswerRequest
{
    [ProtoMember(1)] public string AskId { get; set; } = "";
    [ProtoMember(2)] public bool Accept { get; set; }
}

[ProtoContract]
public sealed class AskIdRequest
{
    [ProtoMember(1)] public string AskId { get; set; } = "";
}

[ProtoContract]
public sealed class LinkRequest
{
    [ProtoMember(1)] public string ParentId { get; set; } = "";
    [ProtoMember(2)] public string ChildId { get; set; } = "";
}

[ProtoContract]
public sealed class PersonRequest : IScopedRequest
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";

    public string ScopeId => PersonId;
}

[ProtoContract]
public sealed class UpdatePersonRequest : IScopedRequest
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
    [ProtoMember(2)] public string? Name { get; set; }
    [ProtoMember(3)] public DateTime? BirthDate { get; set; }
    [ProtoMember(4)] public bool ClearBirthDate { get; set; }

    public string ScopeId => PersonId;
}

[ProtoContract]
public sealed class SignInCodeReply
{
    [ProtoMember(1)] public string Code { get; set; } = "";
    [ProtoMember(2)] public DateTime ExpiresAt { get; set; }
}

[ProtoContract]
public sealed class SafeguardingRequest : IScopedRequest
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public SafeguardingRecord Record { get; set; } = new();

    public string ScopeId => PersonId;
}

[ProtoContract]
public sealed class FlaggedRequest;

[ProtoContract]
public sealed class FlaggedList
{
    [ProtoMember(1)] public List<FlaggedPerson> People { get; set; } = [];
}

[ProtoContract]
public sealed class FlaggedPerson
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Kind { get; set; } = "";
    [ProtoMember(4)] public DateTime? When { get; set; }
    [ProtoMember(5)] public string Words { get; set; } = "";
}
