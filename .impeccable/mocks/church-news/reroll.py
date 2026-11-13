from make import phone as base, pic, write, masthead, POSTS, from_line

SOLID = '<header class="topbar topbar--solid"><div class="topbar__start"></div><span class="topbar__title">News</span><div class="topbar__end"><button class="icon-btn" aria-label="Notifications"><i data-icon="bell" data-size="md"></i></button></div></header>'


def phone(title, body, scroll=0):
    return base(title, body, scroll=scroll, topbar=SOLID if scroll else "")

def tabs(on):
    t = lambda name, count="": f'<button class="tab-strip__tab" role="tab" aria-selected="{str(name == on).lower()}">{name}{count}</button>'
    return f'<div class="tab-strip tab-strip--sticky sk-tabs" role="tablist">{t("The church")}{t("Your ministries", " <span class=tab-strip__count>3</span>")}</div>'

def lead(p):
    urgent = '<span class="pill">Today</span> ' if p.get("urgent") else ""
    return f'<a class="sk-story sk-story--lead">{pic(p["pic"] or "a")}<span class="sk-story__text"><span class="sk-story__title">{urgent}{p["t"]}</span><span class="sk-story__excerpt">{p["x"]}</span>{from_line(p)}</span></a>'

def side(p, left=False, day=None):
    thumb = f'<span class="sk-thumb">{pic(p["pic"])}</span>' if p["pic"] else '<span class="sk-thumb sk-thumb--none"></span>' if left else ""
    urgent = '<span class="pill">Today</span> ' if p.get("urgent") else ""
    text = f'<span class="row__body"><span class="row__title">{urgent}{p["t"]}</span><span class="row__subtitle sk-clamp">{p["x"]}</span>{from_line(p)}</span>'
    return f'<a class="row row--reading sk-brief sk-side{" sk-side--left" if left else ""}">{(thumb + text) if left else (text + thumb)}</a>'

church = [dict(POSTS[0]), dict(POSTS[3], pic="b"), dict(POSTS[1], tag=""), dict(POSTS[2]), dict(POSTS[4], tag="", pic="c")]
mine = [dict(POSTS[4]), dict(POSTS[1], pic="c"), dict(t="Band practice moves to Thursday", x="Same time, 7:30pm, in the hall. The set list is in Files.", by="Music", ago="Wed", pic="a", tag="Music"), dict(t="Roster for December is out", x="Check your dates and ask for a swap by Sunday.", by="Music", ago="Mon", pic="b", tag="Music")]

s5 = masthead("News", "From the church and the ministries you're in") + tabs("Your ministries") + '<section class="section section--first">' + lead(mine[0]) + '<div class="list">' + "".join(side(p, left=True) for p in mine[1:]) + "</div></section>"
write("r-a-ministries-tab.html", phone("Tabs under the masthead, pictures at the left", s5, scroll=150))

pair = '<div class="sk-pair">' + "".join(f'<a class="sk-mini">{pic(p["pic"])}<span class="sk-mini__title">{p["t"]}</span><span class="sk-meta"><span>{p["ago"]}</span></span></a>' for p in [church[1], church[4]]) + "</div>"
s7 = masthead("News", "From the church and the ministries you're in") + tabs("The church") + '<section class="section section--first">' + lead(church[0]) + pair + '<div class="list">' + "".join(side(p) for p in church[2:4]) + "</div></section>"
write("r-b-lead-and-pair.html", phone("A lead, two beside each other, then rows", s7, scroll=150))

pinned = '<a class="sk-pinned"><span class="row__icon row__icon--warm"><i data-icon="megaphone"></i></span><span class="row__body"><span class="row__title">Building works: the car park is closed on Sunday</span><span class="row__subtitle">Today · Park on the street or in the school grounds</span></span><i data-icon="chevron-right"></i></a>'
rest = [church[0], church[1], church[2], church[4]]
s2 = masthead("News", "From the church and the ministries you're in") + tabs("The church") + '<section class="section section--first">' + pinned + lead(rest[0]) + '<div class="list">' + "".join(side(p) for p in rest[1:]) + "</div></section>"
write("r-c-today-first.html", phone("What's urgent today first, then the lead", s2, scroll=150))
