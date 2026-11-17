from pathlib import Path

HERE = Path(__file__).parent

C = {1: "#5275b2", 2: "#008378", 3: "#b75b1d", 4: "#8466b5", 5: "#33853f"}
FIG = {1: (301, "2 h 42 min"), 2: (405, "3 h 16 min"), 3: (336, "2 h 54 min"), 4: (546, "2 h 48 min"), 5: (209, "1 h 49 min")}

STYLE = """
*{box-sizing:border-box;margin:0;padding:0}
:root{--night:#141d31;--surface:#1d2840;--strong:#26334f;--ink:#f2ede4;--ink-2:#b2b8c5;--ink-3:#9ca2b1;--line:rgb(255 255 255/.09);
--aqua:#2ec4b6;--on-aqua:#062522;--orange:#e7ae74;--oink:#f0c493;--sel:#34446a;--field:#2d374d}
html,body{width:1440px;height:900px;overflow:hidden;background:var(--night);color:var(--ink);font:400 15px/1.4 Montserrat,-apple-system,system-ui,sans-serif}
.map{position:absolute;inset:0}
.panel{position:absolute;left:16px;top:16px;bottom:44px;width:380px;background:var(--surface);border-radius:14px;box-shadow:0 8px 30px rgb(0 0 0/.45);display:flex;flex-direction:column;overflow:hidden}
.ph{display:flex;align-items:center;gap:8px;padding:12px 14px;border-bottom:1px solid var(--line)}
.job{display:inline-flex;gap:6px;align-items:center;background:var(--field);border-radius:999px;padding:6px 12px;font-weight:600;font-size:13px}
.pb{padding:16px 20px;overflow:hidden;flex:1}
.pb h2{font-size:20px;font-weight:700;letter-spacing:-.02em}
.lead{color:var(--ink-2);font-size:13px;margin-top:4px}
.seg{display:grid;grid-template-columns:1fr 1fr;background:var(--field);border-radius:12px;padding:3px;margin:14px 0 10px;font-weight:600;font-size:13px;text-align:center}
.seg span{padding:7px 0;border-radius:9px;color:var(--ink-2)}.seg .on{background:var(--sel);color:var(--ink)}
.cap{font-size:13px;color:var(--ink-2);margin-bottom:6px}.cap b{color:var(--aqua);font-weight:600}
.row{display:grid;grid-template-columns:1fr 54px 82px;gap:6px;padding:9px 0;border-bottom:1px solid var(--line);font-size:13px;align-items:start}
.row.h{color:var(--ink-3);font-size:12px;font-weight:600}
.row .v{text-align:right;font-variant-numeric:tabular-nums}
.row b{font-weight:600}.row small{display:block;color:var(--ink-3);font-size:12px}
.rev{display:inline-block;font-size:11px;font-weight:700;color:var(--oink);background:rgb(231 174 116/.16);border-radius:999px;padding:1px 8px;margin-top:3px}
.dot{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:6px}
.pf{padding:12px 20px;border-top:1px solid var(--line);font-size:12px;color:var(--ink-2)}
.pf .b{display:inline-block;margin-top:8px;background:var(--aqua);color:var(--on-aqua);font-weight:700;border-radius:22px;padding:9px 16px;font-size:13px}
.rail{position:absolute;left:410px;top:16px;background:var(--surface);border-radius:22px;padding:5px;display:grid;gap:4px;box-shadow:0 6px 20px rgb(0 0 0/.4)}
.rail i{width:34px;height:34px;border-radius:50%;display:block;border:1.5px solid var(--ink-3);opacity:.9}.rail i.on{background:var(--aqua);border-color:var(--aqua)}
.leave{position:absolute;right:16px;top:16px;background:var(--surface);border-radius:999px;padding:8px 14px;font-weight:600;font-size:13px;box-shadow:0 6px 20px rgb(0 0 0/.4)}
.leave small{color:var(--ink-3);margin-left:6px;border:1px solid var(--line);border-radius:5px;padding:0 4px}
.chip{position:absolute;display:inline-flex;align-items:center;gap:6px;background:var(--surface);border-radius:999px;padding:6px 12px;font-weight:600;font-size:13px;white-space:nowrap;box-shadow:0 4px 14px rgb(0 0 0/.4);user-select:none}
.ring{border:2px solid}
.menu{position:absolute;background:var(--surface);border-radius:12px;padding:6px;width:250px;font-size:13px;box-shadow:0 10px 30px rgb(0 0 0/.5),0 0 0 1px var(--line)}
.menu div{padding:8px 10px;border-radius:9px}.menu div.on{background:var(--sel)}.menu hr{border:0;border-top:1px solid var(--line);margin:4px 0}.menu .danger{color:#ff8a80}
.disc{position:absolute;width:26px;height:26px;border-radius:50%;color:#fff;font-weight:700;font-size:13px;display:grid;place-items:center;border:2px solid var(--night);box-shadow:0 2px 6px rgb(0 0 0/.5)}
.fig{position:absolute;font-size:12px;font-weight:600;color:var(--ink);background:rgb(20 29 49/.86);border-radius:8px;padding:3px 8px;white-space:nowrap}
.inset{position:absolute;right:16px;top:64px;width:250px;height:190px;border-radius:14px;overflow:hidden;box-shadow:0 8px 30px rgb(0 0 0/.5),0 0 0 1px var(--line);background:var(--night)}
.inset .t{position:absolute;left:10px;top:8px;font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--ink-3)}
.key{position:absolute;left:470px;bottom:44px;background:var(--surface);border-radius:12px;padding:10px 14px;font-size:12px;display:grid;gap:6px;box-shadow:0 6px 20px rgb(0 0 0/.4)}
.key div{display:flex;align-items:center;gap:8px}
.attr{position:absolute;right:16px;bottom:12px;font-size:11px;color:var(--ink-3)}
.strip{position:absolute;left:470px;right:280px;top:16px;display:flex;gap:8px}
.strip .chip{position:static}
"""

