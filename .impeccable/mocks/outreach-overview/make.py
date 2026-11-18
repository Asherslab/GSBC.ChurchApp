import base64, json, math, re
from pathlib import Path

HERE = Path(__file__).parent
KIT = HERE.parent / "design-system"
_t = (KIT / "icons.js").read_text()
ICONS = json.loads(_t[_t.index("{"):_t.rindex("}") + 1])
W, H = 812, 750

SCENES = {
    "s1": (-26.22, 152.70, 10.1),
    "s2": (-27.06, 152.98, 10.3),
    "s3": (-26.9, 152.7, 7.6),
    "s4": (-28.3, 151.6, 7.4),
}

CHURCH = (-27.34335, 152.96467)
KM_LAT = 1 / 110.574


def km_lon(lat):
    return 1 / (111.32 * math.cos(math.radians(lat)))


def project(scene, lat, lon):
    clat, clon, z = SCENES[scene]
    world = 512 * 2 ** z

    def xy(la, lo):
        s = math.sin(math.radians(la))
        return world * (lo + 180) / 360, world * (0.5 - math.log((1 + s) / (1 - s)) / (4 * math.pi))
    cx, cy = xy(clat, clon)
    x, y = xy(lat, lon)
    return x - cx + W / 2, y - cy + H / 2


def grid(centre, cell_km, rows, cols, origin_row=0, origin_col=0):
    lat0, lon0 = centre
    dlat, dlon = cell_km * KM_LAT, cell_km * km_lon(lat0)
    cells = {}
    for r in rows:
        for c in cols:
            north = lat0 + dlat / 2 - (r - origin_row) * dlat
            west = lon0 - dlon / 2 + (c - origin_col) * dlon
            cells[(r, c)] = (north - dlat, west, north, west + dlon)
    return cells


CHURCH_CELLS = grid(CHURCH, 8, range(-2, 3), range(-2, 3))
GYMPIE = (-26.19, 152.665)
GYMPIE_CELLS = grid(GYMPIE, 6.67, range(-1, 2), range(-1, 2))
GYMPIE_HOMES = {(-1, -1): "No homes", (-1, 0): "1,240", (-1, 1): "860", (0, -1): "1,910", (0, 0): "9,780", (0, 1): "2,350",
                (1, -1): "640", (1, 0): "3,120", (1, 1): "No homes"}
OFF = {(2, 1), (2, 2)}
WAITING = {(-3, 0)}
EDGE_HINT = {(-3, -1): ("Woodford", "about 2,300 letterboxes · 1.8 MB"), (-3, 1): ("Narangba and Burpengary", "about 11,600 letterboxes · 2.3 MB")}


def css_of(path):
    text = path.read_text()

    def embed(m):
        target = (path.parent / m.group(1)).resolve()
        if not target.exists():
            return m.group(0)
        kind = {"woff2": "font/woff2", "svg": "image/svg+xml"}.get(target.suffix[1:], "application/octet-stream")
        return 'url("data:' + kind + ';base64,' + base64.b64encode(target.read_bytes()).decode() + '")'

    text = re.sub(r'url\("?(\.\./(?:fonts|img)/[^")]+)"?\)', embed, text)
    return re.sub(r'@import url\("([^"]+)"\);', lambda m: css_of((path.parent / m.group(1)).resolve()), text)


def ic(name, cls="icon--sm"):
    return f'<svg class="icon {cls}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{ICONS.get(name, "")}</svg>'


def art(name):
    return "data:image/webp;base64," + base64.b64encode((HERE / "art" / f"{name}.webp").read_bytes()).decode()


NAV = [("Sundays & sermons", [("Sermons", "sermons"), ("Weekly digest", "megaphone")]),
       ("Calendar & news", [("Calendar", "calendar"), ("Announcements", "send")]),
       ("Ministries & groups", [("Life Groups", "book-open"), ("Ministries", "users"), ("Outreach", "footprints"), ("Prayer", "prayer")]),
       ("The app", [("App styling", "pencil"), ("Media", "image")]),
       ("People", [("Chats", "message-square"), ("People", "users")])]


