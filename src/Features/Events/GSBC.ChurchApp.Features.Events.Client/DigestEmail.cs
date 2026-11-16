using System.Net;
using System.Text;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed record EmailItem(DigestItemKind Kind, DigestWords Words, string? PictureUrl, string Place, string Contacts)
{
    public static EmailItem Of(DigestItem item) => new(item.Kind, item.Shown, DigestEmail.PictureUrl(item.Picture), item.Place, item.Contacts);
}

public static class DigestEmail
{
    private const string Heading = "What's Next at Good Shepherd";

    private const string Navy = "#2A3D5E";
    private const string Beige = "#D5C9B7";
    private const string Aqua = "#007a70";
    private const string Rust = "#9a5a1c";
    private const string Sand = "#F3EFE8";
    private const string Ink = "#1F2A3C";
    private const string Ink2 = "#4a5568";
    private const string Line = "#E7E2DA";
    private const string Font = "Montserrat,Arial,Helvetica,sans-serif";

    public static string? PictureUrl(MediaRef? picture) =>
        picture?.At(MediaSizeName.ForPhones) is { IsPrivate: false, Url: { } url } && IsWeb(url) ? url : null;

    public const int Width = 600;

    public static string Html(string range, IReadOnlyList<EmailItem> items, string allEventsLink)
    {
        StringBuilder html = new();
        html.Append($"""<table role="presentation" width="{Width}" cellpadding="0" cellspacing="0" border="0" bgcolor="#ffffff" style="width:{Width}px;max-width:100%;background:#ffffff;border-collapse:collapse">""");
        html.Append($"""<tr><td bgcolor="{Navy}" style="background:{Navy};padding:30px 32px 26px;font-family:{Font};color:#ffffff">""");
        html.Append($"""<div style="font-size:32px;font-weight:700;letter-spacing:-.02em;line-height:1.1;color:#ffffff">{Heading}</div>""");
        html.Append($"""<div style="font-size:15px;font-weight:600;color:{Beige};margin-top:8px">{Encode(range)}</div></td></tr>""");

        for (int index = 0; index < items.Count; index++)
            AppendItem(html, items[index], first: index == 0);

        html.Append($"""<tr><td style="padding:30px 32px 32px;font-family:{Font};color:{Ink}">""");
        html.Append($"""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr><td bgcolor="{Sand}" style="background:{Sand};border-radius:14px;padding:18px 20px;font-family:{Font};color:{Ink}">""");
        html.Append("""<div style="font-size:15px;font-weight:700">Everything else is in the app</div>""");
        html.Append($"""<p style="font-size:14px;line-height:1.5;color:{Ink2};margin:4px 0 14px">The calendar in the Good Shepherd app has every service, group and event, and updates when plans change.</p>""");
        html.Append(ButtonOf(allEventsLink, "See all events"));
        html.Append("</td></tr></table></td></tr></table>");
        return html.ToString();
    }

    private static void AppendItem(StringBuilder html, EmailItem item, bool first)
    {
        DigestWords words = item.Words;
        html.Append($"""<tr><td style="padding:{(first ? "26px" : "22px")} 32px 4px;font-family:{Font};color:{Ink}">""");
        html.Append(first ? "<div>" : $"""<div style="border-top:1px solid {Line};padding-top:22px">""");
        if (item.PictureUrl is { } picture && item.Kind != DigestItemKind.NotOn)
            html.Append($"""<img src="{Encode(picture)}" alt="" width="{Width - 64}" border="0" style="display:block;width:100%;max-width:{Width - 64}px;height:auto;border:0;border-radius:10px;margin-bottom:14px" />""");
        if (words.Label.Length > 0)
            html.Append($"""<div style="font-size:13px;font-weight:700;color:{(item.Kind == DigestItemKind.NotOn ? Rust : Aqua)}">{Encode(words.Label)}</div>""");
        html.Append($"""<div style="font-size:21px;font-weight:700;margin-top:2px">{Encode(words.Title)}</div>""");
        if (WhatsNextDigest.WhenLine(words, " @ ") is { Length: > 0 } when)
            html.Append($"""<div style="font-size:15px;font-weight:700;color:{Navy};margin-top:4px">{Encode(when)}</div>""");
        foreach (string paragraph in words.Description.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            html.Append($"""<p style="font-size:15px;line-height:1.5;margin:10px 0 0">{Encode(paragraph)}</p>""");
        if (FactsOf(item) is { Length: > 0 } facts)
            html.Append($"""<p style="font-size:14px;line-height:1.5;color:{Ink2};margin:8px 0 0">{Encode(facts)}</p>""");
        if (IsWeb(words.RegisterLink))
            html.Append("""<div style="margin-top:14px">""").Append(ButtonOf(words.RegisterLink, "Register")).Append("</div>");
        html.Append("</div></td></tr>");
    }

    private static string FactsOf(EmailItem item) =>
        EventWords.Join(item.Place, item.Contacts.Length > 0 ? $"Contact {item.Contacts}" : null);

    private static string ButtonOf(string link, string words) =>
        $"""<table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr><td bgcolor="{Navy}" style="background:{Navy};border-radius:20px">""" +
        $"""<a href="{Encode(link)}" target="_blank" style="display:inline-block;padding:11px 20px;font-family:{Font};font-size:14px;font-weight:600;line-height:1.2;color:#ffffff;text-decoration:none">{words}</a></td></tr></table>""";

    private static bool IsWeb(string link) =>
        Uri.TryCreate(link, UriKind.Absolute, out Uri? uri) && uri.Scheme is "https" or "http";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
