from sketch import page, BAND, rows, phone, ITEMS

CHOICES = ('<div class="choices"><div class="f"><span class="l">From</span><span class="i" style="width:190px">14 Dec 2026</span></div>'
           '<div class="f"><span class="l">How long</span><div class="seg"><span>1 week</span><span class="on">2 weeks</span><span>1 month</span><span>Other</span></div></div></div>'
           '<div class="range">Monday 14 – Sunday 27 December</div>')

ADD3 = ('<div style="display:flex;gap:10px;margin-top:16px"><span class="btn s">▦ From the calendar</span><span class="btn s">✎ A church news post</span>'
        '<span class="btn s">+ Something else</span></div>')

SEND = ('<div style="display:flex;gap:10px;justify-content:flex-end;align-items:center"><span class="btn">⧉ Copy for email</span>'
        '<span class="btn p">➤ Send to the app and notify people</span></div>')

EMAIL_MINI = """
.canvas{flex:1;background:#dfe4ec;border-radius:14px;overflow:hidden;display:flex;justify-content:center;padding:22px 0 0}
.em{width:440px;background:#fff;color:#18233a;font-size:11px}
.em .hd{background:#2a3d5e;color:#fff;padding:20px 22px}.em .hd b{font-size:21px;display:block;letter-spacing:-.02em}.em .hd small{color:#d5c9b7;font-size:11.5px;font-weight:600}
.em .it{display:flex;gap:14px;padding:16px 22px;border-top:1px solid #e7e2da}.em .it:first-of-type{border-top:0}
.em .p{width:130px;height:86px;border-radius:7px;flex:none;background:linear-gradient(135deg,#2a3d5e,#009c8f)}.em .p.b{background:linear-gradient(135deg,#4a7355,#84a87d)}.em .p.c{background:linear-gradient(135deg,#e7ae74,#b9895a)}
.em .l{font-size:10px;font-weight:700;color:#007a70}.em .l.r{color:#9a5a1c}.em .t{font-size:14.5px;font-weight:700;margin-top:1px}.em .w{font-size:11px;font-weight:700;color:#2a3d5e;margin-top:2px}.em p{font-size:11px;line-height:1.45;margin-top:5px;color:#333}
"""


def email_mini():
    def item(pic, lab, title, when, about, rust=False):
        picture = f'<span class="p {pic}"></span>' if pic is not None else ""
        return f'<div class="it">{picture}<div><div class="l {"r" if rust else ""}">{lab}</div><div class="t">{title}</div><div class="w">{when}</div><p>{about}</p></div></div>'
    return ('<div class="em"><div class="hd"><b>What\'s Next at Good Shepherd</b><small>Monday 14 – Sunday 27 December</small></div>'
            + item(None, "Services on break", "No Bible Study &amp; Prayer", "The next two Wednesdays", "Bible Study &amp; Prayer is off for the Christmas break, and back on Wednesday 13 January.", True)
            + item("", "Baptisms this Sunday", "Baptisms", "This Sunday @ 10:30am", "Five people from our church family are being baptised. Bring a friend and stay for morning tea.")
            + item("c", "Food pantry", "Christmas offering for the food pantry", "Read the post", "This year's Christmas offering goes to the food pantry's holiday hampers.")
            + item("b", "Christmas", "Christmas Day service", "Friday 25 December @ 9am", "One service at 9am, with carols for all ages.")
            + "</div>")


def preview_led():
    left = (f'<div style="width:600px;flex:none;padding:18px 26px;border-right:1px solid var(--line);overflow:hidden">'
            f'<div class="status"><b>✓ Saved</b> · not sent to the app yet</div>{CHOICES}'
            f'<div style="margin-top:18px">{rows(ITEMS, chosen=1).replace("<span class=\"th \"></span>", "<span class=\"th\"></span>")}</div>{ADD3}</div>')
    right = ('<div style="flex:1;padding:18px 26px 20px;display:flex;flex-direction:column;gap:14px;min-width:0">'
             '<div style="display:flex;justify-content:space-between;align-items:center"><div class="seg" style="height:40px"><span>In the app</span><span class="on">Email</span></div>'
             '<span class="h">Click anything in the preview to change its words</span></div>'
             f'<div class="canvas">{email_mini()}</div>{SEND}</div>')
    body = BAND.format(actions="") + f'<div style="flex:1;display:flex;min-height:0">{left}{right}</div>'
    page("a-preview-led.html", body, EMAIL_MINI)


RUN = """
.run{max-width:880px;margin:0 auto;padding:18px 0;width:100%}
.big{display:flex;gap:18px;padding:16px;border-radius:14px;background:var(--surface);margin-top:10px;align-items:flex-start}
.big .pic{width:170px;height:112px;border-radius:10px;flex:none;background:linear-gradient(135deg,#2a3d5e,#009c8f)}.big .pic.b{background:linear-gradient(135deg,#4a7355,#84a87d)}.big .pic.c{background:linear-gradient(135deg,#e7ae74,#b9895a)}
.big .pic.off{background:#3a3326;display:grid;place-items:center;color:var(--marker);font-weight:700;font-size:13px}.big .pic.none{background:var(--surface2);display:grid;place-items:center;color:var(--ink3);font-size:12.5px}
.big .tt{font-size:18px}.big .wh{font-size:14px;font-weight:700;color:var(--accent);margin-top:3px}.big p{font-size:13.5px;color:var(--ink2);line-height:1.5;margin-top:6px}
.addbar{display:grid;grid-template-columns:repeat(3,1fr);gap:10px;margin-top:14px}.addbar span{border:1.5px dashed var(--edge);border-radius:14px;padding:14px;font-size:14px;font-weight:600}.addbar small{display:block;font-weight:400;color:var(--ink3);font-size:12.5px;margin-top:3px}
"""