def sidebar():
    def link(t, i, on=False):
        return f'<div class="module-nav__row"><a href="#" class="module-nav__link" {"aria-current=page" if on else ""}>{ic(i)}<span class="module-nav__title">{t}</span></a></div>'
    areas = ""
    for area, pages in NAV:
        op = any(t == "Outreach" for t, _ in pages)
        inner = "".join(link(t, i, t == "Outreach") for t, i in pages)
        areas += f'<div class="module-nav__group"><div class="module-nav__area {"module-nav__area--open" if op else ""}"><button class="module-nav__area-head" aria-expanded="{str(op).lower()}">{ic("chevron-right", "icon--sm module-nav__chevron")}<span>{area}</span><span class="module-nav__pages">{len(pages)}</span></button><div class="module-nav__area-pages" {"" if op else "hidden"}>{inner}</div></div></div>'
    return f'<aside class="admin-sidebar"><div class="admin-sidebar__head"><a class="admin-brand" href="#"><span class="brand-mark" style="width:28px"></span><span>Good Shepherd</span></a></div><div class="admin-sidebar__nav"><nav class="module-nav"><button class="module-nav__jump">{ic("search")}<span>Jump to…</span><kbd>⌘K</kbd></button><div class="module-nav__group">{link("Home", "home")}</div>{areas}</nav></div></aside>'


