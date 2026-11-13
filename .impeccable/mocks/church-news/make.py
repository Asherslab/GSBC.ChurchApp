import base64
import re
from pathlib import Path

HERE = Path(__file__).parent
MOCKS = HERE.parent
ROOT = MOCKS.parent.parent


def _css(path):
    text = path.read_text()

    def embed(m):
        target = (path.parent / m.group(1)).resolve()
        if not target.exists():
            return m.group(0)
        kind = {"woff2": "font/woff2", "svg": "image/svg+xml"}.get(target.suffix[1:], "application/octet-stream")
        return 'url("data:' + kind + ';base64,' + base64.b64encode(target.read_bytes()).decode() + '")'

    text = re.sub(r'url\("(\.\./(?:fonts|img)/[^"]+)"\)', embed, text)
    return re.sub(r'@import url\("([^"]+)"\);', lambda m: _css((path.parent / m.group(1)).resolve()), text)


def _inline():
    css = "".join(_css(f) for f in [
        MOCKS / "design-system" / "kit.css",
        ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/css/rich-text.css",
        ROOT / "src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Client/wwwroot/css/ministries.css",
        HERE / "news.css"])
    js = (MOCKS / "design-system" / "icons.js").read_text() + "\n" + (MOCKS / "design-system" / "kit.js").read_text()
    return "<style>" + css + "</style><script>" + js + "</script>"


TABS = "Home:home,News:newspaper,Sermons:sermons,Calendar:calendar,More:menu"


def phone(title, body, tab="News", overlay="", scroll=0, tabs=TABS, topbar=""):
    return (f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>
{_inline()}
</head>
<body class="sk-phone" data-tab="{tab}" data-tabs="{tabs}">
<div class="sk-screen"><div class="page-scroll">{topbar or '<header class="topbar"><div class="topbar__start"></div><span class="topbar__title"></span><div class="topbar__end"><button class="icon-btn" aria-label="Notifications"><i data-icon="bell" data-size="md"></i></button></div></header>'}
<div class="page">{body}</div></div>{overlay}</div>
<script>addEventListener("load",()=>{{document.querySelector(".page-scroll").scrollTop={scroll};}})</script>
</body></html>""")


def masthead(title, note, extra=""):
    return f'<header class="masthead"><h1>{title}</h1><p class="masthead__note">{note}</p>{extra}</header>'


def pic(tone="a", label=""):
    return f'<span class="sk-pic sk-pic--{tone}"><i data-icon="image" data-size="md"></i>{label}</span>'


def write(name, html):
    (HERE / name).write_text(html)


POSTS = [
    dict(t="Christmas at Good Shepherd: every service, and what's on", x="Carols on the lawn on the 20th, Christmas Eve at 7pm and Christmas Day at 9am. Bring a friend; there's a place for everyone.", by="Church office", ago="2h", pic="a", tag=""),
    dict(t="Holiday program volunteers needed", x="Three mornings in January. Training on Saturday the 6th; a current blue card is needed.", by="Church office", ago="Yesterday", pic="", tag="Kids ministry"),
    dict(t="Building works: the car park is closed on Sunday", x="Park on the street or in the school grounds across the road. The side gate is open from 9am.", by="Church office", ago="Tue", pic="", tag="", urgent=True),
    dict(t="Baptisms on 30 November", x="Four people are being baptised at the 10:30 service. Come and celebrate with them.", by="Church office", ago="Mon", pic="b", tag=""),
    dict(t="Youth camp photos are up", x="Thanks to every leader who came. The photos and a short video are in the post.", by="Youth", ago="Last week", pic="c", tag="Youth"),
]


def from_line(p):
    bits = []
    if p.get("tag"):
        bits.append(f'<span class="sk-from"><i data-icon="users"></i>From {p["tag"]}</span>')
    bits.append(f'<span>{p["ago"]}</span>')
    return '<span class="sk-meta">' + "".join(bits) + "</span>"


def a_item(p, lead=False):
    if p["pic"]:
        cls = "sk-story sk-story--lead" if lead else "sk-story"
        return f'<a class="{cls}">{pic(p["pic"])}<span class="sk-story__text"><span class="sk-story__title">{p["t"]}</span><span class="sk-story__excerpt">{p["x"]}</span>{from_line(p)}</span></a>'
    urgent = '<span class="pill">Today</span> ' if p.get("urgent") else ""
    return f'<a class="row row--reading sk-brief"><span class="row__body"><span class="row__title">{urgent}{p["t"]}</span><span class="row__subtitle">{p["x"]}</span>{from_line(p)}</span></a>'


a_body = masthead("News", "From the church and its ministries") + '<section class="section section--first sk-front">' + "".join(a_item(p, i == 0) for i, p in enumerate(POSTS)) + "</section>"
write("a-front-page.html", phone("Picture-led front page", a_body))

seg = '<div class="masthead__extra"><div class="segmented segmented--on-masthead" role="radiogroup"><button class="segmented__option" role="radio" aria-checked="true">The church</button><button class="segmented__option" role="radio" aria-checked="false">Your ministries <span class="sk-count">3</span></button></div></div>'


def b_item(p):
    thumb = f'<span class="sk-thumb">{pic(p["pic"])}</span>' if p["pic"] else ""
    urgent = '<span class="pill">Today</span> ' if p.get("urgent") else ""
    return f'<a class="row row--reading sk-brief">{"" }<span class="row__body"><span class="row__title">{urgent}{p["t"]}</span><span class="row__subtitle sk-clamp">{p["x"]}</span>{from_line(p)}</span>{thumb}</a>'


b_body = masthead("News", "For the whole church, and from the ministries you're in", seg) + '<section class="section section--first"><div class="list">' + "".join(b_item(p) for p in POSTS) + "</div></section>"
write("b-church-and-yours.html", phone("One News: the church and your ministries", b_body))

def c_item(p, new):
    dot = '<span class="row__unread"></span>' if new else ""
    urgent = '<span class="pill">Today</span> ' if p.get("urgent") else ""
    icon = '<span class="row__icon row__icon--warm"><i data-icon="megaphone"></i></span>' if p.get("urgent") else (
        f'<span class="row__icon row__icon--accent"><i data-icon="users"></i></span>' if p.get("tag") else '<span class="row__icon"><i data-icon="church"></i></span>')
    return f'<a class="row sk-inbox">{icon}<span class="row__body"><span class="row__title">{dot}{urgent}{p["t"]}</span><span class="row__subtitle sk-one">{(("From " + p["tag"] + " · ") if p.get("tag") else "") + p["x"]}</span></span><span class="row__trail">{p["ago"]}</span></a>'


c_body = (masthead("News", "3 new since you last looked")
          + '<section class="section section--first"><div class="section__head"><h2 class="section__title">New</h2></div><div class="list">' + "".join(c_item(p, True) for p in POSTS[:3]) + "</div></section>"
          + '<section class="section"><div class="section__head"><h2 class="section__title">Earlier</h2></div><div class="list">' + "".join(c_item(p, False) for p in POSTS[3:]) + "</div></section>")
write("c-whats-new.html", phone("What's new, as a list", c_body))
