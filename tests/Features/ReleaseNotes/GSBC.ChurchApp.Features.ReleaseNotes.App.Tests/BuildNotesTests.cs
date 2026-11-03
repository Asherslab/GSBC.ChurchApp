using GSBC.ChurchApp.Features.ReleaseNotes.Generator;

namespace GSBC.ChurchApp.Features.ReleaseNotes.App.Tests;

public sealed class BuildNotesTests
{
    private const string AdminScreen = """
        ---
        title: A new admin screen
        audience: staff
        ---
        Staff can arrange the Home tiles from the admin panel.
        """;

    private const string Passkeys = """
        ---
        title: Sign in with Face ID
        audience: everyone
        highlight: true
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
    public void WhatToTestStaysWithinApplesLimit()
    {
        Note[] notes = Enumerable.Range(1, 60).Select(index => NoteFile.Parse("unreleased", $"change-{index}.md", Passkeys)).ToArray();

        string text = WhatToTest.Render(notes);

        Assert.InRange(text.Length, 1, WhatToTest.AppleLimit);
        Assert.EndsWith("more under More → What's new in the app.", text);
    }
}
