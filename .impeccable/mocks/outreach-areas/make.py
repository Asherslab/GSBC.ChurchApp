import csv
import math
from pathlib import Path

HERE = Path(__file__).parent
REGIONS = {}
for row in csv.reader((HERE / "regions.csv").read_text().splitlines()):
    if len(row) == 8 and row[0].startswith("church-"):
        REGIONS[row[0]] = dict(name=row[1], boxes=int(row[2]), bytes=int(row[3]), s=float(row[4]), w=float(row[5]), n=float(row[6]), e=float(row[7]))

ROWS, COLS = "abcde", range(1, 6)
STEP_LAT = 0.0718645
STEP_LON = 0.0809043
NORTH, WEST = -27.163687765001796, 152.76240915331815
EXTRA = {"church-e2": ("Brookfield and Kenmore", 21000, 3.1), "church-e3": ("Toowong and St Lucia", 88000, 7.4),
         "church-e4": ("Brisbane City and West End", 131000, 11.5), "church-e5": ("Coorparoo and Carindale", 97000, 7.9)}
OFF = {"church-e3", "church-e4", "church-e5"}
ADDING = {"church-f3", "church-f4"}
WATER = [(153.30, -27.10), (153.125, -27.10), (153.11, -27.18), (153.14, -27.22), (153.115, -27.30), (153.075, -27.315),
         (153.055, -27.36), (153.085, -27.40), (153.13, -27.42), (153.175, -27.45), (153.19, -27.55), (153.30, -27.60)]
RIVER = [(152.86, -27.53), (152.93, -27.50), (152.98, -27.52), (153.01, -27.48), (153.03, -27.47), (153.02, -27.50),
         (153.05, -27.46), (153.09, -27.43), (153.13, -27.42)]
PARKS = [(152.80, -27.36, 0.07, 0.10), (152.84, -27.25, 0.06, 0.07), (152.90, -27.43, 0.05, 0.04), (152.95, -27.17, 0.07, 0.05)]


def cell(id_):
    row = ord(id_.split("-")[1][0]) - ord("a")
    col = int(id_.split("-")[1][1:]) - 1
    n = NORTH - row * STEP_LAT
    w = WEST + col * STEP_LON
    return n - STEP_LAT, w, n, w + STEP_LON


def all_cells():
    for r in "abcdef":
        for c in range(0, 7):
            yield f"church-{r}{c}"


def facts(id_):
    if id_ in REGIONS:
        r = REGIONS[id_]
        return r["name"], r["boxes"], r["bytes"] / 1e6
    if id_ in EXTRA:
        return EXTRA[id_]
    return None


