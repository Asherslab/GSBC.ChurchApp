using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Chats;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Chats;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Chats.Api.Tests;

[Integration("Chats fan each message out to every recipient's own Sync rows in Postgres, and take them back when someone leaves, is paused or waits for a parent")]
public sealed class ChatDeliveryTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private readonly Church _church = new();
    private ServiceProvider _services = null!;
    private Guid _leader, _helper, _teen, _parent, _outsider;

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        ServiceCollection services = new();
        services
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddSingleton<TimeProvider>(_clock)
            .AddLogging()
            .AddSingleton<IPermissionHolders>(_church).AddSingleton<IAccessEvaluator>(_church).AddSingleton<IRoleAssignments>(_church)
            .AddSingleton<IFamilies>(_church).AddSingleton<ISafeguardingStandings>(_church).AddSingleton<IChatPlaces>(_church)
            .AddSingleton<IPeopleIndex>(_church).AddSingleton<INotifications>(_church)
            .AddSingleton<IMediaReferences>(_church).AddSingleton<IMediaSystemFolders>(_church)
            .AddScoped<ChatFacts>().AddScoped<ChatPublisher>().AddScoped<ChatNotifier>().AddScoped<ChatAudit>().AddScoped<OversightMarks>()
            .AddScoped<ChatActions>().AddScoped<ChatOversight>().AddScoped<ChatsPersonData>().AddScoped<ChatMedia>().AddScoped<ChatMediaAccess>().AddScoped<ChatReads>()
            .AddSingleton(new FeedRegistry([new FeedDefinition(ChatsFeeds.Chats, FeedAudience.OwnRecords), new FeedDefinition(ChatsFeeds.Messages, FeedAudience.OwnRecords)]))
            .AddSingleton<FeedChangeBroadcaster>()
            .AddScoped<SyncService>()
            .AddSingleton<ISignedInSessions, AlwaysSignedIn>()
            .AddChatsData()
            .AddSyncData();
        _services = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<ChatsDbContext>().Database.MigrateAsync(Cancel);

        _leader = _church.Person("Alex Sample");
        _helper = _church.Person("Sam Example");
        _parent = _church.Person("Jo Parent");
        _teen = _church.Child("Tom Young", _parent);
        _outsider = _church.Person("Pat Outside");
        _church.Leads(_leader);
        _church.OnTeam(_helper);
        _church.OnTeam(_teen);
        _church.Carded(_leader, _helper);
    }

    [Fact]
    public async Task AMessageReachesOnlyThoseInTheChatAndAYoungPersonOnlyOnceAParentSaysYes()
    {
        Guid chat = await StartAsync("Sunday team", "Here's why we're starting this chat.");

        Assert.Equal(["Here's why we're starting this chat."], await TextsAsync(_leader, chat));
        Assert.Equal(["Here's why we're starting this chat."], await TextsAsync(_helper, chat));
        Assert.Equal(["Here's why we're starting this chat."], await TextsAsync(_parent, chat));
        Assert.Empty(await TextsAsync(_teen, chat));
        Assert.Empty(await TextsAsync(_outsider, chat));
        Assert.Contains(_church.Raised, raised => raised.Kind == ChatsNotifications.Consent && raised.Audience == NotificationAudience.Person(_parent));
        Assert.Equal([_teen.ToString()], (await CardAsync(_parent, chat))!.AwaitingYourConsent.Select(child => child.Id));

        await RefusedAsync(actions => actions.ConsentAsync(_helper, new Consent { ChatId = chat.ToString(), ChildId = _teen.ToString(), Approve = true }, Cancel));
        await ActAsync(actions => actions.ConsentAsync(_parent, new Consent { ChatId = chat.ToString(), ChildId = _teen.ToString(), Approve = true }, Cancel));

        Assert.Equal(["Here's why we're starting this chat."], await TextsAsync(_teen, chat));
        Assert.True((await CardAsync(_teen, chat))!.ParentsCanRead);
    }

    [Fact]
    public async Task APhonePullsOnlyItsOwnChatRowsAndNothingFromChatsItIsNotIn()
    {
        Guid chat = await StartAsync("Sunday team", "Hello all");
        await SendAsync(_helper, chat, "On my way", "way");

        FeedBatch helper = await PullAsync(_helper, ChatsFeeds.Messages);
        FeedBatch outsider = await PullAsync(_outsider, ChatsFeeds.Messages);

        Assert.Equal(["Hello all", "On my way"],
            helper.Changes.Select(change => ChatsContractsModel.Instance.Deserialize<ChatMessage>(new MemoryStream(change.Payload)).Text));
        Assert.All(helper.Changes, change => Assert.StartsWith(_helper.ToString("N"), change.Id, StringComparison.Ordinal));
        Assert.Empty(outsider.Changes);
    }

    [Fact]
    public async Task APhotoIsTheLibrarysUploadFiledUnderTheChatAndOnlyThoseInTheChatMayReadIt()
    {
        Guid chat = await StartAsync("Sunday team", "Hello all");
        _church.Uploaded.Add("photo-1");

        await RefusedAsync(actions => actions.SendAsync(_helper, new SendMessage
        {
            ChatId = chat.ToString(), ClientId = "claimed", Attachments = [new Attachment { Key = "someone-elses.jpg", Name = "x.jpg" }],
        }, Cancel));
        await ActAsync(actions => actions.SendAsync(_helper, new SendMessage
        {
            ChatId = chat.ToString(), ClientId = "photo", Attachments = [new Attachment { MediaId = "photo-1" }],
        }, Cancel));

        Attachment shown = (await MessagesAsync(_leader, chat)).Last().Attachments.Single();
        Assert.Equal(("photo-1.jpg", "photo-1", true), (shown.Key, shown.Media?.Id, shown.Media?.IsPrivate));
        Assert.Equal([(chat.ToString(), "Sunday team", "photo-1")], _church.Filed);

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        ChatMediaAccess access = scope.ServiceProvider.GetRequiredService<ChatMediaAccess>();
        Assert.True(await access.MayReadAsync(Signed(_helper), chat.ToString(), Cancel));
        Assert.False(await access.MayReadAsync(Signed(_teen), chat.ToString(), Cancel));
        Assert.False(await access.MayReadAsync(Signed(_outsider), chat.ToString(), Cancel));
        Assert.False(await access.MayUploadAsync(Signed(_outsider), chat.ToString(), Cancel));
    }

    [Fact]
    public async Task EditingAndDeletingKeepEveryVersionForWhoeverChecksAndEveryCheckIsLogged()
    {
        Guid chat = await StartAsync("Sunday team", "");
        await SendAsync(_helper, chat, "first words", "one");
        Guid message = await MessageIdAsync(chat, "one");
        await ActAsync(actions => actions.EditAsync(_helper, new EditMessage { MessageId = message.ToString(), Text = "second words" }, Cancel));
        await ActAsync(actions => actions.DeleteMessageAsync(_helper, new MessageRef { MessageId = message.ToString() }, Cancel));

        ChatMessage seen = (await MessagesAsync(_leader, chat)).Single();
        Assert.True(seen.Deleted);
        Assert.Equal("", seen.Text);

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        ChatOversight oversight = scope.ServiceProvider.GetRequiredService<ChatOversight>();
        await Assert.ThrowsAsync<RpcException>(async () => await oversight.CheckAsync(_helper, new CheckRequest { ChatId = chat.ToString(), Reason = "Curious" }, Cancel));
        CheckedChat checkedChat = await oversight.CheckAsync(_leader, new CheckRequest { ChatId = chat.ToString(), Reason = "A parent asked me to" }, Cancel);

        CheckedMessage found = checkedChat.Messages.Single();
        Assert.Equal("second words", found.DeletedText);
        Assert.Equal([("sent", "first words"), ("edited", "second words"), ("message-deleted", "second words")],
            found.Versions.Select(version => (version.Action, version.Text)));
        Assert.Contains(checkedChat.Checks, line => line.Words == "A parent asked me to" && line.ActorName == "Alex Sample");
    }

    [Fact]
    public async Task APausedPersonCanNeitherSendNorReadAndNothingTheyTryIsStored()
    {
        Guid auditor = _church.Person("Casey Auditor");
        _church.Audits(auditor);
        Guid chat = await StartAsync("Sunday team", "Hello all");

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ChatOversight>()
                .PauseAsync(auditor, new PauseRequest { PersonId = _helper.ToString(), Paused = true, Reason = "Reported for a message" }, Cancel);

        await RefusedAsync(actions => actions.SendAsync(_helper, new SendMessage { ChatId = chat.ToString(), ClientId = "paused", Text = "still here?" }, Cancel));
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<ChatsDbContext>().Messages.AnyAsync(message => message.ClientId == "paused", Cancel));
        Assert.Empty(await TextsAsync(_helper, chat));
        Assert.Null(await CardAsync(_helper, chat));
        MyChats onComputer = await MineAsync(_helper);
        Assert.True(onComputer.Paused);
        Assert.Empty(onComputer.Chats);

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ChatOversight>()
                .PauseAsync(auditor, new PauseRequest { PersonId = _helper.ToString(), Paused = false }, Cancel);
        Assert.Equal(["Hello all"], await TextsAsync(_helper, chat));
    }

    [Fact]
    public async Task TheAdminPanelReadsExactlyWhatSyncGivesThatPersonsPhone()
    {
        Guid chat = await StartAsync("Sunday team", "Hello all");
        await SendAsync(_helper, chat, "first words", "one");
        Guid edited = await MessageIdAsync(chat, "one");
        await ActAsync(actions => actions.EditAsync(_helper, new EditMessage { MessageId = edited.ToString(), Text = "second words" }, Cancel));
        await SendAsync(_leader, chat, "See you Sunday", "two");

        Assert.Equal(["Hello all", "second words", "See you Sunday"], (await ReadAsync(_helper, chat)).Messages.Select(message => message.Text));
        Assert.Equal(await TextsAsync(_helper, chat), (await ReadAsync(_helper, chat)).Messages.Select(message => message.Text));
        Assert.Equal(["See you Sunday"], (await ReadAsync(_helper, chat, beforeSeq: 0, limit: 1)).Messages.Select(message => message.Text));
        Assert.Equal(["Hello all", "second words"], (await ReadAsync(_helper, chat, beforeSeq: 3)).Messages.Select(message => message.Text));
        Assert.Empty((await ReadAsync(_outsider, chat)).Messages);
        Assert.Empty((await MineAsync(_outsider)).Chats);
        Assert.Equal([chat.ToString()], (await MineAsync(_helper)).Chats.Select(card => card.Id));

        _church.OffTeam(_helper);
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ChatPublisher>().PublishAllAsync(Cancel);

        Assert.Empty((await MineAsync(_helper)).Chats);
        Assert.Empty((await ReadAsync(_helper, chat)).Messages);
    }

    [Fact]
    public async Task LeavingTheAudienceTakesTheChatOffTheirPhone()
    {
        Guid chat = await StartAsync("Sunday team", "Hello all");
        _church.OffTeam(_helper);

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ChatPublisher>().PublishAllAsync(Cancel);

        Assert.Empty(await TextsAsync(_helper, chat));
        Assert.Null(await CardAsync(_helper, chat));
        Assert.Equal(["Hello all"], await TextsAsync(_leader, chat));
    }

    [Fact]
    public async Task AskingToJoinIsAConversationWithTheLeadersOnly()
    {
        Guid ask = Guid.NewGuid();
        await ActAsync(actions => actions.AskToJoinAsync(_outsider, new AskToJoin { ChatId = ask.ToString(), MinistryId = _church.MinistryId.ToString(), Text = "" }, Cancel));

        Assert.Equal([ChatCopy.AskToJoin("Sample Ministry")], await TextsAsync(_outsider, ask));
        Assert.Equal([ChatCopy.AskToJoin("Sample Ministry")], await TextsAsync(_leader, ask));
        Assert.Empty(await TextsAsync(_helper, ask));
        Assert.Equal(_outsider.ToString(), (await CardAsync(_leader, ask))!.Asker?.Id);
    }

    [Fact]
    public async Task AYoungPersonAskingToJoinReachesTheLeadersAndTheirParentButGetsTheChatOnlyOnceTheParentSaysYes()
    {
        Guid ask = Guid.NewGuid();
        await ActAsync(actions => actions.AskToJoinAsync(_teen, new AskToJoin { ChatId = ask.ToString(), MinistryId = _church.MinistryId.ToString(), Text = "Can I help?" }, Cancel));

        Assert.Equal(["Can I help?"], await TextsAsync(_leader, ask));
        Assert.Equal(["Can I help?"], await TextsAsync(_parent, ask));
        Assert.Empty(await TextsAsync(_teen, ask));

        await ActAsync(actions => actions.ConsentAsync(_parent, new Consent { ChatId = ask.ToString(), ChildId = _teen.ToString(), Approve = true }, Cancel));
        Assert.Equal(["Can I help?"], await TextsAsync(_teen, ask));
    }

    [Fact]
    public async Task ADeletedAccountsMessagesStayAsFromADeletedAccount()
    {
        Guid chat = await StartAsync("Sunday team", "");
        await SendAsync(_helper, chat, "I'll bring the urn", "urn");

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ChatsPersonData>().ForgetAsync(_helper, Cancel);

        ChatMessage kept = (await MessagesAsync(_leader, chat)).Single();
        Assert.Equal(("I'll bring the urn", ChatCopy.DeletedAccount, ""), (kept.Text, kept.SenderName, kept.SenderId));
    }

    [Fact]
    public async Task TheTeamStartsAGroupChatForTheTeamOnlyWhereTheMinistryLetsIt()
    {
        StartChat Chat(string kind) => new()
        {
            ChatId = Guid.NewGuid().ToString(), MinistryId = _church.MinistryId.ToString(), Name = "Setup crew", Kind = kind, Audience = ChatAudiences.Team, FirstMessage = "Who's in?",
        };

        await RefusedAsync(actions => actions.StartAsync(_helper, Chat(ChatKinds.Group), Cancel));
        _church.TeamMayStart = true;
        await RefusedAsync(actions => actions.StartAsync(_helper, Chat(ChatKinds.Channel), Cancel));
        await RefusedAsync(actions => actions.StartAsync(_outsider, Chat(ChatKinds.Group), Cancel));

        await ActAsync(actions => actions.StartAsync(_helper, Chat(ChatKinds.Group), Cancel));
    }

    private async Task<Guid> StartAsync(string name, string first)
    {
        Guid chat = Guid.NewGuid();
        await ActAsync(actions => actions.StartAsync(_leader, new StartChat
        {
            ChatId = chat.ToString(), MinistryId = _church.MinistryId.ToString(), Name = name, Kind = ChatKinds.Group, Audience = ChatAudiences.Team, FirstMessage = first,
        }, Cancel));
        return chat;
    }

    private Task SendAsync(Guid sender, Guid chat, string text, string clientId) =>
        ActAsync(actions => actions.SendAsync(sender, new SendMessage { ChatId = chat.ToString(), ClientId = clientId, Text = text }, Cancel));

    private async Task ActAsync(Func<ChatActions, Task> act)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await act(scope.ServiceProvider.GetRequiredService<ChatActions>());
        _clock.Advance(TimeSpan.FromSeconds(1));
    }

    private async Task RefusedAsync(Func<ChatActions, Task> act) => await Assert.ThrowsAsync<RpcException>(() => ActAsync(act));

    private async Task<MyChats> MineAsync(Guid person)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ChatReads>().MineAsync(person, Cancel);
    }

    private async Task<ChatMessages> ReadAsync(Guid person, Guid chat, long beforeSeq = 0, int limit = 0)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ChatReads>()
            .MessagesAsync(person, new MessagesRequest { ChatId = chat.ToString(), BeforeSeq = beforeSeq, Limit = limit }, Cancel);
    }

    private async Task<Guid> MessageIdAsync(Guid chat, string clientId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ChatsDbContext>().Messages
            .Where(message => message.ChatId == chat && message.ClientId == clientId).Select(message => message.Id).SingleAsync(Cancel);
    }

    private async Task<List<string>> TextsAsync(Guid person, Guid chat) => (await MessagesAsync(person, chat)).Select(message => message.Text).ToList();

    private async Task<List<ChatMessage>> MessagesAsync(Guid person, Guid chat) =>
        (await LatestAsync(ChatsFeeds.Messages, person))
        .Select(row => ChatsContractsModel.Instance.Deserialize<ChatMessage>(new MemoryStream(row.Payload)))
        .Where(message => message.ChatId == chat.ToString())
        .OrderBy(message => message.Seq)
        .ToList();

    private async Task<ChatCard?> CardAsync(Guid person, Guid chat) =>
        (await LatestAsync(ChatsFeeds.Chats, person))
        .Where(row => row.GroupKey == "chat")
        .Select(row => ChatsContractsModel.Instance.Deserialize<ChatCard>(new MemoryStream(row.Payload)))
        .SingleOrDefault(card => card.Id == chat.ToString());

    private async Task<List<FeedChangeRecord>> LatestAsync(string feed, Guid person)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        string owner = person.ToString();
        List<FeedChangeRecord> rows = await scope.ServiceProvider.GetRequiredService<SyncDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == feed && change.ScopeId == owner).ToListAsync(Cancel);
        return rows.GroupBy(row => row.RecordId).Select(versions => versions.MaxBy(row => row.Cursor)!).Where(row => !row.Deleted).ToList();
    }

    private static ClaimsPrincipal Signed(Guid person) => new(new ClaimsIdentity(
        [new Claim(Caller.UserIdClaim, person.ToString()), new Claim(Caller.SessionIdClaim, Guid.NewGuid().ToString())], "test"));

    private async Task<FeedBatch> PullAsync(Guid person, string feed)
    {
        ClaimsPrincipal caller = Signed(person);
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        PullReply reply = await scope.ServiceProvider.GetRequiredService<SyncService>().PullAsync(
            new PullRequest { Cursors = [new FeedCursor { Feed = feed, Cursor = 0 }] }, new CallContext(caller, new PersonCallContext(caller)));
        return reply.Feeds.SingleOrDefault() ?? new FeedBatch { Feed = feed };
    }

    private sealed class PersonCallContext(ClaimsPrincipal person) : ServerCallContext
    {
        private readonly Dictionary<object, object> _state = new() { ["__HttpContext"] = new DefaultHttpContext { User = person } };

        protected override IDictionary<object, object> UserStateCore => _state;
        protected override string MethodCore => "";
        protected override string HostCore => "";
        protected override string PeerCore => "";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => TestContext.Current.CancellationToken;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }

    private sealed class AlwaysSignedIn : ISignedInSessions
    {
        public ValueTask<bool> IsCurrentAsync(string sessionId, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
