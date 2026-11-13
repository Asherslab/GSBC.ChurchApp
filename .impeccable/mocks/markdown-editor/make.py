import re
from pathlib import Path

HERE = Path(__file__).parent
ROOT = HERE.parent.parent.parent
SOURCE = (ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/IconPaths.cs").read_text()
ICONS = dict(re.findall(r'\["([a-z-]+)"\] = """(.*?)"""', SOURCE))
ICONS.update({
    "strike": '<path d="M16 4H9a3 3 0 0 0-2.83 4"/><path d="M14 12a4 4 0 0 1 0 8H6"/><line x1="4" x2="20" y1="12" y2="12"/>',
    "highlight": '<path d="m9 11-6 6v3h9l3-3"/><path d="m22 12-4.6 4.6a2 2 0 0 1-2.8 0l-5.2-5.2a2 2 0 0 1 0-2.8L14 4"/>',
    "code": '<polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/>',
    "code-block": '<path d="m10 9-3 3 3 3"/><path d="m14 15 3-3-3-3"/><rect x="3" y="3" width="18" height="18" rx="2"/>',
    "table": '<path d="M12 3v18"/><rect width="18" height="18" x="3" y="3" rx="2"/><path d="M3 9h18"/><path d="M3 15h18"/>',
    "superscript": '<path d="m4 19 8-8"/><path d="m12 19-8-8"/><path d="M20 12h-4c0-1.5.44-2 1.5-2.5S20 8.33 20 7c0-.47-.17-.93-.48-1.29a2.1 2.1 0 0 0-2.62-.44c-.42.24-.74.62-.9 1.07"/>',
    "subscript": '<path d="m4 5 8 8"/><path d="m12 5-8 8"/><path d="M20 19h-4c0-1.5.44-2 1.5-2.5S20 15.33 20 14c0-.47-.17-.93-.48-1.29a2.1 2.1 0 0 0-2.62-.44c-.42.24-.74.62-.9 1.07"/>',
    "video-link": '<path d="M2.5 17a24 24 0 0 1 0-10 2 2 0 0 1 1.4-1.4 49.6 49.6 0 0 1 16.2 0A2 2 0 0 1 21.5 7a24 24 0 0 1 0 10 2 2 0 0 1-1.4 1.4 49.6 49.6 0 0 1-16.2 0A2 2 0 0 1 2.5 17"/><path d="m10 15 5-3-5-3z"/>',
    "footnote": '<path d="M12 6v12"/><path d="M17.2 9 6.8 15"/><path d="m6.8 9 10.4 6"/>',
    "rule": '<path d="M3 12h18"/><path d="M8 7h8" opacity=".35"/><path d="M8 17h8" opacity=".35"/>',
    "card-split": '<rect x="3" y="3" width="18" height="7" rx="2"/><rect x="3" y="14" width="18" height="7" rx="2"/>',
    "type": '<path d="M4 7V4h16v3"/><path d="M9 20h6"/><path d="M12 4v16"/>',
})


def icon(name, size=18):
    return f'<svg width="{size}" height="{size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{ICONS[name]}</svg>'


CSS = """:root{--nav:#2a3d5e;--note:#d5c9b7;--page:#fff;--sand:#f4efe7;--sand2:#e9e1d4;--ink:#18233a;--ink2:#545e70;--ink3:#626a79;--accent:#00776d;--marker:#e7ae74;--line:rgba(42,61,94,.12);--field:#f7f4ef;--mark:#fbe7a1}
*{box-sizing:border-box;margin:0;padding:0}
body{width:1440px;height:900px;overflow:hidden;background:var(--sand);font-family:Montserrat,-apple-system,sans-serif;color:var(--ink);display:grid;grid-template-columns:64px 1fr 440px;font-size:15px}
.rail{background:var(--nav)}
.desk{display:flex;flex-direction:column;min-width:0;overflow:hidden}
.band{padding:20px 36px 14px;background:var(--page);border-bottom:1px solid var(--line)}
.crumb{font-size:13px;font-weight:600;color:var(--accent)}.band h1{font-size:25px;font-weight:700;letter-spacing:-.02em;margin-top:4px}
.sheet{margin:18px 36px 0;background:var(--page);border-radius:14px 14px 0 0;box-shadow:0 1px 0 var(--line),0 8px 28px rgba(42,61,94,.08);padding:0 32px 28px;flex:1;overflow:hidden;position:relative}
.tabs{display:flex;gap:26px;border-bottom:1px solid var(--line)}.tabs span{padding:12px 0;font-size:14.5px;font-weight:600;color:var(--ink2)}.tabs span.on{color:var(--ink);box-shadow:inset 0 -3px 0 var(--accent)}
.title{font-size:28px;font-weight:700;letter-spacing:-.015em;padding:16px 0 10px}
.bar{display:flex;align-items:center;gap:2px;padding:6px;border-radius:12px;background:var(--field);border:1px solid var(--line);position:relative}
.bar .t{width:36px;height:36px;border-radius:9px;display:grid;place-items:center;color:var(--ink2);flex:none}
.bar .t.on{background:var(--nav);color:#fff}
.bar .w{height:36px;border-radius:9px;display:inline-flex;align-items:center;gap:7px;padding:0 10px;color:var(--ink);font-size:13.5px;font-weight:600;flex:none}
.bar .w.sel{background:var(--page);border:1px solid var(--line);color:var(--ink);min-width:118px;justify-content:space-between}
.bar .d{width:1px;height:22px;background:var(--line);margin:0 6px;flex:none}.bar .gap{flex:1}
.bar.rows{flex-wrap:wrap;row-gap:4px}.bar .br{flex-basis:100%;height:1px;background:var(--line);margin:2px 4px}.bar .lead{font-size:12.5px;font-weight:700;color:var(--ink2);padding:0 6px 0 8px}
.bar .w.add{background:var(--nav);color:#fff;padding:0 14px}
.text{font-size:16px;line-height:1.65;padding:14px 4px 0}.text p{margin:0 0 12px}.text h2{font-size:20px;margin:6px 0 8px}
.text ul{padding-left:22px;margin:0 0 12px}.text a{color:var(--accent);text-decoration:underline;text-underline-offset:3px}
.text mark{background:var(--mark);border-radius:3px;padding:0 2px}.text s{color:var(--ink2)}.text code{font:500 14px ui-monospace,Menlo,monospace;background:var(--sand);border-radius:5px;padding:1px 5px}
.fig{margin:4px 0 14px;border-radius:12px;position:relative}.fig .pic{height:190px;border-radius:12px;background:linear-gradient(160deg,#c9d5c2 0%,#84a87d 55%,#4a7355 100%)}
.fig .pic.two{display:grid;grid-template-columns:1fr 1fr;gap:6px;background:none}.fig .pic.two i{border-radius:12px;background:linear-gradient(160deg,#d5c9b7,#b99e7d)}.fig .pic.two i+i{background:linear-gradient(160deg,#b9c7d8,#6f86a6)}
.fig figcaption{font-size:13.5px;color:var(--ink2);margin-top:7px}.fig.on{outline:2px solid var(--accent);outline-offset:4px}
.fig .x{position:absolute;top:10px;right:10px;display:flex;gap:6px}.fig .x b{height:30px;border-radius:15px;background:rgba(24,35,58,.78);color:#fff;font-size:12.5px;font-weight:600;padding:0 11px;display:inline-flex;align-items:center;gap:6px}
.filerow{display:flex;gap:12px;align-items:center;background:var(--sand);border-radius:12px;padding:10px 14px;margin:0 0 14px}.filerow .fi{width:34px;height:34px;border-radius:10px;background:rgba(0,119,109,.14);color:var(--accent);display:grid;place-items:center}
.filerow b{display:block;font-size:14.5px}.filerow small{font-size:12.5px;color:var(--ink2)}
.split{display:flex;align-items:center;gap:10px;color:var(--ink3);font-size:12px;font-weight:600;margin:6px 0 16px}.split:before,.split:after{content:"";flex:1;border-top:1.5px dashed rgba(98,106,121,.4)}
.caret{display:inline-block;width:2px;height:19px;background:var(--accent);vertical-align:-4px;margin-left:1px}
.menu{position:absolute;z-index:5;background:var(--page);border-radius:14px;box-shadow:0 12px 36px rgba(24,35,58,.18),0 0 0 1px var(--line);padding:6px;min-width:240px}
.menu h4{font-size:11.5px;font-weight:700;color:var(--ink3);padding:8px 10px 4px;letter-spacing:.02em}
.menu .i{display:flex;align-items:center;gap:11px;padding:8px 10px;border-radius:9px;font-size:14px;font-weight:600}.menu .i svg{color:var(--ink2)}.menu .i.hl{background:var(--sand)}
.menu .i kbd{margin-left:auto;font:600 12px Montserrat;color:var(--ink3)}.menu .i small{margin-left:auto;font-size:12px;color:var(--ink3);font-weight:500}
.menu .sep{height:1px;background:var(--line);margin:4px 6px}
.side{padding:24px 24px 0;display:flex;flex-direction:column;align-items:center;gap:12px;border-left:1px solid var(--line)}
.side>p{font-size:12.5px;font-weight:700;color:var(--ink2);align-self:flex-start}
.phone{width:390px;height:812px;border-radius:44px;background:var(--page);box-shadow:0 0 0 10px #18233a,0 20px 50px rgba(24,35,58,.25);overflow:hidden;position:relative;display:flex;flex-direction:column}
.ptop{height:96px;padding:50px 18px 0;display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid var(--line);font-weight:600;font-size:15.5px}.ptop .c{color:var(--accent)}.ptop .p{color:var(--accent);font-weight:700}
.pbody{padding:12px 18px;flex:1;overflow:hidden}.pbody .title{font-size:21px;padding:4px 0 10px}.pbody .text{font-size:15.5px;padding:0}.pbody .fig .pic{height:150px}
.dock{position:absolute;left:0;right:0;bottom:0}
.kbar{display:flex;align-items:center;gap:2px;padding:5px 6px;background:var(--page);border-top:1px solid var(--line);overflow:hidden;white-space:nowrap}
.kbar .t{width:44px;height:40px;border-radius:10px;display:grid;place-items:center;color:var(--ink2);flex:none}.kbar .t.on{background:var(--nav);color:#fff}
.kbar .w{height:40px;border-radius:20px;display:inline-flex;align-items:center;gap:6px;padding:0 12px;font-size:14px;font-weight:600;flex:none;background:var(--field)}
.kbar .d{width:1px;height:22px;background:var(--line);margin:0 4px;flex:none}
.kbar .fade{position:absolute;right:0;top:0;bottom:0;width:36px;background:linear-gradient(90deg,rgba(255,255,255,0),#fff)}
.kb{height:291px;background:#d1d4db;padding:8px 3px 0;display:grid;grid-template-rows:repeat(4,42px);gap:11px}
.kr{display:flex;gap:6px;justify-content:center}.kr i{display:block;width:32px;height:42px;border-radius:5px;background:#fff;box-shadow:0 1px 0 rgba(0,0,0,.3)}.kr i.w{width:42px;background:#abb0ba}.kr i.sp{width:180px}
.seg{display:flex;background:var(--sand2);border-radius:20px;padding:3px;flex:none;margin-right:4px}.seg span{height:34px;padding:0 12px;border-radius:17px;display:inline-flex;align-items:center;gap:5px;font-size:13.5px;font-weight:700;color:var(--ink2)}.seg span.on{background:var(--page);color:var(--ink);box-shadow:0 1px 2px rgba(24,35,58,.12)}
.psheet{position:absolute;left:0;right:0;bottom:0;background:var(--page);border-radius:20px 20px 0 0;box-shadow:0 -10px 40px rgba(24,35,58,.22);padding:8px 14px 30px}
.psheet .grab{width:38px;height:5px;border-radius:3px;background:var(--sand2);margin:0 auto 10px}
.psheet h4{font-size:12px;font-weight:700;color:var(--ink3);padding:6px 4px}
.grid4{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin-bottom:6px}.grid4 span{display:flex;flex-direction:column;align-items:center;gap:6px;padding:12px 4px;border-radius:14px;background:var(--sand);font-size:12.5px;font-weight:600;color:var(--ink)}
.grid4 span svg{color:var(--accent)}
.scrim{position:absolute;inset:0;background:rgba(24,35,58,.28)}
"""

KB = '<div class="kb">' + "".join('<div class="kr">' + "".join(f'<i class="{c}"></i>' for c in row) + "</div>" for row in [[""] * 10, [""] * 9, ["w"] + [""] * 7 + ["w"], ["w", "w", "sp", "w"]]) + "</div>"

DESK_TEXT = f"""<div class="text"><p>Christmas is a few weeks away, and there's a place for everyone. <strong>Bring a friend</strong>: invitation cards are at the <a>Welcome desk</a>.</p>
<ul><li><strong>Carols on the lawn</strong>, Saturday 20 December, 6pm</li><li><strong>Christmas Eve</strong>, 7pm, <mark>with the kids' nativity</mark></li><li><strong>Christmas Day</strong>, 9am <s>and 10:30</s>, one service only<span class="caret"></span></li></ul>
<figure class="fig on"><div class="pic"></div><div class="x"><b>Replace</b><b>Remove</b></div><figcaption>Carols on the lawn last year</figcaption></figure>
<div class="filerow"><span class="fi">{icon("file")}</span><div><b>Christmas roster.pdf</b><small>PDF · 240 KB</small></div></div>
<div class="split">Next card</div>
<h2>Helping on the day</h2><p>Set-up starts at 4pm. Wi-Fi for the sound desk is <code>GSBC-Hall</code>.</p></div>"""

PHONE_TEXT = """<div class="title">Christmas at Good Shepherd</div><div class="text"><p>There's a place for everyone. <strong>Bring a friend</strong>: cards are at the <a>Welcome desk</a>.</p>
<ul><li><strong>Carols</strong>, Sat 20 Dec, 6pm</li><li><strong>Christmas Eve</strong>, 7pm<span class="caret"></span></li></ul>
<figure class="fig"><div class="pic"></div><figcaption>Carols on the lawn last year</figcaption></figure></div>"""


def t(name, on=False, label=""):
    return f'<span class="t{" on" if on else ""}" title="{label}">{icon(name)}</span>'


def menu_items(items):
    out = []
    for item in items:
        if item == "-":
            out.append('<div class="sep"></div>')
        elif item[0] == "#":
            out.append(f"<h4>{item[1:]}</h4>")
        else:
            name, label, hint = (item + ("",))[:3]
            hl = " hl" if label.startswith("!") else ""
            label = label.lstrip("!")
            out.append(f'<div class="i{hl}">{icon(name)}{label}{f"<small>{hint}</small>" if hint else ""}</div>')
    return "".join(out)


def page(title, bar, menu, phone_body, phone_dock, note):
    return f"""<!doctype html><html lang="en-AU"><head><meta charset="utf-8"><link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;500;600;700&display=swap" rel="stylesheet"><title>{title}</title><style>{CSS}</style></head>
<body><div class="rail"></div><main class="desk"><div class="band"><p class="crumb">‹ Church news</p><h1>New post</h1></div>
<div class="sheet"><div class="tabs"><span class="on">Write</span><span>Preview on a phone</span></div><div class="title">Christmas at Good Shepherd: every service</div>{bar}{menu}{DESK_TEXT}</div></main>
<aside class="side"><p>{note}</p><div class="phone"><div class="ptop"><span class="c">Cancel</span><span>New post</span><span class="p">Post</span></div><div class="pbody">{phone_body}</div>{phone_dock}</div></aside></body></html>"""


FORMAT = t("bold", True, "Bold ⌘B") + t("italic", label="Italic ⌘I") + t("strike", label="Strikethrough ⌘⇧X") + t("highlight", label="Highlight ⌘⇧H")

A_BAR = ('<div class="bar rows"><span class="w sel">Text ' + icon("chevron-down", 16) + '</span><span class="d"></span>' + FORMAT + '<span class="d"></span>'
         + t("list") + t("list-ordered") + t("quote") + t("link") + t("code") + '<span class="br"></span><span class="lead">Add</span>'
         + f'<span class="w">{icon("image")}Picture</span><span class="w">{icon("video")}Video</span><span class="w">{icon("video-link")}Video link</span><span class="w">{icon("file")}File</span>'
         + f'<span class="gap"></span><span class="w" style="background:var(--page);border:1px solid var(--line)">Extras {icon("chevron-down", 16)}</span></div>')
A_MENU = ('<div class="menu" style="right:32px;top:212px">' + menu_items([("code-block", "Code block"), ("table", "Table"), ("rule", "Line across"), ("card-split", "!Split into cards"), "-",
                                                                         ("footnote", "Footnote"), ("superscript", "Superscript", "x²"), ("subscript", "Subscript", "H₂O"), ("smile", "Emoji")]) + "</div>")
A_DOCK = ('<div class="dock"><div class="kbar">' + t("bold", True) + t("italic") + t("list") + t("link") + '<span class="d"></span>'
          + f'<span class="w">{icon("image")}Picture</span><span class="w">{icon("video")}Video</span><span class="w">{icon("video-link")}Link</span><span class="fade"></span></div>{KB}</div>')

B_BAR = ('<div class="bar"><span class="w sel">Heading 2 ' + icon("chevron-down", 16) + '</span><span class="d"></span>' + FORMAT + '<span class="d"></span>'
         + t("list") + t("list-ordered") + t("quote") + t("link") + t("code") + '<span class="d"></span>' + t("more", label="More formatting")
         + f'<span class="gap"></span><span class="w add">{icon("plus")}Insert</span></div>')
B_MENU = ('<div class="menu" style="right:32px;top:166px;min-width:280px">' + menu_items(["#Pictures and media", ("image", "!Picture", "From the library or upload"), ("video", "Video", "Uploaded"), ("video-link", "Video link", "YouTube, Vimeo"), ("file", "File"), "-",
                                                                                       "#Layout", ("card-split", "Split into cards"), ("rule", "Line across"), ("table", "Table"), ("code-block", "Code block"), "-", "#In the text", ("footnote", "Footnote"), ("smile", "Emoji")]) + "</div>")
B_DOCK = ('<div class="scrim"></div><div class="psheet"><div class="grab"></div><h4>Insert</h4><div class="grid4">'
          + "".join(f"<span>{icon(n, 22)}{l}</span>" for n, l in [("image", "Picture"), ("video", "Video"), ("video-link", "Video link"), ("file", "File")])
          + '</div><h4>Layout</h4><div class="grid4">' + "".join(f"<span>{icon(n, 22)}{l}</span>" for n, l in [("card-split", "Cards"), ("rule", "Line"), ("table", "Table"), ("smile", "Emoji")]) + "</div></div>")

C_BAR = ('<div class="bar">' + t("heading", label="Heading sizes") + '<span class="d"></span>' + FORMAT + t("code") + '<span class="d"></span>'
         + t("list") + t("list-ordered") + t("quote") + t("link") + '<span class="d"></span>'
         + t("image", label="Picture") + t("video", label="Video") + t("video-link", label="Video link") + t("file", label="File")
         + '<span class="d"></span>' + t("card-split", label="Split into cards") + '<span class="gap"></span>' + t("ellipsis", label="Extras") + "</div>")
C_MENU = ('<div class="menu" style="left:250px;top:166px;min-width:220px">' + menu_items([("type", "Text"), ("heading", "!Heading 1", "#"), ("heading", "Heading 2", "##"), ("heading", "Heading 3", "###")]) + "</div>"
          '<div class="menu" style="right:32px;top:166px">' + menu_items([("code-block", "Code block"), ("table", "Table"), ("rule", "Line across"), "-", ("footnote", "Footnote"), ("superscript", "Superscript"), ("subscript", "Subscript"), ("smile", "Emoji")]) + "</div>")
C_DOCK = ('<div class="dock"><div class="kbar"><span class="seg"><span>Aa</span><span class="on">' + icon("plus", 16) + 'Add</span></span>'
          + t("image") + t("video") + t("video-link") + t("file") + t("card-split") + t("table") + t("ellipsis") + f"</div>{KB}</div>")

for name, title, bar, menu, dock, note in [
    ("a-words-for-adding.html", "Words for what you add", A_BAR, A_MENU, A_DOCK, "On the phone: one row on the keyboard, sliding sideways"),
    ("b-insert-button.html", "One Insert button", B_BAR, B_MENU, B_DOCK, "On the phone: + on the keyboard bar opens Insert"),
    ("c-icons-and-two-rows.html", "Every tool an icon, two rows on the phone", C_BAR, C_MENU, C_DOCK, "On the phone: Aa for formatting, Add for what goes in"),
]:
    (HERE / name).write_text(page(title, bar, menu, PHONE_TEXT, dock, note))
