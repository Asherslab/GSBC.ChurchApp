import base64
import re
from pathlib import Path

HERE = Path(__file__).parent
KIT = HERE.parent / "design-system"
ROOT = HERE.parents[2]


def css_of(path):
    text = path.read_text()

    def embed(match):
        target = (path.parent / match.group(1)).resolve()
        if not target.exists():
            return match.group(0)
        kind = {"woff2": "font/woff2", "svg": "image/svg+xml"}.get(target.suffix[1:], "application/octet-stream")
        return 'url("data:' + kind + ';base64,' + base64.b64encode(target.read_bytes()).decode() + '")'

    text = re.sub(r'url\("(\.\./(?:fonts|img)/[^"]+)"\)', embed, text)
    return re.sub(r'@import url\("([^"]+)"\);', lambda match: css_of((path.parent / match.group(1)).resolve()), text)


LOCAL = """
.page-tabs { margin-bottom: 18px; }
.pill { display: inline-flex; align-items: center; height: 22px; padding: 0 9px; border-radius: 999px; background: var(--fill-on-surface); color: var(--ink-2); font-size: var(--text-pill); font-weight: 700; margin-left: 6px; }
.gen { display: flex; align-items: center; gap: 18px; padding: 12px 16px; margin-bottom: 14px; border-radius: var(--radius); background: var(--sand); font-size: var(--text-caption); color: var(--ink-2); }
.gen b { color: var(--ink); font-size: var(--text-label); }
.gen .lab { font-weight: 700; color: var(--ink); font-size: var(--text-label); }
.gen .sep { width: 1px; align-self: stretch; background: var(--line); }
.gen .end { margin-left: auto; }
.cc { display: grid; gap: 6px; padding: 12px 14px 12px; border-radius: var(--radius-tile); background: var(--surface); box-shadow: 0 1px 0 var(--line); font-size: var(--text-caption); color: var(--ink-2); }
.cc.open { box-shadow: 0 0 0 2px var(--marker); }
.cc.wrap { box-shadow: inset 3px 0 0 var(--marker), 0 1px 0 var(--line); }
.cc__head { display: flex; justify-content: space-between; align-items: start; gap: 8px; }
.cc__title { font-size: var(--text-row); font-weight: 600; color: var(--ink); line-height: 1.3; }
.cc__dots { color: var(--ink-3); font-weight: 700; letter-spacing: 1px; }
.cc__when { color: var(--ink); font-weight: 600; font-variant-numeric: tabular-nums; }
.cc__fig { display: flex; gap: 14px; font-variant-numeric: tabular-nums; }
.cc__fig b { color: var(--ink); font-size: var(--text-label); }
.cc__day { display: flex; align-items: center; gap: 6px; padding-top: 6px; border-top: 1px solid var(--line); }
.cc__day.none { color: var(--marker-ink, #9a5b12); }
.cc__told { display: inline-flex; align-items: center; gap: 5px; color: var(--ink-2); }
.tl { position: relative; height: 18px; margin: 2px 0 0; }
.tl__bar { position: absolute; left: 0; right: 0; top: 8px; height: 3px; border-radius: 2px; background: var(--fill-on-surface); }
.tl__done { position: absolute; left: 0; top: 8px; height: 3px; border-radius: 2px; background: var(--accent); }
.tl__wrap { position: absolute; top: 8px; height: 3px; right: 0; border-radius: 2px; background: var(--marker); opacity: 0.55; }
.tl__now { position: absolute; top: 4px; width: 11px; height: 11px; margin-left: -5px; border-radius: 50%; background: var(--surface); border: 2.5px solid var(--accent); }
.tl__k { display: flex; justify-content: space-between; font-size: var(--text-micro, 0.68rem); color: var(--ink-3); font-variant-numeric: tabular-nums; }
.fin { display: grid; grid-template-columns: 1fr auto; gap: 2px 8px; padding: 9px 4px; border-bottom: 1px solid var(--line); font-size: var(--text-caption); color: var(--ink-2); }
.fin b { color: var(--ink); font-weight: 600; font-size: var(--text-label); }
.fin span:nth-child(2) { text-align: right; font-variant-numeric: tabular-nums; }
.rec { display: grid; gap: 14px; }
.f { display: grid; gap: 5px; }
.f label { font-size: var(--text-caption); font-weight: 600; color: var(--ink-2); }
.f .in { display: flex; align-items: center; justify-content: space-between; height: 40px; padding: 0 12px; border-radius: var(--radius-field); background: var(--field); border: 1px solid var(--field-edge); font-size: var(--text-label); color: var(--ink); font-variant-numeric: tabular-nums; }
.f .in em { font-style: normal; color: var(--ink-3); }
.f small { color: var(--ink-3); font-size: var(--text-caption); }
.pair { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; align-items: start; }
.sw { display: flex; justify-content: space-between; align-items: center; gap: 14px; padding: 4px 0; }
.sw b { display: block; font-size: var(--text-label); font-weight: 600; }
.sw span { font-size: var(--text-caption); color: var(--ink-2); }
.sw i.k { flex: none; width: 42px; height: 24px; border-radius: 12px; background: var(--fill-on-surface); position: relative; }
.sw i.k::after { content: ""; position: absolute; left: 3px; top: 3px; width: 18px; height: 18px; border-radius: 50%; background: #fff; box-shadow: 0 1px 2px rgb(0 0 0 / .2); }
.sw i.k.on { background: var(--accent); }
.sw i.k.on::after { left: 21px; }
.foot { display: flex; justify-content: space-between; align-items: center; padding-top: 14px; border-top: 1px solid var(--line); }
.tiles3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; }
.t3 { padding: 8px 10px; border-radius: var(--radius-field); background: var(--fill-on-surface); }
.t3 b { display: block; font-size: var(--text-title); font-variant-numeric: tabular-nums; }
.t3 span { font-size: var(--text-caption); color: var(--ink-2); }
.callout { display: flex; gap: 8px; padding: 10px 12px; border-radius: var(--radius-field); background: color-mix(in srgb, var(--marker) 14%, transparent); font-size: var(--text-caption); color: var(--ink); }
.grp { font-size: var(--text-caption); font-weight: 700; color: var(--ink-3); padding: 16px 12px 6px; display: flex; justify-content: space-between; }
.lrow { display: grid; grid-template-columns: 1fr auto; gap: 2px 12px; padding: 10px 12px; border-bottom: 1px solid var(--line); font-size: var(--text-caption); color: var(--ink-2); }
.lrow b { color: var(--ink); font-size: var(--text-label); font-weight: 600; }
.lrow .n { text-align: right; font-variant-numeric: tabular-nums; color: var(--ink); }
.lrow.on { background: var(--selected-fill); }
.lrow.folded { color: var(--ink-3); }
.split { display: grid; grid-template-columns: minmax(0, 1fr) 420px; gap: 0; border-top: 1px solid var(--line); min-height: 660px; }
.split > aside { padding: 18px 0 0 24px; border-left: 1px solid var(--line); }
.split > div { padding-right: 24px; }
.board-b .board__col.wide { flex: 2.3 1 0; }
.board-b .board__col.thin { flex: 0.8 1 0; min-width: 170px; }
"""

