using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class SvgIconTests
{
    private const string Dove = """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><path d="M2 12 L12 2 L22 12 Z" fill="#2a3d5e" stroke="none"/></svg>""";

    [Theory]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><script>alert(1)</script><path d="M0 0h24v24z"/></svg>""", "script")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" onload="alert(1)"><path d="M0 0h24v24z" onclick="alert(1)"/></svg>""", "alert")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><foreignObject><div xmlns="http://www.w3.org/1999/xhtml">hi</div></foreignObject><path d="M0 0h24v24z"/></svg>""", "foreignObject")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 24 24"><image xlink:href="https://evil.example/x.png"/><use xlink:href="https://evil.example/s.svg#a"/><path d="M0 0h24v24z"/></svg>""", "evil")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><image href="data:image/png;base64,AAAA"/><path d="M0 0h24v24z"/></svg>""", "data:")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M0 0h24v24z" fill="url(https://evil.example/#g)" style="fill:url(javascript:alert(1))"/></svg>""", "evil")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><a href="javascript:alert(1)"><path d="M1 1h2z"/></a><path d="M0 0h24v24z"/><style>@import url(https://evil.example/x.css)</style></svg>""", "javascript")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><animate attributeName="href" to="javascript:alert(1)"/><set attributeName="onload" to="alert(1)"/><path d="M0 0h24v24z"/></svg>""", "alert")]
    public void HostileDrawingsLoseEverythingThatCouldRunFetchOrEmbed(string hostile, string mustNotSurvive)
    {
        string? clean = SvgIcons.Sanitise(hostile);

        Assert.NotNull(clean);
        Assert.DoesNotContain(mustNotSurvive, clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<path", clean);
    }

    [Theory]
    [InlineData("""<?xml version="1.0"?><!DOCTYPE svg [<!ENTITY x SYSTEM "file:///etc/passwd">]><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M0 0h24v24z"/></svg>""")]
    [InlineData("""<html><body><svg viewBox="0 0 24 24"><path d="M0 0z"/></svg></body></html>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><script>alert(1)</script></svg>""")]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg"><path d="M0 0h24v24z"/></svg>""")]
    [InlineData("not a drawing")]
    public void ADrawingThatCannotBeMadeSafeAndSizedIsRefused(string bad) => Assert.Null(SvgIcons.Sanitise(bad));

    [Fact]
    public void ACleanDrawingIsStoredNormalisedAndCleaningTwiceChangesNothing()
    {
        string clean = SvgIcons.Sanitise(Dove)!;

        Assert.StartsWith("""<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">""", clean);
        Assert.Equal(clean, SvgIcons.Sanitise(clean));
        Assert.Null(HubSurfaces.Icon.Drawing.ProblemWith(clean));
        Assert.NotNull(HubSurfaces.Icon.Drawing.ProblemWith(Dove));
        Assert.Equal(clean, HubSurfaces.Icon.Drawing.Normalise(Dove));
    }

    [Fact]
    public void FollowingTheThemeDrawsEveryPaintInTheTilesColourButKeepsHoles()
    {
        string mono = SvgIcons.Monochrome(SvgIcons.Sanitise(Dove)!);

        Assert.DoesNotContain("#2a3d5e", mono);
        Assert.Contains("fill=\"currentColor\"", mono);
        Assert.Contains("stroke=\"none\"", mono);
    }
}
