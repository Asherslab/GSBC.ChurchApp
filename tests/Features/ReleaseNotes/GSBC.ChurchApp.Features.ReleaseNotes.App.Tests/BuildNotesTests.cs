using GSBC.ChurchApp.Features.ReleaseNotes.Generator;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.ReleaseNotes.App.Tests;

public sealed class BuildNotesTests
{
    private const string AdminScreen = """
        ---
        title: A new admin screen
        audience: staff
        added: 2026-11-02
        ---
        Staff can arrange the Home tiles from the admin panel.
        """;

    private const string Passkeys = """
        ---
        title: Sign in with Face ID
        audience: everyone
        added: 2026-11-01
        highlight: true
        link: account
        ---
        Sign in with Face ID or Touch ID.

        ## For staff
        Where: More → Account
        Try: Sign in, close the app, open it again.
        """;

    [Fact]
    public void AProductionBuildCarriesNoStaffNotesOrStaffWording()
    {
        Note[] notes = [NoteFile.Parse("unreleased", "admin-screen.md", AdminScreen), NoteFile.Parse("unreleased", "passkeys.md", Passkeys)];

        Note only = Assert.Single(BuildNotes.For(BuildNotes.AudienceOf("everyone"), notes));

        Assert.Equal("passkeys", only.Id);
        Assert.Null(only.Where);
        Assert.Null(only.Try);
        Assert.Equal(2, BuildNotes.For(BuildNotes.AudienceOf("staff"), notes).Count);
    }

    [Fact]
    public void WhatsNewListsTheNewestNoteFirst()
    {
        Note[] notes = [NoteFile.Parse("unreleased", "passkeys.md", Passkeys), NoteFile.Parse("unreleased", "admin-screen.md", AdminScreen)];

        Assert.Equal(["admin-screen", "passkeys"], BuildNotes.For(BuildNotes.AudienceOf("staff"), notes).Select(note => note.Id));
    }

    [Fact]
    public void ShowMeOpensAScreenInTheApp()
    {
        Note note = NoteFile.Parse("unreleased", "passkeys.md", Passkeys);

        Assert.Equal(LinkKind.InApp, LinkTargets.KindOf(note.Link!));
    }

    [Fact]
    public void WhatToTestStaysWithinApplesLimit()
    {
        Note[] notes = Enumerable.Range(1, 300).Select(index => NoteFile.Parse("unreleased", $"change-{index}.md", Passkeys)).ToArray();

        string text = WhatToTest.Render(notes);

        Assert.InRange(text.Length, 1, WhatToTest.AppleLimit);
        Assert.EndsWith("more under More → What's new in the app.", text);
    }

    [Fact]
    public void WhatToTestGivesHighlightsInFullAndNamesEveryOtherChange()
    {
        Note[] notes =
        [
            .. Enumerable.Range(1, 30).Select(index => NoteFile.Parse("unreleased", $"admin-{index}.md", AdminScreen.Replace("A new admin screen", $"Admin change {index}"))),
            NoteFile.Parse("unreleased", "passkeys.md", Passkeys),
        ];

        string text = WhatToTest.Render(notes);

        Assert.InRange(text.Length, 1, WhatToTest.AppleLimit);
        Assert.StartsWith("Sign in with Face ID\nSign in with Face ID or Touch ID.\nWhere: More → Account\nTry: Sign in, close the app, open it again.", text);
        Assert.All(Enumerable.Range(1, 30), index => Assert.Contains($"Admin change {index} (staff only)", text));
        Assert.DoesNotContain("…and", text);
    }
}
