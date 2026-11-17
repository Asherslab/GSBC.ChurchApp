import base64
import re
from pathlib import Path

HERE = Path(__file__).parent
ROOT = HERE.parent.parent.parent
UI = ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot"


def data(path, kind):
    return f"data:{kind};base64," + base64.b64encode(path.read_bytes()).decode()


def tokens():
    text = (UI / "css/tokens.css").read_text()
    return re.sub(r'url\("?\.\./fonts/([^")]+)"?\)', lambda m: f'url("{data(UI / "fonts" / m.group(1), "font/woff2")}")', text)


MARK = data(UI / "img/gsbc-mark.svg", "image/svg+xml")
MARK_WHITE = data(UI / "img/gsbc-mark-white.svg", "image/svg+xml")
PHOTO = "data:image/svg+xml;base64," + base64.b64encode(
    b'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 270"><rect width="480" height="270" fill="#e9e1d4"/>'
    b'<path d="M0 200 L150 120 L260 185 L340 140 L480 210 V270 H0Z" fill="#d5c9b7"/><circle cx="380" cy="70" r="26" fill="#e7ae74"/>'
    b'<g fill="#2a3d5e" opacity=".55"><rect x="90" y="150" width="6" height="40"/><rect x="130" y="140" width="6" height="50"/><rect x="170" y="155" width="6" height="35"/></g></svg>').decode()

CSS = """
*{box-sizing:border-box;margin:0;padding:0}
html{font-size:17px}
body{width:1440px;height:900px;overflow:hidden;font:400 var(--text-body)/1.45 var(--font);color:var(--ink);background:#d9d4cb;display:flex;gap:24px;padding:20px;-webkit-font-smoothing:antialiased}
.win{width:1002px;height:860px;flex:none;border-radius:12px;overflow:hidden;background:var(--page);display:flex;flex-direction:column;box-shadow:0 8px 30px rgb(0 0 0 / .18)}
.win__bar{height:38px;flex:none;background:#ece9e4;display:flex;align-items:center;gap:8px;padding:0 14px;border-bottom:1px solid #d6d1c8}
.win__bar i{width:11px;height:11px;border-radius:50%;background:#cfc9bf}
.win__url{margin-left:18px;flex:1;max-width:520px;height:24px;border-radius:7px;background:#fff;font-size:12px;color:#6b7280;display:flex;align-items:center;padding:0 10px}
.win__view{flex:1;overflow:hidden;position:relative}
.phone{width:390px;height:844px;flex:none;border-radius:44px;border:10px solid #111;overflow:hidden;background:var(--page);display:flex;flex-direction:column;position:relative}
.phone__status{height:44px;flex:none;display:flex;justify-content:space-between;align-items:flex-end;padding:0 26px 6px;font-size:13px;font-weight:600}
.phone__url{height:40px;flex:none;margin:0 10px 0;border-radius:12px;background:#ece9e4;font-size:12px;color:#4b5563;display:flex;align-items:center;justify-content:center}
.phone__view{flex:1;overflow:hidden;position:relative}
.brand{display:flex;align-items:center;gap:10px;font-weight:700;font-size:var(--text-row)}
.brand img{width:30px;height:30px}
.post{padding-top:0}
.post__head{border-top:3px solid var(--primary);padding-top:14px;display:grid;grid-template-columns:62px 1fr;gap:14px;align-items:start;margin-bottom:16px}
.post__day{background:var(--sand);border-radius:12px;text-align:center;padding:8px 0 7px;line-height:1}
.post__day small{display:block;font-size:var(--text-pill);font-weight:700;letter-spacing:.08em;color:var(--accent);text-transform:uppercase}
.post__day b{display:block;font-size:1.65rem;font-weight:700;margin:4px 0 3px;letter-spacing:-.02em}
.post__day span{font-size:var(--text-pill);font-weight:600;color:var(--ink-2);letter-spacing:.06em;text-transform:uppercase}
.post h1{font-size:var(--text-headline);line-height:1.15;letter-spacing:-.02em;text-wrap:balance;margin-bottom:6px}
.post__meta{font-size:var(--text-caption);color:var(--ink-2)}
.post figure{margin:0 0 16px}
.post figure img{display:block;width:100%;aspect-ratio:16/9;object-fit:cover;border-radius:12px}
.post figcaption{font-size:var(--text-caption);color:var(--ink-2);margin-top:6px}
.post__lead{font-size:var(--text-lead);line-height:1.55;margin-bottom:.8em}
.post p.t{line-height:1.62;margin-bottom:.85em;color:var(--ink)}
.card{display:flex;gap:12px;justify-content:space-between;border-left:4px solid var(--accent);padding:2px 0 2px 12px;margin:6px 0 18px}
.card b{display:block;font-size:var(--text-row);color:var(--ink)}
.card em{display:block;font-style:normal;font-size:var(--text-caption);font-weight:600;color:var(--accent);margin:2px 0}
.card span{display:block;font-size:var(--text-caption);color:var(--ink-2)}
.card a{display:inline-block;margin-top:6px;font-size:var(--text-caption);font-weight:600;color:var(--accent);margin-right:14px}
.card .card__tile{flex:none;width:64px;height:64px;border-radius:9px;background:var(--live);color:#fff;display:grid;place-items:center;font-weight:700}
.foot{background:var(--sand);border-top:1px solid var(--line);font-size:var(--text-caption);color:var(--ink-2)}
.foot b{color:var(--ink);font-weight:600}
.applines{font-size:var(--text-caption);color:var(--ink-2);line-height:1.5}
.applines b{color:var(--ink);font-weight:600}
"""


