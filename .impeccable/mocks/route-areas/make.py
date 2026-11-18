import re
from pathlib import Path

HERE = Path(__file__).parent
_src = (HERE.parent / "outreach-workspace" / "make.py").read_text()
_helpers = _src[:_src.index("def page(")]
_g = {"__file__": str(HERE.parent / "outreach-workspace" / "make.py")}
exec(_helpers, _g)
ic, sidebar, streets, STYLE, ATTR, zoom = _g["ic"], _g["sidebar"], _g["streets"], _g["STYLE"], _g["ATTR"], _g["zoom"]

COLOURS = ["#5275b2", "#008378", "#b75b1d", "#8466b5", "#33853f", "#c34c65", "#b54e9c", "#867416", "#906e4e"]

LOCAL = """
.drawer{width:400px;flex:none;background:var(--surface,#fff);border-right:1px solid var(--line);position:relative;display:flex;flex-direction:column}
.drawer .in{padding:18px 20px 0;overflow:hidden;flex:1}
.drawer h2{font-size:var(--text-title)}.lead{color:var(--ink-2);font-size:var(--text-caption);margin-top:4px;line-height:1.4}
.linkish{color:var(--accent);font-weight:600;font-size:var(--text-label);margin:10px 0 12px;display:block}
.seg{display:grid;grid-template-columns:1fr 1fr;padding:3px;border-radius:12px;background:var(--fill-on-surface);font-size:var(--text-label);font-weight:600;text-align:center}
.seg span{padding:7px 0;border-radius:9px;color:var(--ink-2)}.seg span.on{background:#2a3d5e;color:#fff}
.seg.sm{display:inline-grid;font-size:var(--text-caption)}.seg.sm span{padding:3px 10px;white-space:nowrap}
.rt{display:grid;grid-template-columns:1fr 70px 84px;gap:6px;align-items:center;padding:8px 0;border-bottom:1px solid var(--line);font-size:var(--text-label)}
.rt.h{color:var(--ink-3);font-size:var(--text-caption);font-weight:600;padding:6px 0}
.rt .n{display:flex;align-items:center;gap:8px;min-width:0}.rt .n b{font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.rt .v{text-align:right;font-variant-numeric:tabular-nums;color:var(--ink-2)}
.rt small{display:block;font-size:var(--text-caption);color:var(--ink-3)}
.dot{width:10px;height:10px;border-radius:50%;flex:none}
.rev{display:inline-block;font-size:var(--text-pill);font-weight:700;color:#7a4a12;background:#f6e3c7;border-radius:999px;padding:1px 7px;margin-left:4px}
.pen{color:var(--ink-3);display:inline-flex}
.grp{display:flex;align-items:center;gap:8px;padding:12px 0 6px;border-bottom:1px solid var(--line)}
.grp b{font-size:var(--text-label)}.grp small{display:block;color:var(--ink-3);font-size:var(--text-caption)}.grp .r{margin-left:auto;display:flex;align-items:center;gap:8px}
.adds{display:flex;flex-wrap:wrap;gap:8px;margin-top:14px}
.dfoot{padding:12px 20px;border-top:1px solid var(--line);background:var(--surface,#fff)}
.dfoot p{font-size:var(--text-caption);color:var(--ink-2);margin-bottom:8px}
.dfoot .acts{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.dfoot .acts .gap{flex-basis:100%;height:0}
.rail{position:absolute;left:12px;top:12px;display:grid;gap:2px;padding:4px}
.rail span{width:36px;height:36px;display:grid;place-items:center;border-radius:10px;color:var(--ink)}.rail span.on{background:#2a3d5e;color:#fff}
.rail hr{border:0;border-top:1px solid var(--line);margin:2px 4px}
.tip{position:absolute;background:#18233a;color:#fff;font-size:var(--text-caption);font-weight:600;padding:5px 9px;border-radius:8px;white-space:nowrap}
.amenu{position:absolute;background:var(--surface,#fff);border-radius:12px;box-shadow:0 6px 24px rgb(24 35 58/.22),0 0 0 1px var(--line);padding:6px;width:290px;font-size:var(--text-label)}
.amenu div{padding:8px 10px;border-radius:8px;display:flex;align-items:center;gap:8px}.amenu div.on{background:var(--selected-fill)}.amenu hr{border:0;border-top:1px solid var(--line);margin:4px 0}
.amenu .danger{color:#b4372f}.amenu small{margin-left:auto;white-space:nowrap;color:var(--ink-3);font-size:var(--text-caption)}
.filt{display:flex;gap:6px;flex-wrap:wrap;margin:4px 0 8px}
.key{position:absolute;left:60px;bottom:30px;padding:10px 14px;font-size:var(--text-caption);display:grid;gap:6px}
.key div{display:flex;align-items:center;gap:8px}
.sw2{width:22px;height:12px;border-radius:3px;flex:none}
"""

