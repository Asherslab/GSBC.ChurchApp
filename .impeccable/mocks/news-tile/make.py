import base64
import json
import re
from pathlib import Path

HERE = Path(__file__).parent
MOCKS = HERE.parent
ROOT = MOCKS.parent.parent
UI = ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/css"


def _css(path):
    text = path.read_text()

    def embed(m):
        target = (path.parent / m.group(1)).resolve()
        if not target.exists():
            return m.group(0)
        kind = {"woff2": "font/woff2", "svg": "image/svg+xml", "png": "image/png"}.get(target.suffix[1:], "application/octet-stream")
        return 'url("data:' + kind + ';base64,' + base64.b64encode(target.read_bytes()).decode() + '")'

    return re.sub(r'url\("(\.\./(?:fonts|img|images)/[^"]+)"\)', embed, text)


ICONS = dict(re.findall(r'\["([a-z0-9-]+)"\] = """(.*?)"""', (ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/IconPaths.cs").read_text(), re.S))


def icon(name, cls="icon--sm"):
    return f'<svg class="icon {cls}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{ICONS.get(name, "")}</svg>'


SERMON = "data:image/jpeg;base64," + base64.b64encode((HERE / "sermon.jpg").read_bytes()).decode()

STYLE = "<style>" + _css(UI / "tokens.css") + _css(UI / "ui.css") + (HERE / "themes.css").read_text() + (HERE / "board.css").read_text() + "</style>"


def tile(shape, tone, title, lines=(), ic=None, picture=None, corner=False, action=None, extra="", cls="", fade="masthead", go=None, go_beside=False, dot=False):
    dark = tone in ("masthead", "primary", "live", "marker")
    classes = f"hub-tile hub-tile--{shape} hub-tone--{tone} {'hub-tile--dark' if dark and tone != 'marker' else ''} {'hub-fade--' + fade if shape == 'behind' else ''} {cls}"
    icon_cls = "hub-icon--on-dark" if dark and tone != "marker" else ("row__icon--accent" if tone == "live-soft" else "row__icon--warm" if tone == "marker-soft" else "")
    hub_icon = f'<span class="row__icon hub-icon {icon_cls}">{icon(ic)}</span>' if ic else ""
    out = f'<div class="{classes}">'
    if shape in ("picture", "behind", "beside"):
        out += f'<span class="hub-tile__picture">{picture}</span>'
        if corner:
            out += f'<span class="hub-tile__corner">{icon("chevron-right")}</span>'
    out += '<span class="hub-tile__body">'
    if shape in ("square", "poster") and ic:
        out += f'<span class="hub-tile__top">{hub_icon}{icon("chevron-right", "icon--sm hub-tile__go-icon") if shape == "square" else ""}</span>'
    title_html = f'<strong class="hub-tile__title">{"<i class=hub-tile__dot></i>" if dot else ""}{title}</strong>'
    out += '<span class="hub-tile__words">'
    if go and go_beside:
        out += f'<span class="hub-tile__head">{title_html}<span class="hub-tile__go hub-tile__go--beside">{go}{icon("chevron-right")}</span></span>'
    else:
        out += title_html
    out += "".join(f'<span class="hub-tile__line">{line}</span>' for line in lines if line)
    out += extra
    if go and not go_beside and shape != "square":
        out += f'<span class="hub-tile__go">{go}{icon("chevron-right")}</span>'
    out += "</span>"
    if shape in ("row", "picture", "beside") and ic and not action:
        out += hub_icon
    out += "</span>"
    if action:
        out += f'<span class="hub-tile__action">{action}</span>'
    return out + "</div>"


def photo(kind):
    return f'<span class="sk-photo sk-photo--{kind}" aria-hidden="true"></span>'


def sermon_cover():
    return f'<span class="hub-cover"><img src="{SERMON}" alt=""></span>'


PLAY = f'<span class="hub-play">{icon("play")}</span>'

DEV_HOME = [
    ("sermon", 2, 2, lambda: tile("picture", "raised", "Thou Hast Asked A Hard Thing", ["Sunday 8 November · 1 hr"], picture=sermon_cover(), action=PLAY)),
    ("bulletin", 1, 2, lambda: tile("poster", "masthead", "Almighty God", ["Sunday 15 November · Job 33:4"], ic="bulletin", go="Read the bulletin", extra="", cls="")),
    ("prayer", 1, 1, lambda: tile("square", "live-soft", "How can we pray for you?", ["Share a request with the pastoral team"], ic="prayer")),
    ("give", 1, 1, lambda: tile("square", "sand", "Give", ["Online at baptist.com.au"], ic="heart")),
    ("week", 2, 1, lambda: tile("square", "raised", "This week", go="Calendar", go_beside=True, cls="hub-tile--list", extra=WEEK)),
    ("outreach", 2, 1, lambda: tile("row", "marker-soft", "Got an hour?", ["Walk from your door: a loop of letterbox drops"], ic="footprints")),
    ("notifications", 1, 1, lambda: tile("square", "sand", "All caught up", ["Notifications"], ic="bell")),
    ("bible", 1, 1, lambda: tile("square", "sand", "Bible", ["King James Version"], ic="book-open")),
    ("life-groups", 1, 1, lambda: tile("square", "marker-soft", "Life Groups", ["Find a group near you"], ic="users")),
    ("ministries", 1, 1, lambda: tile("square", "live-soft", "Ministries", ["Kids, youth, music and more"])),
    ("visit", 2, 1, lambda: tile("row", "raised", "Visit us", ["Sundays 10:30am and 6pm · 185 Old Northern Road, Albany Creek"], ic="map-pin")),
]

WEEK = ('<span class="hub-list">'
        '<span class="hub-list__entry"><span class="hub-list__when">10:30am<small>Sunday</small></span><span class="hub-list__what">Morning worship</span></span>'
        '<span class="hub-list__entry"><span class="hub-list__when">7pm<small>Wednesday</small></span><span class="hub-list__what">Bible study and prayer</span></span>'
        '</span>')


def pack(columns, spans):
    taken, places = set(), []
    for w, h in spans:
        w = min(w, columns)
        row = 1
        while True:
            spot = next((c for c in range(1, columns - w + 2) if all((row + dy, c + dx) not in taken for dx in range(w) for dy in range(h))), None)
            if spot:
                break
            row += 1
        for dx in range(w):
            for dy in range(h):
                taken.add((row + dy, spot + dx))
        places.append((spot, row, w, h))
    return places


def grid(tiles, columns=2, lines=()):
    places = pack(columns, [(w, h) for _, w, h, _ in tiles])
    cells = ""
    for (key, w, h, draw), (c, r, pw, ph) in zip(tiles, places):
        line = " hub-cell--line" if key in lines else ""
        cells += f'<div class="hub-cell{line}" style="grid-column:{c} / span {pw};grid-row:{r} / span {ph};--tile-rows:{ph}">{draw()}</div>'
    return f'<div class="hub-grid" style="--hub-columns:{columns}">{cells}</div>'


TABS = [("Home", "home"), ("News", "newspaper"), ("Sermons", "sermons"), ("Calendar", "calendar"), ("More", "menu")]


def phone(palette, scheme, tiles, caption, note="", lines=(), shift=0):
    tabbar = "".join(f'<span class="{"on" if label == "Home" else ""}">{icon(name, "")}{label}</span>' for label, name in TABS)
    return f'''<figure class="sk-device" data-palette="{palette}" data-preview-scheme="{scheme}">
<figcaption><b>{caption}</b>{note}</figcaption>
<div class="sk-device__screen"><div class="sk-device__scroll" style="margin-top:-{shift}px">
<div class="page"><header class="masthead masthead--home"><span class="brand-mark masthead__mark" aria-hidden="true"></span><h1>Good Shepherd</h1><div class="home-line"><p class="masthead__note">Friday 13 November</p></div></header>
<section class="section section--first">{grid(tiles, lines=lines)}</section></div></div>
<div class="sk-status"><span>9:41</span><span class="sk-status__right"><i></i><i></i><i></i></span></div>
<nav class="sk-tabbar">{tabbar}</nav></div></figure>'''


def sizes(palette, scheme, groups, title):
    blocks = ""
    for label, tiles, lines in groups:
        blocks += f'<div class="sk-size"><p class="sk-size__label">{label}</p><div class="sk-size__grid">{grid(tiles, lines=lines)}</div></div>'
    return f'<section class="sk-sizes" data-palette="{palette}" data-preview-scheme="{scheme}"><h2>{title}</h2>{blocks}</section>'


def home_with(news, at=1):
    tiles = list(DEV_HOME)
    for item in reversed(news):
        tiles.insert(at, item)
    return tiles


def board(title, devices, panel):
    return f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>{STYLE}</head>
<body class="sk-board"><div class="sk-row">{devices}{panel}</div></body></html>'''


def write(name, html):
    (HERE / name).write_text(html)
