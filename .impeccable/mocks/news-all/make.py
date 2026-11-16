from pathlib import Path

HERE = Path(__file__).parent

CSS = """:root{--navy:#2a3d5e;--beige:#d5c9b7;--page:#fff;--sand:#f4efe7;--sand2:#e9e1d4;--ink:#18233a;--ink2:#545e70;--ink3:#626a79;--accent:#00776d;--marker:#e7ae74;--mink:#8a5422;--line:rgba(42,61,94,.12);--field:#f4efe7;--edge:rgba(98,106,121,.7)}
*{box-sizing:border-box;margin:0;padding:0}
body{width:1440px;height:900px;overflow:hidden;background:#fff;font-family:Montserrat,-apple-system,sans-serif;color:var(--ink);display:grid;grid-template-columns:1000px 440px;font-size:15px}
.desk{display:flex;flex-direction:column;height:900px;overflow:hidden;border-right:1px solid var(--line)}
.band{display:flex;justify-content:space-between;align-items:flex-start;padding:26px 28px 18px;border-bottom:1px solid var(--line)}
.band h1{font-size:27px;font-weight:700;letter-spacing:-.02em}.band p{font-size:13.5px;color:var(--ink2);margin-top:4px;max-width:520px;line-height:1.4}
.btns{display:flex;gap:8px}.btn{height:40px;border-radius:22px;padding:0 16px;display:flex;align-items:center;gap:6px;font-size:13.5px;font-weight:600;background:var(--sand);color:var(--ink)}.btn.p{background:var(--navy);color:#fff}
.wb{flex:1;display:grid;min-height:0}
.rail{background:var(--sand);padding:20px 12px;display:flex;flex-direction:column;gap:2px;overflow:hidden}
.rail h4{font-size:13px;font-weight:700;color:var(--ink2);padding:0 10px 8px}.rail h4.next{margin-top:16px;padding-top:14px;border-top:1px solid var(--line)}
.f{display:flex;justify-content:space-between;align-items:center;padding:9px 10px;border-radius:10px;font-size:14px;font-weight:600;color:var(--ink2)}.f b{font-weight:600;color:var(--ink3);font-variant-numeric:tabular-nums}
.f.on{background:#fff;color:var(--ink);box-shadow:0 0 0 1px var(--line)}.f.on b{color:var(--ink)}
.f.new{color:var(--ink)}.f i{font-style:normal;font-size:11px;font-weight:700;color:var(--mink);background:rgba(231,174,116,.3);border-radius:9px;padding:1px 7px;margin-left:6px}
.list{padding:8px 24px;overflow:hidden;border-right:1px solid var(--line)}
.lh{font-size:12.5px;color:var(--ink3);padding:10px 0 8px;display:flex;justify-content:space-between}
.r{display:flex;gap:12px;padding:12px 10px;border-top:1px solid var(--line);align-items:flex-start;margin:0 -10px}
.r.on{background:var(--sand);border-radius:12px;border-top-color:transparent}.r.on+.r{border-top-color:transparent}
.r .t{flex:1;min-width:0}.r b{display:block;font-size:14.5px;font-weight:600}.r span{display:block;font-size:13px;color:var(--ink2);margin-top:3px}
.r span.from{color:var(--accent);font-weight:600}
.th{width:52px;height:52px;border-radius:10px;flex:none;background:linear-gradient(135deg,#2a3d5e,#009c8f)}.th.b{background:linear-gradient(135deg,#4a7355,#84a87d)}.th.c{background:linear-gradient(135deg,#e7ae74,#d5c9b7)}
.pill{display:inline-block;font-size:11.5px;font-weight:700;border-radius:999px;padding:2px 9px;margin-top:6px;margin-right:4px}
.pill.in{background:rgba(0,119,109,.12);color:var(--accent)}.pill.ask{background:rgba(231,174,116,.3);color:var(--mink)}.pill.for{background:var(--sand2);color:var(--ink2)}
table{width:100%;border-collapse:collapse;font-size:13.5px}
th{text-align:left;font-size:12.5px;font-weight:600;color:var(--ink3);padding:10px 10px 8px;border-bottom:1px solid var(--line)}
td{padding:12px 10px;border-bottom:1px solid var(--line);vertical-align:top;color:var(--ink2)}td:first-child{color:var(--ink);font-weight:600}
tr.on td{background:var(--sand)}tr.on td:first-child{border-radius:10px 0 0 10px}tr.on td:last-child{border-radius:0 10px 10px 0}
td.num{font-variant-numeric:tabular-nums;white-space:nowrap}
.pane{padding:20px 22px;overflow:hidden;display:flex;flex-direction:column;gap:12px}
.ph{display:flex;justify-content:space-between;align-items:flex-start;gap:10px}.ph h2{font-size:19px;font-weight:700;letter-spacing:-.015em}.ph p{font-size:13px;color:var(--ink2);margin-top:3px}
.ib{width:36px;height:36px;border-radius:18px;background:var(--sand);display:grid;place-items:center;font-weight:700;color:var(--ink2);flex:none}
.menu{position:absolute;right:22px;top:66px;background:#fff;border-radius:12px;box-shadow:0 10px 30px rgba(24,35,58,.18),0 0 0 1px var(--line);padding:6px;width:270px;font-size:14px;z-index:2}
.menu div{padding:10px 12px;border-radius:9px}.menu div.hi{background:var(--sand)}.menu small{display:block;color:var(--ink3);font-size:12px;margin-top:2px}
.post{border-top:3px solid var(--navy);padding-top:12px}
.post .hd{display:flex;gap:12px;align-items:center}.date{width:52px;text-align:center;border-right:1px solid var(--line);padding-right:10px}.date b{display:block;font-size:22px;font-weight:700;line-height:1}.date small{font-size:11px;font-weight:700;color:var(--ink3)}
.post h3{font-size:18px;font-weight:700;line-height:1.2}.post .by{font-size:12.5px;color:var(--ink2);margin-top:3px}
.post .pic{height:150px;border-radius:12px;background:linear-gradient(135deg,#4a7355,#84a87d);margin:12px 0}
.post p{font-size:14px;line-height:1.6;color:var(--ink2);margin-bottom:8px}.post p.lead{font-size:15px;color:var(--ink)}
.postfoot{background:var(--sand);border-radius:12px;padding:12px 14px;font-size:13px;color:var(--ink2)}
.phone{background:#eef0f3;display:grid;place-items:center}
.scr{width:390px;height:844px;border-radius:44px;background:#fff;overflow:hidden;position:relative;box-shadow:0 0 0 10px #000,0 20px 50px rgba(0,0,0,.25);display:flex;flex-direction:column}
.mast{background:var(--navy);color:#fff;padding:58px 20px 20px}.mast h1{font-size:38px;font-weight:700;letter-spacing:-.03em;line-height:1}.mast p{color:var(--beige);font-size:14px;margin-top:6px}
.tabs{display:flex;gap:20px;padding:0 20px;border-bottom:1px solid var(--line);font-size:14px;font-weight:600;color:var(--ink3)}
.tabs span{padding:13px 0 11px;white-space:nowrap}.tabs span.on{color:var(--ink);box-shadow:inset 0 -3px 0 var(--marker)}
.chips{display:flex;gap:8px;padding:12px 20px 0;overflow:hidden}.chip{height:34px;border-radius:999px;background:var(--field);box-shadow:inset 0 0 0 1px var(--edge);padding:0 13px;display:flex;align-items:center;font-size:13px;color:var(--ink2);white-space:nowrap;flex:none}.chip.on{background:var(--navy);color:#fff;box-shadow:none}
.feed{padding:4px 20px;flex:1;overflow:hidden}
.lead{margin-top:16px}.lead .pic{height:190px;border-radius:14px;background:linear-gradient(135deg,#2a3d5e,#009c8f)}.lead h3{font-size:19px;font-weight:700;margin-top:10px;line-height:1.2}.lead p{font-size:14px;color:var(--ink2);margin-top:4px;line-height:1.45}
.meta{font-size:12.5px;color:var(--ink3);margin-top:6px}.meta em{font-style:normal;color:var(--accent);font-weight:600}.meta i{font-style:normal;color:var(--mink);font-weight:600}
.pr{display:flex;gap:12px;padding:14px 0;border-top:1px solid var(--line);margin-top:14px}.pr+.pr{margin-top:0}.pr .t{flex:1}.pr b{font-size:15px;font-weight:600;display:block;line-height:1.25}.pr p{font-size:13px;color:var(--ink2);margin-top:3px;line-height:1.4}
.pr .th{width:64px;height:64px;border-radius:12px}
.tabbar{height:82px;border-top:1px solid var(--line);display:flex;justify-content:space-around;padding-top:10px;font-size:10.5px;font-weight:600;color:var(--ink3)}.tabbar span.on{color:var(--accent)}
.cap{position:absolute;left:0;right:0;bottom:16px;text-align:center;font-size:12px;color:var(--ink3)}
"""