def mapsvg(width, height, box, mode="area", chosen=None, label=True, small=False):
    w0, e0, s0, n0 = box
    kx = math.cos(math.radians((s0 + n0) / 2))
    scale = min(width / ((e0 - w0) * kx), height / (n0 - s0))
    ox = (width - (e0 - w0) * kx * scale) / 2
    oy = (height - (n0 - s0) * scale) / 2

    def p(lon, lat):
        return round(ox + (lon - w0) * kx * scale, 1), round(oy + (n0 - lat) * scale, 1)

    out = [f'<svg viewBox="0 0 {width} {height}" width="{width}" height="{height}" role="img" aria-label="Map">',
           f'<rect width="{width}" height="{height}" fill="var(--map-land)"/>']
    for lon, lat, dw, dh in PARKS:
        x, y = p(lon, lat)
        x2, y2 = p(lon + dw, lat - dh)
        out.append(f'<rect x="{x}" y="{y}" width="{x2 - x}" height="{y2 - y}" rx="18" fill="var(--map-park)"/>')
    out.append('<path d="M' + " L".join("%s,%s" % p(*pt) for pt in WATER) + ' Z" fill="var(--map-water)"/>')
    out.append('<path d="M' + " L".join("%s,%s" % p(*pt) for pt in RIVER) + '" stroke="var(--map-water)" stroke-width="5" fill="none"/>')
    for k in range(9):
        a = p(152.78 + k * 0.05, -27.15)
        b = p(152.80 + k * 0.045, -27.55)
        out.append(f'<path d="M{a[0]},{a[1]} Q{(a[0] + b[0]) / 2 + 30},{(a[1] + b[1]) / 2} {b[0]},{b[1]}" stroke="var(--map-road)" stroke-width="{2.2 if k % 3 else 3.4}" fill="none"/>')
    for k in range(7):
        a = p(152.76, -27.18 - k * 0.055)
        b = p(153.12, -27.20 - k * 0.05)
        out.append(f'<path d="M{a[0]},{a[1]} Q{(a[0] + b[0]) / 2},{(a[1] + b[1]) / 2 + 18} {b[0]},{b[1]}" stroke="var(--map-road)" stroke-width="2" fill="none"/>')
    if mode == "area":
        for id_ in all_cells():
            s, w, n, e = cell(id_)
            x, y = p(w, n)
            x2, y2 = p(e, s)
            f = facts(id_)
            row, col = id_[7], int(id_[8:])
            inside = row in ROWS and 1 <= col <= 5
            if id_ in ADDING:
                cls, txt = "cell cell--adding", f
            elif inside and id_ in OFF:
                cls, txt = "cell cell--off", f
            elif inside and f:
                cls, txt = "cell cell--on" + (" cell--chosen" if id_ == chosen else ""), f
            elif not inside and (row in "abcdef") and (0 <= col <= 6):
                cls, txt = "cell cell--edge", None
            else:
                continue
            out.append(f'<rect class="{cls}" x="{x + 1.5}" y="{y + 1.5}" width="{x2 - x - 3}" height="{y2 - y - 3}" rx="6"/>')
            if label and txt and not small:
                name = txt[0].split(" and ")[0][:16]
                out.append(f'<text class="cl" x="{x + 10}" y="{y + 20}">{name}</text>')
                state = "Adding" if id_ in ADDING else ("Off" if id_ in OFF else f"{txt[2]:.1f} MB")
                out.append(f'<text class="cl cl--q" x="{x + 10}" y="{y + 36}">{txt[1]:,} · {state}</text>')
            elif label and cls == "cell cell--edge" and not small:
                out.append(f'<text class="cl cl--add" x="{(x + x2) / 2}" y="{(y + y2) / 2 + 5}" text-anchor="middle">+</text>')
    cx, cy = p(152.96467, -27.34335)
    out.append(f'<circle cx="{cx}" cy="{cy}" r="7" fill="var(--marker)" stroke="#fff" stroke-width="2.5"/>')
    out.append("</svg>")
    return "".join(out)


AREA_BOX = (152.72, 153.21, -27.67, -27.13)