LOCAL = """
.ws{position:relative;height:750px;margin:0 calc(-1 * var(--wa-gutter, 40px));border-top:1px solid var(--line);overflow:hidden;display:flex}
.ws .map{position:relative;flex:1;min-width:0;overflow:hidden;background:#cde2e3}
.ground{position:absolute;left:50%;top:50%;width:812px;height:750px;transform:translate(-50%,-50%)}
.ground>img,.ground>svg{position:absolute;inset:0;width:812px;height:750px}
.attr{position:absolute;right:10px;bottom:8px;font-size:.6rem;color:var(--ink-3);background:rgb(247 244 238/.9);padding:2px 6px;border-radius:6px}
.float{position:absolute;background:var(--surface,#fff);border-radius:14px;box-shadow:0 2px 10px rgb(24 35 58/.18),0 0 0 1px var(--line)}
.zoom{right:12px;top:12px;display:grid;overflow:hidden}.zoom span{width:36px;height:36px;display:grid;place-items:center;border-bottom:1px solid var(--line);font-weight:600}
.zoom span.dim{color:var(--ink-3);opacity:.45}
.pane{background:var(--surface,#fff);padding:18px 20px;overflow:hidden;flex:none;position:relative;border-right:1px solid var(--line)}
.ph h2{font-size:var(--text-title)}.ph p{color:var(--ink-2);font-size:var(--text-caption);margin-top:4px;line-height:1.4}
.cap{color:var(--ink-3);font-size:var(--text-caption);line-height:1.4}
.back{display:inline-flex;align-items:center;gap:6px;font-size:var(--text-caption);font-weight:600;color:var(--ink-2);margin-bottom:12px}
.fld{margin-top:14px}.fld label{display:block;font-size:var(--text-caption);font-weight:600;color:var(--ink-2);margin-bottom:6px}
.inp{display:flex;align-items:center;gap:8px;height:40px;padding:0 12px;border-radius:var(--radius-field);background:var(--field);border:1px solid var(--field-edge);font-size:var(--text-label)}
.inp.typed{border-color:var(--accent);box-shadow:0 0 0 3px rgb(0 119 109/.18)}
.inp .u{margin-left:auto;color:var(--ink-3)}
.row{display:flex;align-items:center;gap:10px;padding:9px 0;border-bottom:1px solid var(--line);font-size:var(--text-label)}
.row b{font-weight:600}.row small{display:block;color:var(--ink-3);font-size:var(--text-caption);font-weight:400}
.row.on{background:var(--selected-fill);margin:0 -20px;padding-left:20px;padding-right:20px}
.est{margin-top:14px;padding:10px 12px;border-radius:var(--radius-field);background:var(--fill-on-surface);font-size:var(--text-label);line-height:1.4}
.dock{width:76px;flex:none;background:var(--surface,#fff);border-right:1px solid var(--line);display:flex;flex-direction:column;align-items:center;padding:10px 0;gap:4px}
.dock a{width:64px;display:grid;justify-items:center;gap:3px;padding:8px 0;border-radius:12px;color:var(--ink-2);font-size:.62rem;font-weight:600;text-align:center;line-height:1.15;position:relative}
.dock a.on{background:var(--selected-fill);color:var(--ink)}.dock a.on:before{content:"";position:absolute;left:-6px;top:10px;bottom:10px;width:3px;border-radius:2px;background:#e7ae74}
.dock .n{position:absolute;right:8px;top:4px;min-width:18px;height:18px;border-radius:9px;background:#e7ae74;color:#2a1a08;font-size:.6rem;display:grid;place-items:center;padding:0 4px}
.dock hr{width:40px;border:0;border-top:1px solid var(--line);margin:6px 0}
.foot{position:absolute;left:0;right:0;bottom:0;padding:12px 20px;background:var(--sand);border-top:1px solid var(--line);display:flex;justify-content:space-between;align-items:center;gap:10px}
.legend{left:12px;bottom:12px;padding:10px 14px;width:auto}
.legend h3{font-size:var(--text-caption);font-weight:700;margin-bottom:8px}
.legend li{display:flex;align-items:center;gap:10px;font-size:var(--text-caption);padding:3px 0;list-style:none;color:var(--ink-2)}
.legend li b{color:var(--ink);font-weight:600}
.sw{width:22px;height:14px;border-radius:3px;flex:none;border:1px solid var(--line)}
.chipbar{position:absolute;left:50%;top:14px;transform:translateX(-50%);display:flex;align-items:center;gap:10px;padding:6px 6px 6px 14px;border-radius:14px;background:#2a3d5e;color:#fff;font-size:var(--text-label);white-space:nowrap;box-shadow:0 6px 20px rgb(24 35 58/.28)}
.chipbar .wbtn{background:#fff;color:#18233a;border:0;border-radius:10px;padding:0 14px;height:32px;font-weight:600;display:inline-flex;align-items:center;gap:6px}
.note{position:absolute;padding:8px 12px;border-radius:10px;background:rgb(255 255 255/.94);box-shadow:0 0 0 1px var(--line);font-size:var(--text-caption);color:var(--ink-2);line-height:1.35;max-width:260px}
.note b{color:var(--ink);display:block;font-size:var(--text-label)}
.pop{padding:12px 14px;width:270px}
.pop h3{font-size:var(--text-label);font-weight:700}.pop p{font-size:var(--text-caption);color:var(--ink-2);margin:4px 0 10px;line-height:1.4}
.scene-head{padding:28px 40px 12px;max-width:900px}
.scene-head h2{font-size:var(--text-title);font-weight:700}.scene-head p{color:var(--ink-2);font-size:var(--text-label);margin-top:4px;line-height:1.45}
"""

STYLE = re.sub(r"\s*\n\s*", " ", css_of(KIT / "kit.css") + css_of(KIT / "parts.css") + LOCAL)
DEFS = ('<defs><pattern id="hatch" width="10" height="10" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
        '<line x1="0" y1="0" x2="0" y2="10" stroke="#626a79" stroke-width="1.2" opacity=".45"/></pattern>'
        '<pattern id="hatchd" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
        '<line x1="0" y1="0" x2="0" y2="7" stroke="#626a79" stroke-width="1.2" opacity=".55"/></pattern>'
        '<mask id="VEILID"><rect width="812" height="750" fill="#fff"/>{holes}</mask></defs>')


def rect(scene, box):
    s, w, n, e = box
    x1, y1 = project(scene, n, w)
    x2, y2 = project(scene, s, e)
    return x1, y1, x2 - x1, y2 - y1


