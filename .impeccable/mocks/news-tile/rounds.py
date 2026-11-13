from make import tile, photo, icon, phone, sizes, home_with, board, write, DEV_HOME

LEAD = "Christmas at Good Shepherd: every service, and what's on"
NEXT = "Holiday program volunteers needed"
URGENT = "The car park is closed on Sunday"
URGENT_LINE = "Park on the street or in the school grounds"


def after(html, extra):
    return html[:-6] + extra + "</div>"


def before(html, extra):
    head = html.index(">") + 1
    return html[:head] + extra + html[head:]


def today_band(words=URGENT):
    return f'<span class="sk-today">{icon("megaphone")}<span>Today: {words}</span></span>'


def next_row(title=NEXT, when="Yesterday"):
    return f'<span class="sk-next"><span class="sk-next__title">{title}</span><span class="sk-next__when">{when}</span></span>'


def empty(tone="sand"):
    return tile("square", tone, "News", ["Nothing new yet"], ic="newspaper")


def front_2x2(urgent=False):
    html = after(tile("picture", "raised", LEAD, ["Church office · 2 hours ago"], picture=photo("christmas"), corner=True, cls="sk-lead"), next_row())
    return before(html, today_band()) if urgent else html


A_PHONE = [("news", 2, 2, lambda: front_2x2())]
A_PHONE_URGENT = [("news", 2, 2, lambda: front_2x2(urgent=True))]
a_panel = sizes("brand", "light", [
    ("2 × 1: the picture beside the newest headline", [("n", 2, 1, lambda: tile("beside", "raised", LEAD, ["2 hours ago"], picture=photo("christmas"), corner=True, cls="sk-3"))], ()),
    ("2 × 1 on a day something is urgent: it takes the tile", [("n", 2, 1, lambda: tile("row", "marker", URGENT, [f"Today · {URGENT_LINE}"], ic="megaphone"))], ()),
    ("1 × 2, 1 × 1, and 1 × 1 with no news", [
        ("a", 1, 2, lambda: tile("picture", "raised", LEAD, ["2 hours ago"], picture=photo("christmas"), cls="sk-3")),
        ("b", 1, 1, lambda: tile("behind", "raised", "Baptisms on 30 November", ["Monday"], picture=photo("baptism"), corner=True, cls="sk-3")),
        ("c", 1, 1, lambda: empty()),
    ], ()),
], "Every size<span>Brand, light</span>")
write("a-front-page.html", board("A front page in miniature",
    phone("brand", "light", home_with(A_PHONE), "Brand, light", "<span>The newest post leads with its picture; the one before it under a line</span>")
    + phone("brand", "dark", home_with(A_PHONE_URGENT), "Brand, dark", "<span>On a day a post is urgent, a band across the top</span>")
    + phone("y2026", "light", home_with(A_PHONE), "2026 theme, light", "<span>Same tile, the yearly theme's colours</span>"),
    a_panel))


def quiet_row(urgent=False):
    if urgent:
        return tile("row", "marker", URGENT, [f"Today · {URGENT_LINE}"], ic="megaphone")
    return tile("row", "sand", "2 new in News", [LEAD], ic="newspaper", dot=True)


B_PHONE = [("news", 2, 1, lambda: quiet_row())]
B_PHONE_URGENT = [("news", 2, 1, lambda: quiet_row(urgent=True))]
b_panel = sizes("brand", "light", [
    ("2 × 2: the newest headline large, and how many more", [("n", 2, 2, lambda: tile("poster", "sand", LEAD, ["2 hours ago · 1 more new"], ic="newspaper", go="Read it"))], ()),
    ("1 × 2, 1 × 1, and 1 × 1 with nothing new", [
        ("a", 1, 2, lambda: tile("poster", "sand", "2 new in News", [LEAD], ic="newspaper", dot=True)),
        ("b", 1, 1, lambda: tile("square", "sand", "2 new", ["News"], ic="newspaper", dot=True)),
        ("c", 1, 1, lambda: tile("square", "sand", "News", ["Nothing new since Tuesday"], ic="newspaper")),
    ], ()),
], "Every size<span>Brand, light</span>")
write("b-quiet-until-urgent.html", board("Quiet until it matters",
    phone("brand", "light", home_with(B_PHONE), "Brand, light", "<span>How many are new since you last looked, and the newest</span>")
    + phone("brand", "dark", home_with(B_PHONE_URGENT), "Brand, dark", "<span>On a day a post is urgent, the tile turns to it</span>")
    + phone("y2026", "light", home_with(B_PHONE), "2026 theme, light", "<span>Same tile, the yearly theme's colours</span>"),
    b_panel))