CSS = """
:root { --navy:#2a3d5e; --aqua:#00776d; --live:#008176; --marker:#e7ae74; --marker-ink:#8a5422; --sand:#f4efe7; --sand-strong:#e9e1d4;
 --ink:#18233a; --ink-2:#545e70; --ink-3:#626a79; --line:rgb(42 61 94 / .12); --surface:#fff; --field:#f4efe7; --field-edge:rgb(98 106 121 / .7);
 --danger:#b4372f; --map-land:#f6f3ee; --map-water:#cfe0e6; --map-park:#dfe7d6; --map-road:#e4ddd1; }
* { box-sizing:border-box; margin:0; padding:0; }
body { font-family:Montserrat, -apple-system, system-ui, sans-serif; font-size:16px; color:var(--ink); background:var(--surface); width:1440px; height:900px; overflow:hidden; display:grid; grid-template-columns:248px 1fr; }
.side { background:var(--navy); color:#d5c9b7; padding:22px 18px; font-size:13.5px; }
.side b { display:block; color:#fff; font-size:15px; margin-bottom:26px; }
.side h6 { font-size:10.5px; letter-spacing:.06em; text-transform:uppercase; color:#d5c9b7; margin:18px 0 8px; }
.side a { display:block; padding:7px 10px; border-radius:8px; color:rgb(255 255 255 / .76); }
.side a.on { background:rgb(255 255 255 / .12); color:#fff; box-shadow:inset 3px 0 0 var(--marker); }
main { display:flex; flex-direction:column; min-width:0; height:900px; position:relative; }
.band { display:flex; justify-content:space-between; align-items:flex-end; padding:26px 32px 14px; }
.band h1 { font-size:27px; letter-spacing:-.02em; } .band p { color:var(--ink-2); font-size:14px; margin-top:4px; }
.crumb { font-size:13px; color:var(--aqua); font-weight:600; margin-bottom:6px; }
.tabs { display:flex; gap:24px; padding:0 32px; border-bottom:1px solid var(--line); font-size:14px; font-weight:600; color:var(--ink-3); }
.tabs span { padding:10px 0; } .tabs .on { color:var(--ink); box-shadow:inset 0 -3px 0 var(--marker); }
.btn { display:inline-flex; align-items:center; gap:8px; height:40px; padding:0 18px; border-radius:22px; background:var(--field); border:1px solid var(--field-edge); font:600 14px Montserrat, system-ui, sans-serif; color:var(--ink); white-space:nowrap; }
.btn--p { background:var(--navy); color:#fff; border-color:var(--navy); } .btn--sm { height:32px; padding:0 12px; font-size:13px; }
.pill { display:inline-flex; align-items:center; height:22px; padding:0 9px; border-radius:999px; background:var(--sand); color:var(--ink-2); font-size:11.5px; font-weight:700; white-space:nowrap; }
.pill--live { background:var(--live); color:#fff; } .pill--mark { color:var(--marker-ink); background:#fbeedf; } .pill--danger { color:var(--danger); background:#f8e4e2; }
.num { font-variant-numeric:tabular-nums; } .cap { font-size:12.5px; color:var(--ink-3); line-height:1.4; }
.bar { height:6px; border-radius:3px; background:var(--sand-strong); overflow:hidden; } .bar i { display:block; height:100%; background:var(--live); }
.field { display:flex; align-items:center; gap:10px; height:44px; padding:0 14px; border-radius:12px; background:var(--field); border:1px solid var(--field-edge); font-size:15px; }
.field b { font-weight:600; } .lbl { font-size:13.5px; font-weight:600; margin-bottom:6px; display:block; }
.sw { width:40px; height:24px; border-radius:12px; background:var(--live); position:relative; flex:none; } .sw::after { content:""; position:absolute; top:3px; right:3px; width:18px; height:18px; border-radius:50%; background:#fff; }
.sw--off { background:#c9c2b6; } .sw--off::after { right:auto; left:3px; }
.cell { stroke-width:1.5; } .cell--on { fill:rgb(0 129 118 / .10); stroke:var(--live); } .cell--chosen { fill:rgb(0 129 118 / .22); stroke:var(--navy); stroke-width:3; }
.cell--off { fill:rgb(255 255 255 / .55); stroke:var(--ink-3); stroke-dasharray:5 4; } .cell--edge { fill:transparent; stroke:rgb(98 106 121 / .35); stroke-dasharray:3 5; }
.cell--adding { fill:rgb(231 174 116 / .22); stroke:var(--marker-ink); stroke-width:2.5; }
.cl { font:600 12px Montserrat, system-ui, sans-serif; fill:var(--ink); } .cl--q { font-weight:500; font-size:11px; fill:var(--ink-2); } .cl--add { font-size:22px; fill:var(--ink-3); font-weight:500; }
.row { display:flex; align-items:center; gap:12px; padding:11px 0; border-top:1px solid var(--line); font-size:14px; }
.row .t { flex:1; min-width:0; } .row .t b { display:block; font-size:14.5px; font-weight:600; } .row .t span { font-size:12.5px; color:var(--ink-3); }
.ic { width:34px; height:34px; border-radius:10px; background:var(--sand); display:grid; place-items:center; color:var(--navy); font-weight:700; flex:none; }
.dots { width:32px; height:32px; border-radius:50%; display:grid; place-items:center; color:var(--ink-2); font-weight:700; flex:none; }
.foot { display:flex; justify-content:space-between; align-items:center; gap:12px; padding:12px 20px; background:var(--sand); }
.group { background:#fff; border-radius:14px; box-shadow:0 1px 2px rgb(24 35 58 / .06), 0 6px 18px rgb(24 35 58 / .06); overflow:hidden; }
.group .in { padding:16px 20px 18px; } .group h3 { font-size:18px; } .group .lead { font-size:13.5px; color:var(--ink-2); margin:4px 0 12px; }
"""


