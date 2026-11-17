using System.Net;
using System.Text;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed record EmailItem(
    DigestItemKind Kind, DigestWords Words, string? PictureUrl, string Place, string Contacts, DigestPictureAt PictureAt = DigestPictureAt.Beside, string Link = "")
{
    public static EmailItem Of(DigestItem item, Func<string, string> shareable)
    {
        DigestWords words = item.Shown;
        return new EmailItem(item.Kind, words, DigestEmail.PictureUrl(words.Picture), item.Place, item.Contacts, item.PictureAt,
            item.Link.Length > 0 ? shareable(item.Link) : "");
    }
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
    public const string Behind = "#E9EEF4";
    private const string Font = "Montserrat,Arial,Helvetica,sans-serif";

    public static string? PictureUrl(MediaRef? picture) =>
        picture?.At(MediaSizeName.ForPhones) is { IsPrivate: false, Url: { } url } && IsWeb(url) ? url : null;

    public const int Width = 600;
    private const string LightOnly = "<style>:root{color-scheme:light only;supported-color-schemes:light only}</style>";
    private const int Inner = Width - 64;
    private const string PictureShare = "34%";

    public static string Html(string range, IReadOnlyList<EmailItem> items, string allEventsLink, string? topPictureUrl = null)
    {
        StringBuilder html = new();
        html.Append(LightOnly);
        html.Append($"""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" bgcolor="{Behind}" style="width:100%;background:{Behind};border-collapse:collapse;color-scheme:light only">""");
        html.Append("""<tr><td align="center" style="padding:0 0 32px">""");
        html.Append($"""<!--[if mso]><table role="presentation" width="{Width}" align="center" cellpadding="0" cellspacing="0" border="0"><tr><td><![endif]-->""");
        html.Append($"""<table role="presentation" width="100%" align="center" cellpadding="0" cellspacing="0" border="0" bgcolor="#ffffff" style="width:100%;max-width:{Width}px;margin:0 auto;background:#ffffff;border-collapse:collapse">""");
        if (topPictureUrl is { } top && IsWeb(top))
            html.Append($"""<tr><td style="padding:0;line-height:0"><img src="{Encode(top)}" alt="" width="{Width}" border="0" style="display:block;width:100%;max-width:{Width}px;height:auto;border:0" /></td></tr>""");
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
        html.Append("<!--[if mso]></td></tr></table><![endif]-->");
        html.Append("</td></tr></table>");
        return html.ToString();
    }

    public static string ItemHtml(EmailItem item)
    {
        StringBuilder html = new();
        html.Append($"""<table role="presentation" width="{Width}" cellpadding="0" cellspacing="0" border="0" bgcolor="#ffffff" style="width:{Width}px;background:#ffffff;border-collapse:collapse">""");
        AppendItem(html, item, first: true);
        html.Append("""<tr><td style="padding:0 0 22px"></td></tr></table>""");
        return html.ToString();
    }

    private static void AppendItem(StringBuilder html, EmailItem item, bool first)
    {
        html.Append($"""<tr><td style="padding:{(first ? "26px" : "22px")} 32px 4px;font-family:{Font};color:{Ink}">""");
        html.Append(first ? "<div>" : $"""<div style="border-top:1px solid {Line};padding-top:22px">""");
        string? picture = item.Kind == DigestItemKind.NotOn ? null : item.PictureUrl;
        if (picture is not null && item.PictureAt == DigestPictureAt.Beside)
        {
            html.Append("""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="border-collapse:collapse"><tr>""");
            html.Append($"""<td width="{PictureShare}" valign="top" style="width:{PictureShare};padding:0 18px 0 0;vertical-align:top">""");
            html.Append($"""<img src="{Encode(picture)}" alt="" width="190" border="0" style="display:block;width:100%;max-width:190px;height:auto;border:0;border-radius:8px" /></td>""");
            html.Append($"""<td valign="top" style="vertical-align:top;font-family:{Font};color:{Ink}">""");
            AppendWords(html, item);
            html.Append("</td></tr></table>");
        }
        else
        {
            if (picture is not null)
                html.Append($"""<img src="{Encode(picture)}" alt="" width="{Inner}" border="0" style="display:block;width:100%;max-width:{Inner}px;height:auto;border:0;border-radius:10px;margin-bottom:14px" />""");
            AppendWords(html, item);
        }
        html.Append("</div></td></tr>");
    }

    private static void AppendWords(StringBuilder html, EmailItem item)
    {
        DigestWords words = item.Words;
        if (words.Label.Length > 0 && words.Label != words.Title)
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
        if (item.Kind == DigestItemKind.News && IsWeb(item.Link))
            html.Append("""<div style="margin-top:14px">""").Append(ButtonOf(item.Link, "Read the post")).Append("</div>");
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