STYLE = css_of(KIT / "kit.css") + css_of(KIT / "parts.css") + css_of(ROOT / "src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Client/wwwroot/css/ministries.css") + LOCAL
SCRIPT = (KIT / "icons.js").read_text() + "\nwindow.SK_NAV=[[\"Sundays & sermons\",[[\"Sermons\",\"sermons\"]]],[\"This week\",[[\"Calendar\",\"calendar\"]]],[\"Ministries & groups\",[[\"Life Groups\",\"book-open\"],[\"Ministries\",\"users\"],[\"Outreach\",\"footprints\"]]],[\"The app\",[[\"Media\",\"image\"]]]];\n" + (KIT / "kit.js").read_text()

TABS = ("Campaigns", "Days", "Areas", "Coverage", "Numbers", "Places", "Follow-ups", "Reports", "Settings")


def tabs():
    counts = {"Follow-ups": "1", "Reports": "1"}
    return '<div class="tab-strip page-tabs" role="tablist">' + "".join(
        f'<button class="tab-strip__tab" role="tab" aria-selected="{str(n == "Campaigns").lower()}">{n}{f"<span class=pill>{counts[n]}</span>" if n in counts else ""}</button>'
        for n in TABS) + "</div>"


def page(title, body, note):
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>
<style>{STYLE}</style><script>{SCRIPT}</script></head>
<body class="sk-admin" data-current="Outreach" style="height:1000px;overflow:hidden">
<main><header class="title-band"><div class="title-band__text"><h1 class="title-band__title">Outreach</h1><p class="title-band__lead">Campaigns, outreach days and their routes, where outreach has been, what it adds up to, and what walkers send in.</p></div>
<div class="title-band__actions"><button class="btn btn--primary"><i data-icon="plus"></i>New campaign</button></div></header>
{tabs()}{body}</main><p class="sk-note">{note}</p></body></html>"""


def general():
    return """<div class="gen"><span class="lab">Always on</span><span class="sep"></span><span><b>General outreach</b> · Tracts · bundles of 50</span><span><b>481</b> letterboxes this month</span><span>Next day <b>Sat 6 Dec</b> · 4 routes</span><span class="end"><button class="btn btn--quiet btn--small">Open</button></span></div>"""


def timeline(done, wrap, left, right):
    return f'<div class="tl"><span class="tl__bar"></span><span class="tl__wrap" style="width:{wrap}%"></span><span class="tl__done" style="width:{done}%"></span>{"" if done <= 0 else f"<span class=tl__now style=left:{done}%></span>"}</div><div class="tl__k"><span>{left}</span><span>{right}</span></div>'


def card(title, handed, when, extra, fig, day, cls="", line=None):
    return f"""<article class="cc {cls}"><div class="cc__head"><span class="cc__title">{title}</span><span class="cc__dots">···</span></div>