def shell(body):
    nav = [("Sundays & sermons", ["Sermons"]), ("Calendar & news", ["Calendar", "News"]),
           ("Ministries & groups", ["Life Groups", "Ministries", "Outreach", "Prayer"]), ("The app", ["Media", "App styling"])]
    side = "".join(f"<h6>{h}</h6>" + "".join(f'<a class="{"on" if x == "Outreach" else ""}">{x}</a>' for x in items) for h, items in nav)
    return f'<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Map areas</title><style>{CSS}</style></head><body><nav class="side"><b>GSBC Admin</b>{side}</nav><main>{body}</main></body></html>'


TABS = '<div class="tabs">' + "".join(f'<span class="{"on" if t == "Settings" else ""}">{t}</span>' for t in ["Campaigns", "Days", "Coverage", "Numbers", "Places", "Follow-ups", "Reports", "Settings"]) + "</div>"
BAND = '<div class="band"><div><h1>Outreach</h1><p>Campaigns, outreach days, coverage and settings</p></div></div>'


def area_rows():
    return ("<div class='row'><div class='ic'>▦</div><div class='t'><b>Around the church</b><span class='num'>22 regions on, 3 off · 829,000 letterboxes · 96 MB in all · Built 15 Nov</span></div><span class='pill'>Built</span><div class='dots'>⋯</div></div>"
            "<div class='row'><div class='ic'>▦</div><div class='t'><b>Gympie</b><span class='num'>Building Gympie and Southside (2 of 4) · started 4 minutes ago</span><div class='bar' style='width:260px;margin-top:6px'><i style='width:38%'></i></div></div><span class='pill pill--live'>Building</span><div class='dots'>⋯</div></div>")


GYMPIE = [("Gympie", -26.19, 152.665, "town"), ("Southside", -26.21, 152.645, "suburb"), ("Monkland", -26.21, 152.68, "suburb"),
          ("Victory Heights", -26.17, 152.64, "suburb"), ("Jones Hill", -26.23, 152.68, "suburb"), ("Two Mile", -26.17, 152.70, "suburb"),
          ("Chatsworth", -26.15, 152.62, "locality"), ("Glastonbury", -26.22, 152.52, "locality"), ("Kybong", -26.30, 152.71, "locality"),
          ("Curra", -26.08, 152.60, "locality"), ("Pie Creek", -26.24, 152.60, "locality"), ("The Dawn", -26.24, 152.71, "locality")]


