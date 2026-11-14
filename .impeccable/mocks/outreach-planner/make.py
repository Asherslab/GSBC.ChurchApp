import json
import math
from pathlib import Path

HERE = Path(__file__).parent
ROOT = HERE.parent.parent.parent
DATA = json.loads((ROOT / "spikes/app-poc/App/wwwroot/data/outreach.json").read_text())
EDGES = DATA["edges"]
CHURCH = DATA["church"]
ROUTES = DATA["outreachRoutes"]
COLOURS = ["#2a3d5e", "#00776d", "#b0662a", "#6b4e9b", "#2f7d3c", "#a8384f", "#1f6f9e", "#7a6a1e", "#4d5b70"]
FIRST = ["Sam", "Ruth", "Joel", "Mia", "Priya", "Tom", "Grace", "Eli", "Hannah", "Ben", "Leah"]


def route_facts(route):
    edges = [EDGES[i] for i in route["edges"]]
    houses = sum(sum(e["boxes"]) for e in edges)
    units = sum(sum(e["units"]) for e in edges)
    metres = sum(e["length"] for e in edges) * 2
    minutes = round(metres / 4000 * 60 + (houses * 8 + units * 3) / 60)
    return houses + units, metres, minutes


FACTS = [route_facts(route) for route in ROUTES]


def window():
    points = [p for r in ROUTES for i in r["edges"] for p in EDGES[i]["line"]] + [[CHURCH[1], CHURCH[0]]]
    lons = [p[0] for p in points]
    lats = [p[1] for p in points]
    pad = 0.002
    return min(lons) - pad, max(lons) + pad, min(lats) - pad, max(lats) + pad


def projector(width, height, box=None):
    w, e, s, n = box or window()
    kx = math.cos(math.radians((s + n) / 2))
    scale = min(width / ((e - w) * kx), height / (n - s))
    ox = (width - (e - w) * kx * scale) / 2
    oy = (height - (n - s) * scale) / 2
    return lambda lon, lat: (round(ox + (lon - w) * kx * scale, 1), round(oy + (n - lat) * scale, 1)), (w, e, s, n)


def svg_map(width, height, routes=True, faded=(), chosen=None, labels=True, progress=None, box=None, mono=False):
    project, (w, e, s, n) = projector(width, height, box)
    out = [f'<svg viewBox="0 0 {width} {height}" width="100%" height="100%" preserveAspectRatio="xMidYMid slice" role="img" aria-label="Map">',
           f'<rect width="{width}" height="{height}" fill="var(--map-ground)"/>']
    route_of = {i: k for k, r in enumerate(ROUTES) for i in r["edges"]}
    for index, edge in enumerate(EDGES):
        line = edge["line"]
        if not any(w <= lon <= e and s <= lat <= n for lon, lat in line):
            continue
        d = "M" + " L".join(f"{x},{y}" for x, y in (project(lon, lat) for lon, lat in line))
        major = edge["road"] in ("trunk", "primary", "secondary", "primary_link", "secondary_link", "trunk_link")
        out.append(f'<path d="{d}" stroke="var({"--map-major" if major else "--map-street"})" stroke-width="{5 if major else 2.4}" fill="none" stroke-linecap="round"/>')
        if routes and index in route_of:
            k = route_of[index]
            colour = "#111" if mono else COLOURS[k]
            dim = (chosen is not None and k != chosen) or k in faded
            width_ = 7 if k == chosen else 4.5
            out.append(f'<path d="{d}" stroke="{colour}" stroke-opacity="{0.28 if dim else 0.92}" stroke-width="{width_}" fill="none" stroke-linecap="round"/>')
            if progress and k in progress and (index * 7919) % 100 < progress[k]:
                out.append(f'<path d="{d}" stroke="var(--done)" stroke-width="2" stroke-dasharray="1 5" fill="none" stroke-linecap="round"/>')
    if routes and labels:
        for k, route in enumerate(ROUTES):
            pts = [p for i in route["edges"] for p in EDGES[i]["line"]]
            lon = sum(p[0] for p in pts) / len(pts)
            lat = sum(p[1] for p in pts) / len(pts)
            x, y = project(lon, lat)
            boxes, _, minutes = FACTS[k]
            colour = COLOURS[k]
            out.append(f'<g transform="translate({x},{y})"><rect x="-44" y="-13" width="88" height="26" rx="13" fill="var(--surface)" stroke="{colour}" stroke-width="2"/>'
                       f'<circle cx="-31" cy="0" r="9" fill="{colour}"/><text x="-31" y="4" text-anchor="middle" font-size="11" font-weight="700" fill="#fff">{k + 1}</text>'
                       f'<text x="-17" y="4" font-size="10.5" font-weight="600" fill="var(--ink)">{boxes} · {minutes}m</text></g>')
    cx, cy = project(CHURCH[1], CHURCH[0])
    out.append(f'<g transform="translate({cx},{cy})"><circle r="11" fill="var(--marker)" stroke="var(--surface)" stroke-width="3"/><text y="4" text-anchor="middle" font-size="11" font-weight="700" fill="#2a1a08">M</text></g>')
    out.append("</svg>")
    return "".join(out)