<span>{handed}</span><span class="cc__when">{when}</span>{line or ""}{extra}<div class="cc__fig">{fig}</div><div class="cc__day {'none' if day.startswith('No ') else ''}"><i data-icon="calendar"></i>{day}</div></article>"""


def col(name, count, inner, cls=""):
    return f'<section class="board__col {cls}"><h2 class="board__head"><span>{name}</span><span class="board__count">{count}</span></h2>{inner}</section>'


def finished():
    return "".join(f'<div class="fin"><b>{n}</b><span>{l}</span><span>{d}</span><span></span></div>' for n, d, l in [
        ("Easter invitations", "9 Mar to 5 Apr", "3,480"), ("Mother's Day flyers", "20 Apr to 10 May", "1,904"), ("Winter tracts", "1 Jun to 31 Aug", "6,215")])


PREP = [
    card("Gympie mission", "Tracts · bundles of 50", "6 to 28 Feb 2027 · starts in 12 weeks", "", "<span>Area drawn · 2 regions</span>", "Sat 6 Feb · 0 of 9 routes planned",
         line=timeline(0, 25, "6 Feb", "28 Feb")),
    card("Easter 2027 invitations", "Invitations to Easter · bundles of 40", "8 Mar to 4 Apr 2027", "", "<span>No area yet</span>", "No outreach day planned",
         line=timeline(0, 25, "8 Mar", "4 Apr")),
]
ONGOING = card("Carols sample", "Invitations to carols · bundles of 40", "7 Nov to 14 Dec · wraps up 7 Dec", "",
               "<span><b>60</b> letterboxes</span><span><b>60</b> this week</span>", "Sat 22 Nov · 6 routes", line=timeline(22, 19, "7 Nov", "14 Dec"))
WRAPPING = card("Remembrance flyers", "Flyers · bundles of 50", "1 to 20 Nov · ends in 5 days", "<span class=cc__told><i data-icon=\"bell\"></i>Church told 13 Nov</span>",
                "<span><b>1,312</b> letterboxes</span><span><b>214</b> this week</span>", "No outreach day before it ends", cls="wrap", line=timeline(74, 35, "1 Nov", "20 Nov"))


def sketch_a():
    board = '<div class="board">' + col("Preparing", 2, "".join(PREP) + '<button class="board__add"><i data-icon="plus"></i>New campaign</button>') \
        + col("Ongoing", 1, ONGOING) + col("Wrapping up", 1, WRAPPING) \
        + col("Finished in 2026", 3, finished(), "board__col--narrow") + "</div>"
    return page("Campaigns: a board of time lines", general() + board,
                "Sketch A · Each card carries its own time line: start to end, today's dot, the wrap-up stretch in marker. Opening a card goes to the campaign's own page (/outreach/campaigns/…). Drag or ⋯ to move by hand.")


def record(compact=False):
    pipe = '<ol class="stage-pipeline" aria-label="Where it is"><li class="stage-pipeline__step"><button class="stage-pipeline__stage stage-pipeline__stage--passed"><i data-icon="check"></i><span>Preparing</span></button></li><li class="stage-pipeline__step"><button class="stage-pipeline__stage stage-pipeline__stage--current"><span>Ongoing</span></button></li><li class="stage-pipeline__step"><button class="stage-pipeline__stage stage-pipeline__stage--ahead"><span>Wrapping up</span></button></li><li class="stage-pipeline__step"><button class="stage-pipeline__stage stage-pipeline__stage--ahead"><span>Finished</span></button></li></ol>'
    return f"""<div class="rec"><div class="ph" style="display:flex;justify-content:space-between"><div><h2 style="font-size:var(--text-title)">Carols sample</h2><p class="cap" style="color:var(--ink-2);font-size:var(--text-caption)">Walkers carry it now. It wraps up on 7 Dec.</p></div><span><button class="btn btn--quiet btn--icon"><i data-icon="ellipsis"></i></button> <button class="btn btn--quiet btn--icon"><i data-icon="x"></i></button></span></div>
{pipe}
<div class="tiles3"><div class="t3"><b>60</b><span>letterboxes</span></div><div class="t3"><b>4.2 km</b><span>walked</span></div><div class="t3"><b>1</b><span>outreach day</span></div></div>
<div class="f"><label>Name</label><div class="in">Carols sample</div></div>
<div class="pair"><div class="f"><label>What's handed out</label><div class="in">Invitations to carols</div></div><div class="f"><label>Bundle size</label><div class="in">40 <em>letterboxes</em></div><small>Usually 50. Walkers are told how many bundles to take.</small></div></div>
<div class="pair"><div class="f"><label>Starts</label><div class="in">Sat 7 Nov 2026 <i data-icon="calendar"></i></div></div><div class="f"><label>Ends</label><div class="in">Mon 14 Dec 2026 <i data-icon="calendar"></i></div></div></div>
<div class="f"><label>Wrap up before it ends</label><div class="in">7 <em>days</em></div><small>On Mon 7 Dec it moves to Wrapping up and the church is told it ends soon.</small></div>
<div class="sw"><div><b>Show a leaderboard</b><span>Walkers counted by name see who's done the most.</span></div><i class="k"></i></div>
<div class="foot"><button class="btn btn--quiet">Cancel</button><button class="btn btn--primary">Save</button></div></div>"""


def sketch_b():
    board = '<div class="board board-b">' + col("Preparing", 2, "".join(PREP), "thin") \
        + col("Ongoing", 1, f'<article class="cc open" style="padding:16px 18px">{record(True)}</article>', "wide") \
        + col("Wrapping up", 1, WRAPPING, "thin") + col("Finished in 2026", 3, finished(), "thin") + "</div>"
    return page("Campaigns: cards open in place", general() + board,
                "Sketch B · Choosing a card opens it where it sits: its column widens and the card becomes the record (stage line, numbers, fields, Cancel and Save). The others narrow but stay in view.")


def sketch_c():
    rows = '<div class="grp"><span>Always on</span></div><div class="lrow"><b>General outreach</b><span class="n">481</span><span>Tracts · bundles of 50</span><span></span></div>'
    rows += '<div class="grp"><span>Wrapping up</span><span>1</span></div><div class="lrow"><b>Remembrance flyers</b><span class="n">1,312</span><span>Ends in 5 days · church told 13 Nov</span><span></span></div>'
    rows += '<div class="grp"><span>Ongoing</span><span>1</span></div><div class="lrow on"><b>Carols sample</b><span class="n">60</span><span>7 Nov to 14 Dec · next day Sat 22 Nov</span><span></span></div>'
    rows += '<div class="grp"><span>Preparing</span><span>2</span></div><div class="lrow"><b>Gympie mission</b><span class="n">—</span><span>Starts 6 Feb 2027 · 0 of 9 routes</span><span></span></div><div class="lrow"><b>Easter 2027 invitations</b><span class="n">—</span><span>Starts 8 Mar 2027 · no area yet</span><span></span></div>'
    rows += '<div class="grp"><span>Finished in 2026 ▸</span><span>3</span></div>'
    head = '<div style="display:flex;justify-content:space-between;padding:10px 12px;font-size:var(--text-caption);color:var(--ink-3);font-weight:600"><span>Campaign</span><span>Letterboxes</span></div>'
    body = f'<div class="split"><div>{head}{rows}</div><aside>{record()}</aside></div>'
    return page("Campaigns: a list by stage", body,
                "Sketch C · The workbench kept: the list grouped by stage (what needs attention first), the chosen campaign in the record pane with its stage line. Finished folds away by year.")


for name, html in [("a-time-lines", sketch_a()), ("b-open-in-place", sketch_b()), ("c-list-by-stage", sketch_c())]:
    (HERE / f"{name}.html").write_text(html)
print("ok")
