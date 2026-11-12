using GSBC.ChurchApp.Features.Chats.Contracts;

namespace GSBC.ChurchApp.Features.Chats.Api.Tests;

public sealed class ChatReviewTests
{
    private static readonly DateTime Sunday = new(2026, 11, 8, 23, 0, 0, DateTimeKind.Utc);
    private static readonly IReadOnlySet<string> NoneReported = new HashSet<string>();
    private static readonly IReadOnlySet<string> NoneOpened = new HashSet<string>();

    [Fact]
    public void ANarrowedReviewShowsEachMatchWithTheMessageEitherSideAndFoldsTheRest()
    {
        List<CheckedMessage> chat = Enumerable.Range(1, 9).Select(number => Message($"m{number}", number == 5 ? "Who has the desk on Sunday?" : "Fine")).ToList();

        IReadOnlyList<ReviewLine> lines = ChatReview.Lines(chat, new ReviewFilter(Words: "sunday"), NoneReported, NoneOpened, DayOf);

        Assert.Equal(["m4", "m5", "m6"], lines.OfType<ReviewMessage>().Select(line => line.Found.Message.Id));
        Assert.Equal("m5", Assert.Single(lines.OfType<ReviewMessage>(), line => line.Match).Found.Message.Id);
        Assert.Equal([3, 3], lines.OfType<ReviewFold>().Select(fold => fold.Ids.Count));
    }

    [Fact]
    public void ALongChatOpensOnItsNewestMessagesWithTheEarlierOnesFolded()
    {
        List<CheckedMessage> chat = Enumerable.Range(1, ChatReview.Recent + 10).Select(number => Message($"m{number}", "Fine")).ToList();

        IReadOnlyList<ReviewLine> lines = ChatReview.Lines(chat, new ReviewFilter(), NoneReported, NoneOpened, DayOf);

        Assert.Equal(10, Assert.IsType<ReviewFold>(lines[0]).Ids.Count);
        Assert.Equal($"m{ChatReview.Recent + 10}", lines.OfType<ReviewMessage>().Last().Found.Message.Id);
    }

    [Fact]
    public void AnOpenedFoldStaysOpenWhileTheSearchStays()
    {
        List<CheckedMessage> chat = Enumerable.Range(1, 5).Select(number => Message($"m{number}", number == 5 ? "Sunday" : "Fine")).ToList();

        IReadOnlyList<ReviewLine> lines = ChatReview.Lines(chat, new ReviewFilter(Words: "sunday"), NoneReported, new HashSet<string> { "m1", "m2", "m3" }, DayOf);

        Assert.Empty(lines.OfType<ReviewFold>());
    }

    [Fact]
    public void SearchingFindsWhatADeletedMessageSaidAndEarlierVersions()
    {
        CheckedMessage deleted = Message("gone", "", deletedText: "meet me after youth");
        deleted.Message.Deleted = true;
        CheckedMessage edited = Message("edited", "See you Sunday");
        edited.Versions.Add(new MessageVersion { Number = 1, Action = "sent", Text = "Don't tell your parents" });

        Assert.True(ChatReview.Matches(deleted, new ReviewFilter(Words: "after youth"), NoneReported, DayOf));
        Assert.True(ChatReview.Matches(edited, new ReviewFilter(Words: "parents"), NoneReported, DayOf));
    }

    private static DateOnly DayOf(DateTime at) => DateOnly.FromDateTime(at.AddHours(10));

    private static CheckedMessage Message(string id, string text, string deletedText = "") => new()
    {
        Message = new ChatMessage { Id = id, Kind = MessageKinds.Text, SenderId = "jordan", SenderName = "Jordan Example", SentAt = Sunday, Text = text },
        DeletedText = deletedText,
    };
}