CSS = """
:root { --navy:#2a3d5e; --aqua:#00776d; --live:#008176; --marker:#e7ae74; --marker-ink:#8a5422; --sand:#f4efe7; --sand-strong:#e9e1d4;
  --ink:#18233a; --ink-2:#545e70; --ink-3:#626a79; --line:rgb(42 61 94 / .12); --surface:#fff; --field:#f4efe7; --field-edge:rgb(98 106 121 / .7);
  --danger:#b4372f; --map-ground:#f3f1ec; --map-street:#d9d4cb; --map-major:#c9bfae; --done:#ffffff; --sel:#f4efe7; }
* { box-sizing:border-box; margin:0; padding:0; }
body { font-family:Montserrat, -apple-system, system-ui, sans-serif; font-size:16px; color:var(--ink); background:var(--surface); width:1440px; height:900px; overflow:hidden; display:grid; grid-template-columns:248px 1fr; }
.side { background:var(--navy); color:#d5c9b7; padding:22px 18px; font-size:13.5px; }
.side b { display:block; color:#fff; font-size:15px; margin-bottom:26px; }
.side h6 { font-size:10.5px; letter-spacing:.06em; text-transform:uppercase; color:rgb(255 255 255 / .6); margin:18px 0 8px; }
.side a { display:block; padding:7px 10px; border-radius:8px; color:#fff; text-decoration:none; }
.side a.on { background:rgb(255 255 255 / .12); box-shadow:inset 3px 0 0 var(--marker); }
main { display:flex; flex-direction:column; min-width:0; height:900px; }
.band { display:flex; justify-content:space-between; align-items:flex-end; padding:26px 32px 16px; }
.band h1 { font-size:27px; letter-spacing:-.02em; }
.band p { color:var(--ink-2); font-size:14px; margin-top:4px; }
.crumb { font-size:13px; color:var(--aqua); font-weight:600; margin-bottom:6px; }
.acts { display:flex; gap:10px; }
.btn { display:inline-flex; align-items:center; gap:8px; height:38px; padding:0 16px; border-radius:9px; border:1px solid var(--field-edge); background:var(--surface); font:600 14px Montserrat; color:var(--ink); }
.btn--p { background:var(--navy); color:#fff; border-color:var(--navy); }
.btn--q { border-color:transparent; background:transparent; color:var(--aqua); }
.tabs { display:flex; gap:24px; padding:0 32px; border-bottom:1px solid var(--line); font-size:14px; font-weight:600; color:var(--ink-2); }
.tabs span { padding:10px 0; }
.tabs .on { color:var(--ink); box-shadow:inset 0 -3px 0 var(--marker); }
.pill { display:inline-flex; align-items:center; height:21px; padding:0 9px; border-radius:999px; background:var(--sand); color:var(--ink-2); font-size:11.5px; font-weight:700; }
.pill--live { background:var(--live); color:#fff; }
.pill--warn { background:#fbe9d6; color:var(--marker-ink); }
.dot { width:12px; height:12px; border-radius:50%; display:inline-block; flex:none; }
.num { font-variant-numeric:tabular-nums; }
.cap { font-size:12.5px; color:var(--ink-3); }
.bar { height:6px; border-radius:3px; background:var(--sand-strong); overflow:hidden; }
.bar i { display:block; height:100%; background:var(--live); }
.note { position:fixed; right:12px; bottom:8px; font-size:11px; color:var(--ink-3); background:rgb(255 255 255 / .85); padding:3px 8px; border-radius:6px; }
"""