ROADS = [
    "M440,250 C700,230 900,180 1440,140", "M600,900 C640,700 700,500 760,300", "M1150,120 C1180,400 1220,650 1260,900",
    "M440,620 C700,600 1000,640 1440,700", "M900,0 C910,200 930,350 960,900", "M500,420 C800,380 1100,420 1440,430",
]


def basemap(w, h, scale=1.0):
    roads = "".join(f'<path d="{d}" stroke="#2a3754" stroke-width="{9*scale}" fill="none"/>' for d in ROADS)
    green = '<path d="M440,700 C560,640 640,760 700,900 L440,900Z" fill="#1d3328"/><path d="M1300,120 C1380,200 1440,180 1440,120Z" fill="#1d3328"/>'
    minor = "".join(f'<path d="M{440+i*70},{260+(i%3)*40} l{60+(i%4)*20},{300}" stroke="#222d47" stroke-width="{3*scale}" fill="none"/>' for i in range(14))
    return f'<rect width="{w}" height="{h}" fill="#141d31"/>{green}{minor}{roads}'


ROUTES = {
    1: dict(outline="560,170 860,150 900,360 610,400", streets=["M590,220 L840,200", "M600,290 L860,270", "M620,360 L880,330", "M700,200 L720,370", "M800,190 L820,350"], gaps=["M840,200 L860,270"]),
    2: dict(outline="560,430 820,420 840,640 580,660", streets=["M590,470 L800,460", "M600,540 L810,530", "M610,610 L820,600", "M680,470 L690,610"], gaps=[]),
    3: dict(outline="600,690 880,680 900,860 620,870", streets=["M630,730 L870,720", "M640,800 L880,790", "M760,730 L770,850"], gaps=["M870,720 L880,790"]),
    4: dict(outline="930,190 1180,180 1240,650 960,680", streets=["M960,240 L1160,230", "M970,320 L1170,310", "M1000,440 L1200,430", "M990,560 L1210,550", "M1060,240 L1070,320", "M1100,440 L1110,560"],
            gaps=["M1070,320 L1090,440", "M970,320 C960,380 985,410 1000,440"]),
    5: dict(outline="920,700 1200,690 1220,880 940,890", streets=["M950,740 L1190,730", "M960,820 L1200,810"], gaps=["M1080,735 L1085,815"]),
}


