using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Platform.Client.Location;

[JsonConverter(typeof(JsonStringEnumConverter<WalkPhase>))]
public enum WalkPhase
{
    [JsonStringEnumMemberName("walking")] Walking,
    [JsonStringEnumMemberName("paused")] Paused,
    [JsonStringEnumMemberName("finding")] Finding,
    [JsonStringEnumMemberName("done")] Done,
}

[JsonConverter(typeof(JsonStringEnumConverter<WalkTurn>))]
public enum WalkTurn
{
    [JsonStringEnumMemberName("none")] None,
    [JsonStringEnumMemberName("left")] Left,
    [JsonStringEnumMemberName("right")] Right,
    [JsonStringEnumMemberName("straight")] Straight,
    [JsonStringEnumMemberName("back")] Back,
}

[JsonConverter(typeof(JsonStringEnumConverter<WalkTapKind>))]
public enum WalkTapKind
{
    [JsonStringEnumMemberName("open")] Open,
    [JsonStringEnumMemberName("noJunkMail")] NoJunkMail,
    [JsonStringEnumMemberName("spoke")] Spoke,
    [JsonStringEnumMemberName("undo")] Undo,
    [JsonStringEnumMemberName("pause")] Pause,
    [JsonStringEnumMemberName("resume")] Resume,
    [JsonStringEnumMemberName("finish")] Finish,
    [JsonStringEnumMemberName("door")] Door,
}

public sealed record WalkActivityState(
    WalkPhase Phase,
    string Status,
    string Totals,
    string Street,
    string Side,
    int ToGo,
    WalkTurn Turn,
    string Next,
    string Said,
    bool CanUndo,
    bool Doorknock);

public sealed record WalkTap(long Seq, WalkTapKind Kind, DateTimeOffset At);

public interface IWalkActivity
{
    event Action? TapArrived;

    Task ShowAsync(string name, WalkActivityState state, string? alert);

    Task EndAsync();

    Task<IReadOnlyList<WalkTap>> TapsAsync(long since);

    Task ClearTapsAsync(long upTo);
}
