import base64, json, math, random, re
from pathlib import Path

HERE = Path(__file__).parent
KIT = HERE.parent / "design-system"
_t = (KIT / "icons.js").read_text()
ICONS = json.loads(_t[_t.index("{"):_t.rindex("}") + 1])


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
.ws{position:relative;height:calc(900px - 150px);margin:0 calc(-1 * var(--wa-gutter, 40px));border-top:1px solid var(--line);overflow:hidden;display:flex}
.ws .map{position:relative;flex:1;min-width:0;overflow:hidden;background:#f7f4ee}
.map>svg{position:absolute;inset:0;width:100%;height:100%}
.attr{position:absolute;right:10px;bottom:8px;font-size:.6rem;color:var(--ink-3);background:rgb(247 244 238/.9);padding:2px 6px;border-radius:6px}
.float{position:absolute;background:var(--surface,#fff);border-radius:14px;box-shadow:0 2px 10px rgb(24 35 58/.18),0 0 0 1px var(--line)}
.zoom{right:12px;top:12px;display:grid;overflow:hidden}.zoom span{width:36px;height:36px;display:grid;place-items:center;border-bottom:1px solid var(--line)}
.pane{background:var(--surface,#fff);border-left:1px solid var(--line);padding:18px 20px;overflow:hidden;flex:none;position:relative}
.pane--l{border-left:0;border-right:1px solid var(--line)}
.ph{display:flex;justify-content:space-between;align-items:flex-start;gap:10px;margin-bottom:12px}.ph h2{font-size:var(--text-title)}.ph p{color:var(--ink-2);font-size:var(--text-caption);margin-top:2px}
.lbl{font-size:var(--text-caption);font-weight:600;color:var(--ink-3);margin:16px 0 8px}
.cap{color:var(--ink-3);font-size:var(--text-caption);line-height:1.35}
.sel{display:flex;align-items:center;justify-content:space-between;height:38px;padding:0 12px;border-radius:var(--radius-field);background:var(--field);border:1px solid var(--field-edge);font-size:var(--text-label);font-weight:600}
.ramp{height:10px;border-radius:5px;background:linear-gradient(90deg,#00594f,#00776d 25%,#5fb3a9 60%,#cfe7e3 90%,#f7f4ee);border:1px solid var(--line)}
.rk{display:flex;justify-content:space-between;font-size:var(--text-caption);color:var(--ink-3);margin-top:4px}
.figs{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin:12px 0}.fig{padding:9px 12px;border-radius:var(--radius-field);background:var(--fill-on-surface)}
.fig b{display:block;font-size:var(--text-title);font-variant-numeric:tabular-nums}.fig span{font-size:var(--text-caption);color:var(--ink-2)}
.row{display:flex;align-items:center;gap:10px;padding:9px 0;border-bottom:1px solid var(--line);font-size:var(--text-label)}
.row b{font-weight:600}.row small{display:block;color:var(--ink-3);font-size:var(--text-caption);font-weight:400}.row .r{margin-left:auto;color:var(--ink-3);font-size:var(--text-caption);font-variant-numeric:tabular-nums;text-align:right}
.row.on{background:var(--selected-fill);margin:0 -20px;padding-left:20px;padding-right:20px}
.sw{width:34px;height:20px;border-radius:999px;background:#2a3d5e;position:relative;flex:none}.sw:after{content:"";position:absolute;right:3px;top:3px;width:14px;height:14px;border-radius:50%;background:#fff}
.sw.off{background:var(--field-edge)}.sw.off:after{right:auto;left:3px}
.search{display:flex;align-items:center;gap:8px;height:40px;padding:0 12px;border-radius:var(--radius-field);background:var(--field);border:1px solid var(--field-edge);font-size:var(--text-label);color:var(--ink-3)}
.search.typed{color:var(--ink);border-color:var(--accent);box-shadow:0 0 0 3px rgb(0 119 109/.18)}
.bar{display:flex;align-items:center;gap:8px;padding:6px 6px 6px 16px;border-radius:14px;background:#2a3d5e;color:#fff;font-size:var(--text-label);white-space:nowrap;box-shadow:0 6px 20px rgb(24 35 58/.28)}
.bar .btn{height:34px}.bar .wbtn{background:#fff;color:#18233a;border:0;border-radius:10px;padding:0 14px;height:34px;font-weight:600;display:inline-flex;align-items:center;gap:6px}
.bar .gbtn{background:rgb(255 255 255/.12);color:#fff;border:0;border-radius:10px;padding:0 12px;height:34px;font-weight:600;display:inline-flex;align-items:center;gap:6px}
.dock{width:76px;flex:none;background:var(--surface,#fff);border-right:1px solid var(--line);display:flex;flex-direction:column;align-items:center;padding:10px 0;gap:4px}
.dock a{width:64px;display:grid;justify-items:center;gap:3px;padding:8px 0;border-radius:12px;color:var(--ink-2);font-size:.62rem;font-weight:600;text-align:center;line-height:1.15;position:relative}
.dock a.on{background:var(--selected-fill);color:var(--ink)}.dock a.on:before{content:"";position:absolute;left:-6px;top:10px;bottom:10px;width:3px;border-radius:2px;background:#e7ae74}
.dock .n{position:absolute;right:8px;top:4px;min-width:18px;height:18px;border-radius:9px;background:#e7ae74;color:#2a1a08;font-size:.6rem;display:grid;place-items:center;padding:0 4px}
.dock hr{width:40px;border:0;border-top:1px solid var(--line);margin:6px 0}
.fold{position:absolute;top:14px;width:26px;height:26px;border-radius:8px;display:grid;place-items:center;color:var(--ink-3)}
.tool{display:grid;gap:2px;padding:4px;left:12px;top:12px}.tool span{width:36px;height:36px;display:grid;place-items:center;border-radius:10px;color:var(--ink)}.tool span.on{background:#2a3d5e;color:#fff}.tool hr{border:0;border-top:1px solid var(--line);margin:2px 4px}
.chip{display:inline-flex;align-items:center;gap:6px;height:24px;padding:0 10px;border-radius:999px;background:var(--fill-on-surface);font-size:var(--text-caption);font-weight:600;color:var(--ink-2)}
.pill{display:inline-flex;align-items:center;height:20px;padding:0 8px;border-radius:999px;background:#e7ae74;color:#2a1a08;font-size:var(--text-pill);font-weight:700}
.sec{border-bottom:1px solid var(--line)}.sec>.h{display:flex;align-items:center;gap:10px;padding:12px 0;font-size:var(--text-label);font-weight:600}.sec>.h .r{margin-left:auto;color:var(--ink-3);font-weight:400;font-size:var(--text-caption)}
.photo{height:120px;border-radius:var(--radius-field);background:linear-gradient(135deg,#9aa58f,#c9c0ae 60%,#7d8a77);position:relative;overflow:hidden}
.photo:after{content:"";position:absolute;left:30%;top:35%;width:34%;height:44%;background:#5a4a3a;border-radius:4px 4px 0 0;box-shadow:inset 0 -18px 0 #3e3226}
.btn--block{width:100%;justify-content:center}
.foot{position:absolute;left:0;right:0;bottom:0;padding:12px 20px;background:var(--sand);border-top:1px solid var(--line);display:flex;justify-content:space-between;align-items:center;gap:10px}
"""

STYLE = re.sub(r"\s*\n\s*", " ", css_of(KIT / "kit.css") + css_of(KIT / "parts.css") + LOCAL)
AGES = ["#00594f", "#00776d", "#2f9488", "#5fb3a9", "#9fd0c8", "#cfe7e3"]


def streets(w, h, seed=7, lasso=False, drawing=False, reports=True, zones=True, chosen=None):
    rng = random.Random(seed)
    o = [f'<svg viewBox="0 0 {w} {h}" preserveAspectRatio="xMidYMid slice">']
    o.append(f'<path d="M{w*.64} -10 C {w*.6} {h*.3},{w*.72} {h*.55},{w*.68} {h+10}" stroke="#cde2e3" stroke-width="16" fill="none"/>')
    o.append(f'<rect x="{w*.08}" y="{h*.64}" width="{w*.16}" height="{h*.22}" rx="18" fill="#d9e6cf"/><rect x="{w*.76}" y="{h*.1}" width="{w*.12}" height="{h*.14}" rx="10" fill="#ebe2d2"/>')
    lines = []
    for row in range(-1, int(h / 46) + 2):
        y0, x = row * 46 + rng.uniform(-8, 8), -20
        while x < w + 20:
            seg, y1 = rng.uniform(70, 150), y0 + rng.uniform(-14, 14)
            lines.append((x, y0, x + seg, y1)); x += seg; y0 = y1
    for col in range(-1, int(w / 92) + 2):
        x0, y = col * 92 + rng.uniform(-14, 14), -20
        while y < h + 20:
            seg = rng.uniform(46, 120)
            if rng.random() < .7:
                lines.append((x0, y, x0 + rng.uniform(-10, 10), y + seg))
            y += seg
    for c, sw in (("#ddd5c7", 7), ("#ffffff", 5)):
        o.extend(f'<line x1="{a:.0f}" y1="{b:.0f}" x2="{c2:.0f}" y2="{d:.0f}" stroke="{c}" stroke-width="{sw}" stroke-linecap="round"/>' for a, b, c2, d in lines)
    cx, cy = w * .42, h * .48
    for a, b, c2, d in lines:
        dd = math.hypot((a + c2) / 2 - cx, ((b + d) / 2 - cy) * 1.3)
        if dd < w * .32 and rng.random() < .82:
            age = min(5, int(dd / (w * .055)) + rng.choice([0, 0, 1]))
            o.append(f'<line x1="{a:.0f}" y1="{b:.0f}" x2="{c2:.0f}" y2="{d:.0f}" stroke="{AGES[age]}" stroke-width="4.5" stroke-linecap="round"/>')
    o.append(f'<path d="M -10 {h*.3} C {w*.3} {h*.26},{w*.6} {h*.36},{w+10} {h*.31}" stroke="#a99c88" stroke-width="12" fill="none"/><path d="M -10 {h*.3} C {w*.3} {h*.26},{w*.6} {h*.36},{w+10} {h*.31}" stroke="#fbf6ee" stroke-width="9" fill="none"/>')
    o.append(f'<path d="M {w*.93} -10 L {w*.93} {h+10}" stroke="#18233a" stroke-width="1.2"/><rect x="{w*.93}" y="0" width="{w*.07+2}" height="{h}" fill="url(#hatch)"/>')
    o.append('<defs><pattern id="hatch" width="10" height="10" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="10" height="10" fill="#f7f4ee"/><line x1="0" y1="0" x2="0" y2="10" stroke="#626a79" stroke-width="1.2" opacity=".5"/></pattern></defs>')
    if zones:
        for zx, zy, zw, zh in ((.14, .12, .1, .12), (.56, .7, .08, .1)):
            o.append(f'<rect x="{w*zx}" y="{h*zy}" width="{w*zw}" height="{h*zh}" rx="6" fill="rgb(180 55 47/.12)" stroke="#b4372f" stroke-width="2" stroke-dasharray="6 4"/>')
    for i in range(12):
        px, py = rng.uniform(w * .15, w * .62), rng.uniform(h * .35, h * .9)
        o.append(f'<circle cx="{px:.0f}" cy="{py:.0f}" r="4.5" fill="#2a3d5e" stroke="#fff" stroke-width="1.5"/>')
    if reports:
        for i, (px, py) in enumerate([(w * .46, h * .66), (w * .3, h * .22), (w * .7, h * .5)]):
            ring = '<circle r="17" fill="none" stroke="#18233a" stroke-width="2"/>' if chosen == "report" and i == 0 else ""
            o.append(f'<g transform="translate({px:.0f} {py:.0f})">{ring}<circle r="12" fill="#e7ae74" stroke="#fff" stroke-width="2"/><path d="M-3 -5 v10 M-3 -5 h7 l-2 3 l2 3 h-7" stroke="#2a1a08" stroke-width="1.6" fill="none"/></g>')
    if lasso:
        o.append(f'<path d="M {w*.4} {h*.46} C {w*.48} {h*.36},{w*.6} {h*.42},{w*.58} {h*.6} S {w*.42} {h*.76},{w*.4} {h*.46} Z" fill="rgb(42 61 94/.10)" stroke="#18233a" stroke-width="2" stroke-dasharray="7 5"/>')
    if drawing:
        pts = [(w * .2, h * .44), (w * .3, h * .4), (w * .34, h * .56), (w * .25, h * .62)]
        p = " ".join(f"{x:.0f},{y:.0f}" for x, y in pts)
        o.append(f'<polyline points="{p}" fill="rgb(180 55 47/.12)" stroke="#b4372f" stroke-width="2.5"/><line x1="{pts[-1][0]:.0f}" y1="{pts[-1][1]:.0f}" x2="{w*.21:.0f}" y2="{h*.55:.0f}" stroke="#b4372f" stroke-width="2" stroke-dasharray="5 4"/>')
        o.extend(f'<circle cx="{x:.0f}" cy="{y:.0f}" r="{7 if i == 0 else 5}" fill="#fff" stroke="#b4372f" stroke-width="2.5"/>' for i, (x, y) in enumerate(pts))
    o.append("</svg>")
    return "".join(o)


def zoom():
    return '<div class="float zoom"><span>+</span><span>−</span></div>'


ATTR = '<span class="attr">© OpenStreetMap contributors · Protomaps · Queensland Government</span>'


def page(name, body):
    tabs = "".join(f'<button class="tab-strip__tab" role="tab" aria-selected="{str(t == "Map").lower()}">{t}{" <span class=pill>2</span>" if t == "Follow-ups" else ""}</button>' for t in ("Campaigns", "Map", "Days", "Numbers", "Follow-ups", "Settings"))
    html = f"""<!doctype html>
<html lang="en" data-scheme="light"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Outreach map: {name}</title>
<style>{STYLE}</style></head>
<body style="height:900px;overflow:hidden"><div class="admin-shell">{sidebar()}
<main class="admin-main"><header class="title-band"><div class="title-band__text"><h1 class="title-band__title">Outreach</h1></div><div class="title-band__actions"><button class="btn btn--quiet">{ic("map-pin")}Add a place</button><button class="btn btn--primary">{ic("plus")}Plan an outreach day</button></div></header>
<div class="tab-strip" role="tablist">{tabs}</div>
{body}
</main></div></body></html>
"""
    (HERE / f"{name}.html").write_text(html)


def dock():
    items = [("Coverage", "layers", False, ""), ("Places", "map-pin", True, ""), ("Saved areas", "shapes", False, ""), ("Reports", "flag", False, "3"), ("Map areas", "grid", False, "")]
    d = "".join(f'<a class="{"on" if on else ""}">{ic(i, "")}{t}{f"<span class=n>{n}</span>" if n else ""}</a>' for t, i, on, n in items)
    d += f'<hr><a>{ic("calendar-days", "")}Plan a day</a>'
    rows = [("Bunya Rd bushland", "Area · Not homes", False), ("Albany Hills school", "Area · School grounds", False),
            ("Kingfisher Village", "Area · Retirement village, asked us", False), ("14 Kingfisher St", "Address · Asked not to", False),
            ("7 Wren Ct", "Address · Dog loose", False), ("22 Ellis Rd", "No junk mail", False), ("3 Pine Ct", "Hostile", False)]
    r = "".join(f'<div class="row"><div><b>{a}</b><small>{b}</small></div></div>' for a, b, _ in rows)
    drawer = f"""<div class="pane pane--l" style="width:320px"><span class="fold" style="right:10px">{ic("chevron-left")}</span>
<div class="ph"><div><h2>Places outreach skips</h2><p>Walkers never get these on a route.</p></div></div>
<div class="search">{ic("search")}Type an address to skip…</div>
<div style="display:flex;gap:6px;margin:12px 0 4px;flex-wrap:wrap"><span class="chip" style="background:#2a3d5e;color:#fff">Everything 23</span><span class="chip">Areas 9</span><span class="chip">Addresses 14</span><span class="chip">No junk mail 212</span></div>
{r}<p class="cap" style="margin-top:12px">Or draw an area on the map: {ic("pencil")} Draw a skip area</p></div>"""
    card = f"""<div class="float" style="right:60px;top:74px;width:330px;padding:16px 18px"><div class="ph"><div><h2>Report: not a home</h2><p>Bunya Rd · from a walk on Saturday</p></div><span>{ic("ellipsis")}</span></div>
<div class="photo"></div><p style="font-size:var(--text-label);margin:10px 0">"Driveway goes to a water treatment gate, no letterbox."</p>
<button class="btn btn--primary btn--block">Never deliver there</button><div style="display:flex;gap:8px;margin-top:8px"><button class="btn btn--quiet" style="flex:1;justify-content:center">Skip an area round it</button><button class="btn btn--quiet" style="flex:1;justify-content:center">Dismiss it</button></div></div>"""
    bar = f'<div style="position:absolute;left:50%;top:14px;transform:translateX(-50%)" class="bar">{ic("pencil")}<span><b>Skip area:</b> click each corner</span><button class="gbtn">{ic("undo")}Undo last point</button><button class="gbtn">Cancel</button><button class="wbtn">{ic("check")}Finish</button></div>'
    legend = '<div class="float" style="left:12px;bottom:12px;padding:10px 14px;width:300px"><div style="display:flex;justify-content:space-between;font-size:var(--text-caption);font-weight:600"><span>General outreach</span><span style="color:var(--ink-3)">1,912 of 4,338 streets</span></div><div class="ramp" style="margin-top:6px"></div><div class="rk"><span>This week</span><span>Open again</span></div></div>'
    body = f'<div class="ws"><div class="dock">{d}</div>{drawer}<div class="map">{streets(1000, 750, drawing=True)}{bar}{card}{legend}{zoom()}{ATTR}</div></div>'
    page("dock", body)


def search():
    res = f"""<div class="float" style="left:12px;top:12px;width:400px;padding:10px"><div style="display:flex;gap:8px"><div class="search typed" style="flex:1">{ic("search")}14 Kingfi<span style="border-left:2px solid #18233a;height:18px"></span></div><button class="btn btn--quiet">{ic("layers")}Layers</button></div>
<div class="lbl" style="margin:12px 6px 4px">Addresses</div>
<div class="row on" style="margin:0;padding:9px 8px;border-radius:10px">{ic("map-pin")}<div><b>14 Kingfisher St, Albany Creek</b><small>No junk mail since 3 Oct · done 12 days ago</small></div></div>
<div class="row" style="padding:9px 8px">{ic("map-pin")}<div><b>14 Kingfisher Ct, Bridgeman Downs</b><small>Done 40 days ago</small></div></div>
<div class="lbl" style="margin:10px 6px 4px">Streets</div><div class="row" style="padding:9px 8px">{ic("footprints")}<div><b>Kingfisher St, Albany Creek</b><small>38 letterboxes · both sides done</small></div></div>
<div class="lbl" style="margin:10px 6px 4px">Saved areas</div><div class="row" style="padding:9px 8px;border:0">{ic("shapes")}<div><b>Kingfisher Village loop</b><small>Saved area · 212 letterboxes</small></div></div></div>"""
    layers = [("Coverage: General outreach", True, ""), ("Places outreach skips", True, "23"), ("No junk mail and hostile", True, "214"), ("Walkers' reports", True, "3 new"), ("Saved areas", False, "6"), ("Map areas and regions", False, "25")]
    lay = "".join(f'<div class="row" style="padding:8px 0"><span class="sw {"" if on else "off"}"></span><span>{t}</span><span class="r">{n}</span></div>' for t, on, n in layers)
    pop = f'<div class="float" style="left:422px;top:12px;width:290px;padding:12px 16px"><div style="font-weight:700;font-size:var(--text-label);margin-bottom:4px">Show on the map</div>{lay}<p class="cap" style="margin-top:8px">Kept as you left it on this computer.</p></div>'
    figs = '<div class="float" style="left:12px;bottom:12px;padding:10px 14px;display:flex;gap:18px;align-items:center;font-size:var(--text-caption)"><div class="ramp" style="width:120px"></div><span><b style="font-size:var(--text-label)">1,912</b> of 4,338 streets</span><span><b style="font-size:var(--text-label)">44%</b> of letterboxes in view</span></div>'
    pane = f"""<div class="pane" style="width:400px"><span class="fold" style="left:-13px;background:#fff;box-shadow:0 0 0 1px var(--line)">{ic("chevron-right")}</span>
<div class="ph"><div><h2>14 Kingfisher St</h2><p>Albany Creek · 1 letterbox, left side</p></div><span>{ic("ellipsis")}</span></div>
<div class="row"><div><b>No junk mail</b><small>Marked by a walker on 3 Oct</small></div><span class="r">Clear in ⋯</span></div>
<div class="row"><div><b>General outreach</b><small>Done 12 days ago, on a walk from home</small></div></div>
<div class="row"><div><b>Christmas 2026</b><small>Not done yet</small></div></div>
<div class="lbl">Skip this address</div><p class="cap">Walkers won't get it on any route. Say why, and whether they see a Don't letterbox marker.</p>
<div class="foot"><button class="btn btn--quiet">Street not done</button><button class="btn btn--primary">Skip this address</button></div></div>"""
    body = f'<div class="ws"><div class="map">{streets(1100, 750, seed=11, chosen="report")}{res}{pop}{figs}{zoom()}{ATTR}</div>{pane}</div>'
    page("search", body)


def layers():
    cov = """<div class="sec"><div class="h"><span class="sw"></span>Coverage<span class="r">open</span></div>
<div class="sel">General outreach<span>▾</span></div><div class="ramp" style="margin-top:12px"></div><div class="rk"><span>This week</span><span>6 weeks</span><span>Open again</span></div>
<div class="figs"><div class="fig"><b>1,912</b><span>of 4,338 streets</span></div><div class="fig"><b>44%</b><span>of letterboxes in view</span></div></div>
<p class="cap" style="margin-bottom:12px">Click a street, or Draw round streets, to mark them not done.</p></div>"""
    closed = [("Places outreach skips", "23", True), ("No junk mail and hostile", "214", True), ("Saved areas", "6", False), ("Walkers' reports", "3 new", True), ("Map areas", "25 regions", False)]
    cl = "".join(f'<div class="sec"><div class="h"><span class="sw {"" if on else "off"}"></span>{t}<span class="r">{n} ›</span></div></div>' for t, n, on in closed)
    left = f'<div class="pane pane--l" style="width:320px"><span class="fold" style="right:10px">{ic("chevron-left")}</span><div class="ph"><div><h2>On the map</h2><p>Open one to work in it.</p></div></div>{cov}{cl}<button class="btn btn--quiet btn--block" style="margin-top:16px">{ic("calendar-days")}Plan an outreach day here</button></div>'
    tools = f'<div class="float tool"><span>{ic("hand")}</span><span class="on">{ic("shapes")}</span><span>{ic("pencil")}</span><hr><span>{ic("undo")}</span></div>'
    hint = '<div style="position:absolute;left:50%;top:14px;transform:translateX(-50%)" class="bar"><span>Drag a loop round the streets.</span><button class="wbtn">Stop drawing</button></div>'
    right = f"""<div class="pane" style="width:360px"><div class="ph"><div><h2>86 street sides</h2><p>Lassoed in Albany Creek</p></div><span>{ic("x")}</span></div>
<div class="row"><div><b>1,140 letterboxes</b><small>Done 12 to 40 days ago in General outreach</small></div></div>
<div class="row"><div><b>Kingfisher St, Wren Ct, Ellis Rd and 9 more</b><small>3 have No junk mail</small></div></div>
<p class="cap" style="margin-top:12px">Marking them not done opens them to walks in General outreach straight away. Other campaigns keep theirs. You can undo it.</p>
<div class="foot"><button class="btn btn--quiet">Clear</button><button class="btn btn--primary">Mark as not done</button></div></div>"""
    body = f'<div class="ws">{left}<div class="map">{streets(800, 750, seed=5, lasso=True)}{tools}{hint}{zoom()}{ATTR}</div>{right}</div>'
    page("layers", body)


dock()
search()
layers()