def place_map(width, height):
    s0, w0, n0, e0 = -26.38, 152.44, -26.00, 152.89
    kx = math.cos(math.radians(-26.19))
    scale = min(width / ((e0 - w0) * kx), height / (n0 - s0))
    def p(lon, lat):
        return round((lon - w0) * kx * scale, 1), round((n0 - lat) * scale, 1)
    out = [f'<svg viewBox="0 0 {width} {height}" width="{width}" height="{height}" role="img" aria-label="Places around Gympie">', f'<rect width="{width}" height="{height}" fill="var(--map-land)"/>']
    river = [(152.55, -26.02), (152.62, -26.10), (152.66, -26.17), (152.67, -26.20), (152.70, -26.26), (152.76, -26.36)]
    out.append('<path d="M' + " L".join("%s,%s" % p(*pt) for pt in river) + '" stroke="var(--map-water)" stroke-width="6" fill="none"/>')
    lat_step, lon_step = 0.0898, 0.1003
    counts = [[0, 210, 0], [1800, 23500, 2900], [0, 4100, 900]]
    for r in range(3):
        for c in range(3):
            n = -26.19 + 1.5 * lat_step - r * lat_step; w = 152.665 - 1.5 * lon_step + c * lon_step
            x, y = p(w, n); x2, y2 = p(w + lon_step, n - lat_step)
            k = counts[r][c]
            cls = "cell cell--on" if k else "cell cell--off"
            out.append(f'<rect class="{cls}" x="{x+1.5}" y="{y+1.5}" width="{x2-x-3}" height="{y2-y-3}" rx="6"/>')
            out.append(f'<text class="cl cl--q" x="{x+10}" y="{y2-12}">{("about %s homes" % format(k, ",")) if k else "No homes, not made"}</text>')
    for name, lat, lon, kind in GYMPIE:
        x, y = p(lon, lat)
        r = 6 if kind == "town" else 3.5
        out.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="var(--navy)"/><text x="{x+8}" y="{y+4}" style="font:{"700 13px" if kind == "town" else "500 11px"} Montserrat, system-ui, sans-serif;fill:var(--ink)">{name}</text>')
    out.append("</svg>")
    return "".join(out)


def sheet_card():
    results = "".join(f"<div class='row' style='padding:9px 12px;border:0;{'background:var(--sand);border-radius:12px' if i == 0 else ''}'><div class='t'><b>{n}</b><span>{k}</span></div></div>"
                      for i, (n, k) in enumerate([("Gympie", "Town · 132 km north of church"), ("Gympie Terrace, Noosaville", "Locality · 128 km north"), ("East Deep Creek, near Gympie", "Locality · 136 km north")]))
    body = f"""{BAND}{TABS}
<div style="padding:22px 32px;display:grid;gap:18px;width:900px"><div class="group"><div class="in"><h3>Map areas</h3><p class="lead">Where the app has maps and streets.</p>{area_rows()}</div></div></div>
<div style="position:absolute;inset:0;background:rgb(24 35 58 / .42)"></div>
<div style="position:absolute;left:50%;top:30px;transform:translateX(-50%);width:860px;height:850px;background:#fff;border-radius:22px;display:flex;flex-direction:column;overflow:hidden">
 <div style="padding:20px 28px 10px"><h2 style="font-size:22px">Add a map area</h2><p class="cap" style="font-size:13.5px;margin-top:4px">Find the town or suburb. The area is cut into regions of about 10 km; you can switch regions off or add more once it's built.</p></div>
 <div style="display:grid;grid-template-columns:300px 1fr;gap:20px;padding:6px 28px 0;flex:1;min-height:0">
  <div><span class="lbl">Where</span><div class="field">⌕ <b>Gympie</b></div><div style="margin-top:8px">{results}</div>
   <span class="lbl" style="margin-top:18px">How far around it</span><div class="field num" style="justify-content:space-between"><b>15</b><span class="cap">km each way</span></div><p class="cap" style="margin-top:6px">From 1 to 30 km. Around the church is 20.</p>
   <span class="lbl" style="margin-top:18px">Name</span><div class="field"><b>Gympie</b></div></div>
  <div><div style="border-radius:14px;overflow:hidden;height:470px">{place_map(500, 470)}</div>
   <p class="cap num" style="margin-top:10px;font-size:13px"><b style="color:var(--ink)">5 regions with homes, about 32,000 letterboxes, about 9 MB to download in all.</b> 4 squares with no homes aren't made. There's no street map here yet; the places come from OpenStreetMap.</p></div>
 </div>
 <div class="foot"><span class="btn">Cancel</span><span class="btn btn--p">Add and build it</span></div>
</div>"""
    return shell(body)


