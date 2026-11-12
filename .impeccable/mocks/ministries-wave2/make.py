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
        HERE / "wave2.css"])
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


def phone(title, body, tab="Calendar", tabs=TABS, overlay="", note="Sundays 9am, Auditorium", back="Life Groups", scroll=230):
    return HEAD.format(title=title) + f"""<body class="sk-phone" data-tab="Home">
<div class="sk-screen"><div class="page-scroll"><header class="topbar"><div class="topbar__start"><button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>{back}</button></div><div class="topbar__end"></div></header>
<div class="page"><header class="masthead"><h1>Summer Fit Life Group</h1><p class="masthead__note">{note}</p><div class="masthead__extra"><div class="ministry-hero__action"><span class="pill pill--masthead">You lead it</span></div></div></header>
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


tools = """<section class="section section--first"><div class="ministry-tools"><button class="btn btn--primary"><i data-icon="calendar-plus"></i>Add a calendar</button><button class="btn btn--quiet"><i data-icon="link"></i>Link a calendar</button></div></section>"""

row_menu = """<div class="menu__panel sk-float" style="top:452px;right:20px"><div class="menu__item"><i data-icon="circle-off"></i><span class="menu__text"><span class="menu__label">No class this Sunday</span><span class="menu__detail">The group sees it struck through here and on Home</span></span></div><div class="menu__item"><i data-icon="pencil"></i><span class="menu__text"><span class="menu__label">Add a note for the group</span><span class="menu__detail">"Bring a plate", "We're in the hall"</span></span></div><div class="menu__item"><i data-icon="calendar"></i><span class="menu__text"><span class="menu__label">Open the event</span></span></div></div>"""

coming_a = (
    '<section class="section"><div class="section__head"><div><h2 class="section__title">Coming up</h2><p class="section__lead">Its Sundays, 15 Nov to 6 Dec. Use ⋯ on a Sunday to change it for the group.</p></div></div></section>'
    + day("Sunday", "15 November", agenda("9am", "Life Groups", "Auditorium", trail=more()))
    + day("Sunday", "22 November", agenda("9am", "Life Groups", "We're joining the 10:30 service instead", off="No class this Sunday", trail=more()))
    + day("Sunday", "29 November", agenda("9am", "Life Groups", "Note: bring a plate to share", trail=more()))
)
write("a-each-sunday.html", phone("Each Sunday carries its own change", tools + coming_a + linked_section("Only its events from 15 Nov to 6 Dec show from these, while it runs."), overlay=row_menu))

chips = """<section class="section"><div class="section__head"><div><h2 class="section__title">Its Sundays</h2><p class="section__lead">15 Nov to 6 Dec. Tap a Sunday to tell the group about it.</p></div></div>
<div class="sk-chips"><button class="sk-date"><b>15</b><span>Nov</span></button><button class="sk-date sk-date--off"><b>22</b><span>No class</span></button><button class="sk-date sk-date--note"><b>29</b><span>Note</span></button><button class="sk-date"><b>6</b><span>Dec</span></button></div></section>"""
coming_b = (
    '<section class="section"><div class="section__head"><div><h2 class="section__title">Coming up</h2></div></div></section>'
    + day("Sunday", "15 November", agenda("9am", "Life Groups", "Auditorium"))
    + day("Sunday", "22 November", agenda("9am", "Life Groups", "We're joining the 10:30 service instead", off="No class this Sunday"))
)
sheet_b = """<div class="sk-scrim"></div><div class="sheet sk-sheet"><div class="sheet__grabber"></div><h2 class="sheet__title">Sunday 22 November</h2><p class="sheet__lead">Tell the group about this Sunday. They see it in Coming up and on Home.</p><div class="sheet__body"><div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">No class this Sunday</span></span><label class="switch"><input type="checkbox" checked><span></span></label></div><label class="field"><span class="field__label">What to say</span><input class="field__input" value="We're joining the 10:30 service instead"></label></div><div class="sk-savebar sk-sheet-foot"><button class="btn btn--quiet">Cancel</button><button class="btn btn--primary">Save</button></div></div>"""
write("b-sunday-strip.html", phone("A strip of its Sundays", tools + chips + coming_b, overlay=sheet_b))