def poly(scene, box):
    x, y, w, h = rect(scene, box)
    return f"M{x:.1f} {y:.1f}h{w:.1f}v{h:.1f}h{-w:.1f}z"


def built_cells():
    return {k: v for k, v in CHURCH_CELLS.items() if k not in OFF}


def ground(scene, layers):
    svg = "".join(layers).replace("VEILID", f"veil-{scene}")
    return f'<div class="ground"><img src="{art(scene)}" alt=""><svg viewBox="0 0 812 750">{svg}</svg></div>'


def label(scene, lat, lon, text, sub="", size=12, colour="#18233a", halo="#fff", weight=600):
    x, y = project(scene, lat, lon)
    t = f'<text x="{x:.0f}" y="{y:.0f}" text-anchor="middle" font-family="Montserrat" font-size="{size}" font-weight="{weight}" fill="{colour}" stroke="{halo}" stroke-width="3" paint-order="stroke">{text}</text>'
    if sub:
        t += f'<text x="{x:.0f}" y="{y + size + 2:.0f}" text-anchor="middle" font-family="Montserrat" font-size="{size - 2}" fill="#545e70" stroke="{halo}" stroke-width="3" paint-order="stroke">{sub}</text>'
    return t


def centre(box):
    s, w, n, e = box
    return (s + n) / 2, (w + e) / 2


def church_squares(scene, style):
    out = []
    for k, box in CHURCH_CELLS.items():
        x, y, w, h = rect(scene, box)
        if k in OFF:
            out.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="url(#hatch)" stroke="#626a79" stroke-width="1.4" stroke-dasharray="5 4"/>')
        elif style == "navy":
            out.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="rgb(42 61 94/.62)" stroke="#fff" stroke-width="1"/>')
    for k in WAITING:
        box = grid(CHURCH, 8, [k[0]], [k[1]])[k]
        x, y, w, h = rect(scene, box)
        out.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="rgb(0 119 109/.10)" stroke="#00776d" stroke-width="2" stroke-dasharray="6 4"/>')
    return out


def church_outline(scene, colour="#18233a", width=1.6, opacity=.7):
    b = built_cells()
    edges = []
    for (r, c), box in b.items():
        s, w, n, e = box
        if (r - 1, c) not in b:
            edges.append(((n, w), (n, e)))
        if (r + 1, c) not in b:
            edges.append(((s, w), (s, e)))
        if (r, c - 1) not in b:
            edges.append(((s, w), (n, w)))
        if (r, c + 1) not in b:
            edges.append(((s, e), (n, e)))
    out = []
    for (a, b2) in edges:
        x1, y1 = project(scene, *a)
        x2, y2 = project(scene, *b2)
        out.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="{colour}" stroke-width="{width}" stroke-linecap="round" opacity="{opacity}"/>')
    return out


def gympie_preview(scene, clear=False):
    out = []
    for k, box in GYMPIE_CELLS.items():
        x, y, w, h = rect(scene, box)
        empty = GYMPIE_HOMES[k] == "No homes"
        fill = "url(#hatch)" if empty else "rgb(0 119 109/.08)"
        out.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="{fill}" stroke="#00776d" stroke-width="2" stroke-dasharray="{"4 4" if empty else "none"}"/>')
    for k, box in GYMPIE_CELLS.items():
        la, lo = centre(box)
        out.append(label(scene, la, lo, GYMPIE_HOMES[k], size=11, colour="#00594f" if GYMPIE_HOMES[k] != "No homes" else "#626a79"))
    x, y = project(scene, *GYMPIE)
    out.append(f'<circle cx="{x:.0f}" cy="{y + 14:.0f}" r="7" fill="#2a3d5e" stroke="#fff" stroke-width="3"/>')
    return out


def veil_holes(scene, boxes):
    return "".join(f'<path d="{poly(scene, b)}" fill="#000"/>' for b in boxes)


