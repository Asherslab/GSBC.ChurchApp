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
        HERE / "calendars.css"])
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
<div class="sk-screen"><div class="page-scroll"><header class="topbar{' topbar--solid' if scroll else ''}"><div class="topbar__start"><button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>{back}</button></div><div class="topbar__end"></div></header>
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




PILLS = '<span class="pill pill--masthead">You lead it</span>'
NOTE = "Growing in faith together, one conversation at a time"
TABS3 = ["About", "News", "Calendar", "Files", "Chats", "People"]
TEAM = "The Discipleship Team"


def toggle(title, desc, on):
    checked = " checked" if on else ""
    return f'<div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">{title}</span><span class="toggle-row__description">{desc}</span></span><label class="switch"><input type="checkbox"{checked}><span></span></label></div>'


def tabs_group():
    return '<p class="grp-head">Its tabs</p><section class="grp"><p class="grp-lead">On and in the order they show. About is always first.</p></section>'


def seg(title, detail, on):
    return f'<div class="sk-seg-row"><span class="sk-radio{" sk-radio--on" if on else ""}"></span><span><b>{title}</b><small>{detail}</small></span></div>'


a_body = ('<div class="sk-grouped">' + tabs_group()
          + '<p class="grp-head">Calendars</p><section class="grp"><p class="grp-lead">Leaders add its events in Calendar. A ministry has a Public calendar, a Team calendar, both or neither.</p><div class="sk-seg-rows">'
          + seg("No calendar of its own", "Only calendars you link show in its Calendar tab.", False)
          + seg("Public calendar", "Everyone with the app sees its events.", False)
          + seg("Team calendar", f"Only {TEAM} sees its events.", False)
          + seg("Both", f"A Public calendar for everyone, and a Team calendar for {TEAM}.", True)
          + '</div><p class="sk-who">So its events show in people\'s Calendar and calendar links as “Discipleship”, and the team\'s as “The Discipleship Team”.</p></section>'
          + '<p class="grp-foot">These change straight away. Turning a calendar off asks first and says which events go with it.</p></div>')
write("a-one-choice.html", phone("One choice of four", a_body, tab="Settings", tabs=TABS3, note=NOTE, back="Ministries", name="Discipleship", pills=PILLS, scroll=330))


def cal_row(title, sub, on):
    tone = "" if on else " row__icon--quiet"
    return f'<div class="row"><span class="row__icon{tone}"><i data-icon="calendar"></i></span><span class="row__body"><span class="row__title">{title}</span><span class="row__subtitle">{sub}</span></span><span class="row__trail"><i data-icon="chevron-right"></i></span></div>'


b_list = ('<div class="sk-grouped">' + tabs_group()
          + '<p class="grp-head">Calendars</p><section class="grp"><div class="list">'
          + cal_row("Public calendar", "On · Everyone sees it · 12 events coming up", True)
          + cal_row("Team calendar", f"Off · For events only {TEAM} sees", False)
          + '</div></section></div>')
b_sheet = ('<div class="sk-scrim"></div><div class="sheet sk-sheet"><div class="sheet__grabber"></div><h2 class="sheet__title">Team calendar</h2>'
           + f'<p class="sheet__lead">For events only {TEAM} sees: rehearsals, planning nights, set-up.</p><div class="sheet__body">'
           + toggle("Use a Team calendar", "Off", False)
           + '<div class="list">'
           + '<div class="row"><span class="row__icon row__icon--quiet"><i data-icon="users"></i></span><span class="row__body"><span class="row__title">Who sees it</span><span class="row__subtitle">The Discipleship Team: its leaders and its team</span></span></div>'
           + '<div class="row"><span class="row__icon row__icon--quiet"><i data-icon="calendar"></i></span><span class="row__body"><span class="row__title">Called “The Discipleship Team”</span><span class="row__subtitle">In people\'s Calendar and calendar links. It follows the ministry\'s name.</span></span></div>'
           + '</div></div><div class="sk-sheet-foot" style="display:grid;gap:8px"><button class="btn btn--quiet">Done</button></div></div>')
write("b-row-and-sheet.html", phone("Each calendar a row with its own sheet", b_list, tab="Settings", tabs=TABS3, note=NOTE, back="Ministries", name="Discipleship", pills=PILLS, overlay=b_sheet, scroll=0))

savebar = '<p class="sk-warn" style="display:block;text-align:right;color:var(--ink-2);font-size:var(--text-caption);margin:0 0 8px">Saving deletes the Team calendar and its 4 events.</p><div class="sk-savebar"><button class="btn btn--quiet">Cancel</button><button class="btn btn--primary">Save the calendars</button></div>'
c_body = ('<div class="sk-grouped">' + tabs_group()
          + '<p class="grp-head">Calendars</p><section class="grp"><p class="grp-lead">Leaders add its events in Calendar.</p>'
          + toggle("Public calendar", "Everyone with the app sees its events. Called “Discipleship”.", True)
          + toggle("Team calendar", f"Only {TEAM} sees its events. Called “The Discipleship Team”.", False)
          + '</section><div class="sk-foot">' + savebar + '</div></div>')
write("c-switches-and-save.html", phone("Two switches, saved with Save", c_body, tab="Settings", tabs=TABS3, note=NOTE, back="Ministries", name="Discipleship", pills=PILLS, scroll=260))
