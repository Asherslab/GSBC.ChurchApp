import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE.parent / "church-news"))
from make import pic, _inline, TABS

BACK = '<header class="topbar topbar--solid"><div class="topbar__start"><button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>News</button></div><span class="topbar__title"></span><div class="topbar__end"><button class="icon-btn" aria-label="More"><i data-icon="more" data-size="md"></i></button></div></header>'
WRITE = '<header class="topbar topbar--solid"><div class="topbar__start"><button class="btn btn--link">Cancel</button></div><span class="topbar__title">New post</span><div class="topbar__end"><button class="btn btn--link"><b>Post</b></button></div></header>'

BASE = """<style>
.ae-post{padding:16px var(--gutter) 40px}
.ae-post h1{font-size:var(--text-title);line-height:1.2;letter-spacing:-.015em;margin:0 0 8px}
.ae-meta{font-size:var(--text-caption);color:var(--ink-2);margin-bottom:16px}.ae-meta b{color:var(--accent);font-weight:600}
.ae-post .rich-text{font-size:var(--text-body);line-height:1.6}.ae-post .rich-text p{margin:0 0 .85em}
.ae-card{margin:4px 0 18px;position:relative}
.ae-post .rich-text .ae-card p,.ae-field .rich-text .ae-card p,.ae-states .ae-card p{margin:0}.rich-text .ae-card .ae-l,.ae-card .ae-l{margin-top:3px}.rich-text .ae-card .ae-change,.rich-text .ae-card .ae-off{margin-top:5px}
.topbar .btn--link{color:var(--on-masthead)}
.sk-pic{display:grid;place-items:center}
.ae-t{font-size:var(--text-row);font-weight:700;line-height:1.3;color:var(--ink);margin:0}
.ae-l{font-size:var(--text-caption);color:var(--ink-2);line-height:1.45;margin:2px 0 0;display:flex;gap:6px;align-items:flex-start}
.ae-l .icon{width:15px;height:15px;flex:none;margin-top:2px;color:var(--ink-3)}
.ae-change{font-size:var(--text-caption);font-weight:600;color:var(--marker-ink);margin:4px 0 0}
.ae-off{font-size:var(--text-caption);font-weight:700;color:var(--danger);margin:4px 0 0}
.ae-gone{font-size:var(--text-caption);color:var(--ink-2);padding:12px 14px;border-radius:var(--radius-inner);background:var(--fill-on-surface);display:flex;gap:8px;align-items:center}
.ae-gone .icon{color:var(--ink-3)}
.ae-prog{display:flex;align-items:center;gap:8px;margin-top:8px;font-size:var(--text-caption);color:var(--ink-2)}.ae-prog .progress-track{flex:1;max-width:140px}
.ae-acts{display:flex;gap:8px;flex-wrap:wrap;margin-top:10px}
.ae-tile{display:grid;place-items:center;border-radius:var(--radius-tile);color:#fff}
.ae-art{background:linear-gradient(140deg,#4a7355,#84a87d);color:#fffcd1;font-weight:700;font-size:9px;letter-spacing:.04em;text-align:center;line-height:1.15;padding:4px}
.ae-note{font-size:var(--text-caption);color:var(--ink-2);display:flex;gap:6px;align-items:center;margin-top:6px}.ae-note .icon{width:15px;height:15px;color:var(--ink-3)}
.ae-ed{padding:12px var(--gutter) 40px}.ae-ed .ae-title{font-size:var(--text-headline);font-weight:700;letter-spacing:-.015em;padding:6px 0 10px;border-bottom:1px solid var(--line);margin-bottom:10px}
.ae-field{background:var(--field);border-radius:var(--radius-field);padding:12px 12px 4px}
.ae-field .rich-text{font-size:var(--text-body);line-height:1.6}.ae-field .rich-text p{margin:0 0 .7em}
.ae-chosen{outline:2px solid var(--accent);outline-offset:4px;border-radius:var(--radius-inner)}
.cm-media__tools{position:absolute;top:8px;right:8px;display:flex;gap:2px;padding:2px;border-radius:999px;background:var(--surface);box-shadow:var(--lift);z-index:2}
.cm-media__tool{display:grid;place-items:center;width:32px;height:32px;border-radius:999px;color:var(--ink-2)}
.ae-bar{display:flex;gap:4px;align-items:center;padding:6px 0 10px;overflow:hidden;white-space:nowrap}
.ae-bar .btn--primary{flex:none}.ae-bar span{width:36px;height:36px;display:grid;place-items:center;color:var(--ink-2);flex:none}
.ae-states h2{font-size:var(--text-caption);font-weight:700;color:var(--ink-3);margin:18px 0 8px}
</style>"""