def page(name, title, scenes):
    tabs = "".join(f'<button class="tab-strip__tab" role="tab" aria-selected="{str(t == "Map").lower()}">{t}{" <span class=pill>2</span>" if t == "Follow-ups" else ""}</button>' for t in ("Campaigns", "Map", "Days", "Numbers", "Follow-ups", "Settings"))
    body = ""
    for i, (head, lead, ws) in enumerate(scenes):
        if i:
            body += f'<div class="scene-head"><h2>{head}</h2><p>{lead}</p></div>'
        body += ws
    html = f"""<!doctype html>
<html lang="en" data-scheme="light"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Outreach map overview: {title}</title>
<style>{STYLE}</style></head>
<body><div class="admin-shell">{sidebar()}
<main class="admin-main"><header class="title-band"><div class="title-band__text"><h1 class="title-band__title">Outreach</h1></div><div class="title-band__actions"><button class="btn btn--quiet">{ic("map-pin")}Add a place</button><button class="btn btn--primary">{ic("plus")}Plan an outreach day</button></div></header>
<div class="tab-strip" role="tablist">{tabs}</div>
{body}
</main></div></body></html>
"""
    (HERE / f"{name}.html").write_text(html)


def dock(open_job):
    items = [("Coverage", "layers", ""), ("Places", "map-pin", ""), ("Saved areas", "shapes", ""), ("Reports", "flag", "3"), ("Map areas", "grid", "")]
    d = "".join(f'<a class="{"on" if t == open_job else ""}">{ic(i, "")}{t}{f"<span class=n>{n}</span>" if n else ""}</a>' for t, i, n in items)
    return f'<div class="dock">{d}<hr><a>{ic("calendar-days", "")}Plan a day</a></div>'


def adding_drawer(extra=""):
    return f"""<div class="pane" style="width:320px"><span class="back">{ic("arrow-left")}All map areas</span>
<div class="ph"><h2>Add a map area</h2><p>Find the town or suburb, or click the map. The area is cut into regions of about 10 km; once it's built you can switch regions off or add more on its edge.</p></div>
<div class="fld"><label>Find a town or suburb</label><div class="inp typed">{ic("search")}Gympie</div></div>
<div class="row on" style="margin-top:8px"><div><b>Gympie</b><small>Town · 130 km north of the church</small></div></div>
<div class="row"><div><b>Gympie South</b><small>Suburb · 128 km north</small></div></div>
<div class="fld"><label>How far around it</label><div class="inp">10<span class="u">km</span></div></div>
<div class="fld"><label>Name</label><div class="inp">Gympie</div></div>
<div class="est">7 regions with homes, about 19,900 letterboxes, about 13 MB for phones in all. 2 squares have no homes and aren't made.</div>
{extra}
<div class="foot"><button class="btn btn--quiet">Cancel</button><button class="btn btn--primary">Add and build it</button></div></div>"""


def coverage_drawer(note):
    return f"""<div class="pane" style="width:320px">
<div class="ph"><h2>Coverage</h2><p>Where walkers have letterboxed, newest first.</p></div>
<div class="fld"><label>Campaign</label><div class="inp">General outreach<span class="u">▾</span></div></div>
<div style="height:10px;border-radius:5px;background:linear-gradient(90deg,#00594f,#00776d 25%,#5fb3a9 60%,#cfe7e3 90%,#f7f4ee);border:1px solid var(--line);margin-top:16px"></div>
<div style="display:flex;justify-content:space-between" class="cap"><span>This week</span><span>Open again</span></div>
{note}</div>"""


def zoom(dim_in=False):
    return f'<div class="float zoom"><span class="{"dim" if dim_in else ""}">+</span><span>−</span></div>'


ATTR = '<span class="attr">© OpenStreetMap contributors · Protomaps</span>'