def routes_svg(gap_style, chosen=4):
    out = []
    for n, r in ROUTES.items():
        col = C[n]
        out.append('<g transform="translate(-60,0)">' if n in (4, 5) else "<g>")
        out.append(f'<polygon points="{r["outline"]}" fill="{col}" fill-opacity=".06" stroke="{col}" stroke-width="2"/>')
        for d in r["streets"]:
            out.append(f'<path d="{d}" stroke="#141d31" stroke-width="10" stroke-linecap="round" fill="none"/>')
            out.append(f'<path d="{d}" stroke="{col}" stroke-width="6" stroke-linecap="round" fill="none"/>')
        for d in r["gaps"]:
            out.append(gap_style(d, col))
        out.append("</g>")
    return "".join(out)


def dotted(d, col):
    return f'<path d="{d}" stroke="{col}" stroke-width="5.5" stroke-linecap="round" stroke-dasharray="0.1 10" fill="none"/>'


def dashed(d, col):
    return f'<path d="{d}" stroke="{col}" stroke-width="3.5" stroke-linecap="butt" stroke-dasharray="7 6" fill="none"/>'


def arrowed(d, col, i=[0]):
    i[0] += 1
    mid = f"a{i[0]}"
    return (f'<defs><marker id="{mid}" viewBox="0 0 10 10" refX="5" refY="5" markerWidth="5" markerHeight="5" orient="auto">'
            f'<path d="M2,1 L8,5 L2,9" stroke="{col}" stroke-width="2" fill="none"/></marker></defs>'
            f'<path d="{d}" stroke="{col}" stroke-width="2.5" fill="none" marker-mid="url(#{mid})" marker-end="url(#{mid})"/>')


def panel():
    rows = "".join(
        f'<div class="row"><div><span class="dot" style="background:{C[n]}"></span><b>{n} · {name}</b><small>✎ Drawn by hand</small><span class="rev">Needs review</span></div>'
        f'<div class="v">{FIG[n][0]}</div><div class="v">{FIG[n][1]}</div></div>'
        for n, name in [(1, "Kingfisher St"), (2, "Cressbrook Dr"), (3, "Boondooma Cct"), (4, "Faheys Rd W")])
    return f"""<div class="panel"><div class="ph"><span class="job">Plan a day ⌄</span></div><div class="pb">
<h2>Outreach Saturday 21 November</h2><p class="lead">8:30 to 11:30am, meeting at the church car park · 5 routes, 1,797 letterboxes</p>
<div class="seg"><span class="on">Plan</span><span>On the day</span></div>
<p class="cap">5 routes drawn by hand · <b>5 need a look</b></p>
<div class="row h"><div>Route</div><div class="v">Letterboxes</div><div class="v">Time</div></div>{rows}</div>
<div class="pf">Not published yet: walkers can't see these routes.<br><span class="b">Publish 5 routes</span></div></div>
<div class="rail">{''.join('<i class="on"></i>' if i == 0 else '<i></i>' for i in range(8))}</div>
<div class="leave">⤡ Leave full map<small>Esc</small></div>"""


INSET_POS = {1: (95, 78), 2: (85, 110), 3: (105, 135), 4: (140, 88), 5: (138, 128)}


def inset(extra=""):
    roads = "".join(f'<path d="M{x},0 L{x+30},190" stroke="#2a3754" stroke-width="2.5"/>' for x in range(15, 250, 45))
    roads += "".join(f'<path d="M0,{y} L250,{y-20}" stroke="#2a3754" stroke-width="2.5"/>' for y in range(30, 200, 42))
    blobs = "".join(f'<circle cx="{x}" cy="{y}" r="12" fill="{C[n]}" fill-opacity=".18" stroke="{C[n]}" stroke-width="1.5"/>' for n, (x, y) in INSET_POS.items())
    labels = '<text x="14" y="44" fill="#9ca2b1" font-size="10" font-weight="700" letter-spacing="1.2">EATONS HILL</text><text x="120" y="176" fill="#9ca2b1" font-size="10" font-weight="700" letter-spacing="1.2">BRIDGEMAN DOWNS</text>'
    return f'<div class="inset"><svg width="250" height="190">{roads}{labels}{blobs}</svg><span class="t">Zoomed out</span>{extra}</div>'


def discs(points, size=24, font=12):
    return "".join(f'<div class="disc" style="left:{x-size/2}px;top:{y-size/2}px;width:{size}px;height:{size}px;font-size:{font}px;background:{C[n]}">{n}</div>' for n, (x, y) in points.items())