def running_order():
    abouts = ["Bible Study &amp; Prayer is off for the Christmas break, and back on Wednesday 13 January.",
              "Five people from our church family are being baptised. Bring a friend and stay for morning tea.",
              "This year's Christmas offering goes to the food pantry's holiday hampers.",
              "One service at 9am, with carols for all ages."]
    cards = ""
    for item, about in zip(ITEMS[:4], abouts):
        th = item["th"]
        pic = f'<span class="pic {th}">{"Not on" if th == "off" else ""}</span>'
        when = item["when"] + (f' · {item["time"]}' if item["time"] else "") if item["when"] else "Read the post"
        pills = "".join(item.get("pills", []))
        cards += (f'<div class="big"><span class="grip" style="margin-top:46px"></span>{pic}<div class="tx"><div class="kind">{item["icon"]} {item["kind"]}</div>'
                  f'<div class="lab {"off" if item.get("off") else ""}" style="margin-top:4px">{item["lab"]}</div><div class="tt">{item["title"]}{pills}</div>'
                  f'<div class="wh">{when}</div><p>{about}</p></div><span class="dots">⋯</span></div>')
    adds = ('<div class="addbar"><span>▦ From the calendar<small>Anything on in these two weeks</small></span><span>✎ A church news post<small>A post you want people to see</small></span>'
            '<span>+ Something else<small>Not on the calendar or in the news</small></span></div>')
    actions = '<div style="display:flex;gap:10px"><span class="btn">◱ Preview</span><span class="btn">⧉ Copy for email</span><span class="btn p">➤ Send to the app</span></div>'
    body = BAND.format(actions=actions) + (f'<div style="flex:1;overflow:hidden;padding:0 40px"><div class="run"><div style="display:flex;justify-content:space-between;align-items:flex-end">'
                                           f'<div>{CHOICES}</div><div class="status"><b>✓ Saved</b> · not sent to the app yet</div></div>{cards}{adds}</div></div>')
    page("b-running-order.html", body, RUN)


COMPOSER = """
.comp{background:var(--surface);border-radius:14px;padding:6px 16px 12px;margin-top:16px}
.res{display:flex;gap:12px;align-items:center;padding:9px 0;border-top:1px solid var(--line);font-size:14px}.res:first-of-type{border-top:0}
.res .d{width:72px;flex:none;font-size:12.5px;font-weight:700;color:var(--ink2)}.res .n{flex:1}.res .n small{display:block;font-size:12.5px;color:var(--ink3)}
.res .add{font-size:13px;font-weight:600;color:var(--accent);white-space:nowrap}.res .done{font-size:13px;color:var(--ink3)}
.chipz{display:flex;gap:6px;margin:10px 0 8px}.chipz span{height:30px;border-radius:999px;padding:0 11px;display:flex;align-items:center;font-size:12.5px;background:var(--field);color:var(--ink2)}.chipz span.on{background:var(--sel);color:var(--ink)}
"""


def composer():
    results = [("Wed 16", "No Bible Study &amp; Prayer", "Services on break · not on", "done"),
               ("Sun 20", "Baptisms", "Services · special date", "done"),
               ("Sun 20", "Sunday services", "Services · 10:30am and 6pm", "add"),
               ("Thu 24", "Christmas Eve candlelight service", "Services · one-off", "add"),
               ("Fri 25", "Christmas Day service", "Services · one-off", "done")]
    found = "".join(f'<div class="res"><span class="d">{d}</span><span class="n">{n}<small>{s}</small></span>'
                    f'{"<span class=done>✓ In the digest</span>" if state == "done" else "<span class=add>+ Add</span>"}</div>' for d, n, s, state in results)
    box = (f'<div class="comp"><div class="tabs"><span class="on">From the calendar</span><span>Church news</span><span>Something else</span></div>'
           f'<div class="chipz"><span class="on">✓ Services</span><span>Discipleship</span><span>Kids</span><span>Youth</span></div>{found}</div>')
    left = (f'<div style="flex:1;padding:18px 32px;overflow:hidden;min-width:0"><div class="status"><b>✓ Saved</b> · not sent to the app yet</div>{CHOICES}{box}'
            f'<div style="margin-top:16px;font-size:13px;font-weight:700;color:var(--ink2)">In the digest</div>{rows(ITEMS[:3])}</div>')
    right = (f'<div style="width:420px;flex:none;border-left:1px solid var(--line);padding:18px 24px;display:flex;flex-direction:column;align-items:center;gap:14px">'
             f'<div class="seg" style="height:40px;align-self:flex-start"><span class="on">In the app</span><span>Email</span></div>{phone(ITEMS[:3])}</div>')
    body = BAND.format(actions="") + f'<div style="flex:1;display:flex;min-height:0">{left}{right}</div>'
    page("c-add-panel.html", body, COMPOSER)


if __name__ == "__main__":
    preview_led()
    running_order()
    composer()
