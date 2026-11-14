import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE.parent / "church-news"))
from make import _css, MOCKS, ROOT

CSS = "".join(_css(f) for f in [MOCKS / "design-system" / "kit.css", ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/css/rich-text.css"])


def svg(paths, size=18):
    return f'<svg width="{size}" height="{size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{paths}</svg>'


I = dict(
    grip=svg('<circle cx="9" cy="6" r="1"/><circle cx="9" cy="12" r="1"/><circle cx="9" cy="18" r="1"/><circle cx="15" cy="6" r="1"/><circle cx="15" cy="12" r="1"/><circle cx="15" cy="18" r="1"/>'),
    plus=svg('<path d="M5 12h14"/><path d="M12 5v14"/>'),
    trash=svg('<path d="M3 6h18"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>'),
    replace=svg('<path d="M3 12a9 9 0 0 1 15-6.7L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 0 1-15 6.7L3 16"/><path d="M8 16H3v5"/>'),
    arrange=svg('<rect width="7" height="7" x="3" y="3" rx="1"/><rect width="7" height="7" x="14" y="3" rx="1"/><rect width="7" height="7" x="14" y="14" rx="1"/><rect width="7" height="7" x="3" y="14" rx="1"/>'),
    x=svg('<path d="M18 6 6 18"/><path d="m6 6 12 12"/>', 16),
    left=svg('<path d="m15 18-6-6 6-6"/>'),
    right=svg('<path d="m9 18 6-6-6-6"/>'),
    more=svg('<circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/>'),
    back=svg('<path d="M9 14 4 9l5-5"/><path d="M4 9h10.5a5.5 5.5 0 0 1 0 11H11"/>'),
    bold=svg('<path d="M6 12h9a4 4 0 0 1 0 8H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1h7a4 4 0 0 1 0 8"/>'),
)
PICS = ["linear-gradient(135deg,#2a3d5e,#009c8f)", "linear-gradient(135deg,#4a7355,#84a87d)", "linear-gradient(135deg,#e7ae74,#d5c9b7)"]
NAMES = ["worship.jpg", "lawn.jpg", "letterbox.jpg"]

BASE = """<style>
html,body{width:1440px;height:900px;overflow:hidden}
body{background:var(--page);display:grid;grid-template-columns:1fr 430px;gap:0}
.desk{padding:28px 48px;overflow:hidden}
.side{background:var(--sand);border-left:1px solid var(--line);padding:28px 20px;overflow:hidden}
.k{font-size:var(--text-micro);font-weight:700;color:var(--ink-3);letter-spacing:.04em;text-transform:uppercase;margin-bottom:10px}
.paper{max-width:800px;margin:0 auto;background:var(--surface);border-radius:14px;padding:22px 28px;box-shadow:0 0 0 1px var(--line)}
.bar{display:flex;gap:4px;align-items:center;padding:0 0 8px;box-shadow:0 1px 0 var(--line);margin-bottom:8px;color:var(--ink-2)}
.bar span{width:36px;height:36px;display:grid;place-items:center}.bar .sel{width:7.5rem;justify-content:space-between;display:flex;padding:0 12px;border-radius:999px;background:var(--field);color:var(--ink);font-size:var(--text-caption);font-weight:600;align-items:center}
.bar .ins{margin-left:auto;background:var(--primary);color:var(--on-primary);border-radius:999px;width:auto;padding:0 14px;height:34px;font-size:var(--text-caption);font-weight:600;display:flex;gap:6px;align-items:center}
.ed{background:var(--field);box-shadow:inset 0 0 0 1px var(--field-edge);border-radius:12px;padding:12px 16px;font-size:var(--text-body);line-height:1.55}
.ed p{margin:0 0 10px}
.g{position:relative;display:grid;gap:8px;margin:6px 0 4px;border-radius:9px}
.g.on{outline:2px solid var(--accent);outline-offset:4px}
.fr{display:grid;grid-template-columns:1fr 1fr;gap:8px}.fr>:first-child{grid-column:1/-1}
.ph{position:relative;border-radius:9px;aspect-ratio:4/3;overflow:hidden}.fr>:first-child.ph{aspect-ratio:16/7}
.desk .fr .ph{aspect-ratio:16/8}.desk .fr>:first-child.ph{aspect-ratio:16/5}.ph.pick{outline:3px solid var(--accent);outline-offset:-3px}
.ph.dim{opacity:.35}
.pill{position:absolute;top:10px;right:10px;display:flex;gap:4px;padding:2px;border-radius:999px;background:var(--surface);box-shadow:var(--lift);color:var(--ink-2);z-index:2}
.pill>span{width:32px;height:32px;display:grid;place-items:center;border-radius:999px}.pill>span.h{background:var(--fill-on-surface);color:var(--ink)}
.pill .w{width:auto;padding:0 10px;font-size:var(--text-caption);font-weight:600;gap:6px;display:flex;align-items:center}
.pill .rule{width:1px;height:20px;align-self:center;background:var(--line)}
.cap{font-size:var(--text-caption);color:var(--ink-3)}
.note{font-size:var(--text-caption);color:var(--ink-2);line-height:1.45;margin-top:14px;max-width:62ch}
.note b{color:var(--ink)}
.tip{position:absolute;background:#111827;color:#fff;font-size:12px;padding:4px 8px;border-radius:6px;white-space:nowrap;z-index:5}
.phone{width:390px;height:780px;border-radius:44px;background:var(--surface);box-shadow:0 0 0 10px #0b0f19,0 20px 40px rgba(0,0,0,.4);overflow:hidden;margin:6px auto 0;position:relative}
.phone .in{padding:58px 16px 0}
.phone .ed{font-size:16px;padding:10px 12px}
.phone .pill>span{width:40px;height:40px}
.menu{position:absolute;z-index:6;background:var(--surface);border-radius:12px;box-shadow:0 0 0 1px var(--line),0 18px 36px -16px var(--scrim);padding:6px;min-width:13rem;display:grid}
.menu span{display:flex;gap:10px;align-items:center;padding:9px 10px;border-radius:9px;font-size:var(--text-row);font-weight:600;color:var(--ink)}
.menu span svg{color:var(--accent)}.menu span.h{background:var(--selected-fill)}.menu span.d{color:var(--danger)}.menu span.d svg{color:var(--danger)}
.menu hr{border:0;height:1px;background:var(--line);margin:4px 6px}
</style>"""


def ph(i, cls="", extra=""):
    return f'<div class="ph {cls}" style="background:{PICS[i]}">{extra}</div>'


def page(name, desk, phone, note, overlay=''):
    bar = f'<div class="bar"><span class="sel">Text <small>▾</small></span><span>{I["bold"]}</span><span><i>I</i></span><span><s>S</s></span><span class="ins">{I["plus"]}Insert</span></div>'
    html = (f'<!doctype html><html data-scheme="dark"><head><meta charset="utf-8"><style>{CSS}</style>{BASE}</head><body>'
            f'<div class="desk"><p class="k">The admin panel · writing a post</p><div class="paper">{bar}<div class="ed"><p>Thanks to everyone who came along on Sunday. A few pictures from the morning:</p>{desk}<p style="margin-top:12px">See you next week.</p></div></div>'
            f'<p class="note">{note}</p></div>'
            f'<div class="side"><p class="k">The same editor on the phone</p><div class="phone"><div class="in"><div class="ed">{phone}</div></div>{overlay}</div></div>'
            '</body></html>')
    (HERE / name).write_text(html)

PILL = f'<div class="pill"><span>{I["grip"]}</span><span>{I["plus"]}</span><span>{I["trash"]}</span></div>'

TILE_CSS = """<style>
.tiles{display:grid;grid-template-columns:repeat(3,1fr);gap:10px;position:relative}
.tiles .ph{aspect-ratio:4/3}
.tiles .t{position:relative}
.chip{position:absolute;display:grid;place-items:center;width:30px;height:30px;border-radius:999px;background:var(--surface);color:var(--ink-2);box-shadow:var(--lift)}
.chip.g1{top:8px;left:8px;cursor:grab}.chip.x1{top:8px;right:8px}
.num{position:absolute;left:8px;bottom:8px;min-width:24px;height:24px;border-radius:999px;background:rgb(0 0 0/.55);color:#fff;font-size:12px;font-weight:700;display:grid;place-items:center}
.drop{position:absolute;top:-4px;bottom:-4px;width:3px;border-radius:3px;background:var(--accent);box-shadow:0 0 0 3px color-mix(in srgb,var(--accent) 25%,transparent)}
.ghost{position:absolute;width:150px;transform:rotate(-2deg);box-shadow:0 18px 26px rgb(0 0 0/.4);border-radius:9px;opacity:.9;z-index:4}
.foot{display:flex;align-items:center;gap:10px;margin-top:10px;font-size:var(--text-caption);color:var(--ink-2)}
.foot .done{margin-left:auto;background:var(--primary);color:var(--on-primary);border-radius:999px;padding:7px 16px;font-weight:600}
.phone .tiles{grid-template-columns:1fr 1fr}.phone .chip{width:36px;height:36px}
</style>"""


def tiles(phone=False):
    order = [0, 2] if False else [0, 1, 2]
    cells = []
    for n, i in enumerate(order):
        cls = "dim" if (i == 1 and not phone) else ""
        cells.append(f'<div class="t">{ph(i, cls)}<span class="chip g1">{I["grip"]}</span><span class="chip x1">{I["x"]}</span><span class="num">{n + 1}</span></div>')
    drop = '' if phone else '<span class="drop" style="right:-7px;left:auto;top:-4px;height:auto" ></span>'
    return f'<div class="tiles">{"".join(cells)}</div>'


A_DESK = (TILE_CSS + '<div class="g on"><div class="pill"><span class="w h">' + I["arrange"] + 'Arranging</span></div>'
          '<div style="height:40px"></div><div class="tiles" style="grid-template-columns:repeat(3,1fr)">'
          f'<div class="t">{ph(0)}<span class="chip g1">{I["grip"]}</span><span class="chip x1">{I["x"]}</span><span class="num">1</span></div>'
          f'<div class="t">{ph(1, "dim")}<span class="num">2</span></div>'
          f'<div class="t">{ph(2)}<span class="chip g1">{I["grip"]}</span><span class="chip x1">{I["x"]}</span><span class="num">3</span><span class="drop" style="left:auto;right:-7px"></span></div>'
          f'<div class="ghost" style="left:470px;top:60px">{ph(1)}</div>'
          '</div><div class="foot">Drag a picture by its handle to change the order; × takes it out of the gallery. With the keyboard: Alt ← or Alt → on its handle.<span class="done">Done</span></div></div>')
A_PHONE = (TILE_CSS + '<div class="g on"><div class="tiles">'
           + "".join(f'<div class="t">{ph(i)}<span class="chip g1">{I["grip"]}</span><span class="chip x1">{I["x"]}</span><span class="num">{i + 1}</span></div>' for i in range(3))
           + '</div><div class="foot">Hold a picture and drag it to change the order.<span class="done">Done</span></div></div>')
page("a-arrange.html", A_DESK, A_PHONE,
     "<b>Arrange, then Done.</b> The gallery's pill gains <b>Arrange</b> (⊞) beside drag, add and take out. Arranging lays the pictures out as equal numbered tiles in their order, each with its own handle and ×, so the order is easy to read and drag; Done (or clicking outside) puts the gallery back as it shows in the post. A gallery left with one picture becomes a single picture.")

B_DESK = ('<div class="g on"><div class="fr">'
          f'{ph(0)}'
          f'<div style="position:relative">{ph(1, "pick")}<div class="pill"><span>{I["left"]}</span><span>{I["right"]}</span><span>{I["replace"]}</span><span class="h">{I["trash"]}</span><span class="rule"></span><span class="w">{I["back"]}Whole gallery</span></div><span class="tip" style="top:52px;right:120px">Take this picture out</span></div>'
          f'{ph(2)}</div><span class="cap">Add a caption</span></div>')
B_PHONE = ('<div class="g on"><div class="fr">'
           f'{ph(0)}<div style="position:relative">{ph(1, "pick")}</div>{ph(2)}</div>'
           f'<div class="pill" style="position:static;margin-top:8px;justify-content:space-between;gap:0"><span>{I["left"]}</span><span>{I["right"]}</span><span>{I["replace"]}</span><span>{I["trash"]}</span><span class="w">{I["back"]}All</span></div>'
           '<span class="cap">Picture 2 of 3 chosen</span></div>')
page("b-choose-a-picture.html", B_DESK, B_PHONE,
     "<b>Click a picture to choose it.</b> Clicking the gallery chooses it as now (drag, add, take out). Clicking one picture inside it rings that picture in aqua and its own pill takes over: move it earlier ‹ or later ›, replace it, take it out, or go back to the whole gallery. On the phone the picture's tools sit under the gallery where a thumb reaches them.")

C_DESK = ('<div class="g on">' + PILL.replace('class="pill"', 'class="pill" style="top:10px;right:10px"') + '<div class="fr">'
          f'{ph(0)}'
          f'<div style="position:relative">{ph(1)}<span class="chip" style="position:absolute;right:8px;bottom:8px;width:32px;height:32px;display:grid;place-items:center;border-radius:999px;background:var(--surface);color:var(--ink);box-shadow:var(--lift)">{I["more"]}</span>'
          f'<div class="menu" style="right:8px;top:calc(100% - 4px)"><span>{I["left"]}Move earlier</span><span class="h">{I["right"]}Move later</span><span>{I["replace"]}Replace</span><hr><span class="d">{I["trash"]}Take this picture out</span></div></div>'
          f'<div style="position:relative">{ph(2)}</div></div><span class="cap">Add a caption</span></div>')
C_PHONE_CSS = '<style>.psheet{position:absolute;left:0;right:0;bottom:0;background:var(--surface);border-radius:22px 22px 0 0;box-shadow:0 -10px 30px rgb(0 0 0/.35);padding:10px 16px 28px;z-index:9}.psheet .grab{width:38px;height:5px;border-radius:3px;background:var(--line);margin:0 auto 12px}.psheet h4{margin:0 0 6px;font-size:17px}.psheet span{display:flex;gap:12px;align-items:center;min-height:48px;border-top:1px solid var(--line);font-weight:600}.psheet span svg{color:var(--accent)}.psheet span.d{color:var(--danger)}.psheet span.d svg{color:var(--danger)}.pscrim{position:absolute;inset:0;background:rgb(0 0 0/.45);z-index:8}</style>'
dot = lambda: f'<span style="position:absolute;right:6px;bottom:6px;width:32px;height:32px;display:grid;place-items:center;border-radius:999px;background:var(--surface);color:var(--ink);box-shadow:var(--lift)">{I["more"]}</span>'
C_PHONE = (C_PHONE_CSS + '<div class="g"><div class="fr">' + "".join(f'<div style="position:relative">{ph(i)}{dot()}</div>' for i in range(3)) + '</div></div>'
           '')
C_SHEET = f'<div class="pscrim"></div><div class="psheet"><div class="grab"></div><h4>Picture 2 of 3</h4><span>{I["left"]}Move earlier</span><span>{I["right"]}Move later</span><span>{I["replace"]}Replace</span><span class="d">{I["trash"]}Take this picture out</span></div>'
page("c-picture-menu.html", C_DESK, C_PHONE,
     "<b>A ⋯ on each picture.</b> In a gallery every picture carries a small ⋯ at its bottom right (on hover at a desk, always on the phone). It opens the shared menu: Move earlier, Move later, Replace, and Take this picture out. The gallery's own pill is unchanged. On the phone the ⋯ opens a sheet.", C_SHEET)
