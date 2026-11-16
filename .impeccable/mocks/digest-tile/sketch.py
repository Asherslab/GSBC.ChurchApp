import json
import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE.parent / "news-tile"))

import make
from make import tile, photo, icon, grid, DEV_HOME, TABS

ROOT = HERE.parent.parent.parent
EVENTS_CSS = (ROOT / "src/Features/Events/GSBC.ChurchApp.Features.Events.Client/wwwroot/css/events.css").read_text()
STYLE = make.STYLE.replace("</style>", EVENTS_CSS + (HERE / "digest.css").read_text() + "</style>")

RANGE = "14 – 27 December"
ITEMS = [
    dict(title="Baptisms", label="Baptisms this Sunday", when="This Sunday", time="10:30am", short="Sun 10:30am", pic="baptism",
         about="Five people from our church family are being baptised. Bring a friend and stay for morning tea."),
    dict(title="No Bible Study & Prayer", label="Services on break", when="The next two Wednesdays", time="", short="Not on Wed", pic=None, off=True,
         about="Back on Wednesday 13 January."),
    dict(title="Christmas Day service", label="Christmas", when="Friday 25 December", time="9am", short="Fri 25 Dec", pic="christmas",
         about="One service at 9am, with carols for all ages."),
    dict(title="Kids holiday club", label="Impact Kids", when="Monday 4 to Wednesday 6 January", time="9am", short="Mon 4 Jan", pic="camp",
         about="Three mornings of games, craft and Bible stories for primary-aged kids."),
]


def when(item, between=" · "):
    return item["when"] + (between + item["time"] if item["time"] else "")


def with_tile(drawn, w, h):
    tiles = list(DEV_HOME)
    index = next(i for i, entry in enumerate(tiles) if entry[0] == "bulletin")
    tiles[index] = ("whats-next", w, h, drawn)
    return tiles


def home_phone(palette, scheme, drawn, caption, note=""):
    tabbar = "".join(f'<span class="{"on" if label == "Home" else ""}">{icon(name, "")}{label}</span>' for label, name in TABS)
    return f'''<figure class="sk-device" data-palette="{palette}" data-preview-scheme="{scheme}">
<figcaption><b>{caption}</b><span>{note}</span></figcaption>
<div class="sk-device__screen"><div class="sk-device__scroll">
<div class="page"><header class="masthead masthead--home"><span class="brand-mark masthead__mark" aria-hidden="true"></span><h1>Good Shepherd</h1><div class="home-line"><p class="masthead__note">Sunday 13 December</p></div></header>
<section class="section section--first">{grid(with_tile(drawn, 1, 2))}</section></div></div>
<div class="sk-status"><span>9:41</span><span class="sk-status__right"><i></i><i></i><i></i></span></div>
<nav class="sk-tabbar">{tabbar}</nav></div></figure>'''


def size(label, w, h, drawn, scheme, cls=""):
    return (f'<div class="sk-size {cls}" data-palette="brand" data-preview-scheme="{scheme}"><p class="sk-size__label">{label}</p>'
            f'<div class="sk-size__grid">{grid([("t", w, h, drawn)], columns=min(w, 2) if cls else 2)}</div></div>')


def sizes_panel(draws):
    narrow = "".join(size(*draw, cls="sk-size--narrow") for draw in draws if draw[1] == 1)
    wide = "".join(size(*draw) for draw in draws if draw[1] == 2)
    return f'<section class="sk-sizes"><h2>Every other size<span>Staff can resize it; each size is designed for what fits.</span></h2><div class="sk-sizes__body"><div class="sk-sizes__pair">{narrow}</div>{wide}</div></section>'


def earlier_rows():
    rows = [("30 November – 13 December", "6 things"), ("16 – 29 November", "4 things"), ("2 – 15 November", "5 things")]
    return "".join(f'<a class="row"><span class="row__body"><span class="row__title">{title}</span><span class="row__subtitle">{count}</span></span>'
                   f'<span class="row__trail">{icon("chevron-right")}</span></a>' for title, count in rows)


def card(item):
    if item.get("off"):
        return (f'<div class="card card--callout whats-next-off"><span class="card__body"><span class="whats-next-off__head">{icon("circle-off")}<strong>{item["title"]}</strong></span>'
                f'<span class="whats-next-off__when">{when(item)}</span><span class="whats-next-off__about">{item["about"]}</span></span></div>')
    return (f'<div class="card card--raised whats-next-card"><span class="card__media"><span class="sk-cover">{photo(item["pic"])}</span></span><span class="card__body">'
            f'<span class="whats-next-card__label">{item["label"]}</span><strong class="whats-next-card__title">{item["title"]}</strong>'
            f'<span class="whats-next-card__when">{when(item)}</span><span class="whats-next-card__about">{item["about"]}</span></span></div>')


