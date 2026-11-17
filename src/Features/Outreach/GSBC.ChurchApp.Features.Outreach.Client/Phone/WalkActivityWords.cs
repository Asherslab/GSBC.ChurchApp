using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Location;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public static class WalkActivityWords
{
    public static WalkActivityState Of(WalkState state, WalkProgress? progress, string? said, bool canUndo, DateTime now)
    {
        StepProgress? step = progress?.Now;
        StepProgress? next = progress?.Next;
        bool doorknock = state.Mode == WalkMode.Doorknock;
        string status = $"{(state.Paused ? "Paused" : "Walking")} · {OutreachPhoneWords.Duration(now - state.StartedAt)}";
        string planned = doorknock ? OutreachPhoneWords.Doors(state.PlannedLetterboxes) : OutreachPhoneWords.Letterboxes(state.PlannedLetterboxes);
        string totals = $"{OutreachWords.Count(progress?.LetterboxesDone ?? 0)} of {planned} · {OutreachPhoneWords.Distance(progress?.Metres ?? 0)}";
        WalkActivityState shown = new(WalkPhase.Walking, status, totals, "", "", 0, WalkTurn.None, "", said ?? "", canUndo && said is not null, doorknock);
        if (step is null)
            return progress?.HasFixes == false
                ? shown with { Phase = WalkPhase.Finding, Street = OutreachPhoneWords.FindingYou, Side = OutreachPhoneWords.FindingYouNote }
                : shown with { Phase = WalkPhase.Done, Street = OutreachPhoneWords.RouteDone, Side = OutreachPhoneWords.RouteDoneNote };
        return shown with
        {
            Phase = state.Paused ? WalkPhase.Paused : WalkPhase.Walking,
            Street = step.Step.Title,
            Side = OutreachPhoneWords.Side(step.Step.Delivers),
            ToGo = Math.Max(0, step.Step.Letterboxes - step.Done),
            Turn = next is null ? WalkTurn.None : OutreachPhoneWords.TurnFor(step.Step, next.Step),
            Next = next is null ? OutreachPhoneWords.LastStreet : $"{OutreachPhoneWords.Turn(step.Step, next.Step)} · {next.Step.Letterboxes}",
        };
    }

    public static bool IsTurn(SavedStep from, SavedStep to) => OutreachPhoneWords.TurnFor(from, to) is WalkTurn.Left or WalkTurn.Right or WalkTurn.Back;
}
