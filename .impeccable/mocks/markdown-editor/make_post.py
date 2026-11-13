import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE.parent / "church-news"))
from make import phone, pic

BACK = '<header class="topbar topbar--solid"><div class="topbar__start"><button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>News</button></div><span class="topbar__title"></span><div class="topbar__end"><button class="icon-btn" aria-label="Share"><i data-icon="share" data-size="md"></i></button></div></header>'
OVER = BACK.replace("topbar topbar--solid", "topbar pp-over")

TITLE = "Christmas at Good Shepherd"

FIRST = """<p>Christmas is close. Here's every service, so you can plan ahead and <mark>bring someone along</mark>.</p>
<ul><li><strong>Carols on the lawn</strong>, Saturday 20 December, 6pm</li><li><strong>Christmas Eve</strong>, 7pm</li><li><strong>Christmas Day</strong>, 9am, <s>and 10:30</s> one service only</li></ul>
<blockquote><p>For to us a child is born, to us a son is given.</p></blockquote>"""

SECOND = """<h2>Helping on the day</h2>
<p>Set-up starts at 4pm on the lawn. Bring gloves if you have them<sup class="pp-fn">1</sup>.</p>
<figure class="rich-text-figure pp-fig">{pic}<figcaption>Carols on the lawn last year</figcaption></figure>"""

NOTES = '<ol class="pp-notes"><li>Spare gloves are in the shed by the hall.</li></ol>'

STYLE = """<style>
.pp{padding-bottom:40px}
.pp .rich-text{font-size:var(--text-body);line-height:1.6}
.pp .rich-text p{margin:0 0 .85em}.pp .rich-text ul,.pp .rich-text ol{margin:0 0 .85em;padding-left:1.3em}.pp .rich-text li{margin:.2em 0}
.pp .rich-text h2{font-size:var(--text-lead);line-height:1.3;margin:1.3em 0 .45em}
.pp .rich-text mark{background:color-mix(in srgb,var(--marker) 45%,transparent);color:inherit;border-radius:3px;padding:0 2px}
.pp .rich-text s{color:var(--ink-2)}.pp-fn{font-size:.7em;color:var(--accent);font-weight:700}
.pp-fig .sk-pic{display:block;height:190px;border-radius:var(--radius-inner)}
.pp-notes{font-size:var(--text-caption);color:var(--ink-2);padding-left:1.2em;margin-top:12px}
.pp-meta{display:flex;gap:10px;align-items:center;font-size:var(--text-caption);color:var(--ink-2)}
.pp-av{width:30px;height:30px;border-radius:50%;background:var(--primary);color:var(--on-primary);display:grid;place-items:center;font-weight:700;font-size:13px}
.pp-meta b{display:block;color:var(--ink);font-weight:600}
.pp-over{background:transparent;border:0;position:absolute;left:0;right:0;top:0;z-index:5;color:#fff}.pp-over .icon-btn{color:#fff}
.pp-band{background:var(--primary);color:var(--on-primary);padding:104px var(--gutter) 22px;position:relative;overflow:hidden}
.pp-band:after{content:"";position:absolute;right:-60px;top:-40px;width:220px;height:220px;border-radius:50%;border:34px solid color-mix(in srgb,var(--on-primary) 9%,transparent)}
.pp-kick{font-size:var(--text-caption);font-weight:700;letter-spacing:.02em;display:flex;gap:6px;align-items:center}
.pp-band .pp-kick{color:color-mix(in srgb,var(--on-primary) 75%,transparent)}
.pp-band h1{font-size:var(--text-title);line-height:1.2;margin:8px 0 0;letter-spacing:-.015em}
.pp-a-body{padding:18px var(--gutter) 0}.pp-a-body .pp-meta{margin-bottom:16px}
.pp-split{height:0;border-top:8px solid var(--sand, #f4efe7);margin:22px calc(-1 * var(--gutter))}
.pp-b{padding:10px var(--gutter) 0}
.pp-b .pp-kick{color:var(--accent);text-transform:uppercase;font-size:12px;letter-spacing:.06em}
.pp-b h1{font-size:30px;line-height:1.15;letter-spacing:-.02em;margin:10px 0 14px}
.pp-b .pp-meta{padding-bottom:16px;margin-bottom:18px;border-bottom:1px solid var(--line)}
.pp-b .rich-text>p:first-child{font-size:1.12em;line-height:1.55;color:var(--ink)}
.pp-orn{display:flex;justify-content:center;gap:10px;margin:26px 0;color:var(--ink-3)}.pp-orn i{width:5px;height:5px;border-radius:50%;background:currentColor}
.pp-c{background:var(--ground, #f4efe7);min-height:100%;padding:12px 12px 0;display:grid;gap:12px}
.pp-card{background:var(--surface,#fff);border-radius:var(--radius-card,14px);padding:18px 16px 6px;box-shadow:0 1px 0 var(--line)}
.pp-card.head{padding:0;overflow:hidden}.pp-card.head .top{height:8px;background:linear-gradient(90deg,var(--primary),var(--accent))}
.pp-card.head .in{padding:16px 16px 18px}.pp-card.head h1{font-size:var(--text-title);line-height:1.2;margin:6px 0 12px;letter-spacing:-.015em}
.pp-card.head .pp-kick{color:var(--accent)}
.pp-card .rich-text h2:first-child{margin-top:0}
.pp-card .pp-fig{margin:0 -16px 12px}.pp-card .pp-fig .sk-pic{border-radius:0}.pp-card .pp-fig figcaption{padding:0 16px}
</style>"""

META = '<div class="pp-meta"><span class="pp-av">A</span><span><b>Church office</b>Saturday 14 November</span></div>'


def text(*parts):
    return '<div class="rich-text">' + "".join(parts) + "</div>"


second = SECOND.format(pic=pic("a"))

A = (STYLE + '<article class="pp"><div class="pp-band"><p class="pp-kick"><i data-icon="megaphone"></i>Church news</p><h1>' + TITLE + '</h1></div>'
     '<div class="pp-a-body">' + META + text(FIRST) + '<div class="pp-split"></div>' + text(second) + NOTES + "</div></article>")
B = (STYLE + '<article class="pp pp-b"><p class="pp-kick">Church news · Sat 14 Nov</p><h1>' + TITLE + "</h1>" + META
     + text(FIRST) + '<div class="pp-orn"><i></i><i></i><i></i></div>' + text(second) + NOTES + "</article>")
C = (STYLE + '<div class="pp pp-c"><div class="pp-card head"><div class="top"></div><div class="in"><p class="pp-kick"><i data-icon="megaphone"></i>Church news</p><h1>' + TITLE + "</h1>" + META + "</div></div>"
     '<div class="pp-card">' + text(FIRST) + '</div><div class="pp-card">' + text(second) + NOTES + "</div></div>")

for name, title, body, top in [("p-a-band.html", "A quiet band when there's no picture", A, OVER),
                               ("p-b-reading-page.html", "A reading page", B, BACK),
                               ("p-c-cards.html", "Each part a card", C, BACK)]:
    (HERE / name).write_text(phone(title, body, topbar=top, tab="News"))