def tiles_card():
    def tile(i):
        f = facts(i)
        if i in OFF:
            return f"<div class='tl tl--off'><b>{f[0]}</b><span class='num'>Off · {f[1]:,}</span></div>"
        if i in ADDING:
            return f"<div class='tl tl--add'><b>{f[0] if f else 'New region'}</b><span>Adding · about 6 MB</span></div>"
        if f:
            return f"<div class='tl'><b>{f[0]}</b><span class='num'>{f[1]:,} · {f[2]:.1f} MB</span></div>"
        return "<div class='tl tl--edge'>+</div>"
    grid = ""
    for r in "abcdef":
        for c in range(0, 7):
            i = f"church-{r}{c}"
            inside = r in ROWS and 1 <= c <= 5
            grid += tile(i) if inside or i in ADDING else ("<div class='tl tl--edge'>+</div>" if r in "abcdef" else "")
    css = """.tg { display:grid; grid-template-columns:repeat(7, 1fr); gap:6px; } .tl { height:62px; border-radius:10px; background:#e2efec; padding:8px 9px; font-size:11px; color:var(--ink-2); overflow:hidden; }
.tl b { display:block; font-size:11.5px; color:var(--ink); line-height:1.2; margin-bottom:3px; } .tl--off { background:#fff; border:1.5px dashed var(--ink-3); } .tl--add { background:#fbeedf; border:2px solid var(--marker-ink); }
.tl--edge { background:transparent; border:1.5px dashed rgb(98 106 121 / .35); display:grid; place-items:center; font-size:20px; color:var(--ink-3); }"""
    body = f"""<style>{css}</style>{BAND}{TABS}
<div style="padding:22px 32px;display:grid;grid-template-columns:1fr 340px;gap:22px;align-items:start;max-width:1192px">
<div class="group"><div class="in"><div style="display:flex;justify-content:space-between;align-items:flex-start"><div><h3>Map areas</h3><p class="lead">Where the app has maps and streets. Each square is a region phones download on its own.</p></div><span class="btn btn--sm">+ Add a map area</span></div>
<div style="display:flex;align-items:center;gap:10px;margin:4px 0 10px"><b style="font-size:15px">Around the church</b><span class="pill">Built 15 Nov</span><span class="cap num">22 on · 3 off · 96 MB</span><span style="flex:1"></span><div class="dots">⋯</div></div>
<div class="tg">{grid}</div>
<div style="display:flex;align-items:center;gap:10px;margin:16px 0 0;padding-top:12px;border-top:1px solid var(--line)"><b style="font-size:15px">Gympie</b><span class="pill pill--live">Building 2 of 4</span><div class="bar" style="width:200px"><i style="width:38%"></i></div><span style="flex:1"></span><div class="dots">⋯</div></div></div>
<div class="foot" style="flex-direction:column;align-items:stretch;gap:8px"><p class="cap" style="text-align:right">3 regions go from phones (26.8 MB); 2 build, about 12 MB. Carols 2026 uses Coorparoo &amp; Carindale.</p><div style="display:flex;justify-content:space-between"><span class="btn">Cancel</span><span class="btn btn--p">Switch off 3 and add 2</span></div></div></div>
<div class="group"><div class="in"><h3 style="font-size:16px">Brisbane City &amp; West End</h3><p class="lead">131,000 letterboxes · 11.5 MB · version 2</p><div style="border-radius:12px;overflow:hidden;height:230px">{mapsvg(300, 230, (152.98, 153.08, -27.53, -27.43), mode="plain")}</div><p class="cap" style="margin-top:10px">Switched off. Phones dropped it; coverage and history there stay.</p></div></div>
</div>"""
    return shell(body)