EVENT = dict(day="SAT", date="20", month="DEC", title="Carols on the lawn", when="Saturday 20 December, 6:30 to 8:00pm",
             where="The front lawn, 1 Church Road", change="Now starts at 6:30pm (was 6pm)")
SERMON = dict(title="The breath of the Almighty", series="Almighty God", part="Part 4", by="Sunday morning", length="38 min", left="14 min left")
MINISTRY = dict(title="Welcome team", line="Greeting people at the door and helping newcomers find their way.", meets="Sundays from 10:00am, at the front doors", colour="#009c8f", icon="hand-heart", act="Join")
GROUP = dict(title="Tuesday night Life Group", line="A dozen people working through Job together over supper.", meets="Tuesdays 7:30pm, in a home in Albany Creek", colour="#e7ae74", icon="users-round", act="Ask to join")
TOOLS = '<span class="cm-media__tools"><span class="cm-media__tool"><i data-icon="grip"></i></span><span class="cm-media__tool"><i data-icon="refresh"></i></span><span class="cm-media__tool"><i data-icon="x"></i></span></span>'


def line(icon, text):
    return f'<p class="ae-l"><i data-icon="{icon}"></i><span>{text}</span></p>'


def btn(text, primary=False, icon=""):
    i = f'<i data-icon="{icon}"></i>' if icon else ""
    return f'<button class="btn btn--small {"btn--primary" if primary else "btn--tinted"}">{i}{text}</button>'


def post(cards):
    intro = '<p>Carols on the lawn is on again, and this year we\'re asking every group to bring a plate. Here\'s everything in one place.</p>'
    middle = '<p>If you missed Sunday, the message is worth the listen before you come.</p>'
    end = '<p>New this year: the Welcome team needs a few more people on the night, and the Tuesday group is hosting supper after.</p>'
    e, s, m, g = cards
    return (BASE + '<article class="ae-post"><h1>Carols on the lawn, and how to help</h1><p class="ae-meta"><b>Church news</b> · Church office · Friday 14 November</p>'
            f'<div class="rich-text">{intro}{e}{middle}{s}{end}{m}{g}</div></article>')


def editor(card_event, card_ministry, gone):
    bar = ('<div class="ae-bar"><button class="btn btn--small btn--primary"><i data-icon="plus"></i>Insert</button>'
           + "".join(f'<span><i data-icon="{n}"></i></span>' for n in ["list", "link", "more"]) + "</div>")
    return (BASE + '<div class="ae-ed"><div class="ae-title">Carols on the lawn, and how to help</div>' + bar
            + '<div class="ae-field"><div class="rich-text"><p>Carols on the lawn is on again, and this year we\'re asking every group to bring a plate.</p>'
            + f'<div class="ae-chosen" style="position:relative">{TOOLS}{card_event}</div><p>The Welcome team needs a few more people on the night.</p>'
            + f'<div style="position:relative">{card_ministry}</div><p class="ae-note"><i data-icon="eye-off"></i>Only people who can see this ministry will see its card.</p>'
            + f'<p style="margin-top:12px">Last year\'s photos:</p>{gone}</div></div></div>')


ICON_OF = dict(t.split(":") for t in TABS.split(","))


def screen(body, topbar):
    tabbar = "".join(f'<span class="{"on" if label == "News" else ""}"><i data-icon="{ICON_OF[label]}" data-size="md"></i>{label}</span>' for label in ICON_OF)
    return (f'<div class="ae-phone"><div class="sk-screen"><div class="page-scroll">{topbar}<div class="page">{body}</div></div></div>'
            f'<div class="sk-status"><span>9:41</span><span>●●● ▮</span></div><nav class="sk-tabbar">{tabbar}</nav></div>')