STYLE2 = STYLE + re.sub(r"\s*\n\s*", " ", LOCAL)


def lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def quad(p, u0, u1, v0, v1):
    tl, tr, br, bl = p
    def at(u, v):
        return lerp(lerp(tl, tr, u), lerp(bl, br, u), v)
    return [at(u0, v0), at(u1, v0), at(u1, v1), at(u0, v1)]


def pts(poly):
    return " ".join(f"{x:.0f},{y:.0f}" for x, y in poly)


def streets_in(poly, colour, faded=False):
    tl, tr, br, bl = poly
    o = []
    op = ' opacity=".35"' if faded else ""
    for t in (.15, .5, .85):
        a, b = lerp(tl, bl, t), lerp(tr, br, t)
        o.append(f'<line x1="{a[0]:.0f}" y1="{a[1]:.0f}" x2="{b[0]:.0f}" y2="{b[1]:.0f}" stroke="{colour}" stroke-width="4.5" stroke-linecap="round"{op}/>')
    for t in (.2, .8):
        a, b = lerp(tl, tr, t), lerp(bl, br, t)
        o.append(f'<line x1="{a[0]:.0f}" y1="{a[1]:.0f}" x2="{b[0]:.0f}" y2="{b[1]:.0f}" stroke="{colour}" stroke-width="4.5" stroke-linecap="round"{op}/>')
    return "".join(o)


def centre(poly):
    return (sum(x for x, _ in poly) / len(poly), sum(y for _, y in poly) / len(poly))


def capsule(poly, number, colour, text, chosen=False):
    x, y = centre(poly)
    w = 22 + len(text) * 6.4
    fill, ink = (colour, "#fff") if chosen else ("#fff", "#18233a")
    return (f'<g transform="translate({x - w / 2:.0f} {y - 12:.0f})"><rect width="{w:.0f}" height="24" rx="12" fill="{fill}" stroke="{colour}" stroke-width="2.5"/>'
            f'<text x="{w / 2:.0f}" y="16" text-anchor="middle" font-size="11.5" font-weight="700" fill="{ink}" font-family="Montserrat,sans-serif">{text}</text></g>')


FAHEYS = [(70, 150), (430, 110), (470, 430), (110, 470)]
R7 = [(480, 120), (640, 112), (668, 300), (500, 312)]
R8 = [(400, 370), (612, 352), (632, 548), (424, 566)]
ROUTES = [("Faheys Rd & Warrigal St", 118, "58 min", None), ("Wylah Ct & Chowchilla St", 121, "1 h 5 min", None), ("Cabbage Tree Rd (part)", 104, "52 min", "review"),
          ("Ellis Rd & Pine Ct", 97, "49 min", None), ("Kingfisher St", 112, "55 min", None), ("Wren Ct & Lyrebird St", 142, "1 h 12 min", None)]
HAND = [("Albany Forest Dr", 88, "47 min"), ("Old Northern Rd (homes side)", 64, "41 min")]