def page_phone():
    return f'''<figure class="sk-device" data-palette="brand" data-preview-scheme="light">
<figcaption><b>What's next, scrolled to its foot</b><span>Same in every option: the last three digests sent, each kept on the phone.</span></figcaption>
<div class="sk-device__screen"><div class="sk-device__scroll" style="margin-top:-520px">
<div class="page"><header class="masthead"><span class="brand-mark masthead__mark" aria-hidden="true"></span><h1>What's next</h1><p class="masthead__note">{RANGE}</p>
<div class="masthead__extra"><p class="whats-next__lead">Four things coming up at Good Shepherd in the next two weeks.</p></div></header>
<section class="section section--first"><div class="section__head"><h2 class="section__title">Next week</h2><span class="whats-next__range">21 – 27 December</span></div>
<div class="whats-next__items">{card(ITEMS[2])}</div></section>
<section class="section"><div class="section__head"><h2 class="section__title">Week of 4 January</h2><span class="whats-next__range">4 – 10 January</span></div>
<div class="whats-next__items">{card(ITEMS[3])}</div></section>
<section class="section"><div class="section__head"><h2 class="section__title">Earlier</h2></div><div class="list">{earlier_rows()}</div></section>
</div></div>
<div class="sk-topbar"><span>{icon("chevron-left")}This week</span><b>What's next</b></div>
<div class="sk-status sk-status--dark"><span>9:41</span><span class="sk-status__right"><i></i><i></i><i></i></span></div></div></figure>'''


NAVY, BEIGE, AQUA, RUST, SAND, INK, INK2, LINE = "#2A3D5E", "#D5C9B7", "#007a70", "#9a5a1c", "#F3EFE8", "#1F2A3C", "#4a5568", "#E7E2DA"
FONT = "Montserrat,Arial,Helvetica,sans-serif"
PIC = {"baptism": "linear-gradient(180deg,#8fc6c1,#1f5d63)", "christmas": "radial-gradient(ellipse at 50% 120%,#c9773a,#7a3b25 40%,#24314f 85%)",
       "camp": "linear-gradient(180deg,#f2b47a,#d0855a 38%,#4f6a4a 39%,#2f4a35)"}


def button(words):
    return (f'<table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr><td bgcolor="{NAVY}" style="background:{NAVY};border-radius:20px">'
            f'<a href="#" style="display:inline-block;padding:11px 20px;font-family:{FONT};font-size:14px;font-weight:600;color:#ffffff;text-decoration:none">{words}</a></td></tr></table>')


def email_block():
    rows = ""
    for index, item in enumerate(ITEMS):
        rule = "" if index == 0 else f"border-top:1px solid {LINE};padding-top:22px;"
        pic = f'<div style="height:200px;border-radius:10px;margin-bottom:14px;background:{PIC[item["pic"]]}"></div>' if item["pic"] else ""
        colour = RUST if item.get("off") else AQUA
        rows += (f'<tr><td style="padding:22px 32px 4px;font-family:{FONT};color:{INK}"><div style="{rule}">{pic}'
                 f'<div style="font-size:13px;font-weight:700;color:{colour}">{item["label"]}</div>'
                 f'<div style="font-size:21px;font-weight:700;margin-top:2px">{item["title"]}</div>'
                 f'<div style="font-size:15px;font-weight:700;color:{NAVY};margin-top:4px">{when(item, " @ ")}</div>'
                 f'<p style="font-size:15px;line-height:1.5;margin:10px 0 0">{item["about"]}</p></div></td></tr>')
    return (f'<table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" bgcolor="#ffffff" style="width:600px;background:#ffffff">'
            f'<tr><td bgcolor="{NAVY}" style="background:{NAVY};padding:30px 32px 26px;font-family:{FONT};color:#ffffff">'
            f'<div style="font-size:32px;font-weight:700;line-height:1.1">What\'s Next at Good Shepherd</div>'
            f'<div style="font-size:15px;font-weight:600;color:{BEIGE};margin-top:8px">Monday 14 – Sunday 27 December</div></td></tr>{rows}'
            f'<tr><td style="padding:30px 32px 32px;font-family:{FONT};color:{INK}"><table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">'
            f'<tr><td bgcolor="{SAND}" style="background:{SAND};border-radius:14px;padding:18px 20px">'
            f'<div style="font-size:15px;font-weight:700">Everything else is in the app</div>'
            f'<p style="font-size:14px;line-height:1.5;color:{INK2};margin:4px 0 14px">The calendar in the Good Shepherd app has every service, group and event, and updates when plans change.</p>'
            f'{button("See all events")}</td></tr></table></td></tr></table>')