def frame(title, panels):
    cells = "".join(f'<figure><figcaption>{label}</figcaption>{doc}</figure>' for label, doc in panels)
    return (f'<!doctype html><html lang="en-AU"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>{_inline()}<style>'
            'body.ae-sheet{margin:0;padding:20px;background:#e9e1d4;display:flex;gap:28px;width:max-content;--sat:47px}'
            '.ae-sheet figure{margin:0}.ae-sheet figcaption{margin:0 0 10px 4px;font-weight:700;font-size:14px;color:#18233a}'
            '.ae-phone{width:390px;height:844px;position:relative;overflow:hidden;border-radius:30px;background:var(--page);box-shadow:0 0 0 8px #18233a,0 18px 40px rgba(24,35,58,.25)}'
            '.ae-phone .sk-screen{position:absolute;inset:0;overflow:hidden}.ae-phone .page-scroll{position:absolute;inset:0;overflow-y:auto;padding-bottom:96px}'
            '.ae-phone .section{padding:0 var(--gutter)}.ae-sheet .sk-theme{position:fixed}'
            f'</style></head><body class="ae-sheet">{cells}</body></html>')


def build(name, title, kind_css, ev, se, mi, gr, ev_off, ev_every, gone):
    style = "<style>" + kind_css + "</style>"
    p1 = screen(style + post([ev, se, mi, gr]), BACK)
    p2 = screen(style + editor(ev, mi, gone), WRITE)
    p3 = screen(style + BASE + '<div class="ae-post ae-states"><h2>Cancelled</h2>' + ev_off + '<h2>A regular event, as its pattern</h2>' + ev_every
               + '<h2>A sermon you\'ve finished</h2>' + se.replace(SERMON["left"], "Heard").replace('style="width:63%"', 'style="width:100%"')
               + '<h2>No longer on</h2>' + gone + "</div>", BACK)
    (HERE / name).write_text(frame(title, [("In the post", p1), ("Writing it", p2), ("Other states", p3)]))


RAIL = """
.ar{display:grid;grid-template-columns:1fr auto;gap:12px;padding:2px 0 2px 14px;border-left:4px solid var(--k,var(--accent))}
.ar-k{font-size:var(--text-pill);font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--k,var(--accent));margin:0 0 3px;display:flex;gap:5px;align-items:center}
.ar-k .icon{width:14px;height:14px}
.ar .sk-pic{width:68px;height:68px;border-radius:var(--radius-inner)}
.ar .ae-tile{width:68px;height:68px}
.ar .ae-acts .btn--link{font-size:var(--text-caption)}
.ar .ae-acts{gap:4px 14px}
"""


def a_card(kicker, icon, k, body, side, acts):
    return (f'<div class="ae-card ar" style="--k:{k}"><div><p class="ar-k"><i data-icon="{icon}"></i>{kicker}</p>{body}'
            f'<div class="ae-acts">{acts}</div></div>{side}</div>')


def link(text):
    return f'<button class="btn btn--link">{text}</button>'


a_ev = a_card("Event · Sat 20 Dec", "calendar", "var(--accent)", f'<p class="ae-t">{EVENT["title"]}</p>' + line("clock", EVENT["when"]) + line("map-pin", EVENT["where"]) + f'<p class="ae-change">{EVENT["change"]}</p>',
              pic("a"), link("Add to calendar") + link("Open"))
a_se = a_card("Sermon · " + SERMON["series"], "sermons", "var(--primary)", f'<p class="ae-t">{SERMON["title"]}</p>' + line("mic", f'{SERMON["by"]} · {SERMON["part"]} · {SERMON["length"]}')
              + f'<div class="ae-prog"><span class="progress-track"><i style="width:63%"></i></span>{SERMON["left"]}</div>',
              '<span class="ae-tile ae-art" style="width:68px;height:68px">ALMIGHTY<br>GOD</span>', link("Open the sermon"))
a_mi = a_card("Ministry", "hand-heart", MINISTRY["colour"], f'<p class="ae-t">{MINISTRY["title"]}</p><p class="ae-l">{MINISTRY["line"]}</p>' + line("clock", MINISTRY["meets"]),
              f'<span class="ae-tile" style="background:{MINISTRY["colour"]}"><i data-icon="{MINISTRY["icon"]}" data-size="lg"></i></span>', link("Join") + link("Open"))