def shell(body, extra_css):
    nav = [("Sundays & sermons", ["Sermons"]), ("Calendar & news", ["Calendar", "News"]),
           ("Ministries & groups", ["Life Groups", "Ministries", "Outreach", "Prayer"]), ("The app", ["Media", "App styling"])]
    side = "".join(f"<h6>{h}</h6>" + "".join(f'<a class="{"on" if x == "Outreach" else ""}">{x}</a>' for x in items) for h, items in nav)
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=1440">
<link rel="preconnect" href="https://fonts.googleapis.com"><link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700&display=swap" rel="stylesheet">
<title>Outreach planner sketch</title><style>{CSS}{extra_css}</style></head>
<body><nav class="side"><b>Good Shepherd</b>{side}</nav><main>{body}</main>
<p class="note">Streets and the nine sample routes: the POC's Albany Creek data. Names on claims are made up.</p></body></html>
"""


def day_band(actions):
    return f"""<header class="band"><div><p class="crumb">Outreach › Days</p><h1>Outreach Saturday, 6 December</h1>
<p>8:30 to 11:30 · meet at church, the hall doors · Carols 2026 · 9 routes, {sum(f[0] for f in FACTS):,} letterboxes</p></div><div class="acts">{actions}</div></header>"""


def card_a():
    claims = {0: ["Sam", "Ruth"], 1: ["Joel"], 3: ["Mia", "Priya", "Tom"], 4: ["Grace"], 6: ["Eli"]}
    progress = {0: 100, 3: 62, 4: 35, 6: 12}
    columns = [("Needs review", [8], "warn"), ("Open", [1, 2, 5], ""), ("Claimed", [6], ""), ("Walking", [3, 4], "live"), ("Done", [0], "")]

    def card(k):
        boxes, metres, minutes = FACTS[k]
        who = claims.get(k)
        p = progress.get(k)
        name = ROUTES[k]["name"]
        extra = '<p class="why">Needs review: 310 m between streets</p>' if k == 8 else ""
        return f"""<article class="bc"><header><span class="dot" style="background:{COLOURS[k]}"></span><b>{k + 1} · {name}</b></header>
<p class="cap num">{boxes} letterboxes · {metres / 1000:.1f} km · {minutes} min</p>{extra}
{f'<p class="who">{", ".join(who)}</p>' if who else ''}{f'<div class="bar"><i style="width:{p}%"></i></div><p class="cap num">{p}% walked</p>' if p is not None else ''}</article>"""

    cols = "".join(f"""<section class="col"><h3>{t} <span class="pill {'pill--' + tone if tone else ''}">{len(ks)}</span></h3>{''.join(card(k) for k in ks)}</section>""" for t, ks, tone in columns)
    body = day_band('<button class="btn">Print 9 maps</button><button class="btn">Plan routes</button><button class="btn">⋯</button>') + \
        '<div class="tabs"><span class="on">Routes</span><span>Day details</span><span>Afterwards</span></div>' + \
        f'<div class="strip">{svg_map(1160, 250, progress=progress, labels=False)}<span class="live"><span class="pill pill--live">On now</span> 6 people walking · 41% of letterboxes done · till 11:30</span></div>' + \
        f'<div class="board">{cols}</div>'
    css = """.strip { position:relative; height:250px; margin:16px 32px 0; border-radius:12px; overflow:hidden; border:1px solid var(--line); }
.live { position:absolute; left:14px; top:12px; background:var(--surface); padding:6px 12px 6px 6px; border-radius:999px; font-size:13px; font-weight:600; display:flex; gap:8px; align-items:center; box-shadow:0 2px 8px rgb(24 35 58 / .12); }
.board { display:grid; grid-template-columns:repeat(5, 1fr); gap:14px; padding:16px 32px 24px; flex:1; min-height:0; }
.col { background:var(--sand); border-radius:12px; padding:12px; display:flex; flex-direction:column; gap:10px; overflow:hidden; }
.col h3 { font-size:13px; display:flex; justify-content:space-between; align-items:center; color:var(--ink-2); }
.bc { background:var(--surface); border-radius:10px; padding:11px 12px; display:grid; gap:6px; box-shadow:0 1px 2px rgb(24 35 58 / .08); }
.bc header { display:flex; gap:8px; align-items:flex-start; font-size:13px; line-height:1.3; }
.bc header .dot { margin-top:2px; }
.who { font-size:12.5px; font-weight:600; color:var(--ink); }
.why { font-size:12px; color:var(--marker-ink); font-weight:600; }"""
    return shell(body, css)


def card_b():
    def sheet(k, on=False):
        boxes, metres, minutes = FACTS[k]
        edges = ROUTES[k]["edges"]
        pts = [p for i in edges for p in EDGES[i]["line"]]
        box = (min(p[0] for p in pts) - .0012, max(p[0] for p in pts) + .0012, min(p[1] for p in pts) - .0012, max(p[1] for p in pts) + .0012)
        streets = []
        for i in edges:
            name = EDGES[i]["name"] or "Path"
            if name not in streets:
                streets.append(name)
        bundles = math.ceil(boxes / 50)
        return f"""<article class="sheet{' on' if on else ''}"><header><b><span class="dot" style="background:{COLOURS[k]}"></span> Route {k + 1}</b><span class="qr"></span></header>