def plan_map(w=716, h=740, chosen=None, hatch=True, split_dashed=True, drawing=False, area_chips=False):
    base = streets(w, h, seed=21, reports=False, zones=False)
    o = [base[:base.rindex("</svg>")]]
    o.append('<defs><pattern id="ov" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="8" height="8" fill="rgb(144 110 78/.18)"/><line x1="0" y1="0" x2="0" y2="8" stroke="#906e4e" stroke-width="2.4"/></pattern>'
             f'<clipPath id="c8"><polygon points="{pts(R8)}"/></clipPath></defs>')
    dash = ' stroke-dasharray="9 6"' if split_dashed else ""
    o.append(f'<polygon points="{pts(FAHEYS)}" fill="rgb(42 61 94/.06)" stroke="#2a3d5e" stroke-width="2.2"{dash}/>')
    cells = [quad(FAHEYS, u0, u1, v0, v1) for v0, v1 in ((0, .5), (.5, 1)) for u0, u1 in ((0, .34), (.34, .67), (.67, 1))]
    for i, cell in enumerate(cells):
        o.append(streets_in([lerp(c, centre(cell), .18) for c in cell], COLOURS[i], faded=chosen is not None and chosen != i))
    for poly, idx in ((R7, 6), (R8, 7)):
        o.append(f'<polygon points="{pts(poly)}" fill="rgb(42 61 94/.05)" stroke="{COLOURS[idx]}" stroke-width="2.6"/>')
        o.append(streets_in([lerp(c, centre(poly), .16) for c in poly], COLOURS[idx], faded=chosen is not None and chosen != idx))
    if hatch:
        o.append(f'<polygon points="{pts(FAHEYS)}" fill="url(#ov)" clip-path="url(#c8)"/>')
    for i, cell in enumerate(cells):
        r = ROUTES[i]
        o.append(capsule(cell, i + 1, COLOURS[i], f"{i + 1} · {r[1]} · {r[2].replace(' min', 'm').replace(' h ', 'h')}", chosen == i))
    for poly, idx in ((R7, 6), (R8, 7)):
        r = HAND[idx - 6]
        o.append(capsule(poly, idx + 1, COLOURS[idx], f"{idx + 1} · {r[1]} · {r[2].replace(' min', 'm').replace(' h ', 'h')}", chosen == idx))
    o.append('<g transform="translate(560 400)"><circle r="9" fill="#2a3d5e" stroke="#fff" stroke-width="3"/></g><text x="574" y="396" font-size="11" font-weight="700" fill="#18233a" font-family="Montserrat,sans-serif">Meet here</text>')
    if drawing:
        d = [(150, 520), (330, 500), (360, 640)]
        o.append(f'<polyline points="{pts(d)}" fill="rgb(51 133 63/.12)" stroke="#33853f" stroke-width="2.5"/><line x1="360" y1="640" x2="170" y2="650" stroke="#33853f" stroke-width="2" stroke-dasharray="5 4"/>')
        o.extend(f'<circle cx="{x}" cy="{y}" r="{7 if i == 0 else 5}" fill="#fff" stroke="#33853f" stroke-width="2.5"/>' for i, (x, y) in enumerate(d))
    o.append("</svg>")
    return "".join(o)


def rail(tools, on):
    out = "".join(f'<span class="{"on" if name == on else ""}" title="{name}">{ic(icon)}</span>' for name, icon in tools)
    return f'<div class="float rail">{out}<hr><span>{ic("undo")}</span><span>{ic("redo")}</span></div>'


TOOLS_TWO = [("Select", "hand"), ("Draw a route", "pencil"), ("Draw an area to split", "shapes"), ("Scissors", "minus"), ("Brush", "paintbrush"), ("Lasso", "rotate-ccw"), ("Eraser", "x"), ("Start pin", "flag"), ("Meeting point", "map-pin")]
TOOLS_ONE = [("Select", "hand"), ("Draw", "shapes"), ("Scissors", "minus"), ("Brush", "paintbrush"), ("Lasso", "rotate-ccw"), ("Eraser", "x"), ("Start pin", "flag"), ("Meeting point", "map-pin")]


def route_row(n, colour, name, boxes, time, extra="", pen=False):
    p = f'<span class="pen">{ic("pencil")}</span>' if pen else ""
    rev = '<span class="rev">review</span>' if extra == "review" else ""
    sub = f"<small>{extra}</small>" if extra and extra != "review" else ""
    return f'<div class="rt"><div class="n"><span class="dot" style="background:{colour}"></span><div style="min-width:0"><b>{n} · {name}</b>{rev}{sub}</div>{p}</div><span class="v">{boxes}</span><span class="v">{time}</span></div>'


def head_rows():
    return '<div class="rt h"><span>Route</span><span class="v">Letterboxes</span><span class="v">Time</span></div>'


