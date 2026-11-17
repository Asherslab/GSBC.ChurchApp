import base64
from pathlib import Path

HERE = Path(__file__).parent
ROOT = HERE.parent.parent.parent
FONTS = ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/fonts"


def font(weight):
    data = base64.b64encode((FONTS / f"montserrat-latin-{weight}-normal.woff2").read_bytes()).decode()
    return f'@font-face{{font-family:Montserrat;font-weight:{weight};src:url("data:font/woff2;base64,{data}") format("woff2")}}'


ICONS = {
    "turn-left": '<path d="M9 14 4 9l5-5"/><path d="M4 9h10.5a5.5 5.5 0 0 1 5.5 5.5V20"/>',
    "no-junk": '<circle cx="12" cy="12" r="10"/><path d="m4.9 4.9 14.2 14.2"/>',
    "spoke": '<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>',
    "pause": '<rect x="6" y="4" width="4" height="16" rx="1"/><rect x="14" y="4" width="4" height="16" rx="1"/>',
    "check": '<path d="M20 6 9 17l-5-5"/>',
    "feet": '<path d="M4 16v-2.4C4 11.5 3 10.5 3 8c0-2.7 1.5-6 4.5-6C9.4 2 10 3.8 10 5.5c0 3.1-2 5.7-2 8.7V16a2 2 0 1 1-4 0Z"/><path d="M20 20v-2.4c0-2.1 1-3.1 1-5.6 0-2.7-1.5-6-4.5-6C14.6 6 14 7.8 14 9.5c0 3.1 2 5.7 2 8.7V20a2 2 0 1 0 4 0Z"/>',
}


def icon(name, size=18, cls=""):
    return (f'<svg class="i {cls}" width="{size}" height="{size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" '
            f'stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{ICONS[name]}</svg>')


BASE = """
*{box-sizing:border-box;margin:0;padding:0}
html,body{width:390px;height:844px;overflow:hidden}
body{font-family:Montserrat,-apple-system,system-ui,sans-serif;background:radial-gradient(120% 70% at 30% 20%,#3a4a63 0%,#1b2333 55%,#121824 100%);color:#fff;-webkit-font-smoothing:antialiased;font-variant-numeric:tabular-nums}
.i{flex:none;display:block}
.status{height:54px;display:flex;align-items:center;justify-content:space-between;padding:0 26px 0 34px;font-size:15px;font-weight:600}
.island{height:37px;border-radius:19px;background:#000;display:flex;align-items:center;justify-content:space-between;padding:0 12px;gap:8px}
.island--compact{width:200px}
.island--minimal{width:37px;padding:0;justify-content:center}
.clock{text-align:center;margin-top:6px}
.clock p{font-size:17px;font-weight:600;opacity:.9}
.clock h1{font-size:84px;font-weight:600;letter-spacing:-.02em;line-height:1;margin-top:2px}
.la{margin:28px 14px 0;border-radius:22px;background:#2a3d5e;padding:14px 14px 12px;height:160px;overflow:hidden;position:relative}
.label{font-size:10.5px;font-weight:600;letter-spacing:.08em;text-transform:uppercase;color:#9ca2b1;margin:0 0 7px 20px}
.di-zone{margin-top:24px}
.expanded{margin:0 10px;border-radius:44px;background:#000;padding:16px 20px 16px}
.di-row{display:flex;align-items:center;gap:28px;margin:12px 20px 0}
.di-row figure{display:grid;gap:6px}
.di-row figcaption{font-size:10.5px;color:#9ca2b1;font-weight:600}
.street{font-size:22px;font-weight:700;letter-spacing:-.02em;line-height:1.1;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.side{font-size:14px;font-weight:600;line-height:1.3}
.note{font-size:12.5px;color:#d5c9b7;line-height:1.3;display:flex;align-items:center;gap:6px}
.meta{font-size:11.5px;font-weight:600;color:rgba(255,255,255,.76)}
.btns{display:flex;gap:6px;align-items:center}
.btn{height:34px;border-radius:17px;background:rgba(255,255,255,.14);color:#fff;font:600 12.5px Montserrat;display:inline-flex;align-items:center;justify-content:center;gap:6px;padding:0 12px;white-space:nowrap}
.btn--wide{flex:1 1 0;min-width:0;padding:0 8px;gap:5px;font-size:12px}
.btn--round{width:34px;padding:0;flex:none}
.btn--finish{background:#fff;color:#2a3d5e}
.di .btn{background:rgba(255,255,255,.16)}
.di .btn--finish{background:#2ec4b6;color:#062522}
.track{height:4px;border-radius:2px;background:rgba(255,255,255,.16);overflow:hidden}
.track i{display:block;height:100%;width:32%;background:#e7ae74;border-radius:2px}
.aqua{color:#2ec4b6}
.orange{color:#e7ae74}
.said{display:flex;align-items:center;justify-content:space-between;gap:10px;font-size:12.5px;font-weight:600;padding:8px 12px;border-radius:12px;background:rgba(46,196,182,.16);margin-top:10px}
.said b{color:#2ec4b6;font-weight:700}
"""


