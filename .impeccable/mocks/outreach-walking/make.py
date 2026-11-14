import base64
import re
from pathlib import Path

HERE = Path(__file__).parent
MOCKS = HERE.parent
ROOT = MOCKS.parent.parent


def _css(path):
    text = path.read_text()

    def embed(m):
        target = (path.parent / m.group(1)).resolve()
        if not target.exists():
            return m.group(0)
        kind = {"woff2": "font/woff2", "svg": "image/svg+xml"}.get(target.suffix[1:], "application/octet-stream")
        return 'url("data:' + kind + ';base64,' + base64.b64encode(target.read_bytes()).decode() + '")'

    text = re.sub(r'url\("(\.\./(?:fonts|img)/[^"]+)"\)', embed, text)
    return re.sub(r'@import url\("([^"]+)"\);', lambda m: _css((path.parent / m.group(1)).resolve()), text)


def _map(name):
    return "data:image/webp;base64," + base64.b64encode((HERE / "maps" / f"{name}.webp").read_bytes()).decode()


SKETCH_CSS = """
html,body{width:1440px;height:900px;overflow:hidden}
.sk-mini .segmented button{white-space:nowrap}
body.sk-board{background:var(--sand-strong);padding:22px 40px 0}
.sk-board__head{display:flex;align-items:baseline;gap:18px;margin:0 0 16px 20px}
.sk-board__head h1{font-size:1.3rem;font-weight:700;letter-spacing:-.02em;color:var(--ink)}
.sk-board__head p{font-size:var(--text-caption);color:var(--ink-2)}
.sk-row{display:flex;gap:22px;justify-content:center}
.sk-slot{width:312px}
.sk-slot figcaption{margin-top:10px;font-size:var(--text-caption);font-weight:600;color:var(--ink-2);text-align:center}
.sk-mini{position:relative;width:390px;height:844px;overflow:hidden;border-radius:34px;background:var(--page);transform:scale(.8);transform-origin:top left;box-shadow:0 0 0 10px var(--device-bezel,#000);--sat:47px;margin-bottom:-169px}
.sk-mini .page-scroll{position:absolute;inset:0;overflow:hidden}
.sk-mini .section{padding:0 var(--gutter)}
.sk-mini .sk-tabbar{position:absolute}
.sk-mini .sk-status{position:absolute}
.sk-mini--light-status .sk-status{color:var(--ink)}
.sk-map{background:var(--map-plan) center/cover no-repeat}
.sk-map--walk{background-image:var(--map-walk)}
.sk-map--fade{position:relative}
.sk-map--fade::after{content:"";position:absolute;inset:0;background:linear-gradient(transparent 60%,var(--page))}
.sk-attrib{font-size:.55rem;color:var(--ink-3);padding:4px var(--gutter) 0}
.sk-figure{display:block;border-radius:var(--radius-card);height:170px;overflow:hidden}
.sk-big{font-size:var(--text-headline);font-weight:700;letter-spacing:-.02em;line-height:1.1;color:var(--ink);font-variant-numeric:tabular-nums}
.sk-mid{font-size:var(--text-title);font-weight:700;letter-spacing:-.015em;line-height:1.2}
.sk-cap{font-size:var(--text-caption);color:var(--ink-2);line-height:1.35}
.sk-micro{font-size:var(--text-micro);font-weight:600;letter-spacing:.08em;text-transform:uppercase;color:var(--ink-3)}
.sk-stack{display:grid;gap:var(--space-md)}
.sk-line{display:flex;gap:var(--space-sm);align-items:center;flex-wrap:wrap}
.sk-note{position:static;max-width:none;box-shadow:none;padding:13px 14px;border-radius:var(--radius-field);background:var(--sand);color:var(--ink);font-size:var(--text-caption);line-height:1.4}
.sk-note b{font-weight:700}
.sk-note .btn--link{display:block;margin-top:6px;padding:0;min-height:0;background:none;color:var(--accent);font-weight:600;font-size:var(--text-caption);text-align:left;border:0}
.sk-pill-row{display:flex;gap:6px;flex-wrap:wrap}
.sk-chip{display:inline-flex;align-items:center;gap:6px;height:32px;padding:0 12px;border-radius:999px;background:var(--field);font-size:var(--text-caption);font-weight:600;color:var(--ink-2)}
.sk-chip.on{background:var(--primary);color:var(--on-primary)}
.sk-chip--masthead{background:var(--masthead-chip);color:var(--on-masthead)}
.sk-chip--masthead.on{background:var(--on-masthead);color:var(--masthead)}
.sk-progress{height:6px;border-radius:3px;background:var(--line);overflow:hidden}
.sk-progress i{display:block;height:100%;background:var(--marker);border-radius:3px}
.sk-progress--masthead{background:var(--masthead-chip)}
.sk-figs{display:grid;grid-template-columns:repeat(3,1fr);gap:var(--space-sm)}
.sk-figs div{padding:10px 12px;border-radius:var(--radius-field);background:var(--sand)}
.sk-figs b{display:block;font-size:var(--text-title);font-weight:700;font-variant-numeric:tabular-nums}
.sk-figs span{font-size:var(--text-caption);color:var(--ink-2)}
.sk-btn2{display:grid;grid-template-columns:1fr 1fr;gap:var(--space-sm)}
.sk-btn2 .btn{height:56px;border-radius:var(--radius-card);font-size:var(--text-label)}
.sk-foot{position:absolute;left:0;right:0;bottom:0;padding:12px var(--gutter) 30px;background:var(--bar);border-top:1px solid var(--line);backdrop-filter:blur(20px)}
.sk-foot .btn{width:100%}
.sk-dot{display:inline-block;width:12px;height:12px;border-radius:50%;background:var(--marker);box-shadow:0 0 0 3px var(--surface)}

.a-now{background:var(--masthead);color:var(--on-masthead);padding:calc(var(--sat) + 8px) var(--gutter) 18px}
.a-now__top{display:flex;justify-content:space-between;align-items:center;font-size:var(--text-caption);font-weight:600;color:var(--masthead-note)}
.a-now h2{margin:14px 0 2px;font-size:var(--text-display);font-weight:700;letter-spacing:-.03em;line-height:1.02}
.a-now__side{font-size:var(--text-title);font-weight:600}
.a-now__next{display:flex;gap:10px;align-items:center;margin-top:14px;padding-top:12px;border-top:1px solid var(--masthead-chip);font-size:var(--text-label);font-weight:600}
.a-now__next .icon{flex:none}
.a-walkmap{position:absolute;top:268px;bottom:212px;left:0;right:0}
.a-walkfoot{position:absolute;left:0;right:0;bottom:0;padding:14px var(--gutter) 30px;background:var(--page);border-top:1px solid var(--line);display:grid;gap:10px}
.a-walkfoot__bar{display:grid;grid-template-columns:1fr auto 1fr;align-items:center}
.a-walkfoot__bar .btn--link{border:0;background:none;color:var(--accent);font-weight:600;font-size:var(--text-label)}
.a-progress{display:flex;justify-content:space-between;font-size:var(--text-caption);color:var(--ink-2);font-variant-numeric:tabular-nums;margin-bottom:6px}
.a-ask{padding:18px 16px;border-radius:var(--radius-card);background:var(--sand)}
.a-ask h2{font-size:var(--text-title);font-weight:700;letter-spacing:-.015em}

.b-order{position:relative;margin:0;padding:0;list-style:none}
.b-order li{position:relative;display:grid;grid-template-columns:28px 1fr auto;gap:10px;padding:10px 0;align-items:start}
.b-order li::before{content:"";position:absolute;left:13px;top:0;bottom:0;width:3px;background:var(--line)}
.b-order li:first-child::before{top:20px}
.b-order li:last-child::before{bottom:calc(100% - 20px)}
.b-order li.done::before,.b-order li.here::before{background:var(--accent)}
.b-order li.here::before{bottom:50%}
.b-stop{position:relative;z-index:1;width:16px;height:16px;margin:3px 0 0 7px;border-radius:50%;background:var(--page);box-shadow:inset 0 0 0 3px var(--ink-3)}
.done .b-stop{background:var(--accent);box-shadow:none}
.here .b-stop{width:22px;height:22px;margin:0 0 0 4px;background:var(--marker);box-shadow:0 0 0 4px color-mix(in srgb,var(--marker) 35%,transparent)}
.b-step b{display:block;font-size:var(--text-row);font-weight:600;line-height:1.3}
.b-step span{display:block;font-size:var(--text-caption);color:var(--ink-2)}
.done .b-step b{color:var(--ink-2)}
.b-step s{color:var(--ink-3)}
.b-n{font-size:var(--text-caption);color:var(--ink-3);font-variant-numeric:tabular-nums;padding-top:2px}
.here .b-step b{font-size:var(--text-title);font-weight:700}
.b-connector .b-step b{font-weight:500;color:var(--ink-3)}
.b-strip{height:236px}
.b-sheet{position:absolute;left:0;right:0;top:220px;bottom:0;background:var(--page);border-radius:22px 22px 0 0;padding:10px var(--gutter) 120px;overflow:hidden}
.b-grab{width:36px;height:5px;border-radius:3px;background:var(--line);margin:0 auto 10px}

.c-full{position:absolute;inset:0}
.c-card{position:absolute;left:12px;right:12px;top:calc(var(--sat) + 8px);padding:12px 14px;border-radius:var(--radius-card);background:var(--masthead);color:var(--on-masthead);box-shadow:var(--lift);display:grid;grid-template-columns:auto 1fr;gap:4px 12px;align-items:center}
.c-card b{font-size:var(--text-title);font-weight:700;letter-spacing:-.015em}
.c-card span{grid-column:2;font-size:var(--text-caption);color:var(--masthead-note)}
.c-sheet{position:absolute;left:0;right:0;bottom:0;background:var(--page);border-radius:22px 22px 0 0;padding:10px var(--gutter) 30px;box-shadow:0 -6px 22px rgb(0 0 0 / .14)}
.c-tools{display:grid;grid-template-columns:repeat(4,1fr);gap:6px;margin:14px 0}
.c-tools span{display:grid;justify-items:center;gap:6px;padding:10px 4px;border-radius:var(--radius-field);background:var(--field);font-size:var(--text-micro);font-weight:600;color:var(--ink);text-align:center}
.c-tools .icon{color:var(--accent)}
"""