a_gr = a_card("Life Group", "users-round", "#b9772f", f'<p class="ae-t">{GROUP["title"]}</p><p class="ae-l">{GROUP["line"]}</p>' + line("clock", GROUP["meets"]),
              f'<span class="ae-tile" style="background:{GROUP["colour"]}"><i data-icon="{GROUP["icon"]}" data-size="lg"></i></span>', link("Ask to join") + link("Open"))
a_off = a_card("Event · Sat 20 Dec", "calendar", "var(--danger)", f'<p class="ae-t"><s>{EVENT["title"]}</s></p><p class="ae-off">Cancelled: the forecast is for storms</p>', pic("a"), link("Open"))
a_every = a_card("Event · Every Sunday", "repeat", "var(--accent)", '<p class="ae-t">Sunday worship</p>' + line("clock", "10:30am and 6:00pm") + line("map-pin", "The main auditorium"), pic("b"), link("Open"))
GONE = '<div class="ae-card ae-gone"><i data-icon="circle-off"></i>This event is no longer on</div>'
build("a-rail.html", "A rule down the side", RAIL, a_ev, a_se, a_mi, a_gr, a_off, a_every, GONE)

TT = """
.tt{display:grid;grid-template-columns:64px 1fr;gap:14px;padding:14px 0;border-top:1px solid var(--line);border-bottom:1px solid var(--line)}
.tt + p, .tt + .tt{margin-top:0}
.tt-day{display:grid;justify-items:center;align-content:start;padding:8px 0 10px;border-radius:var(--radius-tile);background:color-mix(in srgb,var(--live) 12%,transparent);color:var(--accent);line-height:1}
.tt-day small{font-size:var(--text-pill);font-weight:700;letter-spacing:.06em}.tt-day b{font-size:26px;font-weight:700;margin:4px 0 2px;color:var(--ink)}.tt-day em{font-style:normal;font-size:var(--text-pill);font-weight:700}
.tt-day.off{background:color-mix(in srgb,var(--danger) 12%,transparent);color:var(--danger)}
.tt .ae-tile{width:64px;height:64px}
.tt-k{font-size:var(--text-caption);font-weight:600;color:var(--accent);margin:0 0 2px}
"""


def b_card(lead, kicker, body, acts):
    return f'<div class="ae-card tt">{lead}<div><p class="tt-k">{kicker}</p>{body}<div class="ae-acts">{acts}</div></div></div>'


DAY = f'<span class="tt-day"><small>{EVENT["day"]}</small><b>{EVENT["date"]}</b><em>{EVENT["month"]}</em></span>'
b_ev = b_card(DAY, "Event", f'<p class="ae-t">{EVENT["title"]}</p>' + line("clock", "6:30 to 8:00pm") + line("map-pin", EVENT["where"]) + f'<p class="ae-change">{EVENT["change"]}</p>',
              btn("Add to calendar", True, "calendar-plus") + btn("Open"))
b_se = b_card('<span class="ae-tile ae-art">ALMIGHTY<br>GOD</span>', f'Sermon · {SERMON["series"]}, {SERMON["part"].lower()}', f'<p class="ae-t">{SERMON["title"]}</p>' + line("mic", f'{SERMON["by"]} · {SERMON["length"]}')
              + f'<div class="ae-prog"><span class="progress-track"><i style="width:63%"></i></span>{SERMON["left"]}</div>', btn("Open the sermon", False, "sermons"))
b_mi = b_card(f'<span class="ae-tile" style="background:{MINISTRY["colour"]}"><i data-icon="{MINISTRY["icon"]}" data-size="lg"></i></span>', "Ministry",
              f'<p class="ae-t">{MINISTRY["title"]}</p><p class="ae-l">{MINISTRY["line"]}</p>' + line("clock", MINISTRY["meets"]), btn("Join", True) + btn("Open"))
b_gr = b_card(f'<span class="ae-tile" style="background:{GROUP["colour"]}"><i data-icon="{GROUP["icon"]}" data-size="lg"></i></span>', "Life Group",
              f'<p class="ae-t">{GROUP["title"]}</p><p class="ae-l">{GROUP["line"]}</p>' + line("clock", GROUP["meets"]), btn("Ask to join", True) + btn("Open"))