FONT = '<link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700&display=swap" rel="stylesheet">'

POSTS = [
    ("Working bee this Saturday", "Kids church", "Everyone", "Sam Example", "2h ago", "16 Nov", "", "b"),
    ("Camp forms are due Friday", "Youth", "Everyone", "Alex Example", "Yesterday", "15 Nov", "ask", "a"),
    ("New songs for December", "Music team", "Everyone", "Jordan Example", "Sat", "14 Nov", "", "c"),
    ("Thank you from the food pantry", "Community care", "Everyone", "Priya Example", "Thu", "12 Nov", "in", "a"),
    ("Morning tea roster is up", "Welcome team", "Everyone", "Taylor Example", "Wed", "11 Nov", "", "b"),
    ("Term 4 wrap-up night", "Kids church", "Everyone", "Sam Example", "9 Nov", "9 Nov", "", "c"),
    ("Prayer breakfast moves to the hall", "Men's ministry", "Everyone", "Chris Example", "7 Nov", "7 Nov", "", "a"),
]

STATE = {"in": ("in", "In church news"), "ask": ("ask", "Asking for church news")}


def page(title, desk, phone):
    return (f'<!doctype html><html lang="en-AU"><head><meta charset="utf-8">{FONT}<title>{title}</title><style>{CSS}</style></head>'
            f'<body><div class="desk">{BAND}{desk}</div><div class="phone"><div class="scr">{phone}</div></div></body></html>')