def _inline():
    css = "".join(_css(f) for f in [MOCKS / "design-system" / "kit.css"]) + SKETCH_CSS
    css += f":root{{--map-plan:url({_map('plan-light')});--map-walk:url({_map('walk-light')})}}"
    css += f":root[data-scheme=dark]{{--map-plan:url({_map('plan-dark')});--map-walk:url({_map('walk-dark')})}}"
    js = (MOCKS / "design-system" / "icons.js").read_text() + "\n" + (MOCKS / "design-system" / "kit.js").read_text()
    return "<style>" + css + "</style><script>" + js + "</script>"


TABS = "Home:home,Sermons:sermons,Calendar:calendar,Outreach:footprints,More:menu"


def tabbar(on="Outreach"):
    return '<nav class="sk-tabbar">' + "".join(
        f'<span class="{"on" if label == on else ""}"><i data-icon="{icon}" data-size="md"></i>{label}</span>'
        for label, icon in (t.split(":") for t in TABS.split(","))) + "</nav>"


STATUS = '<div class="sk-status"><span>9:41</span><span>●●● ▮</span></div>'


def phone(body, caption, tabs=True, light_status=False):
    cls = "sk-mini sk-mini--light-status" if light_status else "sk-mini"
    return (f'<figure class="sk-slot"><div class="{cls}">{body}{STATUS}{tabbar() if tabs else ""}</div>'
            f"<figcaption>{caption}</figcaption></figure>")


