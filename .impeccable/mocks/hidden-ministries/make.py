from pathlib import Path

HERE = Path(__file__).parent

ROOT = HERE.parent.parent.parent


def _css(path):
    import re
    text = path.read_text()
    def pull(m):
        return _css((path.parent / m.group(1)).resolve())
    import base64
    def embed(m):
        target = (path.parent / m.group(1)).resolve()
        if not target.exists():
            return m.group(0)
        kind = {"woff2": "font/woff2", "svg": "image/svg+xml"}.get(target.suffix[1:], "application/octet-stream")
        return 'url("data:' + kind + ';base64,' + base64.b64encode(target.read_bytes()).decode() + '")'
    text = re.sub(r'url\("(\.\./(?:fonts|img)/[^"]+)"\)', embed, text)
    return re.sub(r'@import url\("([^"]+)"\);', pull, text)


def _inline():
    css = "".join(_css(f) for f in [HERE.parent / "design-system" / "kit.css",
        ROOT / "src/Features/Events/GSBC.ChurchApp.Features.Events.Client/wwwroot/css/events.css",
        ROOT / "src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Client/wwwroot/css/ministries.css",
        HERE / "hidden.css"])
    js = (HERE.parent / "design-system" / "icons.js").read_text() + "\n" + (HERE.parent / "design-system" / "kit.js").read_text()
    return "<style>" + css.replace("{", "{{").replace("}", "}}") + "</style><script>" + js.replace("{", "{{").replace("}", "}}") + "</script>"


HEAD = """<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>
""" + _inline() + """
</head>
"""

TABS = ["About", "News", "Calendar", "Files", "Chats", "People"]


def strip(on, tabs=TABS, settings=True):
    items = "".join(f'<button class="tab-strip__tab" role="tab" aria-selected="{str(t == on).lower()}">{t}</button>' for t in tabs)
    if settings:
        items += f'<button class="tab-strip__tab" role="tab" aria-selected="{str(on == "Settings").lower()}" aria-label="Settings"><i data-icon="settings"></i></button>'
    return f'<div class="tab-strip tab-strip--sticky" role="tablist">{items}</div>'


def phone(title, body, tab="Calendar", tabs=TABS, overlay="", note="Sundays 9am, Auditorium", back="Life Groups", scroll=230, name="Accounts", pills=""):
    return HEAD.format(title=title) + f"""<body class="sk-phone" data-tab="Home">
<div class="sk-screen"><div class="page-scroll"><header class="topbar"><div class="topbar__start"><button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>{back}</button></div><div class="topbar__end"></div></header>
<div class="page"><header class="masthead"><h1>{name}</h1><p class="masthead__note">{note}</p><div class="masthead__extra"><div class="ministry-hero__action">{pills}</div></div></header>
{strip(tab, tabs)}
{body}
</div></div>{overlay}</div>
<script>addEventListener("load",()=>{{const s=document.querySelector(".page-scroll");s.scrollTop={scroll};}})</script>
</body></html>"""


def day(name, date, rows):
    return f'<section class="week-day"><h2 class="week-day__head">{name} <span>{date}</span></h2><div class="list">{rows}</div></section>'


def agenda(time, title, sub=None, off=None, trail=""):
    cls = "row agenda-row" + (" agenda-row--off" if off else "")
    lines = f'<span class="row__title">{title} <i data-icon="repeat"></i></span>'
    if off:
        lines += f'<span class="agenda-row__off">{off}</span>'
    if sub:
        lines += f'<span class="row__subtitle">{sub}</span>'
    return f'<div class="{cls}"><span class="agenda-row__time">{time}</span><span class="row__body">{lines}</span>{trail}</div>'


def more():
    return '<span class="row__trail"><button class="icon-btn" aria-label="More"><i data-icon="more"></i></button></span>'


def linked_section(lead):
    return f"""<section class="section"><div class="section__head"><div><h2 class="section__title">Linked calendars</h2><p class="section__lead">{lead}</p></div></div>
<div class="list"><div class="row"><span class="row__icon row__icon--quiet"><i data-icon="link"></i></span><span class="row__body"><span class="row__title">Life Groups</span><span class="row__subtitle">Its events from 15 Nov to 6 Dec</span></span>{more()}</div></div></section>"""


def write(name, html):
    (HERE / name).write_text(html)



PILLS = '<span class="pill pill--masthead">You lead it</span><span class="pill pill--masthead"><i data-icon="eye-off"></i>Hidden</span>'
NOTE = "Counting the offering and paying the bills"
TABS2 = ["About", "News", "Files", "Chats", "People"]