<div class="mini">{svg_map(260, 170, chosen=k, labels=False, box=box)}</div>
<p class="cap num"><b class="num">{boxes}</b> letterboxes · {bundles} bundles · {minutes} min</p>
<ol>{''.join(f'<li>{s}</li>' for s in streets[:5])}{'<li class="more">+' + str(len(streets) - 5) + ' more</li>' if len(streets) > 5 else ''}</ol></article>"""

    facts = """<aside class="facts"><h3>The day</h3><dl><dt>When</dt><dd>Sat 6 Dec, 8:30 to 11:30</dd><dt>Meet</dt><dd>Church, the hall doors</dd>
<dt>Campaign</dt><dd>Carols 2026</dd><dt>Handed out</dt><dd>Carols invitation</dd><dt>Note</dt><dd>Morning tea after, in the hall.</dd></dl>
<h3>Ready to print</h3><p class="cap">8 of 9 routes reviewed. Route 9 needs a look: 310 m between streets.</p><button class="btn btn--p wide">Print 9 maps (PDF)</button><button class="btn wide">Plan routes</button></aside>"""
    body = day_band('<button class="btn">Day details</button><button class="btn">⋯</button>') + \
        '<div class="tabs"><span class="on">Route maps</span><span>On the day</span></div>' + \
        f'<div class="split">{facts}<div class="sheets">{"".join(sheet(k, k == 2) for k in range(8))}</div></div>'
    css = """.split { display:grid; grid-template-columns:280px minmax(0, 1fr); flex:1; min-height:0; }