def entries(rows):
    out = '<span class="hub-list">'
    for when, what, pill in rows:
        tag = '<span class="pill">Today</span>' if pill else ""
        out += f'<span class="hub-list__entry"><span class="hub-list__when">{when}</span><span class="hub-list__what">{tag}{what}</span></span>'
    return out + "</span>"


ROWS = [("2h", LEAD, False), ("Yesterday", NEXT, False), ("Monday", "Baptisms on 30 November", False)]
ROWS_URGENT = [("8am", URGENT, True), ("2h", LEAD, False), ("Yesterday", NEXT, False)]


def headlines(rows):
    return tile("square", "raised", "News", go="All news", go_beside=True, cls="hub-tile--list sk-headlines", extra=entries(rows))


C_PHONE = [("news", 2, 1, lambda: headlines(ROWS))]
C_PHONE_URGENT = [("news", 2, 1, lambda: headlines(ROWS_URGENT))]
c_panel = sizes("brand", "light", [
    ("1 × 2: three headlines stacked; 1 × 1: the newest; and 1 × 1 with no news", [
        ("a", 1, 2, lambda: headlines(ROWS)),
        ("b", 1, 1, lambda: tile("square", "raised", LEAD, ["2 hours ago"])),
        ("c", 1, 1, lambda: tile("square", "raised", "News", ["Nothing new yet"])),
    ], ()),
], "Every size<span>Brand, light. 2 × 1 and 2 × 2 grow to fit three or five headlines</span>")
write("c-headlines.html", board("Headlines",
    phone("brand", "light", home_with(C_PHONE), "Brand, light", "<span>The newest three, like This week's agenda</span>", lines=("news", "week"))
    + phone("brand", "dark", home_with(C_PHONE_URGENT), "Brand, dark", "<span>An urgent post goes first with Today</span>", lines=("news", "week"))
    + phone("y2026", "light", home_with(C_PHONE), "2026 theme, light", "<span>Same tile, the yearly theme's colours</span>", lines=("news", "week")),
    c_panel))


def inbox(title, line, dot=True):
    return tile("square", "sand", title, [line], ic="bell", dot=dot)


d_panel = sizes("brand", "light", [
    ("Church news already reaches Home: its notification shows on the Notifications tile", [
        ("a", 1, 1, lambda: inbox("1 new", LEAD)),
        ("b", 1, 1, lambda: tile("square", "sand", "Church news", ["What's happening"], ic="newspaper")),
    ], ()),
    ("Notifications at 2 × 1, on a day a post is urgent", [("n", 2, 1, lambda: tile("row", "sand", URGENT, ["1 new · Today, from the church office"], ic="bell", dot=True))], ()),
], "What's left without a News tile<span>Right: a plain tile staff can add today, opening the News tab</span>")
write("d-no-tile.html", board("No News tile",
    phone("brand", "light", list(DEV_HOME), "Brand, light", "<span>Home as it is: News is one tap away on the tab bar</span>")
    + phone("brand", "dark", list(DEV_HOME), "Brand, dark", "<span>Urgent posts arrive as notifications, as now</span>")
    + phone("y2026", "light", list(DEV_HOME), "2026 theme, light", "<span>Nothing new to look after</span>"),
    d_panel))