def field(label, value):
    return f'<div class="sk-field"><b>{label}</b><span>{value}</span></div>'


details_top = field("Name", "Accounts") + field("When and where it meets", "Tuesdays, the church office")


def toggle(title, desc, on):
    checked = " checked" if on else ""
    return f'<div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">{title}</span><span class="toggle-row__description">{desc}</span></span><label class="switch"><input type="checkbox"{checked}><span></span></label></div>'


savebar = '<div class="sk-savebar" style="margin-top:12px"><button class="btn btn--quiet">Cancel</button><button class="btn btn--primary">Save the details</button></div>'
who = '<p class="sk-who">Only its team, its attendees and their parents, the leaders above it and staff who look after ministries see Accounts. It isn\'t in the list of ministries, search or Home for anyone else, and nobody can ask to join.</p>'

a_body = ('<div class="sk-grouped"><p class="grp-head">The details</p><section class="grp"><p class="grp-lead">What people see about it, how they join, and where it shows.</p>'
          + details_top
          + toggle("Show in Life Groups", "Off", False)
          + toggle("Hidden from everyone else", "Only its own people and staff see it", True)
          + who + "</section>" + '<div style="margin:0 var(--gutter)">' + savebar + "</div>"
          + '<p class="grp-head">Its tabs</p><section class="grp"><p class="grp-lead">On and in the order they show. About is always first.</p></section></div>')
write("a-in-the-details.html", phone("In The details, saved with Save", a_body, tab="Settings", tabs=TABS2, note=NOTE, back="Church office", pills=PILLS, scroll=300))

b_body = ('<div class="sk-grouped"><p class="grp-head">The details</p><section class="grp"><p class="grp-lead">What people see about it, how they join, and where it shows.</p>'
          + details_top + "</section>"
          + '<p class="grp-head">Who sees it</p><section class="grp"><div class="sk-seg-rows">'
          + '<div class="sk-seg-row"><span class="sk-radio"></span><span><b>Everyone</b><small>In the list of ministries, search and Home. Anyone can ask to join.</small></span></div>'
          + '<div class="sk-seg-row"><span class="sk-radio sk-radio--on"></span><span><b>Only its people</b><small>Its team, attendees and their parents, the leaders above it and staff. Hidden from everyone else.</small></span></div>'
          + "</div></section><p class=\"grp-foot\">This changes straight away. The ministries under it follow.</p>"
          + '<p class="grp-head">Its tabs</p><section class="grp"><p class="grp-lead">On and in the order they show. About is always first.</p></section></div>')
write("b-own-group.html", phone("Its own group, changes straight away", b_body, tab="Settings", tabs=TABS2, note=NOTE, back="Church office", pills=PILLS, scroll=260))

menu = """<div class="menu__panel sk-float" style="top:52px;right:12px"><div class="menu__item"><i data-icon="eye-off"></i><span class="menu__text"><span class="menu__label">Hide it from everyone else…</span><span class="menu__detail">Only its own people and staff will see it</span></span></div><div class="menu__item"><i data-icon="archive"></i><span class="menu__text"><span class="menu__label">Archive Accounts…</span></span></div><div class="menu__item" style="border-top:1px solid var(--line);color:var(--danger)"><i data-icon="trash"></i><span class="menu__text"><span class="menu__label">Delete Accounts…</span><span class="menu__detail">Shows what goes with it first</span></span></div></div>"""
sheet = """<div class="sk-scrim"></div><div class="sheet sk-sheet"><div class="sheet__grabber"></div><h2 class="sheet__title">Hide Accounts?</h2><p class="sheet__lead">It leaves the list of ministries, search and Home for everyone but its own people. Nobody else can ask to join. Its posts, files and calendars for everyone become for its people only.</p><div class="sheet__body"><p class="sk-who">Still see it: its team, attendees and their parents, the leaders of Church office and staff who look after ministries.</p></div><div class="sk-sheet-foot" style="display:grid;gap:8px"><button class="btn btn--primary">Hide it</button><button class="btn btn--quiet">Keep it shown</button></div></div>"""
c_body = ('<div class="sk-grouped"><p class="grp-head">The details</p><section class="grp"><p class="grp-lead">What people see about it, how they join, and where it shows.</p>'
          + details_top + "</section></div>")
write("c-item-menu.html", phone("In the ministry's ⋯ menu, asking once", c_body, tab="Settings", tabs=TABS2, note=NOTE, back="Church office", pills='<span class="pill pill--masthead">You lead it</span>', overlay=sheet, scroll=0))