def page(body, gap):
    svg = f'<svg class="map" width="1440" height="900">{basemap(1440, 900)}{routes_svg(gap)}</svg>'
    return f"""<!doctype html><html><head><meta charset="utf-8"><link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700&display=swap" rel="stylesheet"><style>{STYLE}</style></head>
<body>{svg}{panel()}{body}<div class="attr">Map © OpenStreetMap contributors · Protomaps</div></body></html>"""


def key(sample, words):
    return f'<div class="key"><div><svg width="34" height="10"><path d="M2,5 L32,5" stroke="#8466b5" stroke-width="6" stroke-linecap="round"/></svg>Delivers letterboxes</div><div><svg width="34" height="10">{sample}</svg>{words}</div></div>'


MID = {1: (730, 280), 2: (700, 540), 3: (760, 775), 4: (1025, 380), 5: (1010, 775)}


def strip():
    chips = "".join(f'<span class="chip ring" style="border-color:{C[n]}">✎ Route {n} ⌄</span>' for n in C)
    body = f'<div class="strip">{chips}</div>'
    body += '<div class="menu" style="left:860px;top:62px"><div>✓ One route as drawn</div><div>Split into routes</div><div>Redraw the outline</div><hr><div class="danger">Take it off the day</div></div>'
    body += discs(MID, 28, 14)
    body += "".join(f'<div class="fig" style="left:{x+20}px;top:{y-11}px">{FIG[n][0]} · {FIG[n][1]} · needs review</div>' for n, (x, y) in MID.items())
    body += inset(discs({n: (x, y) for n, (x, y) in INSET_POS.items()}, 18, 10))
    body += key('<path d="M3,5 L31,5" stroke="#8466b5" stroke-width="5.5" stroke-linecap="round" stroke-dasharray="0.1 9"/>', "Walked, nothing to deliver")
    return page(body, dotted)


def one_label():
    body = "".join(
        f'<div class="chip ring" style="left:{x-120}px;top:{y-16}px;border-color:{C[n]}">✎ {n} · {FIG[n][0]} · {FIG[n][1]} · needs review ⌄</div>'
        for n, (x, y) in MID.items())
    body += '<div class="menu" style="left:905px;top:404px"><div>✓ One route as drawn</div><div>Split into routes</div><div>Redraw the outline</div><hr><div class="danger">Take it off the day</div></div>'
    nudged = {1: (92, 72), 2: (78, 108), 3: (104, 140), 4: (146, 84), 5: (142, 132)}
    hover = '<div class="chip ring" style="left:22px;top:150px;border-color:#8466b5;font-size:10px;padding:3px 8px">✎ 4 · 546 · 2 h 48 min · needs review</div>'
    body += inset(discs(nudged, 18, 10) + hover)
    body += key('<path d="M3,5 L31,5" stroke="#8466b5" stroke-width="3.5" stroke-dasharray="7 6"/>', "Walked, nothing to deliver")
    return page(body, dashed)


def aside():
    tops = {1: (690, 150), 2: (650, 422), 3: (700, 680), 4: (950, 182), 5: (960, 692)}
    body = "".join(
        f'<div class="chip ring" style="left:{x-60}px;top:{y-18}px;border-color:{C[n]}">✎ Route {n} · {FIG[n][0]} · {FIG[n][1]} ⌄</div>'
        for n, (x, y) in tops.items())
    stack = "".join(f'<div class="chip ring" style="left:172px;top:{22+i*30}px;border-color:{C[n]};font-size:10px;padding:2px 7px">✎ Route {n} ⌄</div>' for i, n in enumerate(C))
    leaders = "".join(f'<path d="M172,{34+i*30} L{INSET_POS[n][0]+11},{INSET_POS[n][1]}" stroke="{C[n]}" stroke-width="1"/>' for i, n in enumerate(C))
    body += inset(f'<svg style="position:absolute;inset:0" width="250" height="190">{leaders}</svg>' + discs(INSET_POS, 18, 10) + stack)
    body += key('<path d="M3,5 L31,5" stroke="#8466b5" stroke-width="2.5"/><path d="M13,2 L18,5 L13,8M24,2 L29,5 L24,8" stroke="#8466b5" stroke-width="1.6" fill="none"/>', "Walked, nothing to deliver")
    return page(body, arrowed)


for name, make in {"strip": strip, "one-label": one_label, "aside": aside}.items():
    (HERE / f"{name}.html").write_text(make())