def post(width, gutter, picture=True, foot_extra="", foot_pad="18px", fill="300px"):
    fig = (f'<figure><img src="{PHOTO}" alt=""><figcaption>Last year\'s carols on the front lawn</figcaption></figure>' if picture else "")
    return f"""
<article class="post" style="max-width:{width};margin:0 auto;padding:22px {gutter} 0">
 <header class="post__head"><p class="post__day"><small>Today</small><b>17</b><span>Nov</span></p>
  <div><h1>Carols by Candlelight: help us set up</h1><p class="post__meta">Church office</p></div></header>
 {fig}
 <p class="post__lead">Carols by Candlelight is on the front lawn again this year, and we need a few more hands on the Saturday afternoon to set out chairs, lights and the sound desk.</p>
 <p class="t">If you can give an hour or two from 2pm, put your name on the sheet in the foyer or reply to the church office. Kids are welcome to help with the candles.</p>
 <div class="card"><div><b>Carols by Candlelight</b><em>Event</em><span>Saturday 20 December, 6pm</span><span>Front lawn</span><a>Add to calendar</a><a>See the event</a></div><span class="card__tile">20</span></div>
 <p class="t">Bring a torch for packing up afterwards. Thank you!</p>
</article>
<footer class="foot" style="padding:{foot_pad} 0 {fill}"><div style="max-width:{width};margin:0 auto;padding:0 {gutter}">
 <p>Posted <b>today</b> by <b>Church office</b> for everyone.</p>{foot_extra}</div></footer>"""


APP = ("This post is also in the Good Shepherd app. On a phone with the app, the link opens it there. "
       "Don't have the app yet? Ask the church office how to get it.")


def page(title, desktop, mobile):
    return (f'<!doctype html><html lang="en"><head><meta charset="utf-8"><title>{title}</title>'
            f'<style>{tokens()}{CSS}</style></head><body>'
            f'<div class="win"><div class="win__bar"><i></i><i></i><i></i><span class="win__url">uat-app.baptist.com.au/open/news/…</span></div><div class="win__view">{desktop}</div></div>'
            f'<div class="phone"><div class="phone__status"><span>9:41</span><span>●●● ▮</span></div><div class="phone__url">uat-app.baptist.com.au</div><div class="phone__view">{mobile}</div></div>'
            '</body></html>')


def sheet_on_sand():
    def view(phone):
        if phone:
            return (f'<div style="background:var(--sand);padding:14px 20px 12px"><div class="brand"><img src="{MARK}" alt="">Good Shepherd</div></div>'
                    f'<div style="background:var(--page)">{post("none", "20px", foot_extra=f"<p class=applines style=margin-top:10px>{APP}</p>")}</div>')
        return (f'<div style="background:var(--sand);height:100%;padding:28px 0 0">'
                f'<div style="width:720px;margin:0 auto 16px" class="brand"><img src="{MARK}" alt="">Good Shepherd</div>'
                f'<div style="width:720px;margin:0 auto;background:var(--page);border-radius:14px;box-shadow:var(--lift);overflow:hidden;padding-top:12px">'
                f'{post("none", "40px", fill="18px")}</div>'
                f'<p class="applines" style="width:720px;margin:14px auto 0;padding:0 40px">{APP}</p></div>')
    return page("A bulletin page on the desk", view(False), view(True))


def app_strip():
    def strip(phone):
        words = ("This post is also in the Good Shepherd app. <b>Don't have it yet?</b> Ask the church office how to get it.")
        if phone:
            return (f'<div style="background:var(--sand);border-bottom:1px solid var(--line);padding:12px 20px">'
                    f'<div class="brand" style="margin-bottom:6px"><img src="{MARK}" alt="">Good Shepherd</div><p class="applines">{words}</p></div>')
        return (f'<div style="background:var(--sand);border-bottom:1px solid var(--line);padding:12px 28px;display:flex;align-items:center;gap:24px">'
                f'<div class="brand"><img src="{MARK}" alt="">Good Shepherd</div><p class="applines" style="margin-left:auto;max-width:520px;text-align:right">{words}</p></div>')
    return page("The app strip at the top",
                strip(False) + post("680px", "20px", fill="400px"),
                strip(True) + post("none", "20px"))


def navy_band():
    def band(phone):
        pad = "14px 20px 16px" if phone else "16px 28px 18px"
        return (f'<div style="background:var(--masthead);color:#fff;padding:{pad};display:flex;align-items:center;gap:12px">'
                f'<img src="{MARK_WHITE}" alt="" style="width:30px;height:30px"><span style="font-weight:700;font-size:var(--text-row)">Good Shepherd</span>'
                f'<span style="color:var(--masthead-note);font-weight:600;font-size:var(--text-caption);margin-left:auto">Church news</span></div>')
    extra = f'<p class="applines" style="margin-top:8px">{APP}</p>'
    return page("A navy band, like the app's masthead",
                band(False) + post("680px", "20px", foot_extra=extra, fill="400px"),
                band(True) + post("none", "20px", foot_extra=extra))


for name, make in [("a-sheet-on-sand", sheet_on_sand), ("b-app-strip", app_strip), ("c-navy-band", navy_band)]:
    (HERE / f"{name}.html").write_text(make())
    print(name)
