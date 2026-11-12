import calendar
import importlib.util
from datetime import date
from pathlib import Path

HERE = Path(__file__).parent
spec = importlib.util.spec_from_file_location("wave2", HERE.parent / "ministries-wave2" / "make.py")
EXTRA = (HERE / "away.css").read_text()


def _head(title):
    css_and_js = _wave2_head()
    return css_and_js.replace("{title}", title, 1).replace("{{", "{").replace("}}", "}").replace("</head>", f"<style>{EXTRA}</style></head>")


def _wave2_head():
    source = (HERE.parent / "ministries-wave2" / "make.py").read_text().split("HEAD = ")[0]
    scope = {"__file__": str(HERE.parent / "ministries-wave2" / "make.py")}
    exec(source, scope)
    return '<!doctype html>\n<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>\n' + scope["_inline"]() + "\n</head>\n"


TODAY = date(2026, 11, 12)


def mark(d):
    if date(2026, 11, 21) <= d <= date(2026, 11, 23):
        return "allday"
    if d.weekday() == 6 and d >= date(2026, 11, 15):
        return "skipped" if d.day == 29 else "evening"
    return "none"


def month():
    weeks = calendar.Calendar(firstweekday=0).monthdatescalendar(2026, 11)
    head = "".join(f'<span class="month-days__weekday">{w}</span>' for w in "MTWTFSS")
    rows = f'<div class="month-days__week">{head}</div>'
    for week in weeks:
        cells = ""
        for d in week:
            cls = f"month-days__day month-days__day--{mark(d)}"
            if d.month != 11:
                cls += " month-days__day--outside"
            if d == TODAY:
                cls += " month-days__day--today"
            dis = " disabled" if d < TODAY or d.month != 11 else ""
            cells += f'<button class="{cls}"{dis}><span>{d.day}</span></button>'
        rows += f'<div class="month-days__week">{cells}</div>'
    return f"""<div class="month-days away-month"><div class="month-days__bar"><button class="btn btn--quiet btn--small btn--icon" disabled><i data-icon="chevron-left"></i></button><h3 class="month-days__title">November 2026</h3><button class="btn btn--quiet btn--small btn--icon"><i data-icon="chevron-right"></i></button></div>
<div class="month-days__grid">{rows}</div><p class="month-days__key">Shaded: away. Bottom half: evenings. Struck through: not away that time. Choose a day to change it.</p></div>"""


def row(icon, tone, title, sub=None, chevron=True):
    sub_html = f'<span class="row__subtitle">{sub}</span>' if sub else ""
    trail = '<span class="row__trail"><i data-icon="chevron-right"></i></span>' if chevron else ""
    return f'<div class="row"><span class="row__icon row__icon--{tone}"><i data-icon="{icon}"></i></span><span class="row__body"><span class="row__title">{title}</span>{sub_html}</span>{trail}</div>'


RULE = row("repeat", "warm", "Sunday evenings", "From 15 Nov, until further notice · not 29 Nov")
TRIP = row("circle-off", "quiet", "Sat 21 to Mon 23 Nov", "Away in Sydney for a wedding")
ADD = row("plus", "accent", "Add days away", chevron=False)


def section(title, body, lead=None, first=False):
    lead_html = f'<p class="section__lead">{lead}</p>' if lead else ""
    head = f'<div class="section__head"><div><h2 class="section__title">{title}</h2>{lead_html}</div></div>' if title else ""
    return f'<section class="section{" section--first" if first else ""}">{head}{body}</section>'


def page(title, body, foot=""):
    return _head(title) + f"""<body class="sk-phone" data-tab="Ministries" data-tabs="Home:home,Sermons:play,Calendar:calendar,Ministries:users,More:more">
<div class="sk-screen"><div class="page-scroll"><header class="topbar"><div class="topbar__start"><button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>Roster</button></div><div class="topbar__end"></div></header>
<div class="page"><header class="masthead"><h1>Days away</h1><p class="masthead__note">Leaders of Production and Welcome see these</p></header>
{body}
</div></div>{foot}</div>
</body></html>"""


lead = "Leaders can still put you on a day you're away; it's flagged for them."

a = page("Next away first, Add docked at the foot",
    section("Next away", f'<div class="list">{TRIP}{RULE}</div>', first=True)
    + section(None, month())
    + '<div class="away-dock-space"></div>',
    foot='<div class="away-dock"><button class="btn btn--primary btn--block"><i data-icon="plus"></i>Add days away</button></div>')
(HERE / "a-next-docked.html").write_text(a)

b = page("Month or list",
    section(None, '<div class="segmented away-view" role="radiogroup"><button class="segmented__option" role="radio" aria-checked="true">Month</button><button class="segmented__option" role="radio" aria-checked="false">List</button></div>', first=True)
    + section(None, month())
    + section(None, f'<div class="list">{ADD}</div>'))
(HERE / "b-month-or-list.html").write_text(b)

sundays = "".join(
    f'<button class="sk-date{" sk-date--off" if off else ""}"><b>{d}</b><span>{label}</span></button>'
    for d, label, off in [(15, "Evening", True), (22, "Away", True), (29, "Free", False), (6, "Free", False), (13, "Free", False)])
c = page("Your Sundays, then the month",
    section("Your Sundays", f'<div class="sk-chips away-sundays">{sundays}</div>', lead="Tap a Sunday to say you're away.", first=True)
    + section("Other days", f'<div class="list">{RULE}{TRIP}{ADD}</div>')
    + section(None, month()))
(HERE / "c-sundays-strip.html").write_text(c)