def page(title, css, body):
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=390">
<title>{title}</title>
<style>{font(400)}{font(600)}{font(700)}{BASE}{css}</style></head>
<body>
{body}
</body></html>
"""


def lock(compact, la):
    return f"""<div class="status"><span>4:12</span>{compact}<span>88%</span></div>
<div class="clock"><p>Tuesday 17 November</p><h1>4:12</h1></div>
<p class="label" style="margin-top:22px">Lock Screen</p>
{la}"""


def islands(expanded, compact, minimal):
    return f"""<div class="di-zone"><p class="label">Dynamic Island, held open</p>
<div class="expanded di">{expanded}</div>
<div class="di-row"><figure><div class="island island--compact">{compact}</div><figcaption>Compact, another app open</figcaption></figure>
<figure><div class="island island--minimal">{minimal}</div><figcaption>Minimal</figcaption></figure></div></div>"""


def write(name, title, css, body):
    (HERE / f"{name}.html").write_text(page(title, css, body))


TURN_CSS = """
.ts{display:grid;grid-template-columns:58px 1fr;gap:12px;align-items:center;margin-top:8px}
.ts__sign{width:58px;height:58px;border-radius:14px;background:#fff;color:#2a3d5e;display:grid;place-items:center;align-content:center;gap:1px}
.ts__sign b{font-size:11px;font-weight:700;line-height:1}
.ts__top{display:flex;justify-content:space-between}
.ts-btns{margin-top:12px}
.ts-di{display:grid;grid-template-columns:48px 1fr auto;gap:12px;align-items:center}
.ts-di .ts__sign{width:48px;height:48px;border-radius:12px;background:#2ec4b6;color:#062522}
.ts-di__go{text-align:right}
.ts-di__go b{display:block;font-size:22px;font-weight:700;line-height:1}
"""

turn_la = f"""<div class="la">
<div class="ts__top"><span class="meta">Walking · 38 min</span><span class="meta">112 of 348 letterboxes · 1.2 km</span></div>
<div class="ts"><div class="ts__sign">{icon('turn-left', 26)}<b>13</b></div>
<div><p class="street">Greensill Rd</p><p class="side">Houses on your left · 13 to go</p><p class="note">Then left into Wilson Ave · 17</p></div></div>
<div class="btns ts-btns"><span class="btn btn--wide">{icon('no-junk', 15)}No junk mail</span><span class="btn btn--wide">{icon('spoke', 15)}Spoke to someone</span><span class="btn btn--round" aria-label="Pause">{icon('pause', 15)}</span><span class="btn btn--round btn--finish" aria-label="Finish">{icon('check', 16)}</span></div>
</div>"""

turn_expanded = f"""<div class="ts-di"><div class="ts__sign">{icon('turn-left', 22)}</div>
<div><p class="street" style="font-size:19px">Greensill Rd</p><p class="side" style="font-size:13px">Houses on your left</p></div>
<div class="ts-di__go"><b>13</b><span class="meta">to go</span></div></div>
<p class="said">Marked No junk mail (14 Greensill Rd).<b>Undo</b></p>
<div class="btns" style="margin-top:10px"><span class="btn btn--wide">{icon('no-junk', 15)}No junk mail</span><span class="btn btn--wide">{icon('spoke', 15)}Spoke</span><span class="btn btn--round">{icon('pause', 15)}</span><span class="btn btn--round btn--finish">{icon('check', 16)}</span></div>"""

write("a-turn-sign", "Turn sign", TURN_CSS,
      lock(f'<div class="island island--compact"><span class="aqua" style="display:flex;gap:5px;align-items:center">{icon("turn-left", 16)}<b style="font-size:14px">13</b></span><span style="font-size:13px;font-weight:600">Greensill Rd</span></div>', turn_la)
      + islands(turn_expanded,
                f'<span class="aqua" style="display:flex;gap:5px;align-items:center">{icon("turn-left", 16)}<b style="font-size:14px">13</b></span><span style="font-size:13px;font-weight:600">Greensill Rd</span>',
                f'<span class="aqua">{icon("turn-left", 16)}</span>'))


TALLY_CSS = """
.tl__head{display:flex;justify-content:space-between;align-items:baseline;gap:10px}
.tl__tallies{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr) 38px 38px;gap:6px;align-items:center;margin-top:10px}
.tl__round{width:38px;height:46px;border-radius:14px}
.tally{height:46px;border-radius:14px;background:rgba(255,255,255,.14);display:grid;grid-template-columns:1fr;row-gap:2px;align-content:center;padding:0 9px;font-size:10.5px;font-weight:600;white-space:nowrap;overflow:hidden}
.tally .i{grid-row:1/3;align-self:center}
.tally b{font-size:19px;font-weight:700;letter-spacing:-.02em;line-height:1;order:-1}
.di .tally{background:rgba(255,255,255,.16)}
.tl__foot{display:flex;align-items:center;justify-content:space-between;margin-top:9px}
.tl__foot .btn{height:28px;font-size:12px;padding:0 10px}
"""

tally_la = f"""<div class="la">
<div class="tl__head"><p class="street" style="font-size:20px">Greensill Rd</p><span class="meta">112 of 348 · 1.2 km</span></div>
<p class="side" style="font-size:13px">Houses on your left · 13 to go</p>
<p class="note">{icon('turn-left', 13)}Then left into Wilson Ave · 17</p>
<div class="tl__tallies"><span class="tally"><b>3</b>No junk mail</span><span class="tally"><b>2</b>Conversations</span><span class="btn btn--round tl__round" aria-label="Pause">{icon('pause', 15)}</span><span class="btn btn--round btn--finish tl__round" aria-label="Finish">{icon('check', 16)}</span></div>
</div>"""

tally_expanded = f"""<div class="tl__head"><p class="street" style="font-size:19px">Greensill Rd</p><span class="side aqua" style="font-size:13px">13 to go</span></div>
<p class="side" style="font-size:13px">Houses on your left</p>
<div class="tl__tallies" style="grid-template-columns:1fr 1fr"><span class="tally"><b>4</b>No junk mail</span><span class="tally"><b>2</b>Conversations</span></div>
<p class="said">Marked No junk mail (14 Greensill Rd).<b>Undo</b></p>
<div class="tl__foot"><span class="meta">113 of 348 · 1.2 of 3.0 km</span><span class="btns"><span class="btn">{icon('pause', 13)}Pause</span><span class="btn btn--finish">{icon('check', 13)}Finish</span></span></div>"""

write("b-tallies", "Tallies first", TALLY_CSS,
      lock(f'<div class="island island--compact"><span class="aqua">{icon("feet", 16)}</span><span style="font-size:13px;font-weight:600">13 to go</span></div>', tally_la)
      + islands(tally_expanded,
                f'<span class="aqua">{icon("feet", 16)}</span><span style="font-size:13px;font-weight:600">13 to go</span>',
                f'<span class="aqua">{icon("feet", 16)}</span>'))


STOPS_CSS = """
.st__head{display:flex;justify-content:space-between;align-items:baseline}
.stops{position:relative;display:flex;justify-content:space-between;align-items:center;height:16px;margin:12px 4px 0}
.stops::before{content:"";position:absolute;left:0;right:0;top:7px;height:2px;background:rgba(255,255,255,.22)}
.stops::after{content:"";position:absolute;left:0;width:38%;top:7px;height:2px;background:#2ec4b6}
.stop{position:relative;z-index:1;width:12px;height:12px;border-radius:50%;background:#2a3d5e;box-shadow:inset 0 0 0 2px rgba(255,255,255,.55)}
.stop.done{background:#2ec4b6;box-shadow:none}
.stop.here{width:16px;height:16px;background:#e7ae74;box-shadow:0 0 0 3px #2a3d5e}
.di .stop{background:#000}
.di .stop.done{background:#2ec4b6}
.di .stop.here{background:#e7ae74;box-shadow:0 0 0 3px #000}
.stops-names{display:flex;justify-content:space-between;font-size:10.5px;font-weight:600;color:rgba(255,255,255,.76);margin:5px 0 0}
.stops-names .orange{color:#e7ae74}
.st .btns{margin-top:10px}
"""

stops = '<div class="stops"><i class="stop done"></i><i class="stop done"></i><i class="stop done"></i><i class="stop here"></i><i class="stop"></i><i class="stop"></i><i class="stop"></i><i class="stop"></i><i class="stop"></i></div>'
stop_names = '<p class="stops-names"><span>3 streets done</span><span class="orange">Greensill Rd</span><span>Wilson Ave next</span><span>5 more</span></p>'

stops_la = f"""<div class="la st">
<div class="st__head"><p class="street">Greensill Rd</p><span class="meta">112 of 348 · 1.2 km</span></div>
<p class="side">Houses on your left · 13 to go</p>
{stops}{stop_names}
<div class="btns"><span class="btn btn--wide">{icon('no-junk', 15)}No junk mail</span><span class="btn btn--wide">{icon('spoke', 15)}Spoke to someone</span><span class="btn btn--round">{icon('pause', 15)}</span><span class="btn btn--round btn--finish">{icon('check', 16)}</span></div>
</div>"""

stops_expanded = f"""<div class="st__head"><p class="street" style="font-size:19px">Greensill Rd</p><span class="side aqua" style="font-size:13px">13 to go</span></div>
<p class="side" style="font-size:13px">Houses on your left · then left into Wilson Ave</p>
{stops}
<p class="said">Counted. 3 conversations on this walk.<b>Undo</b></p>
<div class="btns" style="margin-top:10px"><span class="btn btn--wide">{icon('no-junk', 15)}No junk mail</span><span class="btn btn--wide">{icon('spoke', 15)}Spoke</span><span class="btn btn--round">{icon('pause', 15)}</span><span class="btn btn--round btn--finish">{icon('check', 16)}</span></div>"""

ring = '<svg width="22" height="22" viewBox="0 0 22 22" aria-hidden="true"><circle cx="11" cy="11" r="8.5" fill="none" stroke="rgba(255,255,255,.22)" stroke-width="3"/><circle cx="11" cy="11" r="8.5" fill="none" stroke="#2ec4b6" stroke-width="3" stroke-dasharray="17.8 53.4" transform="rotate(-90 11 11)" stroke-linecap="round"/></svg>'

write("c-line-of-stops", "Line of stops", STOPS_CSS,
      lock(f'<div class="island island--compact"><span style="display:flex;gap:6px;align-items:center"><i class="stop here" style="width:10px;height:10px;box-shadow:none;display:block"></i><span style="font-size:13px;font-weight:600">Greensill Rd</span></span><span class="aqua" style="font-size:13px;font-weight:700">13</span></div>', stops_la)
      + islands(stops_expanded,
                f'<span style="display:flex;gap:6px;align-items:center"><i class="stop here" style="width:10px;height:10px;box-shadow:none;display:block"></i><span style="font-size:13px;font-weight:600">Greensill Rd</span></span><span class="aqua" style="font-size:13px;font-weight:700">13</span>',
                ring))
