from sketch import page

CSS = """body{display:flex;gap:28px;padding:24px;background:#d9dde3;grid-template-columns:none}
.mail{width:980px;height:852px;background:#fff;border-radius:12px;overflow:hidden;display:flex;flex-direction:column;box-shadow:0 8px 30px rgba(0,0,0,.15)}
.mh{padding:14px 20px;border-bottom:1px solid #e5e5ea;color:#1c1c1e;font-size:13px}.mh b{display:block;font-size:14px}
.mb{flex:1;overflow:hidden}
.mob{width:372px;height:852px;border-radius:44px;background:#fff;box-shadow:0 0 0 9px #000;overflow:hidden;flex:none;display:flex;flex-direction:column}
.mob .mh{padding:54px 16px 10px}
.cap{position:absolute;font-size:12px;font-weight:700;color:#3b4558;letter-spacing:.02em}
.bg{background:#e9eef4;padding:28px 0}.col{width:600px;margin:0 auto;background:#fff}
.mob .bg{padding:0}.mob .col{width:auto}
.hd{background:#2a3d5e;color:#fff;padding:26px 30px 22px}.hd b{display:block;font-size:28px;letter-spacing:-.02em;line-height:1.1}.hd small{display:block;color:#d5c9b7;font-size:14px;font-weight:600;margin-top:6px}
.mob .hd{padding:20px 18px 16px}.mob .hd b{font-size:22px}.mob .hd small{font-size:12.5px}
.banner{height:210px;background:linear-gradient(160deg,#9cc3e6 0%,#cfe3f1 45%,#e9e4d6 46%,#7a9a5b 70%,#58753e 100%);position:relative}
.banner:after{content:"The church's photo, set once in the admin panel";position:absolute;left:16px;bottom:12px;background:rgba(0,0,0,.45);color:#fff;font-size:12px;font-weight:600;padding:5px 10px;border-radius:12px}
.mob .banner{height:150px}
.it{display:flex;gap:18px;padding:20px 30px;border-top:1px solid #e7e2da;color:#1f2a3c}.it:first-of-type{border-top:0}
.mob .it{padding:16px 18px;gap:12px}
.p{display:block;width:190px;height:126px;border-radius:8px;flex:none;background:linear-gradient(135deg,#2a3d5e,#009c8f)}.p.b{background:linear-gradient(135deg,#4a7355,#84a87d)}.p.c{background:linear-gradient(135deg,#e7ae74,#b9895a)}
.mob .p{width:110px;height:74px}
.l{font-size:12px;font-weight:700;color:#007a70}.l.r{color:#9a5a1c}.t{font-size:19px;font-weight:700;margin-top:1px;line-height:1.2}.w{font-size:13.5px;font-weight:700;color:#2a3d5e;margin-top:3px}
.it p{font-size:13.5px;line-height:1.5;margin-top:6px;color:#333}.mob .t{font-size:16px}.mob .it p{font-size:13px}
.btn2{display:inline-block;margin-top:10px;background:#2a3d5e;color:#fff;border-radius:18px;padding:8px 15px;font-size:12.5px;font-weight:600}
.p.wide{width:100%;height:230px;margin-bottom:12px;border-radius:10px}.mob .p.wide{height:150px}
.stack .it{display:block}.stack .p{width:100%;height:220px;margin-bottom:12px;border-radius:10px}.mob .stack .p{height:150px}
.end{margin:6px 30px 26px;background:#f3efe8;border-radius:12px;padding:16px 18px;color:#1f2a3c;font-size:13px}.end b{display:block;font-size:14.5px}.mob .end{margin:6px 18px 20px}
"""

ITEMS = [
    (None, "Services on break", "No Bible Study &amp; Prayer", "The next two Wednesdays", "Bible Study &amp; Prayer is off for the Christmas break, and back on Wednesday 13 January.", True, ""),
    ("", "Baptisms this Sunday", "Baptisms", "This Sunday @ 10:30am", "Five people from our church family are being baptised. Bring a friend and stay for morning tea.", False, ""),
    ("c", "Food pantry", "Christmas offering for the food pantry", "", "This year's Christmas offering goes to the food pantry's holiday hampers. Read how to give.", False, "Read the post"),
    ("b", "Christmas", "Christmas Day service", "Friday 25 December @ 9am", "One service at 9am, with carols for all ages.", False, ""),
]


def items(lead=False):
    out = ""
    ordered = [ITEMS[1], ITEMS[0], ITEMS[2], ITEMS[3]] if lead else ITEMS
    for index, (pic, lab, title, when, about, off, button) in enumerate(ordered):
        picture = "" if pic is None else f'<span class="p {pic}"></span>'
        body = (f'<div><div class="l {"r" if off else ""}">{lab}</div><div class="t">{title}</div>' + (f'<div class="w">{when}</div>' if when else "")
                + f'<p>{about}</p>' + (f'<span class="btn2">{button}</span>' if button else "") + "</div>")
        cls = "it stack" if lead and index == 0 else "it"
        if lead and index == 0:
            out += f'<div class="stack"><div class="it">{picture}{body}</div></div>'
        else:
            out += f'<div class="{cls}">{picture}{body}</div>'
    return out


END = '<div class="end"><b>Everything else is in the app</b>The calendar in the Good Shepherd app has every service, group and event.<br><span class="btn2">See all events</span></div>'


def frame(name, column, mobile_column):
    head = '<div class="mh"><b>Good Shepherd Baptist Church</b>What\'s Next @ Good Shepherd · Monday 14 December</div>'
    body = (f'<div class="mail">{head}<div class="mb"><div class="bg"><div class="col">{column}</div></div></div></div>'
            f'<div class="mob">{head}<div class="mb"><div class="bg"><div class="col">{mobile_column}</div></div></div></div>')
    page(name, body, CSS, dark=False, shell=False)


HEAD = '<div class="hd"><b>What\'s Next at Good Shepherd</b><small>Monday 14 – Sunday 27 December</small></div>'

frame("g-email-beside.html", '<div class="banner"></div>' + HEAD + items() + END, '<div class="banner"></div>' + HEAD + items() + END)
frame("h-email-on-top.html", HEAD + items().replace('class="it"', 'class="it" style="display:block"').replace('class="p ', 'class="p wide ') + END,
      HEAD + items().replace('class="it"', 'class="it" style="display:block"').replace('class="p ', 'class="p wide ') + END)
frame("i-email-lead.html", HEAD + items(lead=True) + END, HEAD + items(lead=True) + END)
