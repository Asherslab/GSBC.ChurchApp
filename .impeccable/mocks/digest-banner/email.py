from pathlib import Path

HERE = Path(__file__).parent
FONT = '<link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700&display=swap" rel="stylesheet">'

CSS = """*{box-sizing:border-box;margin:0;padding:0}
body{width:1440px;height:900px;overflow:hidden;background:#d9dde3;font-family:Montserrat,Arial,sans-serif;display:flex;gap:28px;padding:24px}
.mail{width:980px;height:852px;background:#fff;border-radius:12px;overflow:hidden;display:flex;flex-direction:column;box-shadow:0 8px 30px rgba(0,0,0,.15)}
.mh{padding:14px 20px;border-bottom:1px solid #e5e5ea;color:#1c1c1e;font-size:13px}.mh b{display:block;font-size:14px}
.mb{flex:1;overflow:hidden}
.mob{width:372px;height:852px;border-radius:44px;background:#fff;box-shadow:0 0 0 9px #000;overflow:hidden;flex:none;display:flex;flex-direction:column}
.mob .mh{padding:54px 16px 10px}
.bg{background:#e9eef4;padding:28px 0}.col{width:600px;margin:0 auto;background:#fff}
.mob .bg{padding:0}.mob .col{width:auto}
.banner{height:228px;background:#7189be;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px}
.banner .w{font-family:Georgia,'Times New Roman',serif;font-style:italic;font-weight:700;font-size:58px;letter-spacing:-.04em;color:#fbf8f0;line-height:1}.banner .w span{color:#22325a}
.banner .a{color:#fbf8f0;font-size:19px;letter-spacing:.32em;font-weight:600}
.mob .banner{height:142px;gap:6px}.mob .banner .w{font-size:36px}.mob .banner .a{font-size:12px}
.hd{background:#2a3d5e;color:#fff;padding:30px 32px 26px}.hd b{display:block;font-size:32px;letter-spacing:-.02em;line-height:1.1}.hd small{display:block;color:#d5c9b7;font-size:15px;font-weight:600;margin-top:8px}
.mob .hd{padding:20px 18px 16px}.mob .hd b{font-size:23px}.mob .hd small{font-size:13px}
.it{display:flex;gap:18px;padding:22px 32px;color:#1f2a3c}.it+.it{border-top:1px solid #e7e2da;margin:0 32px;padding:22px 0}
.mob .it{padding:16px 18px;gap:12px}.mob .it+.it{margin:0 18px;padding:16px 0}
.p{display:block;width:190px;height:126px;border-radius:8px;flex:none;background:linear-gradient(135deg,#2a3d5e,#009c8f)}.p.b{background:linear-gradient(135deg,#4a7355,#84a87d)}
.mob .p{width:110px;height:74px}
.l{font-size:13px;font-weight:700;color:#007a70}.l.r{color:#9a5a1c}.t{font-size:21px;font-weight:700;margin-top:2px;line-height:1.2}.wh{font-size:15px;font-weight:700;color:#2a3d5e;margin-top:4px}
.it p{font-size:15px;line-height:1.5;margin-top:8px;color:#1f2a3c}.mob .t{font-size:17px}.mob .wh{font-size:13.5px}.mob .it p{font-size:13.5px}
"""

ITEMS = [
    (None, "Services on break", "No Bible Study &amp; Prayer", "The next two Wednesdays", "Bible Study &amp; Prayer is off for the Christmas break, and back on Wednesday 13 January.", True),
    ("", "Baptisms this Sunday", "Baptisms", "This Sunday @ 10:30am", "Five people from our church family are being baptised. Bring a friend and stay for morning tea.", False),
    ("b", "Christmas", "Christmas Day service", "Friday 25 December @ 9am", "One service at 9am, with carols for all ages.", False),
]


def items(first_pad=None):
    out = ""
    for index, (pic, lab, title, when, about, off) in enumerate(ITEMS):
        picture = "" if pic is None else f'<span class="p {pic}"></span>'
        style = f' style="padding-top:{first_pad}"' if index == 0 and first_pad else ""
        out += (f'<div class="it"{style}>{picture}<div><div class="l {"r" if off else ""}">{lab}</div><div class="t">{title}</div>'
                f'<div class="wh">{when}</div><p>{about}</p></div></div>')
    return out


BANNER = '<div class="banner"><div class="w">What’s<span>Next</span></div><div class="a">AT GOOD SHEPHERD</div></div>'
TITLE = '<div class="hd"><b>What\'s Next at Good Shepherd</b><small>Monday 14 – Sunday 27 December</small></div>'
RANGE = "Monday 14 – Sunday 27 December"


def frame(name, top, extra_css="", first_pad=None):
    head = '<div class="mh"><b>Good Shepherd Baptist Church</b>What\'s Next @ Good Shepherd · Monday 14 December</div>'
    column = BANNER + top + items(first_pad)
    body = (f'<div class="mail">{head}<div class="mb"><div class="bg"><div class="col">{column}</div></div></div></div>'
            f'<div class="mob">{head}<div class="mb"><div class="bg"><div class="col">{column}</div></div></div></div>')
    (HERE / name).write_text(f'<!doctype html><html lang="en-AU"><head><meta charset="utf-8">{FONT}<title>{name}</title>'
                             f'<style>{CSS}{extra_css}</style></head><body>{body}</body></html>')


frame("a-dateline.html",
      f'<div class="dl"><span>{RANGE}</span></div>',
      ".dl{padding:24px 32px 0}.dl span{display:block;font-size:13px;font-weight:700;letter-spacing:.14em;text-transform:uppercase;color:#2a3d5e;padding-bottom:14px;border-bottom:2px solid #2a3d5e}"
      ".mob .dl{padding:18px 18px 0}.mob .dl span{font-size:11px;padding-bottom:10px}", first_pad="18px")
frame("b-navy-strip.html",
      f'<div class="ns">{RANGE}</div>',
      ".ns{background:#2a3d5e;color:#d5c9b7;font-size:15px;font-weight:600;text-align:center;padding:13px 32px}.mob .ns{font-size:13px;padding:10px 18px}")
frame("c-sand-strip.html",
      f'<div class="ss">{RANGE}</div>',
      ".ss{background:#f3efe8;color:#2a3d5e;font-size:15px;font-weight:700;text-align:center;padding:14px 32px;border-bottom:1px solid #e7e2da}.mob .ss{font-size:13px;padding:11px 18px}")
frame("d-photo-only.html", "")
frame("e-default.html", TITLE)
