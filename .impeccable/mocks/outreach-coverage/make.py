import base64
import math
import random
import re
from pathlib import Path

HERE = Path(__file__).parent
KIT = HERE.parent / "design-system"


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
.page-tabs { margin: 0 0 0; }
.tbl { width: 100%; border-collapse: collapse; font-size: var(--text-label); }
.tbl th { text-align: left; font-size: var(--text-caption); font-weight: 600; color: var(--ink-3); padding: 0 12px 8px; border-bottom: 1px solid var(--line); }
.tbl td { padding: 11px 12px; border-bottom: 1px solid var(--line); vertical-align: top; }
.tbl td small { display: block; color: var(--ink-3); font-size: var(--text-caption); font-weight: 400; margin-top: 2px; }
.tbl tr.on td { background: var(--selected-fill); }
.tbl .num { text-align: right; font-variant-numeric: tabular-nums; }
.pill { display: inline-flex; align-items: center; height: 22px; padding: 0 9px; border-radius: 999px; background: var(--fill-on-surface); color: var(--ink-2); font-size: var(--text-pill); font-weight: 700; margin-left: 6px; }
.ph { display: flex; justify-content: space-between; gap: 12px; margin-bottom: 14px; }
.ph h2 { font-size: var(--text-title); }
.ph p { color: var(--ink-2); font-size: var(--text-caption); margin-top: 2px; }
.cap { color: var(--ink-3); font-size: var(--text-caption); }
.lbl { font-size: var(--text-caption); font-weight: 600; color: var(--ink-3); margin: 18px 0 8px; }
.stage { display: grid; border-top: 1px solid var(--line); height: 640px; margin-top: 0; }
.map { position: relative; overflow: hidden; background: #f7f4ee; }
.map svg { position: absolute; inset: 0; width: 100%; height: 100%; }
.map__attr { position: absolute; left: 10px; bottom: 8px; font-size: 0.6rem; color: var(--ink-3); background: rgb(247 244 238 / 0.9); padding: 2px 6px; border-radius: 6px; }
.map__tools { position: absolute; left: 12px; top: 12px; display: grid; gap: 6px; }
.map__tools span { width: 36px; height: 36px; display: grid; place-items: center; border-radius: 10px; background: #fff; box-shadow: 0 1px 3px rgb(24 35 58 / 0.18); color: var(--ink); }
.map__tools span.on { background: #2a3d5e; color: #fff; }
.map__zoom { position: absolute; right: 12px; top: 12px; display: grid; gap: 1px; border-radius: 10px; overflow: hidden; box-shadow: 0 1px 3px rgb(24 35 58 / 0.18); }
.map__zoom span { width: 36px; height: 36px; display: grid; place-items: center; background: #fff; }
.rail { padding: 18px 0 18px 24px; border-left: 1px solid var(--line); overflow: hidden; }
.lrail { padding: 18px 20px 18px 0; border-right: 1px solid var(--line); overflow: hidden; }
.sel { display: flex; align-items: center; justify-content: space-between; height: 40px; padding: 0 12px; border-radius: var(--radius-field); background: var(--field); border: 1px solid var(--field-edge); font-size: var(--text-label); font-weight: 600; }
.ramp { height: 12px; border-radius: 6px; background: linear-gradient(90deg, #00594f, #00776d 25%, #5fb3a9 60%, #cfe7e3 90%, #ffffff); border: 1px solid var(--line); }
.ramp-k { display: flex; justify-content: space-between; font-size: var(--text-caption); color: var(--ink-3); margin-top: 4px; font-variant-numeric: tabular-nums; }
.chk { display: flex; align-items: center; gap: 10px; padding: 7px 0; font-size: var(--text-label); }
.chk .b { width: 18px; height: 18px; border-radius: 5px; background: #2a3d5e; color: #fff; display: grid; place-items: center; font-style: normal; font-size: 12px; }
.chk .o { width: 18px; height: 18px; border-radius: 5px; border: 1.5px solid var(--field-edge); }
.chk .sw { margin-left: auto; width: 22px; height: 6px; border-radius: 3px; }
.chk .ct { margin-left: auto; color: var(--ink-3); font-variant-numeric: tabular-nums; font-size: var(--text-caption); }
.figs { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin: 10px 0; }
.fig { padding: 10px 12px; border-radius: var(--radius-field); background: var(--fill-on-surface); }
.fig b { display: block; font-size: var(--text-title); font-variant-numeric: tabular-nums; }
.fig span { font-size: var(--text-caption); color: var(--ink-2); }
.tiles { display: grid; grid-template-columns: repeat(4, 1fr); gap: 0; border-bottom: 1px solid var(--line); margin-bottom: 18px; }
.tile { padding: 4px 20px 16px 0; }
.tile + .tile { padding-left: 20px; border-left: 1px solid var(--line); }
.tile b { display: block; font-size: 2rem; font-weight: 700; letter-spacing: -0.02em; font-variant-numeric: tabular-nums; }
.tile span { font-size: var(--text-caption); color: var(--ink-2); }
.tile em { display: block; font-style: normal; font-size: var(--text-caption); color: var(--aqua-deep); font-weight: 600; margin-top: 2px; }
.rank { display: grid; grid-template-columns: 22px 1fr auto auto; gap: 12px; padding: 9px 0; border-bottom: 1px solid var(--line); font-size: var(--text-label); align-items: baseline; }
.rank .n { color: var(--ink-3); font-variant-numeric: tabular-nums; }
.rank .v { color: var(--ink-2); font-variant-numeric: tabular-nums; text-align: right; }
.bar-row { display: flex; gap: 8px; align-items: center; padding: 8px 0; border-top: 1px solid var(--line); }
.tray { position: absolute; left: 50%; bottom: 18px; transform: translateX(-50%); display: flex; align-items: center; gap: 14px; height: 54px; padding: 0 10px 0 20px; border-radius: 14px; background: #2a3d5e; color: #fff; font-size: var(--text-label); box-shadow: 0 6px 20px rgb(24 35 58 / 0.28); white-space: nowrap; }
.tray .btn { background: #fff; color: var(--ink); }
.two { display: grid; grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr); gap: 32px; }
"""

STYLE = css_of(KIT / "kit.css") + css_of(KIT / "parts.css") + LOCAL
SCRIPT = (KIT / "icons.js").read_text() + "\nwindow.SK_NAV=[[\"Sundays & sermons\",[[\"Sermons\",\"sermons\"]]],[\"Calendar & news\",[[\"Calendar\",\"calendar\"]]],[\"Ministries & groups\",[[\"Life Groups\",\"book-open\"],[\"Ministries\",\"users\"],[\"Outreach\",\"footprints\"],[\"Prayer\",\"prayer\"]]],[\"The app\",[[\"Media\",\"image\"]]]];\n" + (KIT / "kit.js").read_text()


def page(title, body, note, action="New campaign"):
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>
<style>{STYLE}</style><script>{SCRIPT}</script></head>
<body class="sk-admin" data-current="Outreach" style="height:900px;overflow:hidden">
<main><header class="title-band"><div class="title-band__text"><h1 class="title-band__title">Outreach</h1><p class="title-band__lead">Campaigns, where outreach has been, what it adds up to, and what walkers send in.</p></div>
<div class="title-band__actions"><button class="btn btn--primary"><i data-icon="plus"></i>{action}</button></div></header>
{body}</main><p class="sk-note">{note}</p></body></html>"""


def tabs(on, names):
    counts = {"Follow-ups": "2", "Reports": "3"}
    items = "".join(
        f'<button class="tab-strip__tab" role="tab" aria-selected="{str(name == on).lower()}">{name}{f"<span class=pill>{counts[name]}</span>" if name in counts else ""}</button>'
        for name in names)
    return f'<div class="tab-strip page-tabs" role="tablist">{items}</div>'


AGES = ["#00594f", "#00776d", "#2f9488", "#5fb3a9", "#9fd0c8", "#cfe7e3"]


def streets(width, height, seed=7, picked=None, lasso=False):
    rng = random.Random(seed)
    out = [f'<svg viewBox="0 0 {width} {height}" preserveAspectRatio="xMidYMid slice">']
    out.append(f'<path d="M{width*0.62} -10 C {width*0.58} {height*0.3}, {width*0.7} {height*0.55}, {width*0.66} {height+10}" stroke="#cde2e3" stroke-width="16" fill="none"/>')
    out.append(f'<rect x="{width*0.08}" y="{height*0.62}" width="{width*0.16}" height="{height*0.22}" rx="18" fill="#d9e6cf"/>')
    out.append(f'<rect x="{width*0.74}" y="{height*0.08}" width="{width*0.12}" height="{height*0.14}" rx="10" fill="#ebe2d2"/>')
    lines = []
    step = 46
    for row in range(-1, int(height / step) + 2):
        y0 = row * step + rng.uniform(-8, 8)
        x = -20
        while x < width + 20:
            seg = rng.uniform(70, 150)
            y1 = y0 + rng.uniform(-14, 14)
            lines.append((x, y0, x + seg, y1))
            x += seg
            y0 = y1
    for col in range(-1, int(width / 92) + 2):
        x0 = col * 92 + rng.uniform(-14, 14)
        y = -20
        while y < height + 20:
            seg = rng.uniform(46, 120)
            if rng.random() < 0.7:
                lines.append((x0, y, x0 + rng.uniform(-10, 10), y + seg))
            y += seg
    for x1, y1, x2, y2 in lines:
        out.append(f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="#ddd5c7" stroke-width="7" stroke-linecap="round"/>')
    for x1, y1, x2, y2 in lines:
        out.append(f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="#ffffff" stroke-width="5" stroke-linecap="round"/>')
    cx, cy = width * 0.38, height * 0.45
    for x1, y1, x2, y2 in lines:
        mx, my = (x1 + x2) / 2, (y1 + y2) / 2
        d = math.hypot(mx - cx, (my - cy) * 1.3)
        if d < width * 0.34 and rng.random() < 0.82:
            age = min(5, int(d / (width * 0.06)) + rng.choice([0, 0, 1]))
            out.append(f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="{AGES[age]}" stroke-width="4.5" stroke-linecap="round"/>')
    out.append(f'<path d="M -10 {height*0.3} C {width*0.3} {height*0.26}, {width*0.6} {height*0.36}, {width+10} {height*0.31}" stroke="#a99c88" stroke-width="12" fill="none"/>')
    out.append(f'<path d="M -10 {height*0.3} C {width*0.3} {height*0.26}, {width*0.6} {height*0.36}, {width+10} {height*0.31}" stroke="#fbf6ee" stroke-width="9" fill="none"/>')
    zx, zy = width * 0.12, height * 0.12
    out.append(f'<rect x="{zx}" y="{zy}" width="{width*0.1}" height="{height*0.12}" rx="6" fill="rgb(180 55 47 / 0.08)" stroke="#b4372f" stroke-width="2" stroke-dasharray="6 4"/>')
    for i in range(14):
        px, py = rng.uniform(width * 0.15, width * 0.6), rng.uniform(height * 0.35, height * 0.9)
        out.append(f'<circle cx="{px:.0f}" cy="{py:.0f}" r="4.5" fill="#2a3d5e" stroke="#fff" stroke-width="1.5"/>')
    for px, py in [(width * 0.44, height * 0.7), (width * 0.3, height * 0.22)]:
        out.append(f'<g transform="translate({px:.0f} {py:.0f})"><circle r="11" fill="#e7ae74" stroke="#fff" stroke-width="2"/><path d="M-3 -5 v10 M-3 -5 h7 l-2 3 l2 3 h-7" stroke="#2a1a08" stroke-width="1.6" fill="none"/></g>')
    if picked:
        x1, y1, x2, y2 = lines[picked]
        out.append(f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="#e7ae74" stroke-width="10" stroke-linecap="round" opacity="0.9"/>')
        out.append(f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="#00594f" stroke-width="4.5" stroke-linecap="round"/>')
    if lasso:
        out.append(f'<path d="M {width*0.42} {height*0.5} C {width*0.5} {height*0.38}, {width*0.62} {height*0.44}, {width*0.6} {height*0.62} S {width*0.44} {height*0.78}, {width*0.42} {height*0.5} Z" fill="rgb(42 61 94 / 0.10)" stroke="#2a3d5e" stroke-width="2" stroke-dasharray="7 5"/>')
    out.append("</svg>")
    return "".join(out)


def map_box(width, height, picked=None, lasso=False, tools=True, tray=None):
    tool_html = '<div class="map__tools"><span class="on" title="Move">✋</span><span title="Draw an area">◌</span><span title="Pick streets">／</span></div>' if tools else ""
    tray_html = f'<div class="tray">{tray}</div>' if tray else ""
    return f'<div class="map">{streets(width, height, picked=picked, lasso=lasso)}{tool_html}<div class="map__zoom"><span>+</span><span>−</span></div>{tray_html}<span class="map__attr">© OpenStreetMap contributors · Queensland Addresses · Moreton Bay City Council</span></div>'


def legend():
    return """<div class="lbl">When it was last done</div><div class="ramp"></div><div class="ramp-k"><span>This week</span><span>6 weeks</span><span>3 months: open again</span></div>"""


def checks(items):
    return "".join(f'<div class="chk">{"<span class=b>✓</span>" if on else "<span class=o></span>"}{name}{f"<span class=ct>{count}</span>" if count else ""}</div>' for name, on, count in items)


LAYERS = [("Open streets", True, "2,426"), ("No junk mail", True, "212"), ("Don't deliver: zones and addresses", True, "9"), ("Reports waiting", False, "3")]

ALL_TABS = ("Campaigns", "Coverage", "Numbers", "Places", "Follow-ups", "Reports", "Settings")


def map_desk():
    left = f"""<div class="lrail"><div class="lbl" style="margin-top:0">Campaign</div><div class="sel">General outreach<i data-icon="chevron-down"></i></div>{legend()}
<div class="lbl">Show</div>{checks(LAYERS)}<div class="lbl">Totals in view</div><div class="figs"><div class="fig"><b>1,912</b><span>streets done</span></div><div class="fig"><b>9,640</b><span>letterboxes</span></div></div></div>"""
    right = """<div class="rail"><div class="ph"><div><h2>Kingfisher St</h2><p>Albany Creek · both sides · 38 letterboxes</p></div><button class="btn btn--quiet btn--icon"><i data-icon="ellipsis"></i></button></div>
<dl class="fact-list"><div class="chk">This side: done 12 days ago<span class="ct">casual walk</span></div><div class="chk">Other side: done 40 days ago<span class="ct">outreach day 1 Oct</span></div><div class="chk">No junk mail<span class="ct">2 letterboxes</span></div></dl>
<p class="cap" style="margin:14px 0">Marking it not done opens the street to walks in General outreach straight away. Other campaigns keep theirs.</p>
<button class="btn btn--quiet" style="width:100%">Mark as not done</button></div>"""
    body = tabs("Map", ("Map", "Numbers", "Campaigns", "Follow-ups", "Reports", "Settings")) + f'<div class="stage" style="grid-template-columns: 260px minmax(0,1fr) 340px">{left}{map_box(760, 640, picked=40, tools=True)}{right}</div>'
    return page("Outreach: the map is the desk", body, "Sketch A · The map leads; layers on the left, whatever you click on the right. Numbers is its own tab.")


def trend_svg():
    pts = [12, 30, 26, 44, 38, 52, 47, 61, 58, 74, 69, 88, 80]
    w, h = 620, 170
    xs = [i * (w - 40) / (len(pts) - 1) + 10 for i in range(len(pts))]
    ys = [h - 20 - p * 1.6 for p in pts]
    comp = [20, 22, 28, 30, 25, 33, 35, 31, 38, 36, 40, 42, 44]
    cys = [h - 20 - p * 1.6 for p in comp]
    path = " ".join(f"{'M' if i == 0 else 'L'}{x:.0f} {y:.0f}" for i, (x, y) in enumerate(zip(xs, ys)))
    cpath = " ".join(f"{'M' if i == 0 else 'L'}{x:.0f} {y:.0f}" for i, (x, y) in enumerate(zip(xs, cys)))
    grid = "".join(f'<line x1="10" x2="{w-30}" y1="{y}" y2="{y}" stroke="rgb(42 61 94 / 0.10)"/>' for y in (30, 70, 110, 150))
    labels = "".join(f'<text x="{x:.0f}" y="{h}" font-size="10" fill="#626a79" text-anchor="middle">{lab}</text>' for x, lab in zip(xs[::3], ["Aug", "Sep", "Sep", "Oct", "Nov"]))
    return f'<svg viewBox="0 0 {w} {h+4}" width="100%" height="{h+4}">{grid}<path d="{cpath}" stroke="#626a79" stroke-width="1.5" stroke-dasharray="4 4" fill="none"/><path d="{path}" stroke="#00776d" stroke-width="2.5" fill="none"/><circle cx="{xs[-1]:.0f}" cy="{ys[-1]:.0f}" r="5" fill="#00776d"/><text x="{xs[-1]-6:.0f}" y="{ys[-1]-10:.0f}" font-size="11" font-weight="700" fill="#18233a" text-anchor="end">412 this week</text>{labels}</svg>'


def numbers_first():
    tiles = """<div class="tiles"><div class="tile"><b>4,812</b><span>letterboxes</span><em>+18% on the 3 months before</em></div><div class="tile"><b>168 km</b><span>walked</span><em>+11%</em></div><div class="tile"><b>61</b><span>walks</span><em>14 on outreach days</em></div><div class="tile"><b>23</b><span>conversations</span><em>4 follow-ups asked for</em></div></div>"""
    days = """<table class="tbl"><tr><th>Outreach day</th><th class="num">Routes done</th><th class="num">Letterboxes</th><th class="num">Walkers</th></tr>
<tr><td><b>Sat 1 Nov</b><small>General outreach · Bunya St area</small></td><td class="num">8 of 9</td><td class="num">1,206</td><td class="num">17</td></tr>
<tr><td><b>Sat 4 Oct</b><small>General outreach · Eaton Creek</small></td><td class="num">9 of 9</td><td class="num">1,318</td><td class="num">19</td></tr>
<tr><td><b>Sat 6 Sep</b><small>General outreach · Albany Hills</small></td><td class="num">6 of 8</td><td class="num">902</td><td class="num">12</td></tr></table>"""
    walkers = "".join(f'<div class="rank"><span class="n">{i}</span><span>{name}</span><span class="v">{boxes}</span><span class="v">{km} km</span></div>' for i, (name, boxes, km) in enumerate(
        [("Walker A.", "612", "21"), ("Walker B.", "540", "18"), ("Walker C.", "498", "17"), ("Walker D.", "377", "13"), ("Walker E.", "301", "11"), ("Walker F.", "260", "9")], 1))
    seg = '<div class="segmented" role="radiogroup" style="width:max-content"><button class="segmented__option" role="radio" aria-checked="true">By week</button><button class="segmented__option" role="radio" aria-checked="false">By month</button></div>'
    chips = '<div style="display:flex;gap:8px;align-items:center;margin:16px 0 18px"><span class="filter-chip filter-chip--on" style="display:inline-flex;align-items:center;height:34px;padding:0 12px;border-radius:999px;background:#2a3d5e;color:#fff;font-size:var(--text-caption)">Last 3 months</span><span style="display:inline-flex;align-items:center;height:34px;padding:0 12px;border-radius:999px;background:var(--field);color:var(--ink-2);font-size:var(--text-caption)">This year</span><span style="display:inline-flex;align-items:center;height:34px;padding:0 12px;border-radius:999px;background:var(--field);color:var(--ink-2);font-size:var(--text-caption)">All campaigns</span><span style="display:inline-flex;align-items:center;height:34px;padding:0 12px;border-radius:999px;background:#2a3d5e;color:#fff;font-size:var(--text-caption)">General outreach</span><span style="display:inline-flex;align-items:center;height:34px;padding:0 12px;border-radius:999px;background:var(--field);color:var(--ink-2);font-size:var(--text-caption)">Carols 2026</span></div>'
    body = tabs("Numbers", ("Numbers", "Coverage", "Campaigns", "Follow-ups", "Reports", "Settings")) + chips + tiles + f"""<div class="two"><div><div class="ph"><div><h2>Letterboxes each week</h2><p>Dashed: the same weeks last year.</p></div>{seg}</div>{trend_svg()}<div class="lbl">Outreach days</div>{days}</div>
<div><div class="ph"><div><h2>Walkers</h2><p>Counted by name unless they chose not to be. Leaderboard on for Carols 2026.</p></div></div><div class="rank" style="color:var(--ink-3);font-size:var(--text-caption)"><span></span><span>Name</span><span class="v">Letterboxes</span><span class="v">Walked</span></div>{walkers}<div class="rank"><span class="n"></span><span>and 9 others, not by name</span><span class="v">1,024</span><span class="v">79 km</span></div></div></div>"""
    return page("Outreach: numbers lead", body, "Sketch B · Numbers open the page: tiles, the trend and walkers. Coverage is the next tab.")


def coverage_rail():
    rail = f"""<div class="rail"><div class="lbl" style="margin-top:0">Campaign</div><div class="sel">General outreach<i data-icon="chevron-down"></i></div>{legend()}
<div class="figs"><div class="fig"><b>1,912</b><span>of 4,338 streets done</span></div><div class="fig"><b>44%</b><span>of letterboxes</span></div></div>
<div class="lbl">Show on the map</div>{checks(LAYERS)}
<div class="lbl">Chosen</div><div class="ph" style="margin-bottom:6px"><div><h2 style="font-size:var(--text-row-title)">An area you drew</h2><p>86 street sides · 1,140 letterboxes · last done 9 to 70 days ago</p></div></div>
<p class="cap" style="margin:6px 0 12px">Marking it not done opens these streets to walks in General outreach straight away. You can undo it for a minute.</p>
<div style="display:flex;justify-content:space-between"><button class="btn btn--quiet">Clear</button><button class="btn btn--primary">Mark as not done</button></div></div>"""
    body = tabs("Coverage", ALL_TABS) + f'<div class="stage" style="grid-template-columns: minmax(0,1fr) 360px">{map_box(1000, 640, lasso=True)}{rail}</div>'
    return page("Outreach: coverage with a rail", body, "Sketch C · Coverage is a tab: the map, and a rail for the campaign, the age ramp, layers and the selection. Numbers and Places are tabs.")


for name, make in [("map-desk", map_desk), ("numbers-first", numbers_first), ("coverage-rail", coverage_rail)]:
    (HERE / f"{name}.html").write_text(make())