BAND = ('<div class="band"><div><h1>Church news</h1><p>News for the whole church, in the app\'s News tab. A post can send a notification that opens it, '
        'and a ministry\'s post can be put in here too.</p></div><div class="btns"><span class="btn">◷ Earlier announcements</span><span class="btn p">+ New post</span></div></div>')


def rail(extra=""):
    groups = [("Ministries asking", 1), ("Being written", 0), ("Scheduled", 0), ("Published", 4), ("Kept in ministries", 0), ("Archived", 0)]
    rows = "".join(f'<div class="f"><span>{n}</span><b>{c}</b></div>' for n, c in groups)
    return f'<div class="rail"><h4>Show</h4>{rows}<h4 class="next">Every ministry</h4><div class="f on"><span>All ministries</span><b>23</b></div>{extra}</div>'


def pane(menu=True):
    m = ('<div class="menu"><div class="hi">Put it in church news…<small>Everyone sees it in News; you choose whether to send a notification</small></div>'
         '<div>Open it in Kids church</div></div>') if menu else ""
    return (f'<div class="pane" style="position:relative"><div class="ph"><div><h2>Working bee this Saturday</h2><p>Kids church · for everyone · posted 2h ago</p></div><span class="ib">⋯</span></div>{m}'
            '<div class="post"><div class="hd"><div class="date"><b>Today</b><small>16 NOV</small></div><div><h3>Working bee this Saturday</h3><p class="by">Sam Example · Kids church</p></div></div>'
            '<div class="pic"></div><p class="lead">We\'re tidying the kids\' rooms and the play area from 8am. Bring gloves if you have them.</p>'
            '<p>Morning tea is on us. Families welcome; there are jobs for every age.</p></div>'
            '<div class="postfoot">Posted today by Sam Example in Kids church, for everyone.</div></div>')