def legend_a():
    rows = [("background:#f7f4ee", "<b>Walkers have maps</b>", "Around the church, 23 regions"),
            ("background:rgb(0 119 109/.12);border:2px dashed #00776d", "<b>Waiting to build</b>", "maps in a few minutes"),
            ("background:repeating-linear-gradient(45deg,#f7f4ee 0 3px,#bfc3cb 3px 4px);border:1.5px dashed #626a79", "<b>Switched off</b>", "no maps; history kept"),
            ("background:#dfe1e5", "<b>Overview only</b>", "no walker maps yet"),
            ("background:#c9ccd3;border:1.5px dashed #626a79", "<b>Outside Queensland</b>", "can't be added: no addresses")]
    li = "".join(f'<li><span class="sw" style="{s}"></span><span>{a}</span></li>' for s, a, b in rows)
    return f'<div class="float legend"><h3>Where walkers have maps</h3><ul>{li}</ul></div>'


def card_a():
    veil = "rgb(98 106 121/.30)"
    holes1 = veil_holes("s1", list(built_cells().values()) + list(GYMPIE_CELLS.values()))
    s1 = [DEFS.format(holes=holes1), f'<rect width="812" height="750" fill="{veil}" mask="url(#VEILID)"/>']
    s1 += church_outline("s1") + church_squares("s1", "plain") + gympie_preview("s1")
    s1.append(label("s1", -26.02, 152.48, "Overview only", "no walker maps here yet", size=12, colour="#545e70"))
    ws1 = f'<div class="ws">{dock("Map areas")}{adding_drawer()}<div class="map">{ground("s1", s1)}{legend_a()}{zoom()}{ATTR}</div></div>'

    holes2 = veil_holes("s2", list(built_cells().values()))
    s2 = [DEFS.format(holes=holes2), f'<rect width="812" height="750" fill="{veil}" mask="url(#VEILID)"/>']
    s2 += church_outline("s2") + church_squares("s2", "plain")
    s2.append(label("s2", -26.93, 153.03, "Overview only", "zoom stops here: no streets in the overview", size=12, colour="#545e70"))
    note = '<div class="est" style="margin-top:18px">Coverage is drawn where walkers have maps. Out here there\'s only the overview.</div>'
    ws2 = f'<div class="ws">{dock("Coverage")}{coverage_drawer(note)}<div class="map">{ground("s2", s2)}{legend_a()}{zoom(dim_in=True)}{ATTR}</div></div>'

    nsw = "M0 462 L45 452 L110 465 L205 468 L265 500 L330 560 L345 612 L380 585 L410 540 L440 525 L500 520 L515 480 L495 435 L560 400 L600 380 L630 362 L660 375 L700 385 L760 380 L812 355 L812 750 L0 750 Z"
    s4 = [DEFS.format(holes=""), f'<rect width="812" height="750" fill="{veil}"/>', f'<path d="{nsw}" fill="rgb(98 106 121/.22)"/><path d="{nsw}" fill="url(#hatchd)"/>']
    s4.append('<text x="420" y="640" font-family="Montserrat" font-size="12" font-weight="600" fill="#18233a" stroke="#fff" stroke-width="3" paint-order="stroke">New South Wales: outside Queensland</text>')
    s4.append('<text x="420" y="656" font-family="Montserrat" font-size="10" fill="#545e70" stroke="#fff" stroke-width="3" paint-order="stroke">Areas can\'t be added here: there\'s no address data</text>')
    ws4 = f'<div class="ws">{dock("Map areas")}{adding_drawer()}<div class="map">{ground("s4", s4)}{legend_a()}{zoom()}{ATTR}</div></div>'

    page("ground", "The ground says where walkers have maps", [
        ("", "", ws1),
        ("At the edge of our maps, with Coverage open", "Pan anywhere in Australia. Land with walker maps is drawn clear; everywhere else sits under a grey veil, so the edge reads at a glance. Outside built areas you can zoom in only as far as the overview has detail (about suburb level); Coverage, Places, Reports and Plan a day say plainly that they work where walkers have maps.", ws2),
        ("At the Queensland border", "All of Australia is there to look at. Over the border the veil deepens and hatches: areas can't be added there, because the address data is Queensland's.", ws4),
    ])