coming_c = (
    '<section class="section"><div class="section__head"><div><h2 class="section__title">Coming up</h2></div></div></section>'
    + day("Sunday", "15 November", agenda("9am", "Life Groups", "Auditorium"))
    + day("Sunday", "22 November", agenda("9am", "Life Groups", '<span class="sk-link">News: No class on Sunday 22 Nov</span>', off="No class this Sunday"))
    + day("Sunday", "29 November", agenda("9am", "Life Groups", "Auditorium"))
)
news_c = """<section class="section"><div class="list"><div class="row"><span class="row__icon row__icon--accent"><i data-icon="megaphone"></i></span><span class="row__body"><span class="row__title">Tell the group about a Sunday</span><span class="row__subtitle">A post in News, for one of its Sundays. It shows here under that Sunday too.</span></span><span class="row__trail"><i data-icon="chevron-right"></i></span></div></div></section>"""
write("c-news-post.html", phone("Tell the group in News", tools + news_c + coming_c))

order_a = """<p class="grp-head">Its tabs</p><section class="grp"><p class="grp-lead">On and in the order they show. About is always first.</p>
<div class="list">
<div class="sk-order"><span class="sk-order__name"><b>News</b><small>Posts from its leaders</small></span><span class="sk-order__moves"><button class="icon-btn sk-dim"><i data-icon="chevron-up"></i></button><button class="icon-btn"><i data-icon="chevron-down"></i></button></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="sk-order"><span class="sk-order__name"><b>Calendar</b><small>Its calendars and linked ones</small></span><span class="sk-order__moves"><button class="icon-btn"><i data-icon="chevron-up"></i></button><button class="icon-btn"><i data-icon="chevron-down"></i></button></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="sk-order"><span class="sk-order__name"><b>Chats</b><small>Group chats and channels</small></span><span class="sk-order__moves"><button class="icon-btn"><i data-icon="chevron-up"></i></button><button class="icon-btn"><i data-icon="chevron-down"></i></button></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="sk-order"><span class="sk-order__name"><b>Files</b><small>Documents and links</small></span><span class="sk-order__moves"><button class="icon-btn"><i data-icon="chevron-up"></i></button><button class="icon-btn"><i data-icon="chevron-down"></i></button></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="sk-order sk-order--off"><span class="sk-order__name"><b>Roster</b><small>Off: hidden from everyone</small></span><span class="sk-order__moves"><button class="icon-btn"><i data-icon="chevron-up"></i></button><button class="icon-btn sk-dim"><i data-icon="chevron-down"></i></button></span><label class="switch"><input type="checkbox"><span></span></label></div>
</div></section><p class="grp-foot">These change straight away. People and ⚙ stay at the end for leaders.</p>"""
chat_grp = """<p class="grp-head">Chats</p><section class="grp"><div class="field"><span class="field__label">Who can start a chat</span><div class="segmented" role="radiogroup"><button class="segmented__option" role="radio" aria-checked="true">Leaders</button><button class="segmented__option" role="radio" aria-checked="false">The team too</button></div></div></section>"""
order_tabs = ["About", "News", "Calendar", "Chats", "Files", "People"]
write("t-a-arrows.html", phone("Tabs: switch and arrows in one list", '<div class="sk-grouped">' + order_a + chat_grp + "</div>", tab="Settings", tabs=order_tabs))