def desk_card():
    rows = ""
    groups = [("On", ["church-c3", "church-c4", "church-d4", "church-d5"], ""), ("Switching off", sorted(OFF), "off"), ("Adding", sorted(ADDING), "add")]
    for title, ids, kind in groups:
        rows += f"<div class='cap' style='margin:14px 0 2px;font-weight:600;color:var(--ink-2)'>{title}</div>"
        for i in ids:
            f = facts(i) or ("Mount Gravatt and Holland Park" if i == "church-f3" else "Carindale and Belmont", 64000, 5.9)
            extra = " · used by Carols 2026" if i == "church-e5" else ""
            rows += f"<div class='row'><div class='t'><b>{f[0]}</b><span class='num'>{f[1]:,} letterboxes · {'about ' if kind == 'add' else ''}{f[2]:.1f} MB{extra}</span></div><div class='sw{' sw--off' if kind == 'off' else ''}'></div></div>"
    body = f"""<div class="band" style="padding-bottom:12px"><div><div class="crumb">Outreach · Settings · Map areas</div><h1>Around the church</h1><p class="num">25 regions · 829,000 letterboxes · 96 MB in all · built 15 Nov</p></div><div style="display:flex;gap:10px"><span class="btn">Rebuild</span><div class="dots" style="width:40px;height:40px;background:var(--field)">⋯</div></div></div>
<div style="flex:1;display:grid;grid-template-columns:1fr 460px;border-top:1px solid var(--line);min-height:0">
 <div style="position:relative;overflow:hidden">{mapsvg(732, 760, (152.70, 153.24, -27.70, -27.10), chosen="church-c3")}
  <div style="position:absolute;left:16px;top:16px;background:#fff;border-radius:12px;padding:8px;box-shadow:0 6px 18px rgb(24 35 58 / .16);display:grid;gap:4px;font-size:13px;font-weight:600"><span style="padding:6px 10px;border-radius:9px;background:var(--navy);color:#fff">Switch regions</span><span style="padding:6px 10px">Add on the edge</span></div>
  <div style="position:absolute;left:50%;top:16px;transform:translateX(-50%);background:#fff;border-radius:999px;padding:8px 16px;box-shadow:0 6px 18px rgb(24 35 58 / .16);font-size:13px">Click a region to switch it off or on</div>
  <div style="position:absolute;right:16px;top:16px;background:#fff;border-radius:12px;padding:10px 14px;box-shadow:0 6px 18px rgb(24 35 58 / .16);font-size:12.5px;width:230px"><b style="font-size:13.5px">Albany Creek &amp; Bridgeman Downs</b><br><span class="num">56,803 letterboxes · 5.2 MB · version 3</span></div>
 </div>
 <aside style="display:flex;flex-direction:column;border-left:1px solid var(--line);min-height:0">
  <div style="padding:18px 24px;flex:1;overflow:hidden"><div style="display:flex;align-items:center;gap:10px"><span class="pill">Built</span><span class="cap">Streets, addresses and maps from 15 Nov</span></div>
  <div style="margin-top:12px;padding:12px 14px;border-radius:12px;background:#fbeedf;font-size:13px;line-height:1.45"><b>Carols 2026 uses Coorparoo &amp; Carindale.</b> Its outreach day on 6 Dec plans routes there; walkers would have no map. Keep it on, or plan the day elsewhere.</div>
  {rows}</div>
  <div class="foot" style="flex-direction:column;align-items:stretch;gap:8px"><p class="cap" style="text-align:right">3 regions go from phones, 26.8 MB. 2 build, about 12 MB, in about 4 minutes.</p><div style="display:flex;justify-content:space-between"><span class="btn">Cancel</span><span class="btn btn--p">Switch off 3 and add 2</span></div></div>
 </aside>
</div>"""
    return shell(body)


(HERE / "a-area-sheet.html").write_text(sheet_card())
(HERE / "b-region-tiles.html").write_text(tiles_card())
(HERE / "c-map-desk.html").write_text(desk_card())