def card_b():
    s3 = [DEFS.format(holes="")] + gympie_preview("s1")
    chip = f'<div class="chipbar">{ic("map")}<span><b>Australia view</b> · walkers have maps only in the navy areas</span><button class="wbtn">{ic("arrow-left")}Back to our maps</button></div>'
    ws1 = f'<div class="ws">{dock("Map areas")}{adding_drawer()}<div class="map">{ground("s1", s3)}{chip}{zoom(dim_in=True)}{ATTR}</div></div>'

    holes2 = veil_holes("s2", list(built_cells().values()))
    s2 = [DEFS.format(holes=holes2), '<rect width="812" height="750" fill="#f7f4ee" mask="url(#VEILID)"/><rect width="812" height="750" fill="url(#hatch)" mask="url(#VEILID)"/>']
    s2 += church_outline("s2", "#18233a", 1.6, .6) + church_squares("s2", "plain")
    x, y = project("s2", -26.98, 152.98)
    step = f'<div class="note" style="left:{x - 130:.0f}px;top:{y - 60:.0f}px"><b>Outside our maps</b>Keep dragging, or zoom out, and the map steps back to the Australia view.<div style="margin-top:8px"><button class="btn btn--quiet" style="height:30px">{ic("map")}Show all of Australia</button></div></div>'
    note = '<div class="est" style="margin-top:18px">Coverage, Places, Reports and Plan a day stay on our maps; the Australia view is for finding and placing areas.</div>'
    ws2 = f'<div class="ws">{dock("Coverage")}{coverage_drawer(note)}<div class="map"><div class="ground"><img src="{art("s2")}" alt=""><svg viewBox="0 0 812 750">{"".join(s2).replace("VEILID", "veil-s2")}</svg>{step}</div>{zoom()}{ATTR}</div></div>'

    s4 = [DEFS.format(holes=""), '<path d="M0 462 L45 452 L110 465 L205 468 L265 500 L330 560 L345 612 L380 585 L410 540 L440 525 L500 520 L515 480 L495 435 L560 400 L600 380 L630 362 L660 375 L700 385 L760 380 L812 355 L812 750 L0 750 Z" fill="rgb(98 106 121/.16)"/>']
    s4.append('<text x="300" y="660" font-family="Montserrat" font-size="12" font-weight="600" fill="#18233a" stroke="#fff" stroke-width="3" paint-order="stroke">Outside Queensland: areas can\'t be added</text>')
    ws4 = f'<div class="ws">{dock("Map areas")}{adding_drawer()}<div class="map">{ground("s4", s4)}{chip}{zoom(dim_in=True)}{ATTR}</div></div>'

    page("step-out", "Past the edge, the map steps back to Australia", [
        ("", "", ws1),
        ("At the edge of our maps, with Coverage open", "Our maps work as today, held to the built areas with the hatched edge. Drag on past it, zoom out, or press Show all of Australia, and the map eases back to the Australia view: built areas as navy blocks, zoom held at about suburb level, a navy bar to go back. Your idea, played straight.", ws2),
        ("At the Queensland border, in the Australia view", "The whole country is there; over the border is greyed, and areas can't be added there.", ws4),
    ])


