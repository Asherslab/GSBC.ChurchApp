from sketch import page, BAND, rows, ITEMS
from desk import CHOICES

BEHIND = (BAND.format(actions="") + f'<div style="padding:18px 40px;max-width:900px">{CHOICES}<div style="margin-top:16px">{rows(ITEMS)}</div></div>'
          '<div class="scrim"></div>')

HEAD = ('<div class="hd"><div><h2>Baptisms</h2><p>From the Services calendar, Sunday 20 December. What you change here is for the digest only.</p></div>'
        '<span class="dots" style="background:var(--field)">⋯</span></div>')

FOOT = ('<div class="ft"><span class="btn">Cancel</span><div style="display:flex;gap:14px;align-items:center"><span class="h">The title is reworded for the digest</span>'
        '<span class="btn p">Save</span></div></div>')


def fields(picture=True):
    pic = ('<div class="f"><span class="l">Picture</span><div class="cover"><span>Change picture</span></div>'
           '<span class="h">The event\'s picture. Choose another from the media library for the digest only.</span></div>') if picture else ""
    return (pic +
            '<div class="f"><span class="l">Line above the title</span><span class="i">Baptisms this Sunday</span></div>'
            '<div class="f"><div style="display:flex;justify-content:space-between"><span class="l">Title</span><span class="back">Use the calendar\'s wording</span></div>'
            '<span class="i rw">Baptisms: five people, one family</span></div>'
            '<div class="pair"><div class="f"><span class="l">When</span><span class="i">This Sunday</span></div><div class="f"><span class="l">Time</span><span class="i">10:30am</span></div></div>'
            '<div class="f"><span class="l">Description</span><span class="h">Leave a blank line to start a new paragraph.</span>'
            '<span class="i area">Five people from our church family are being baptised. Bring a friend and stay for morning tea.</span></div>'
            '<div class="f"><span class="l">Registration link</span><span class="h">A web address for signing up. Adds a Register button.</span><span class="i"></span></div>')


def one_column():
    dlg = f'<div class="dlg" style="left:524px;top:40px;width:640px;height:820px">{HEAD}<div class="bd">{fields()}</div>{FOOT}</div>'
    page("d-one-column.html", BEHIND + dlg)


LOOK = """
.look{background:var(--page);border-left:1px solid var(--line);padding:20px 22px;display:flex;flex-direction:column;gap:12px;width:380px;flex:none}
.look .cap{font-size:13px;font-weight:700;color:var(--ink2)}
.acard{background:var(--surface);border-radius:14px;overflow:hidden}.acard .pic{height:150px;background:linear-gradient(135deg,#2a3d5e,#009c8f)}.acard .w{padding:12px 14px 14px}
.acard .lab{font-size:12px}.acard b{display:block;font-size:17px;margin-top:2px}.acard .wh{font-size:13px;font-weight:600;color:var(--ink2);margin-top:3px}.acard p{font-size:13px;color:var(--ink2);line-height:1.45;margin-top:6px}
.erow{background:#fff;color:#18233a;border-radius:10px;padding:12px;display:flex;gap:12px}.erow .p{width:110px;height:74px;border-radius:6px;flex:none;background:linear-gradient(135deg,#2a3d5e,#009c8f)}
.erow .l{font-size:10px;font-weight:700;color:#007a70}.erow .t{font-size:13.5px;font-weight:700}.erow .w{font-size:10.5px;font-weight:700;color:#2a3d5e;margin-top:2px}.erow p{font-size:10.5px;line-height:1.4;margin-top:4px}
"""


def with_look():
    look = ('<div class="look"><span class="cap">In the app</span><div class="acard"><div class="pic"></div><div class="w"><div class="lab">Baptisms this Sunday</div>'
            '<b>Baptisms: five people, one family</b><div class="wh">This Sunday · 10:30am</div><p>Five people from our church family are being baptised. Bring a friend and stay for morning tea.</p></div></div>'
            '<span class="cap" style="margin-top:6px">In the email</span><div class="erow"><span class="p"></span><div><div class="l">Baptisms this Sunday</div><div class="t">Baptisms: five people, one family</div>'
            '<div class="w">This Sunday @ 10:30am</div><p>Five people from our church family are being baptised.</p></div></div></div>')
    dlg = (f'<div class="dlg" style="left:324px;top:40px;width:1040px;height:820px">{HEAD}'
           f'<div style="display:flex;flex:1;min-height:0;border-top:1px solid var(--line)"><div class="bd">{fields()}</div>{look}</div>{FOOT}</div>')
    page("e-with-look.html", BEHIND + dlg, LOOK)


BESIDE = """
.pcol{width:260px;flex:none;padding:16px 0 16px 26px;display:flex;flex-direction:column;gap:10px}
.sq{width:234px;height:234px;border-radius:14px;background:linear-gradient(135deg,#2a3d5e,#009c8f)}
.facts{font-size:13px;color:var(--ink2);line-height:1.5;border-top:1px solid var(--line);padding-top:10px;margin-top:4px}.facts b{color:var(--ink);font-weight:600}
"""


def picture_beside():
    left = ('<div class="pcol"><div class="sq"></div><div style="display:flex;gap:8px"><span class="btn s">Change picture</span><span class="btn s">Remove</span></div>'
            '<div class="facts"><b>From the calendar</b><br>Services · Sunday 20 December, 10:30am<br>Main auditorium<br><span class="back">Open the event</span></div></div>')
    dlg = (f'<div class="dlg" style="left:394px;top:40px;width:900px;height:820px">{HEAD}'
           f'<div style="display:flex;flex:1;min-height:0">{left}<div class="bd">{fields(picture=False)}</div></div>{FOOT}</div>')
    page("f-picture-beside.html", BEHIND + dlg, BESIDE)


one_column()
with_look()
picture_beside()