def foot(publish="Publish 8 routes", note="Not published yet: walkers can't see these routes.", tip=None):
    t = f'<p style="color:var(--ink-3)">{tip}</p>' if tip else ""
    return (f'<div class="dfoot"><p>{note}</p>{t}<div class="acts"><button class="btn btn--quiet btn--sm">{ic("sliders")}Rebalance</button>'
            f'<button class="btn btn--quiet btn--sm">{ic("refresh")}Regenerate</button><span class="gap"></span><button class="btn btn--primary">{publish}</button></div></div>')


DAY = '<h2>Outreach Saturday 21 November</h2><p class="lead">8:30 to 11:30am, meeting at The church car park · General outreach · 8 routes, 846 letterboxes</p><span class="linkish">Leave this day\'s routes</span><div class="seg"><span class="on">Plan</span><span>On the day</span></div>'


def page(name, drawer, mapbody):
    tabs = "".join(f'<button class="tab-strip__tab" role="tab" aria-selected="{str(t == "Map").lower()}">{t}</button>' for t in ("Campaigns", "Map", "Days", "Numbers", "Follow-ups", "Settings"))
    items = [("Coverage", "layers"), ("Places", "circle-off"), ("Saved areas", "bookmark"), ("Reports", "flag"), ("Map areas", "grid")]
    d = "".join(f'<a>{ic(i, "")}{t}</a>' for t, i in items) + f'<hr><a class="on">{ic("calendar-days", "")}Plan a day</a>'
    html = f"""<!doctype html>
<html lang="en" data-scheme="light"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Plan a day: {name}</title>
<style>{STYLE2}</style></head>
<body style="height:900px;overflow:hidden"><div class="admin-shell">{sidebar()}
<main class="admin-main"><header class="title-band"><div class="title-band__text"><h1 class="title-band__title">Outreach</h1></div><div class="title-band__actions"><span style="color:var(--ink-2);font-size:var(--text-label);display:inline-flex;gap:6px;align-items:center">{ic("check")}Draft saved</span><button class="btn btn--quiet">{ic("download")}Print the maps</button></div></header>
<div class="tab-strip" role="tablist">{tabs}</div>
<div class="ws"><div class="dock">{d}</div><div class="drawer">{drawer}</div><div class="map">{mapbody}</div></div>
</main></div></body></html>
"""
    (HERE / f"{name}.html").write_text(html)


def key(lines):
    rows = "".join(f'<div><span class="sw2" style="{s}"></span>{t}</div>' for s, t in lines)
    return f'<div class="float key">{rows}</div>'


HATCH = "background:repeating-linear-gradient(45deg,#906e4e 0 2px,rgb(144 110 78/.18) 2px 6px)"


def map_chips():
    rows = head_rows() + "".join(route_row(i + 1, COLOURS[i], r[0], r[1], r[2], r[3] or "") for i, r in enumerate(ROUTES))
    rows += route_row(7, COLOURS[6], HAND[0][0], HAND[0][1], HAND[0][2], pen=True) + route_row(8, COLOURS[7], HAND[1][0], HAND[1][1], HAND[1][2], pen=True)
    filt = '<div class="filt"><span class="chip" style="background:#2a3d5e;color:#fff">All 8</span><span class="chip">Faheys Rd 6</span><span class="chip">' + ic("pencil") + 'Drawn by hand 2</span></div>'
    drawer = f'<div class="in">{DAY}<p class="lead" style="margin:14px 0 6px">2 areas and 2 routes drawn by hand · about 100 letterboxes a route · 1 to look at</p>{filt}{rows}<span class="linkish" style="margin-top:12px">Change how big routes are</span></div>{foot()}'
    chip = (f'<div class="float" style="left:150px;top:96px;padding:5px 10px 5px 12px;border-radius:999px;display:flex;gap:6px;align-items:center;font-size:var(--text-caption);font-weight:700">{ic("shapes")}Faheys Rd · split into 6 {ic("chevron-down")}</div>'
            f'<div class="amenu" style="left:150px;top:132px"><div>{ic("check")}Split it into routes<small>6 now</small></div><div>{ic("pencil")}Make it one route<small>706 letterboxes</small></div><hr><div>{ic("shapes")}Redraw the outline</div><div>{ic("bookmark")}Save for next time</div><hr><div class="danger">{ic("trash")}Take it off the day</div></div>'
            f'<div class="float" style="left:470px;top:70px;padding:4px 10px;border-radius:999px;font-size:var(--text-caption);font-weight:700;display:flex;gap:6px;align-items:center">{ic("pencil")}Drawn as one route {ic("chevron-down")}</div>'
            f'<div class="tip" style="left:400px;top:585px">Route 8 keeps these: drawn by hand</div>')
    k = key([("border:2px dashed #2a3d5e", "Split by the app"), ("border:2px solid #b54e9c", "Drawn as one route"), (HATCH, "Overlap: counted once, in the drawn route")])
    page("map-chips", drawer, plan_map() + rail(TOOLS_TWO, "Select") + chip + k + zoom() + ATTR)