def card_c():
    lat0, lon0 = CHURCH
    ring = grid(CHURCH, 8, range(-5, 6), range(-5, 6))
    holes1 = veil_holes("s1", list(built_cells().values()) + list(GYMPIE_CELLS.values()))
    s1 = [DEFS.format(holes=holes1), '<rect width="812" height="750" fill="rgb(98 106 121/.16)" mask="url(#VEILID)"/>']
    for k, box in ring.items():
        if k in CHURCH_CELLS:
            continue
        x, y, w, h = rect("s1", box)
        out = f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="none" stroke="#2a3d5e" stroke-width=".8" opacity=".28"/>'
        s1.append(out)
    s1 += church_outline("s1") + church_squares("s1", "plain") + gympie_preview("s1")
    ws1 = f'<div class="ws">{dock("Map areas")}{adding_drawer()}<div class="map">{ground("s1", s1)}{zoom()}{ATTR}</div></div>'

    holes2 = veil_holes("s2", list(built_cells().values()))
    s2 = [DEFS.format(holes=holes2), '<rect width="812" height="750" fill="rgb(98 106 121/.16)" mask="url(#VEILID)"/>']
    for k, box in ring.items():
        if k in CHURCH_CELLS or k in WAITING:
            continue
        x, y, w, h = rect("s2", box)
        touching = any((k[0] + dr, k[1] + dc) in built_cells() for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        s2.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="none" stroke="#2a3d5e" stroke-width="{1.2 if touching else .8}" opacity="{.5 if touching else .25}" stroke-dasharray="{"none" if touching else "3 3"}"/>')
        if touching and k not in EDGE_HINT:
            cx, cy = x + w / 2, y + h / 2
            s2.append(f'<circle cx="{cx:.0f}" cy="{cy:.0f}" r="13" fill="#fff" stroke="#2a3d5e" stroke-width="1.5"/><path d="M{cx - 5:.0f} {cy:.0f}h10M{cx:.0f} {cy - 5:.0f}v10" stroke="#2a3d5e" stroke-width="2"/>')
    for k, (name, facts) in EDGE_HINT.items():
        box = grid(CHURCH, 8, [k[0]], [k[1]])[k]
        x, y, w, h = rect("s2", box)
        hover = k == (-3, 1)
        s2.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="rgb(0 119 109/{.16 if hover else .06})" stroke="#00776d" stroke-width="{2.4 if hover else 1.4}"/>')
    s2 += church_outline("s2") + church_squares("s2", "plain")
    hx, hy, hw, hh = rect("s2", grid(CHURCH, 8, [-3], [1])[(-3, 1)])
    pop = f'<div class="float pop" style="left:{hx - 284:.0f}px;top:{hy - 40:.0f}px"><h3>Narangba and Burpengary</h3><p>A square on the edge of Around the church: about 11,600 letterboxes, about 2.3 MB for phones. Walkers have no maps here yet.</p><button class="btn btn--primary" style="height:32px">{ic("plus")}Add it to Around the church</button></div>'
    note = '<div class="est" style="margin-top:18px">Coverage is drawn where walkers have maps. The faint squares beyond are where Around the church could grow.</div>'
    ws2 = f'<div class="ws">{dock("Coverage")}{coverage_drawer(note)}<div class="map">{ground("s2", s2).replace("</svg></div>", "</svg>" + pop + "</div>")}{zoom(dim_in=True)}{ATTR}</div></div>'

    s4 = [DEFS.format(holes=""), '<rect width="812" height="750" fill="rgb(98 106 121/.16)"/>']
    nsw = "M0 462 L45 452 L110 465 L205 468 L265 500 L330 560 L345 612 L380 585 L410 540 L440 525 L500 520 L515 480 L495 435 L560 400 L600 380 L630 362 L660 375 L700 385 L760 380 L812 355 L812 750 L0 750 Z"
    s4.append(f'<path d="{nsw}" fill="url(#hatchd)"/>')
    s4.append('<text x="330" y="705" font-family="Montserrat" font-size="12" font-weight="600" fill="#18233a" stroke="#fff" stroke-width="3" paint-order="stroke">Outside Queensland: no squares, areas can\'t be added</text>')
    ws4 = f'<div class="ws">{dock("Map areas")}{adding_drawer()}<div class="map">{ground("s4", s4)}{zoom()}{ATTR}</div></div>'

    page("grid", "The grid runs on past the edge", [
        ("", "", ws1),
        ("At the edge of our maps, with Coverage open", "Pan anywhere. Past the edge, each area's grid carries on faintly over the overview, so you can see the squares it could grow into; those touching it carry a +. Point at one for its letterboxes and size, and add it from there, whichever drawer is out.", ws2),
        ("At the Queensland border", "The grid stops at the border; over it is hatched, and areas can't be added there.", ws4),
    ])


card_a()
card_b()
card_c()