order_b = """<p class="grp-head">Its tabs</p><section class="grp"><p class="grp-lead">Hold ≡ and drag to change the order people see. About is always first.</p>
<div class="sk-preview">""" + "".join(f'<span class="{ "on" if t == "About" else ""}">{t}</span>' for t in order_tabs[:5]) + """</div>
<div class="list">
<div class="sk-order"><span class="sk-grip"><i data-icon="grip"></i></span><span class="sk-order__name"><b>News</b></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="sk-order sk-order--lifted"><span class="sk-grip"><i data-icon="grip"></i></span><span class="sk-order__name"><b>Calendar</b></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="sk-order"><span class="sk-grip"><i data-icon="grip"></i></span><span class="sk-order__name"><b>Chats</b></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="sk-order"><span class="sk-grip"><i data-icon="grip"></i></span><span class="sk-order__name"><b>Files</b></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="sk-order sk-order--off"><span class="sk-grip"><i data-icon="grip"></i></span><span class="sk-order__name"><b>Roster</b><small>Off</small></span><label class="switch"><input type="checkbox"><span></span></label></div>
</div></section><p class="grp-foot">These change straight away. A keyboard or screen reader moves a row with its ⋯ menu.</p>"""
write("t-b-drag.html", phone("Tabs: drag with a live preview", '<div class="sk-grouped">' + order_b + chat_grp + "</div>", tab="Settings", tabs=order_tabs))

order_c = """<p class="grp-head">Its tabs</p><section class="grp"><div class="list">
<div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">News</span><span class="toggle-row__description">Posts from its leaders</span></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">Calendar</span><span class="toggle-row__description">Its calendars and linked ones</span></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">Chats</span><span class="toggle-row__description">Group chats and channels</span></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">Files</span><span class="toggle-row__description">Documents and links</span></span><label class="switch"><input type="checkbox" checked><span></span></label></div>
<div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">Roster</span><span class="toggle-row__description">Off: hidden from everyone</span></span><label class="switch"><input type="checkbox"><span></span></label></div>
<div class="row"><span class="row__icon row__icon--accent"><i data-icon="list"></i></span><span class="row__body"><span class="row__title">Change the order of the tabs</span><span class="row__subtitle">About, News, Calendar, Chats, Files</span></span><span class="row__trail"><i data-icon="chevron-right"></i></span></div>
</div></section>"""
sheet_c = """<div class="sk-scrim"></div><div class="sheet sk-sheet"><div class="sheet__grabber"></div><h2 class="sheet__title">The order of its tabs</h2><p class="sheet__lead">Move a tab up or down. About is always first.</p><div class="sheet__body"><div class="list">
<div class="sk-order"><span class="sk-order__name"><b>News</b></span><span class="sk-order__moves"><button class="icon-btn sk-dim"><i data-icon="chevron-up"></i></button><button class="icon-btn"><i data-icon="chevron-down"></i></button></span></div>
<div class="sk-order"><span class="sk-order__name"><b>Calendar</b></span><span class="sk-order__moves"><button class="icon-btn"><i data-icon="chevron-up"></i></button><button class="icon-btn"><i data-icon="chevron-down"></i></button></span></div>
<div class="sk-order"><span class="sk-order__name"><b>Chats</b></span><span class="sk-order__moves"><button class="icon-btn"><i data-icon="chevron-up"></i></button><button class="icon-btn"><i data-icon="chevron-down"></i></button></span></div>
<div class="sk-order"><span class="sk-order__name"><b>Files</b></span><span class="sk-order__moves"><button class="icon-btn"><i data-icon="chevron-up"></i></button><button class="icon-btn sk-dim"><i data-icon="chevron-down"></i></button></span></div>
</div></div><div class="sk-savebar sk-sheet-foot"><button class="btn btn--quiet">Cancel</button><button class="btn btn--primary">Save</button></div></div>"""
write("t-c-sheet.html", phone("Tabs: order in its own sheet", '<div class="sk-grouped">' + order_c + chat_grp + "</div>", tab="Settings", tabs=order_tabs, overlay=sheet_c))