b_off = b_card(DAY.replace("tt-day", "tt-day off"), "Event", f'<p class="ae-t"><s>{EVENT["title"]}</s></p><p class="ae-off">Cancelled: the forecast is for storms</p>', btn("Open"))
b_every = b_card('<span class="tt-day"><small>EVERY</small><b>SUN</b><em>&nbsp;</em></span>', "Event", '<p class="ae-t">Sunday worship</p>' + line("clock", "10:30am and 6:00pm") + line("map-pin", "The main auditorium"), btn("Open"))
build("b-timetable.html", "A line in the bulletin's timetable", TT, b_ev, b_se, b_mi, b_gr, b_off, b_every, GONE)

BOX = """
.bx{background:var(--fill-on-surface);border-radius:var(--radius);overflow:hidden}
.bx .sk-pic{display:block;height:auto;aspect-ratio:16/7;border-radius:0}
.bx-cover{aspect-ratio:16/7;display:grid;place-items:center;color:#fff}.bx-cover .icon{width:40px;height:40px;opacity:.9}
.bx-in{padding:12px 14px 14px}
.bx-k{font-size:var(--text-caption);font-weight:600;color:var(--accent);margin:0 0 2px;display:flex;gap:5px;align-items:center}.bx-k .icon{width:15px;height:15px}
.bx .ae-t{font-size:var(--text-lead)}
.bx .ae-acts .btn--primary{flex:1;justify-content:center}
.bx-se{display:grid;grid-template-columns:96px 1fr;gap:12px;align-items:center;padding:12px 14px 0}
.bx-se .ae-art{width:96px;height:54px;font-size:10px}
"""


def c_card(cover, kicker, icon, body, acts, extra=""):
    return f'<div class="ae-card bx">{cover}<div class="bx-in"><p class="bx-k"><i data-icon="{icon}"></i>{kicker}</p>{body}<div class="ae-acts">{acts}</div></div>{extra}</div>'


c_ev = c_card(pic("a"), "Event · Saturday 20 December", "calendar", f'<p class="ae-t">{EVENT["title"]}</p>' + line("clock", "6:30 to 8:00pm") + line("map-pin", EVENT["where"]) + f'<p class="ae-change">{EVENT["change"]}</p>',
              btn("Add to calendar", True, "calendar-plus") + btn("Open"))
c_se = (f'<div class="ae-card bx"><div class="bx-se"><span class="ae-tile ae-art">ALMIGHTY GOD</span><div><p class="bx-k"><i data-icon="sermons"></i>Sermon · {SERMON["series"]}</p><p class="ae-t">{SERMON["title"]}</p></div></div>'
        f'<div class="bx-in">' + line("mic", f'{SERMON["by"]} · {SERMON["part"]} · {SERMON["length"]}') + f'<div class="ae-prog"><span class="progress-track"><i style="width:63%"></i></span>{SERMON["left"]}</div><div class="ae-acts">{btn("Open the sermon", True, "sermons")}</div></div></div>')
c_mi = c_card(f'<div class="bx-cover" style="background:{MINISTRY["colour"]}"><i data-icon="{MINISTRY["icon"]}"></i></div>', "Ministry", "hand-heart",
              f'<p class="ae-t">{MINISTRY["title"]}</p><p class="ae-l">{MINISTRY["line"]}</p>' + line("clock", MINISTRY["meets"]), btn("Join", True) + btn("Open"))
c_gr = c_card(f'<div class="bx-cover" style="background:{GROUP["colour"]}"><i data-icon="{GROUP["icon"]}"></i></div>', "Life Group", "users-round",
              f'<p class="ae-t">{GROUP["title"]}</p><p class="ae-l">{GROUP["line"]}</p>' + line("clock", GROUP["meets"]), btn("Ask to join", True) + btn("Open"))
c_off = c_card('<div style="filter:grayscale(1);opacity:.6">' + pic("a") + "</div>", "Event · Saturday 20 December", "calendar", f'<p class="ae-t"><s>{EVENT["title"]}</s></p><p class="ae-off">Cancelled: the forecast is for storms</p>', btn("Open"))
c_every = c_card(pic("b"), "Event · Every Sunday", "repeat", '<p class="ae-t">Sunday worship</p>' + line("clock", "10:30am and 6:00pm") + line("map-pin", "The main auditorium"), btn("Open"))
build("c-bulletin-box.html", "A box from the bulletin", BOX, c_ev, c_se, c_mi, c_gr, c_off, c_every, GONE)