def phone_tabs(active="All ministries"):
    tabs = "".join(f'<span class="{"on" if t == active else ""}">{t}</span>' for t in ["The church", "Your ministries", "All ministries"])
    return '<div class="mast"><h1>News</h1><p>From the church and the ministries you\'re in</p></div>' + f'<div class="tabs">{tabs}</div>'


def phone_feed(pills=False, chips=""):
    def meta(p):
        st = f' · <i>{STATE[p[6]][1]}</i>' if pills and p[6] else ""
        return f'<p class="meta"><em>From {p[1]}</em> · {p[4]}{st}</p>'
    lead = POSTS[0]
    rows = "".join(f'<div class="pr"><div class="t"><b>{p[0]}</b><p>{p[3]} shared an update for the {p[1].lower()} families and team.</p>{meta(p)}</div><div class="th {p[7]}"></div></div>' for p in POSTS[1:4])
    return (chips + f'<div class="feed"><div class="lead"><div class="pic"></div><h3>{lead[0]}</h3><p>We\'re tidying the kids\' rooms and the play area from 8am. Bring gloves if you have them.</p>{meta(lead)}</div>{rows}</div>'
            '<div class="tabbar"><span>Home</span><span class="on">News</span><span>Sermons</span><span>More</span></div>')


def table_desk():
    rows = "".join(
        f'<tr class="{"on" if i == 0 else ""}"><td>{p[0]}</td><td>{p[1]}</td><td class="num">{p[5]}</td><td>{STATE[p[6]][1] if p[6] else "—"}</td></tr>'
        for i, p in enumerate(POSTS))
    return (f'<div class="wb" style="grid-template-columns:210px 1fr 380px">{rail()}<div class="list"><div class="lh"><span>23 posts for everyone, newest first</span><span>Ministry ▾</span></div>'
            f'<table><tr><th>Post</th><th>Ministry</th><th>Posted</th><th>Church news</th></tr>{rows}</table></div>{pane()}</div>')


def rows_desk(chip_rail=False):
    rows = "".join(
        f'<div class="r{" on" if i == 0 else ""}"><div class="t"><b>{p[0]}</b><span class="from">{p[1]}</span><span>By {p[3]} · {p[4]}</span>'
        + (f'<em class="pill {STATE[p[6]][0]}">{STATE[p[6]][1]}</em>' if p[6] and not chip_rail else "")
        + f'</div><div class="th {p[7]}"></div></div>'
        for i, p in enumerate(POSTS[:6]))
    extra = ""
    if chip_rail:
        mins = [("Every ministry", 23, True), ("Kids church", 6, False), ("Youth", 5, False), ("Music team", 4, False), ("Community care", 3, False), ("Welcome team", 3, False), ("Men's ministry", 2, False)]
        extra = '<h4 class="next">Ministry</h4>' + "".join(f'<div class="f{" on" if on else ""}" style="padding:7px 10px"><span>{n}</span><b>{c}</b></div>' for n, c, on in mins)
        r = rail().replace('<div class="f on"><span>All ministries</span><b>23</b></div>', '<div class="f on"><span>All ministries</span><b>23</b></div>' + extra)
    else:
        r = rail()
    return f'<div class="wb" style="grid-template-columns:210px 1fr 380px">{r}<div class="list"><div class="lh"><span>Every ministry\'s posts for everyone, newest first</span></div>{rows}</div>{pane()}</div>'


def write(name, html):
    (HERE / name).write_text(html)


write("a-table.html", page("A table", table_desk(), phone_tabs() + phone_feed()))
write("b-rows.html", page("Rows that say where each stands", rows_desk(), phone_tabs() + phone_feed(pills=True)))
chips = '<div class="chips">' + "".join(f'<span class="chip{" on" if i == 0 else ""}">{c}</span>' for i, c in enumerate(["Every ministry", "Kids church", "Youth", "Music team"])) + '</div>'
write("c-narrow.html", page("Narrow by ministry", rows_desk(chip_rail=True), phone_tabs() + phone_feed(chips=chips)))
