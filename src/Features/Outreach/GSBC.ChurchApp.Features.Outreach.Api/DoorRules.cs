using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal enum DoorKeeping
{
    Nothing,
    NoAnswer,
    FollowUp,
    Hostile,
}

internal static class DoorRules
{
    public static DoorKeeping WhatToKeep(DoorOutcome outcome) => outcome.Outcome switch
    {
        DoorOutcomeKind.NoAnswer => DoorKeeping.NoAnswer,
        DoorOutcomeKind.FollowUp when outcome.Consent && OutreachRules.Clip(outcome.Contact, OutreachRules.LongestContact).Length > 0 => DoorKeeping.FollowUp,
        DoorOutcomeKind.Hostile => DoorKeeping.Hostile,
        _ => DoorKeeping.Nothing,
    };
}