def area_cards():
    g1 = (f'<div class="grp"><span>{ic("shapes")}</span><div><b>Faheys Rd</b><small>6 routes · 694 letterboxes</small></div><span class="r"><span class="seg sm"><span>One route</span><span class="on">Split</span></span>{ic("ellipsis")}</span></div>'
          + head_rows() + "".join(route_row(i + 1, COLOURS[i], r[0], r[1], r[2], r[3] or "") for i, r in enumerate(ROUTES)))
    g2 = (f'<div class="grp" style="margin-top:6px"><span>{ic("pencil")}</span><div><b>Drawn by hand</b><small>Each outline is one route</small></div><span class="r">{ic("ellipsis")}</span></div>'
          + route_row(7, COLOURS[6], HAND[0][0], HAND[0][1], HAND[0][2]) + route_row(8, COLOURS[7], HAND[1][0], HAND[1][1], HAND[1][2], "Has 12 letterboxes inside Faheys Rd"))
    adds = f'<div class="adds"><button class="btn btn--quiet btn--sm">{ic("pencil")}Draw a route</button><button class="btn btn--quiet btn--sm">{ic("shapes")}Draw an area to split</button><button class="btn btn--quiet btn--sm">{ic("bookmark")}Use a saved area</button></div>'
    drawer = f'<div class="in" style="padding-top:12px"><h2>Outreach Saturday 21 November</h2><p class="lead">8:30 to 11:30am · The church car park · 8 routes, 846 letterboxes</p>{g1}{g2}{adds}</div>{foot(note="Not published yet. Rebalance and Regenerate change only split areas.")}'
    k = key([("border:2px dashed #2a3d5e", "Area the app splits"), ("border:2px solid #b54e9c", "Route drawn by hand"), (HATCH, "Overlap: counted once, in the drawn route")])
    page("area-cards", drawer, plan_map(chosen=7) + rail(TOOLS_TWO, "Select") + k + zoom() + ATTR)


def draw_mode():
    rows = head_rows() + "".join(route_row(i + 1, COLOURS[i], r[0], r[1], r[2], r[3] or "") for i, r in enumerate(ROUTES))
    rows += route_row(7, COLOURS[6], HAND[0][0], HAND[0][1], HAND[0][2], pen=True) + route_row(8, COLOURS[7], HAND[1][0], HAND[1][1], HAND[1][2], pen=True)
    mode = '<p class="lead" style="margin:14px 0 6px;font-weight:600;color:var(--ink)">When you draw an outline</p><div class="seg"><span class="on">It\'s one route</span><span>Split it into routes</span></div>'
    drawer = f'<div class="in">{DAY}{mode}<p class="lead" style="margin:12px 0 6px">From Faheys Rd and 2 outlines · about 100 letterboxes a route</p>{rows}</div>{foot()}'
    bar = f'<div style="position:absolute;left:50%;top:14px;transform:translateX(-50%)" class="bar">{ic("pencil")}<span><b>Route 9:</b> click each corner</span><button class="gbtn">{ic("undo")}Undo last point</button><button class="gbtn">Cancel</button><button class="wbtn">{ic("check")}Finish</button></div>'
    k = key([("border:2px dashed #2a3d5e", "Split by the app"), ("border:2px solid #b54e9c", "Drawn as one route"), (HATCH, "Overlap: counted once, in the drawn route")])
    page("draw-mode", drawer, plan_map(drawing=True) + rail(TOOLS_ONE, "Draw") + bar + k + zoom() + ATTR)


map_chips()
area_cards()
draw_mode()
