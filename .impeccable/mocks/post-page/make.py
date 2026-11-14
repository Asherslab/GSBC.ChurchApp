import base64
import html
import sys
from pathlib import Path

HERE = Path(__file__).parent
ROOT = HERE.parent.parent.parent
sys.path.insert(0, str(HERE))
from kit import head, phone, _inline

PICTURE = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 320"><rect width="480" height="320" fill="#e9e1d4"/><path d="M0 230 L150 150 L260 215 L340 170 L480 240 V320 H0Z" fill="#d5c9b7"/><circle cx="370" cy="90" r="34" fill="#f4efe7"/></svg>'
PHOTO = "data:image/svg+xml;base64," + base64.b64encode(PICTURE.encode()).decode()
MARK = "data:image/svg+xml;base64," + base64.b64encode((ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/img/gsbc-mark-white.svg").read_bytes()).decode()

SOLID = '<header class="topbar topbar--solid"><div class="topbar__start"><button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>News</button></div><span class="topbar__title"></span><div class="topbar__end"><button class="icon-btn" aria-label="More"><i data-icon="more" data-size="md"></i></button></div></header>'
CLEAR = '<header class="topbar pp-clear"><div class="topbar__start"><button class="pp-chip"><i data-icon="chevron-left" data-size="md"></i>News</button></div><span class="topbar__title"></span><div class="topbar__end"><button class="pp-chip pp-chip--round" aria-label="More"><i data-icon="more" data-size="md"></i></button></div></header>'

CAROLS = dict(
    title="Carols by Candlelight: help us set up",
    lede="Carols by Candlelight is on the front lawn again this year, and we need a few more hands on the Saturday afternoon to set out chairs, lights and the sound desk.",
    rest="<p>If you can give an hour or two from 2pm, put your name on the sheet in the foyer or reply to the church office. Kids are welcome to help with the candles.</p>"
         '<div class="pp-card"><div><a class="pp-card__t">Carols by Candlelight</a><p class="pp-card__k"><i data-icon="calendar"></i>Event</p>'
         '<p class="pp-card__d"><i data-icon="clock"></i>Saturday 20 December, 6pm</p><p class="pp-card__d"><i data-icon="map-pin"></i>Front lawn</p>'
         '<p class="pp-card__a"><a>Add to calendar</a><a>Open</a></p></div><span class="pp-card__tile"><i data-icon="calendar" data-size="md"></i></span></div>'
         "<p>Bring a torch for packing up afterwards. Thank you!</p>",
    caption="Last year's carols on the front lawn",
)
TEA = dict(
    title="Morning tea after the 10:30 service",
    lede="This Sunday we're having morning tea on the verandah after the 10:30 service, to welcome everyone who's joined us this year.",
    rest="<p>Please bring a plate to share if you can. Tea and coffee are on us.</p>",
)

STYLE = """<style>
.pp .rich-text{font-size:var(--text-body);line-height:1.6;color:var(--ink)}
.pp .rich-text p{margin:0 0 .85em}
.pp-lede{font-size:var(--text-lead)!important;line-height:1.55!important;color:var(--ink);font-weight:500}
.pp-meta{font-size:var(--text-caption);color:var(--ink-2);display:flex;flex-wrap:wrap;gap:4px 10px;align-items:center}
.pp-meta b{color:var(--ink);font-weight:600}
.pp-fig{margin:0 0 16px}.pp-fig img{display:block;width:100%;aspect-ratio:3/2;object-fit:cover;border-radius:var(--radius-card,14px)}
.pp-fig figcaption{font-size:var(--text-caption);color:var(--ink-2);margin-top:6px}
.pp-card{display:flex;gap:12px;justify-content:space-between;border-left:4px solid var(--accent);padding:2px 0 2px 12px;margin:6px 0 20px}
.pp-card__t{font-size:var(--text-row);font-weight:700;color:var(--ink);line-height:1.3;display:block}
.pp-card__k{margin:2px 0 4px;font-size:var(--text-caption);font-weight:600;color:var(--accent);display:flex;gap:5px;align-items:center}
.pp-card__d{margin:0;font-size:var(--text-caption);color:var(--ink-2);display:flex;gap:6px;align-items:center}.pp-card__d svg{color:var(--ink-3)}
.pp-card__a{margin:6px 0 0;display:flex;gap:16px;font-size:var(--text-caption);font-weight:600}.pp-card__a a{color:var(--accent)}
.pp-card__tile{flex:none;width:68px;height:68px;border-radius:9px;background:var(--live);color:var(--on-live);display:grid;place-items:center}
.pp-clear{background:transparent!important;border:0!important;position:absolute;left:0;right:0;top:0;z-index:6}
.pp-chip{display:inline-flex;align-items:center;gap:2px;height:34px;padding:0 12px 0 6px;border-radius:999px;border:0;background:rgb(0 0 0 / .38);color:#fff;font:600 var(--text-label) var(--font);backdrop-filter:blur(8px)}
.pp-chip--round{width:34px;padding:0;justify-content:center}
.pa{padding:22px var(--gutter) 0}
.pa-head{border-top:3px solid var(--primary);padding-top:14px;display:grid;grid-template-columns:62px 1fr;gap:14px;align-items:start;margin-bottom:16px}
.pa-date{background:var(--sand);border-radius:12px;text-align:center;padding:8px 0 7px;line-height:1}
.pa-date small{display:block;font-size:var(--text-pill);font-weight:700;letter-spacing:.08em;color:var(--accent)}
.pa-date b{display:block;font-size:28px;font-weight:700;color:var(--ink);margin:4px 0 3px;letter-spacing:-.02em}
.pa-date span{font-size:var(--text-pill);font-weight:600;color:var(--ink-2);letter-spacing:.06em}
.pa h1{margin:0 0 8px;font-size:var(--text-headline);line-height:1.15;letter-spacing:-.02em;text-wrap:balance}
.pa-rule{border:0;border-top:1px solid var(--line);margin:0 0 16px}
.pa-foot{margin:18px calc(-1*var(--gutter)) 0;background:var(--sand);padding:18px var(--gutter) 400px;border-top:1px solid var(--line)}
.pa-foot p{margin:0;font-size:var(--text-caption);color:var(--ink-2)}
.pa-foot b{color:var(--ink);font-weight:600}
.pb{background:var(--sand);min-height:844px;padding-bottom:300px}
.pb-cover{height:400px;position:relative;margin-top:calc(-1*(var(--top-h,44px) + var(--sat)))}
.pb-cover img{width:100%;height:100%;object-fit:cover;display:block}
.pb-cover figcaption{position:absolute;left:var(--gutter);bottom:38px;font-size:var(--text-pill);font-weight:600;color:#fff;background:rgb(0 0 0 / .45);padding:3px 8px;border-radius:999px}
.pb-cover--plain{height:300px;background:var(--masthead);overflow:hidden}
.pb-cover--plain img{position:absolute;right:-50px;top:40px;width:230px;height:auto;opacity:.08}
.pb-sheet{position:relative;margin-top:-26px;background:var(--surface);border-radius:22px;padding:22px var(--gutter) 6px;box-shadow:0 -6px 24px rgb(24 35 58 / .10)}
.pb-sheet .pp-meta{margin-bottom:10px}.pb-sheet .pp-meta .pb-from{color:var(--accent);font-weight:600;display:inline-flex;gap:5px;align-items:center}
.pb h1{margin:0 0 14px;font-size:var(--text-headline);line-height:1.15;letter-spacing:-.02em;text-wrap:balance}
.pc-pic{margin-top:0}.pc-pic img{display:block;width:100%;aspect-ratio:4/3;object-fit:cover}
.pc-poster{background:var(--masthead);color:var(--on-masthead);padding:20px var(--gutter) 22px;position:relative;overflow:hidden;isolation:isolate}
.pc-poster img.mark{position:absolute;right:-56px;bottom:-40px;width:220px;opacity:.08;z-index:-1}
.pc-poster h1{margin:0 0 12px;font-size:30px;line-height:1.1;letter-spacing:-.025em;text-wrap:balance}
.pc-poster--tall{padding:30px var(--gutter) 26px}.pc-poster--tall h1{font-size:36px;margin-bottom:16px}
.pc-poster .pc-meta{font-size:var(--text-caption);color:var(--masthead-note);font-weight:600}
.pc-poster .pc-lede{font-size:var(--text-lead);line-height:1.55;color:var(--on-masthead-2);margin:0 0 16px}
.pc-cap{font-size:var(--text-caption);color:var(--on-masthead-2);margin:0 0 10px}
.pc-body{padding:20px var(--gutter) 0}
</style>"""


def text(post, lede=True):
    return '<div class="rich-text">' + (f'<p class="pp-lede">{post["lede"]}</p>' if lede else "") + post["rest"] + "</div>"


def a(post):
    pic = f'<figure class="pp-fig"><img src="{PHOTO}" alt=""><figcaption>{post["caption"]}</figcaption></figure>' if post.get("caption") else ""
    return (STYLE + '<article class="pp pa"><div class="pa-head"><div class="pa-date"><small>TODAY</small><b>14</b><span>NOV</span></div>'
            f'<div><h1>{post["title"]}</h1><p class="pp-meta"><b>Church office</b><span>Saturday 14 November</span></p></div></div>'
            f'{pic}{text(post)}<div class="pa-foot"><p>Posted <b>today</b> by <b>the church office</b> for everyone.</p></div></article>')


def b(post):
    cover = (f'<figure class="pb-cover"><img src="{PHOTO}" alt=""><figcaption>{post["caption"]}</figcaption></figure>' if post.get("caption")
             else f'<div class="pb-cover pb-cover--plain"><img src="{MARK}" alt=""></div>')
    return (STYLE + f'<article class="pp pb">{cover}<div class="pb-sheet"><p class="pp-meta"><span class="pb-from"><i data-icon="megaphone"></i>Church news</span><span>Church office · Today</span></p>'
            f'<h1>{post["title"]}</h1>{text(post)}</div></article>')


def c(post):
    if post.get("caption"):
        top = (f'<figure class="pc-pic"><img src="{PHOTO}" alt=""></figure><div class="pc-poster"><img class="mark" src="{MARK}" alt="">'
               f'<h1>{post["title"]}</h1><p class="pc-meta">Church office · Today</p></div>')
        return STYLE + f'<article class="pp">{top}<div class="pc-body"><p class="pp-meta" style="margin-bottom:12px">{post["caption"]}</p>{text(post)}</div></article>'
    top = (f'<div class="pc-poster pc-poster--tall"><img class="mark" src="{MARK}" alt=""><h1>{post["title"]}</h1>'
           f'<p class="pc-lede">{post["lede"]}</p><p class="pc-meta">Church office · Today</p></div>')
    return STYLE + f'<article class="pp">{top}<div class="pc-body">{text(post, lede=False)}</div></article>'





for name, title, make, top in [("a-bulletin-item.html", "The bulletin item", a, SOLID),
                               ("b-rises-over-picture.html", "The post rises over its picture", b, CLEAR),
                               ("c-poster.html", "The notice as a poster", c, SOLID)]:
    doc = head(title) + "<body>" + phone(make(CAROLS), topbar=top) + phone(make(TEA), topbar=top) + "</body></html>"
    (HERE / ("raw-" + name)).write_text(doc)
