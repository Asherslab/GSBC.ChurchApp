from sketch import page, DARK, LIGHT

CSS = """body{display:flex;gap:70px;justify-content:center;align-items:center;background:#cfd5de;grid-template-columns:none}
.ph{width:372px;height:806px;border-radius:46px;box-shadow:0 0 0 10px #000,0 20px 50px rgba(0,0,0,.25);overflow:hidden;background:var(--page);color:var(--ink);display:flex;flex-direction:column;position:relative}
.ph>*{flex:none}
.ph .mast{padding:58px 20px 18px}.ph .mast h2{font-size:34px}
.ph .card .pic{height:150px}.ph .card b{font-size:17px}
.newsrow{display:flex;gap:12px;padding:12px 0;border-top:1px solid var(--line);margin:0 16px}.newsrow .t{flex:1}.newsrow b{display:block;font-size:15px;line-height:1.25}.newsrow p{font-size:12.5px;color:var(--ink2);margin-top:3px;line-height:1.4}
.newsrow .th{width:72px;height:72px;border-radius:12px}
.post{margin:12px 16px 0;border-top:3px solid var(--navy);padding-top:10px;display:flex;gap:12px}
.post .dt{width:46px;flex:none;text-align:center;border-right:1px solid var(--line);padding-right:8px}.post .dt b{display:block;font-size:22px;line-height:1}.post .dt small{font-size:10.5px;font-weight:700;color:var(--ink3)}
.post .t b{display:block;font-size:16px;line-height:1.2}.post .t p{font-size:12.5px;color:var(--ink2);margin-top:4px;line-height:1.4}.post .t .go{font-size:13px;font-weight:600;color:var(--accent);margin-top:6px}
.tag{display:inline-flex;gap:5px;align-items:center;font-size:11.5px;font-weight:700;color:var(--ink2)}
.sbar{position:absolute;top:14px;left:30px;right:30px;display:flex;justify-content:space-between;font-size:14px;font-weight:600;color:#fff}
.dark{--page:#141d31;--surface:#1d2840;--surface2:#26334f;--ink:#f2ede4;--ink2:#b2b8c5;--ink3:#9ca2b1;--line:rgba(255,255,255,.09);--accent:#2ec4b6;--navy:#283c62}
.light{--page:#ffffff;--surface:#ffffff;--surface2:#f4efe7;--ink:#18233a;--ink2:#545e70;--ink3:#626a79;--line:rgba(42,61,94,.12);--accent:#00776d;--navy:#2a3d5e;--marker:#8a5422}
.light .card{box-shadow:0 1px 2px rgba(24,35,58,.08),0 4px 14px rgba(24,35,58,.08)}
.light .offc{background:#f4efe7}.light .offc b{color:#18233a}
"""

MAST = '<div class="sbar"><span>9:41</span><span>●●●</span></div><div class="mast"><h2>What\'s next</h2><p>14 – 27 December</p><em>4 things coming up in the next two weeks.</em></div>'
OFF = '<div class="offc"><b>⊘ No Bible Study &amp; Prayer</b>The next two Wednesdays · back on 13 January</div>'
BAPTISMS = ('<div class="card"><div class="pic"></div><div class="w"><div class="lab">Baptisms this Sunday</div><b>Baptisms</b><div class="wh">This Sunday · 10:30am</div>'
            '<p>Five people from our church family are being baptised. Bring a friend and stay for morning tea.</p></div></div>')
WEEK = '<div class="sec">This week<small>14 – 20 Dec</small></div>'


def phones(name, middle, after=""):
    body = "".join(f'<div class="ph {scheme}">{MAST}{WEEK}{OFF}{middle}{BAPTISMS}{after}</div>' for scheme in ("light", "dark"))
    page(name, body, CSS, shell=False)


phones("j-news-as-card.html",
       '<div class="card"><div class="pic c"></div><div class="w"><div class="tag">✎ Church news · Monday 7 December</div><b>Christmas offering for the food pantry</b>'
       '<p>This year\'s Christmas offering goes to the food pantry\'s holiday hampers.</p></div><div class="go">Read the post ›</div></div>')
phones("k-news-section.html", "",
       '<div class="sec" style="margin-top:6px">From church news<small>2 posts</small></div>'
       '<div class="newsrow" style="border-top:0"><div class="t"><b>Christmas offering for the food pantry</b><p>Monday 7 December · The offering goes to holiday hampers.</p></div><span class="th c"></span></div>'
       '<div class="newsrow"><div class="t"><b>Holiday hours for the office</b><p>Friday 11 December · Closed from 24 December.</p></div><span class="th b"></span></div>')
phones("l-news-as-post.html",
       '<div class="post"><div class="dt"><b>7</b><small>DEC</small></div><div class="t"><span class="tag">Church news</span><b>Christmas offering for the food pantry</b>'
       '<p>This year\'s Christmas offering goes to the food pantry\'s holiday hampers.</p><div class="go">Read the post ›</div></div></div>')