def board(title, line, phones):
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>{title}</title>{_inline()}</head>
<body class="sk-board"><div class="sk-board__head"><h1>{title}</h1><p>{line}</p></div>
<div class="sk-row">{"".join(phones)}</div></body></html>"""


def topbar(back="", title="", solid=True, end=""):
    start = f'<button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>{back}</button>' if back else ""
    return (f'<header class="topbar {"topbar--solid" if solid else ""}"><div class="topbar__start">{start}</div>'
            f'<div class="topbar__title">{title}</div><div class="topbar__end">{end}</div></header>')


BELL = '<button class="icon-btn"><i data-icon="bell" data-size="md"></i></button>'


def masthead(title, note, extra=""):
    return f'<header class="masthead"><h1>{title}</h1><p class="masthead__note">{note}</p>{extra}</header>'


def row(title, sub="", icon="", trail="", cls=""):
    ic = f'<span class="row__icon {cls}"><i data-icon="{icon}"></i></span>' if icon else ""
    tr = f'<span class="row__trail">{trail}<i data-icon="chevron-right"></i></span>'
    sb = f'<span class="row__subtitle">{sub}</span>' if sub else ""
    return f'<a class="row">{ic}<span class="row__body"><span class="row__title">{title}</span>{sb}</span>{tr}</a>'


def seg(options, on):
    return '<div class="segmented" role="radiogroup">' + "".join(
        f'<button role="radio" aria-checked="{"true" if o == on else "false"}">{o}</button>' for o in options) + "</div>"


ATTRIB = '<p class="sk-attrib">© OpenStreetMap contributors · State of Queensland</p>'
REACH = ('<div class="sk-note"><b>Streets near you run out at 2.4 km.</b> You asked for 3 km. Albany Creek Road and the '
         'school bound this pocket.<button class="btn--link">Walk 290 m further to reach Flamingo Dr: about 150 more letterboxes</button>'
         '<button class="btn--link">Start from the Bevlin Ct area, 310 m away</button></div>')


a_home = phone(
    '<div class="page-scroll">' + topbar(solid=False, end=BELL) + '<div class="page">'
    + masthead("Outreach", "General outreach · Carols 2026")
    + '<section class="section section--first"><div class="a-ask sk-stack"><h2>Got an hour? Walk from your door</h2>'
    + seg(["30 min", "1 hr", "2 hr", "Distance"], "1 hr")
    + '<div class="sk-cap">From here, Albany Creek · carrying carols flyers and tracts</div>'
    + '<button class="btn btn--primary btn--block">Plan my walk</button></div></section>'
    + '<section class="section"><p class="sk-micro" style="margin-bottom:8px">Around you</p><span class="sk-figure sk-map"></span>'
    + '<p class="sk-cap" style="margin-top:8px">Most streets within 500 m were walked this month. The app starts you where they weren’t.</p></section>'
    + '<section class="section"><div class="list">' + row("Outreach Saturday", "Sat 13 Dec, 8:30 to 11:30 · 9 routes", "calendar")
    + row("My outreach", "14 walks · 1,240 letterboxes", "footprints") + "</div></section></div></div>",
    "Outreach: the question first")

a_plan = phone(
    '<div class="page-scroll">' + topbar("Outreach", "Your walk") + '<div class="page"><div class="sk-map" style="height:300px"></div>' + ATTRIB
    + '<section class="section section--first sk-stack" style="margin-top:12px"><div><div class="sk-big">2.4 km · 52 min</div>'
    + '<div class="sk-cap">88 letterboxes · take 2 bundles · home by 5:40, sunset 6:41</div></div>' + REACH
    + "</section></div></div>"
    + '<div class="sk-foot"><button class="btn btn--primary btn--block">Start the walk</button></div>',
    "The plan: figures, then why", tabs=False)

a_walk = phone(
    '<div class="a-now"><div class="a-now__top"><span>Walking · 24 min</span><span>General + Carols</span></div>'
    + '<h2>Kingfisher Street</h2><div class="a-now__side">Houses on your left · 14 letterboxes</div>'
    + '<div class="a-now__next"><i data-icon="arrow-left"></i>Then left into Robin Court · 8</div></div>'
    + '<div class="a-walkmap sk-map sk-map--walk"></div>'
    + '<div class="a-walkfoot"><div><div class="a-progress"><span>38 of 88 letterboxes</span><span>1.1 of 2.4 km</span></div>'
    + '<div class="sk-progress"><i style="width:43%"></i></div></div>'
    + '<div class="sk-btn2"><button class="btn btn--quiet">No junk mail</button><button class="btn btn--quiet">Spoke to someone</button></div>'
    + '<div class="a-walkfoot__bar"><button class="btn--link" style="justify-self:start">Pause</button><button class="icon-btn"><i data-icon="ellipsis" data-size="md"></i></button><button class="btn--link" style="justify-self:end">Finish</button></div></div>',
    "The walk: the street, big enough for a glance", tabs=False)

a_done = phone(
    '<div class="page-scroll">' + topbar(solid=False) + '<div class="page">'
    + masthead("Well walked", "Tuesday afternoon · General outreach and Carols 2026")
    + '<section class="section section--first"><div class="sk-figs"><div><b>2.3 km</b><span>walked</span></div><div><b>84</b><span>letterboxes</span></div><div><b>2</b><span>chats</span></div></div></section>'
    + '<section class="section"><p class="sk-micro" style="margin-bottom:8px">Check what you covered</p><div class="list">'
    + row("Kingfisher St, both sides", "28 letterboxes", trail="Done") + row("Robin Ct, both sides", "15 letterboxes", trail="Done")
    + row("Flamingo Dr, this side", "12 letterboxes", trail="Skipped") + "</div></section></div></div>"
    + '<div class="sk-foot"><button class="btn btn--primary btn--block">Save the walk</button></div>',
    "The end: correct, then save", tabs=False)



def step(name, sub, n, state=""):
    return (f'<li class="{state}"><span class="b-stop"></span><span class="b-step"><b>{name}</b><span>{sub}</span></span>'
            f'<span class="b-n">{n}</span></li>')


b_home = phone(
    '<div class="page-scroll">' + topbar(solid=False, end=BELL) + '<div class="page">'
    + masthead("Outreach", "Outreach Saturday is on till 11:30",
               '<div class="masthead__extra sk-pill-row"><span class="sk-chip sk-chip--masthead on">Carols 2026</span><span class="sk-chip sk-chip--masthead">General outreach</span></div>')
    + '<section class="section section--first"><p class="sk-micro" style="margin-bottom:6px">5 of 9 routes claimed · pick one</p><div class="list">'
    + row("Route 3 · Cribb Rd loop", "64 letterboxes · 55 min · open", trail="")
    + row("Route 4 · Greensill Rd", "58 letterboxes · 50 min · Ruth + 1", trail="")
    + row("Route 5 · Albany Creek north", "71 letterboxes · 1 hr · open", trail="")
    + row("Route 6 · Old Northern Rd east", "60 letterboxes · 50 min · Sam", trail="")
    + "</div></section>"
    + '<section class="section"><div class="list">' + row("Or walk from your door", "Pick a time; the app makes a loop", "footprints") + "</div></section></div></div>",
    "Outreach: the day leads when it's on")

b_plan = phone(
    '<div class="page-scroll">' + topbar("Outreach", "Your walk") + '<div class="sk-map b-strip"></div><div class="b-sheet"><div class="b-grab"></div>'
    + '<div class="sk-big" style="font-size:var(--text-title-lg)">1 hr from here · 2.4 km</div><p class="sk-cap">88 letterboxes · 2 bundles · back by 5:40</p>'
    + '<ol class="b-order" style="margin-top:12px">'
    + step("Start at Albany Creek Rd", "Walk 120 m to the first street", "", "")
    + step("Kingfisher St, left side", "14 letterboxes · 6 min", "14")
    + step("Robin Ct, both sides", "15 letterboxes · 8 min", "15")
    + step("Kingfisher St, right side", "16 letterboxes · 7 min", "16")
    + '<li class="b-connector"><span class="b-stop"></span><span class="b-step"><b>Walk 90 m, no letterboxes</b></span><span class="b-n"></span></li>'
    + step("Flamingo Dr, both sides", "24 letterboxes · 11 min", "24")
    + "</ol></div></div>"
    + '<div class="sk-foot"><button class="btn btn--primary btn--block">Start the walk</button></div>',
    "The plan: the route as a running order", tabs=False)

b_walk = phone(
    '<div class="page-scroll">' + topbar("End", "Walking · 24 min", end='<button class="icon-btn"><i data-icon="ellipsis" data-size="md"></i></button>')
    + '<div class="sk-map sk-map--walk b-strip"></div><div class="b-sheet"><div class="b-grab"></div>'
    + '<div class="a-progress"><span>38 of 88 letterboxes</span><span>1.1 of 2.4 km</span></div><div class="sk-progress"><i style="width:43%"></i></div>'
    + '<ol class="b-order" style="margin-top:10px">'
    + step("Kingfisher St, left side", "14 letterboxes", "✓", "done")
    + step("Robin Ct, both sides", "Houses on your right · 7 to go", "8/15", "here")
    + step("Kingfisher St, right side", "16 letterboxes", "16")
    + step("Flamingo Dr, both sides", "24 letterboxes", "24")
    + "</ol></div></div>"
    + '<div class="sk-foot"><div class="sk-btn2"><button class="btn btn--quiet">No junk mail</button><button class="btn btn--quiet">Spoke to someone</button></div></div>',
    "The walk: you're a stop on the line", tabs=False)

b_done = phone(
    '<div class="page-scroll">' + topbar("", "The walk") + '<div class="page"><section class="section section--first">'
    + '<div class="sk-big">2.3 km · 84 letterboxes</div><p class="sk-cap">58 min · 2 chats · General outreach and Carols 2026</p>'
    + '<p class="sk-micro" style="margin:18px 0 6px">What you covered · tap to change</p><ol class="b-order">'
    + step("Kingfisher St, left side", "14 letterboxes", "✓", "done")
    + step("Robin Ct, both sides", "15 letterboxes", "✓", "done")
    + step("Kingfisher St, right side", "16 letterboxes", "✓", "done")
    + '<li><span class="b-stop"></span><span class="b-step"><b><s>Flamingo Dr, this side</s></b><span>Skipped: stays open for someone else</span></span><span class="b-n">0</span></li>'
    + step("Flamingo Dr, far side", "12 letterboxes", "✓", "done")
    + "</ol></section></div></div>"
    + '<div class="sk-foot"><button class="btn btn--primary btn--block">Save the walk</button></div>',
    "The end: the same line, corrected", tabs=False)


c_home = phone(
    '<div class="page-scroll">' + topbar(solid=False, end=BELL) + '<div class="page">'
    + masthead("Outreach", "Letterboxing with the church")
    + '<section class="section section--first"><h2 class="section__title">Walk from your door</h2><div class="sk-pill-row" style="margin-top:10px">'
    + '<span class="sk-chip">30 min</span><span class="sk-chip on">1 hr</span><span class="sk-chip">2 hr</span><span class="sk-chip">3 km</span><span class="sk-chip">Other…</span></div>'
    + '<span class="sk-figure sk-map" style="margin-top:12px;height:150px"></span></section>'
    + '<section class="section"><h2 class="section__title">Carrying</h2><div class="sk-pill-row" style="margin-top:10px"><span class="sk-chip on"><i data-icon="check"></i>General outreach</span><span class="sk-chip on"><i data-icon="check"></i>Carols 2026</span></div></section>'
    + '<section class="section"><div class="list">' + row("Outreach days", "Next: Sat 13 Dec, 8:30", "calendar") + row("My outreach", "14 walks · 1,240 letterboxes", "footprints")
    + "</div></section></div></div>",
    "Outreach: a page of sections")

c_plan = phone(
    '<div class="page-scroll">' + topbar("Outreach", "A 1 hour walk") + '<div class="page"><div class="sk-map" style="height:218px"></div>'
    + '<section class="section section--first" style="margin-top:14px"><h2 class="section__title">A 1 hour walk from here</h2><div class="facts" style="margin-top:8px">'
    + '<div class="fact"><span class="fact__icon"><i data-icon="footprints"></i></span><span class="fact__text">2.4 km, about 52 minutes</span></div>'
    + '<div class="fact"><span class="fact__icon"><i data-icon="mail"></i></span><span class="fact__text">88 letterboxes · take 2 bundles</span></div>'
    + '<div class="fact"><span class="fact__icon"><i data-icon="sun"></i></span><span class="fact__text">Back by 5:40, before sunset</span></div></div>'
    + '<div style="margin-top:12px">' + REACH + "</div></section></div></div>"
    + '<div class="sk-foot"><button class="btn btn--primary btn--block">Start the walk</button></div>',
    "The plan: a page with the map on top", tabs=False)

c_walk = phone(
    '<div class="c-full sk-map sk-map--walk"></div>'
    + '<div class="c-card"><i data-icon="arrow-up" data-size="md"></i><b>Kingfisher St · left side</b><span>14 letterboxes · then left into Robin Ct</span></div>'
    + '<div class="c-sheet"><div class="b-grab"></div><div class="a-progress"><span>38 of 88 letterboxes · 24 min</span><span>1.1 of 2.4 km</span></div><div class="sk-progress"><i style="width:43%"></i></div>'
    + '<div class="c-tools"><span><i data-icon="circle-off" data-size="md"></i>No junk mail</span><span><i data-icon="message-circle-warning" data-size="md"></i>Spoke to someone</span><span><i data-icon="flag" data-size="md"></i>Not a home</span><span><i data-icon="redo" data-size="md"></i>Skip this bit</span></div>'
    + '<div class="sk-btn2"><button class="btn btn--quiet">Pause</button><button class="btn btn--primary">Finish</button></div></div>',
    "The walk: a map app, with tools in a sheet", tabs=False, light_status=True)

c_done = phone(
    '<div class="c-full sk-map sk-map--walk"></div>'
    + '<div class="c-sheet" style="top:300px"><div class="b-grab"></div><h2 class="section__title">You walked 2.3 km</h2>'
    + '<p class="sk-cap">84 letterboxes · 58 min · 2 chats</p><div class="list" style="margin-top:12px">'
    + row("Kingfisher St, both sides", "28 letterboxes", trail="Done") + row("Robin Ct, both sides", "15 letterboxes", trail="Done")
    + row("Flamingo Dr, this side", "12 letterboxes", trail="Skipped") + "</div>"
    + '<button class="btn btn--primary btn--block" style="margin-top:14px">Save the walk</button></div>',
    "The end: the walk on the map, the list beneath", tabs=False, light_status=True)

(HERE / "a-words-first.html").write_text(board("Words first", "The walk leads with the street you're on, big, in the masthead's navy; the map sits under it.", [a_home, a_plan, a_walk, a_done]))
(HERE / "b-running-order.html").write_text(board("The route as a running order", "Every step of the walk on one line, like the bulletin's order of service; you are the orange stop.", [b_home, b_plan, b_walk, b_done]))
(HERE / "c-page-and-map.html").write_text(board("A page, and a map app", "Outreach is a page of sections; the walk itself is a full-screen map with its tools in a sheet.", [c_home, c_plan, c_walk, c_done]))
for f in ("a-words-first.html", "b-running-order.html", "c-page-and-map.html"):
    print(f, (HERE / f).stat().st_size // 1024, "KB")