.facts { border-right:1px solid var(--line); padding:20px 24px 20px 32px; display:grid; align-content:start; gap:12px; }
.facts h3 { font-size:15px; margin-top:6px; }
.facts dl { display:grid; grid-template-columns:90px 1fr; gap:8px; font-size:13.5px; }
.facts dt { color:var(--ink-3); }
.wide { width:100%; justify-content:center; }
.sheets { display:grid; grid-template-columns:repeat(4, minmax(0, 1fr)); gap:18px; padding:20px 32px; background:var(--sand); overflow:hidden; align-content:start; }
.sheet { background:var(--surface); aspect-ratio:210/297; padding:12px; display:flex; flex-direction:column; gap:8px; box-shadow:0 1px 3px rgb(24 35 58 / .14); border-radius:3px; }
.sheet.on { outline:3px solid var(--aqua); outline-offset:3px; }
.sheet header { display:flex; justify-content:space-between; align-items:center; font-size:14px; }
.sheet header b { display:flex; gap:7px; align-items:center; }
.qr { width:30px; height:30px; background:repeating-conic-gradient(var(--ink) 0 25%, #fff 0 50%) 0 0/8px 8px; border:2px solid var(--ink); }
.mini { height:150px; border-radius:4px; overflow:hidden; border:1px solid var(--line); }
.sheet ol { font-size:11.5px; color:var(--ink-2); padding-left:16px; display:grid; gap:2px; }
.sheet .more { list-style:none; margin-left:-16px; color:var(--ink-3); }"""
    return shell(body, css)


def card_c():
    claims = {0: "Sam, Ruth", 1: "Joel", 3: "Mia + 2", 4: "Grace", 6: "Eli"}
    rows = []
    for k, route in enumerate(ROUTES):
        boxes, metres, minutes = FACTS[k]
        flag = '<span class="pill pill--warn">Needs review</span>' if k == 8 else ""
        rows.append(f"""<tr class="{'on' if k == 3 else ''}"><td><span class="dot" style="background:{COLOURS[k]}"></span></td><td><b>{k + 1} · {route['name']}</b>{flag}</td>
<td class="num r">{boxes}</td><td class="num r">{minutes}m</td><td>{claims.get(k, '<span class="cap">Open</span>')}</td></tr>""")
    tools = "".join(f'<span class="tool{" on" if t == "Select" else ""}" title="{t}">{g}</span>' for t, g in
                    [("Select", "↖"), ("Scissors", "✂"), ("Brush", "⌇"), ("Lasso", "◌"), ("Eraser", "⌫"), ("Start pin", "⚑")])
    body = day_band('<button class="btn">Print maps</button><button class="btn">⋯</button>') + \
        f"""<div class="desk"><div class="map">{svg_map(800, 720, chosen=3)}<div class="rail">{tools}<hr><span class="tool">↶</span><span class="tool">↷</span></div>
<div class="sel"><b>Route 4</b> · {FACTS[3][0]} letterboxes · {FACTS[3][2]} min <button class="btn btn--q">Merge</button><button class="btn btn--q">Lock</button><button class="btn btn--q">Rename</button></div></div>
<section class="pane"><div class="seg"><span class="on">Plan</span><span>On the day</span></div>
<p class="cap">Drawn from the area <b>Albany Creek north</b> · 100 letterboxes a route · 2 need review</p>
<table><thead><tr><th></th><th>Route</th><th class="r">Letterboxes</th><th class="r">Time</th><th>Claimed</th></tr></thead><tbody>{''.join(rows)}</tbody></table>
<div class="foot"><button class="btn">Rebalance</button><button class="btn">Regenerate</button><span></span><button class="btn btn--p">Publish</button></div></section></div>"""
    css = """.desk { display:grid; grid-template-columns:minmax(0, 1fr) 470px; flex:1; min-height:0; overflow:hidden; border-top:1px solid var(--line); }
.map { position:relative; overflow:hidden; }
.rail { position:absolute; left:16px; top:16px; background:var(--surface); border-radius:12px; padding:6px; display:grid; gap:4px; box-shadow:0 4px 14px rgb(24 35 58 / .16); }
.rail hr { border:0; border-top:1px solid var(--line); margin:2px 4px; }
.tool { width:38px; height:38px; display:grid; place-items:center; border-radius:8px; font-size:17px; color:var(--ink-2); }
.tool.on { background:var(--navy); color:#fff; }
.sel { position:absolute; left:50%; bottom:20px; transform:translateX(-50%); background:var(--surface); border-radius:12px; padding:6px 8px 6px 16px; display:flex; gap:4px; align-items:center; font-size:13.5px; box-shadow:0 4px 14px rgb(24 35 58 / .16); white-space:nowrap; }
.sel b { margin-right:2px; }
.pane { border-left:1px solid var(--line); padding:18px 24px 0; display:flex; flex-direction:column; gap:12px; min-height:0; }
.seg { display:inline-grid; grid-template-columns:1fr 1fr; background:var(--sand); border-radius:9px; padding:3px; font-size:13.5px; font-weight:600; width:240px; }
.seg span { text-align:center; padding:6px; border-radius:7px; color:var(--ink-2); }
.seg .on { background:var(--surface); color:var(--ink); box-shadow:0 1px 2px rgb(24 35 58 / .15); }
table { width:100%; border-collapse:collapse; font-size:13px; }
th { text-align:left; font-size:12px; color:var(--ink-3); font-weight:600; padding:0 6px 8px; border-bottom:1px solid var(--line); }
td { padding:7px 6px; border-bottom:1px solid var(--line); vertical-align:top; }
td b { font-weight:600; display:block; }
td .pill { margin-top:4px; }
.r { text-align:right; }
tr.on td { background:var(--sel); }
.foot { margin-top:auto; display:flex; gap:10px; padding:14px 0 30px; border-top:1px solid var(--line); }
.foot span { flex:1; }"""
    return shell(body, css)


for name, make in [("a-day-board", card_a), ("b-print-run", card_b), ("c-map-and-table", card_c)]:
    (HERE / f"{name}.html").write_text(make())
    print(name, len((HERE / f"{name}.html").read_text().splitlines()), "lines")