def email_panel():
    return f'''<section class="sk-email"><h2>Copy for email, for Elvanto<span>Same in every option. Just the content block: Elvanto's own template adds the logo, footer and unsubscribe. Buttons are tables, so Outlook draws them.</span></h2>
<div class="sk-email__elvanto"><div class="sk-email__chrome">Elvanto · Source</div><div class="sk-email__frame"><div class="sk-email__scale">{email_block()}</div></div></div></section>'''


def board(title, home, draws):
    return f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>{STYLE}</head>
<body class="sk-board"><div class="sk-row">{home}{sizes_panel(draws)}{page_phone()}{email_panel()}</div></body></html>'''


def write(name, html):
    (HERE / name).write_text(html)


def dots(active, count=4):
    return '<span class="sk-dots">' + "".join(f'<i class="{"on" if i == active else ""}"></i>' for i in range(count)) + "</span>"


def slide(item, index, shape, words=True):
    pic = photo(item["pic"]) if item["pic"] else ""
    tone = "sk-slide--off" if not item["pic"] else ""
    text = (f'<span class="sk-slide__words"><span class="sk-slide__kicker">What\'s next · {index + 1} of 4</span>'
            f'<strong class="sk-slide__title">{item["title"]}</strong>'
            + (f'<span class="sk-slide__when">{when(item)}</span>' if words else "") + "</span>")
    return f'<span class="sk-slide sk-slide--{shape} {tone}" style="--i:{index}">{pic}<span class="sk-slide__shade"></span>{text}</span>'


def carousel(shape, words=True):
    slides = "".join(slide(item, i, shape, words) for i, item in enumerate(ITEMS))
    return f'<div class="hub-tile hub-tone--masthead hub-tile--dark sk-carousel sk-carousel--{shape}">{slides}{dots(0)}</div>'


def headline(w, h):
    lead, rest = ITEMS[0], ITEMS[1:]
    if (w, h) == (1, 1):
        return tile("square", "masthead", "What's next", ["4 things coming up"], ic="megaphone")
    if h == 1:
        return tile("row", "masthead", "What's next", [f"{lead['title']} {lead['when'].replace('This', 'this')} and 3 more"], ic="megaphone")
    more = "".join(f'<span class="sk-more__row"><span>{item["title"]}</span>{"" if w == 1 else "<small>" + item["short"] + "</small>"}</span>' for item in rest[:2])
    extra = f'<span class="sk-head__lead"><span class="sk-head__kicker">What\'s next{"" if w == 1 else " · " + RANGE}</span><strong>{lead["title"]}</strong><span class="sk-head__when">{when(lead)}</span></span><span class="sk-more">{more}</span>'
    cls = "sk-head sk-head--wide" if w > 1 else "sk-head"
    pic = f'<span class="sk-head__pic">{photo(lead["pic"])}</span>' if w > 1 else ""
    return f'<div class="hub-tile hub-tone--masthead hub-tile--dark {cls}">{pic}<span class="hub-tile__body">{extra}<span class="hub-tile__go">Read What\'s next{icon("chevron-right")}</span></span></div>'


def agenda(w, h):
    if (w, h) == (1, 1):
        return tile("square", "raised", ITEMS[0]["title"], [when(ITEMS[0], " ")])
    fits = {(1, 2): 3, (2, 1): 2, (2, 2): 3}[(w, h)]
    entries = "".join(
        f'<span class="hub-list__entry {"hub-list__entry--off" if item.get("off") else ""}"><span class="hub-list__when">{"Not on" if item.get("off") else item["time"] or "All day"}<small>{item["short"].split(" ")[0] if not item.get("off") else "Wed"}</small></span><span class="hub-list__what">{item["title"]}</span></span>'
        for item in ITEMS[:fits])
    return tile("square", "raised", "What's next", go="All 4", go_beside=True, cls="hub-tile--list", extra=f'<span class="hub-list">{entries}</span>')


OPTIONS = {
    "a-turning.html": ("A: One thing at a time", lambda w, h: carousel({(1, 1): "square", (1, 2): "tall", (2, 1): "beside", (2, 2): "tall"}[(w, h)], words=(w, h) != (1, 1))),
    "b-headline.html": ("B: The headline and what follows", headline),
    "c-agenda.html": ("C: A short agenda", agenda),
}


def build():
    for name, (title, draw) in OPTIONS.items():
        home = home_phone("brand", "light", lambda draw=draw: draw(1, 2), title, "Dev's Home: the bulletin's 1 × 2 tile, now What's next. A sample digest.")
        draws = [("1 × 1", 1, 1, lambda draw=draw: draw(1, 1), "light"), ("2 × 1", 2, 1, lambda draw=draw: draw(2, 1), "light"),
                 ("2 × 2", 2, 2, lambda draw=draw: draw(2, 2), "light"), ("1 × 2 in dark", 1, 2, lambda draw=draw: draw(1, 2), "dark")]
        write(name, board(title, home, draws))


if __name__ == "__main__":
    build()
