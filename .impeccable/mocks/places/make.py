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


def street_map(w, h, labels=True):
    s = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">'
         f'<rect width="{w}" height="{h}" fill="#f7f4ee"/>'
         f'<path d="M{w*.62} 0 h{w*.38} v{h*.34} q-{w*.2} {h*.08} -{w*.3} -{h*.05}z" fill="#d9e6cf"/>'
         f'<rect x="{w*.08}" y="{h*.62}" width="{w*.22}" height="{h*.26}" rx="6" fill="#ebe2d2"/>'
         f'<path d="M0 {h*.9} q{w*.3} -{h*.12} {w*.5} -{h*.02} t{w*.5} {h*.04} V{h} H0z" fill="#cde2e3"/>')
    minor = "#ffffff"
    for y in (.18, .44, .72):
        s += f'<path d="M0 {h*y} L{w} {h*(y+.06)}" stroke="#ddd5c7" stroke-width="9" fill="none"/><path d="M0 {h*y} L{w} {h*(y+.06)}" stroke="{minor}" stroke-width="6.5" fill="none"/>'
    for x in (.14, .36, .78):
        s += f'<path d="M{w*x} 0 L{w*(x+.04)} {h}" stroke="#ddd5c7" stroke-width="9" fill="none"/><path d="M{w*x} 0 L{w*(x+.04)} {h}" stroke="{minor}" stroke-width="6.5" fill="none"/>'
    s += (f'<path d="M{w*.55} 0 C{w*.5} {h*.3} {w*.62} {h*.6} {w*.56} {h}" stroke="#a99c88" stroke-width="12" fill="none"/>'
          f'<path d="M{w*.55} 0 C{w*.5} {h*.3} {w*.62} {h*.6} {w*.56} {h}" stroke="#fbf6ee" stroke-width="9" fill="none"/>'
          + (f'<text x="{w*.57}" y="{h*.28}" font-family="Montserrat,sans-serif" font-size="11" fill="#545e70" transform="rotate(80 {w*.57} {h*.28})">South Pine Road</text>'
          f'<text x="{w*.18}" y="{h*.42}" font-family="Montserrat,sans-serif" font-size="10" fill="#545e70">Dayboro Road</text>' if labels else "")
          + '</svg>')
    return "data:image/svg+xml;base64," + base64.b64encode(s.encode()).decode()


PIN = ('<svg viewBox="0 0 28 38" width="28" height="38" style="position:absolute;left:50%;top:50%;transform:translate(-50%,-100%)">'
       '<path d="M14 37s12-13.4 12-22.5A12 12 0 0 0 2 14.5C2 23.6 14 37 14 37z" fill="var(--primary)" stroke="#fff" stroke-width="2"/>'
       '<circle cx="14" cy="14.5" r="4.5" fill="#fff"/></svg>')
ICON_PIN = ('<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">'
            '<path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/></svg>')
ICON_SEARCH = ('<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">'
               '<circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>')
CHEVRON = '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2"><path d="m9 6 6 6-6 6"/></svg>'

NAME = "Eatons Hill shops car park"
ADDRESS = "640 South Pine Road, Eatons Hill QLD 4037"
NOTE = "Park at the far end, near the bakery."

CSS = """
*{box-sizing:border-box;margin:0;padding:0}
html{font-size:17px}
body{width:1440px;height:900px;overflow:hidden;font:400 var(--text-body)/1.45 var(--font);color:var(--ink);background:#d9d4cb;display:flex;gap:20px;padding:40px 20px 20px;position:relative;-webkit-font-smoothing:antialiased}
.win{width:640px;height:840px;flex:none;border-radius:12px;overflow:hidden;background:var(--page);display:flex;box-shadow:0 8px 30px rgb(0 0 0 / .18);position:relative}
.side{width:56px;flex:none;background:var(--masthead)}
.work{flex:1;display:flex;flex-direction:column;min-width:0}
.band{padding:16px 24px 12px;border-bottom:1px solid var(--line)}
.band h1{font-size:var(--text-title-lg);letter-spacing:-.02em}
.band p{font-size:var(--text-caption);color:var(--ink-2)}
.pane{padding:18px 24px;overflow:hidden;flex:1}
.lbl{font-size:var(--text-label);font-weight:600;margin-bottom:2px;display:block}
.desc{font-size:var(--text-caption);color:var(--ink-2);margin-bottom:6px}
.fld{white-space:nowrap;overflow:hidden;background:var(--field);border:1px solid var(--field-edge);border-radius:12px;min-height:44px;display:flex;align-items:center;gap:8px;padding:0 12px;font-size:var(--text-body);color:var(--ink)}
.fld.ph{color:var(--ink-3)}
.fld.focus{outline:2px solid var(--accent);outline-offset:1px}
.f{margin-bottom:16px}
.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.seg{display:inline-flex;background:var(--sand-strong);border-radius:999px;padding:3px;gap:2px;margin-bottom:10px}
.seg span{padding:7px 14px;border-radius:999px;font-size:var(--text-caption);font-weight:600;color:var(--ink-2)}
.seg span.on{background:var(--page);color:var(--ink);box-shadow:0 1px 3px rgb(0 0 0 / .15)}
.btn{white-space:nowrap;display:inline-flex;align-items:center;justify-content:center;height:40px;padding:0 16px;border-radius:22px;font-weight:600;font-size:var(--text-caption);background:var(--primary);color:var(--on-primary)}
.btn.q{background:var(--field);color:var(--ink);border:1px solid var(--field-edge)}
.link{color:var(--accent);font-weight:600;font-size:var(--text-caption)}
.map{position:relative;border-radius:12px;overflow:hidden;background-size:cover;background-position:center}
.attr{position:absolute;right:6px;bottom:4px;font-size:9px;color:var(--ink-3);background:rgb(255 255 255 / .7);padding:0 4px;border-radius:4px}
.res{border-top:1px solid var(--line)}
.res div{padding:9px 2px;border-bottom:1px solid var(--line)}
.res b{display:block;font-size:var(--text-row);font-weight:600}
.res span{font-size:var(--text-caption);color:var(--ink-2)}
.res .cur{background:var(--selected-fill);margin:0 -10px;padding:9px 12px;border-radius:12px;border-bottom-color:transparent}
.foot{display:flex;justify-content:space-between;align-items:center;padding:12px 24px;border-top:1px solid var(--line);background:var(--page)}
.phone{width:360px;height:840px;flex:none;border-radius:44px;border:10px solid #111;overflow:hidden;background:var(--page);display:flex;flex-direction:column;position:relative}
.ps{height:44px;flex:none;display:flex;justify-content:space-between;align-items:flex-end;padding:0 26px 6px;font-size:13px;font-weight:600;background:var(--masthead);color:#fff}
.pbar{height:48px;flex:none;display:flex;align-items:center;gap:8px;padding:0 14px;background:var(--masthead);color:#fff;font-weight:600;font-size:var(--text-row)}
.pbar .sp{flex:1;text-align:center}
.pview{flex:1;overflow:hidden;position:relative}
.mast{background:var(--masthead);color:#fff;padding:6px 20px 18px}
.mast h2{font-size:var(--text-headline);letter-spacing:-.02em}
.mast p{color:var(--masthead-note);font-size:var(--text-caption)}
.sec{padding:16px 20px 0}
.sech{font-size:var(--text-micro);font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:var(--ink-2);margin-bottom:8px}
.row{display:flex;gap:12px;align-items:flex-start;padding:12px 0;border-bottom:1px solid var(--line)}
.row .ic{color:var(--accent);flex:none;margin-top:2px}
.row b{display:block;font-size:var(--text-row);font-weight:600}
.row span{display:block;font-size:var(--text-caption);color:var(--ink-2)}
.kb{position:absolute;left:0;right:0;bottom:0;height:270px;background:#d1d4db;border-top:1px solid #b9bcc4;padding:8px 4px}
.kb div{display:flex;gap:5px;justify-content:center;margin-bottom:9px}
.kb i{display:block;width:29px;height:40px;background:#fff;border-radius:5px;box-shadow:0 1px 0 #898a8d;font-style:normal;font-size:16px;text-align:center;line-height:40px}
.cap{position:absolute;top:12px;font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#4b5563}
.lab{position:relative}
.scrim{position:absolute;inset:0;background:var(--scrim)}
.dlg{position:absolute;left:28px;right:28px;top:70px;bottom:40px;background:var(--page);border-radius:14px;box-shadow:0 20px 50px rgb(0 0 0 / .3);display:flex;flex-direction:column;overflow:hidden}
.dlg h3{font-size:var(--text-title);padding:16px 20px 4px}
"""


def keyboard():
    rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"]
    return '<div class="kb">' + "".join('<div>' + "".join(f"<i>{k}</i>" for k in r) + '</div>' for r in rows) + \
        '<div><i style="width:80px;font-size:13px">123</i><i style="width:150px;font-size:13px">space</i><i style="width:80px;font-size:13px;background:var(--accent);color:#fff">search</i></div></div>'


def page(title, admin, choose, show):
    return (f'<!doctype html><html lang="en"><head><meta charset="utf-8"><title>{title}</title>'
            f'<style>{tokens()}{CSS}</style></head><body>'
            '<span class="cap" style="left:20px">Admin panel: choosing a place</span><span class="cap" style="left:680px">Phone: a leader choosing one</span><span class="cap" style="left:1060px">Phone: what everyone sees</span>'
            f'<div class="win"><div class="side"></div><div class="work">{admin}</div></div>'
            f'<div class="phone">{choose}</div>'
            f'<div class="phone">{show}</div>'
            '</body></html>')


def admin_form(where):
    return ('<div class="band"><h1>Saturday 22 November</h1><p>Outreach day · Christmas letterbox drop</p></div>'
            '<div class="pane">'
            '<div class="pair f"><div><span class="lbl">Starts</span><div class="fld">8:30am</div></div><div><span class="lbl">Ends</span><div class="fld">11:00am</div></div></div>'
            f'{where}'
            '</div><div class="foot"><span class="btn q">Cancel</span><span class="btn">Save</span></div>')


def phone_frame(title, back, body):
    return (f'<div class="ps"><span>9:41</span><span>●●● ▮</span></div>'
            f'<div class="pbar"><span>‹ {back}</span><span class="sp">{title}</span><span style="width:50px"></span></div>'
            f'<div class="pview">{body}</div>')


def event_head():
    return ('<div class="mast"><p>Outreach day</p><h2>Christmas letterbox drop</h2></div>'
            '<div class="sec"><div class="row"><span class="ic">◷</span><div><b>Saturday 22 November</b><span>8:30 to 11:00am</span></div></div>')


def row_and_picker():
    row_place = (f'<div class="fld" style="padding:8px 12px;align-items:flex-start"><span style="color:var(--accent);margin-top:2px">{ICON_PIN}</span>'
                 f'<div style="flex:1"><b style="font-weight:600;display:block">{NAME}</b><span style="font-size:var(--text-caption);color:var(--ink-2)">{ADDRESS}</span></div>'
                 '<span class="link">Change</span></div>')
    where = f'<div class="f"><span class="lbl">Where people meet</span><p class="desc">Printed on every route map and shown on the day\'s page.</p>{row_place}</div>'
    dialog = ('<div class="scrim"></div><div class="dlg"><h3>Where people meet</h3>'
              '<div style="padding:4px 20px 0"><div class="seg"><span>At church</span><span class="on">Somewhere else</span></div></div>'
              '<div style="display:grid;grid-template-columns:230px 1fr;gap:14px;padding:0 20px;flex:1;min-height:0">'
              f'<div><div class="fld focus" style="margin-bottom:8px">{ICON_SEARCH}<span>640 south pine</span></div>'
              '<div class="res"><div class="cur"><b>640 South Pine Road</b><span>Eatons Hill · 4 km west of church</span></div>'
              '<div><b>640 South Pine Road</b><span>Brendale · 6 km north-west</span></div><div><b>Eatons Hill Hotel</b><span>646 South Pine Road · 4 km west</span></div></div>'
              '<p class="desc" style="margin-top:10px">Or drag the pin on the map to the exact spot.</p></div>'
              f'<div><div class="map" style="height:230px;background-image:url({street_map(300, 230)})">{PIN}<span class="attr">© OpenStreetMap</span></div>'
              f'<div class="f" style="margin-top:10px"><span class="lbl">Name</span><div class="fld">{NAME}</div></div>'
              f'<div><span class="lbl">Note</span><div class="fld" style="white-space:normal;padding:8px 12px">{NOTE}</div></div></div></div>'
              '<div class="foot"><span class="btn q">Cancel</span><span class="btn">Use this place</span></div></div>')
    admin = admin_form(where) + dialog
    choose = phone_frame("Where it meets", "Settings",
                         '<div style="padding:12px 16px 0"><div class="seg"><span>At church</span><span class="on">Somewhere else</span></div>'
                         f'<div class="fld focus" style="margin-bottom:6px">{ICON_SEARCH}<span>12 kenrick</span></div>'
                         '<div class="res"><div class="cur"><b>12 Kenrick Street</b><span>Albany Creek · 1 km south</span></div><div><b>12 Kenrick Close</b><span>Brendale · 5 km north-west</span></div></div>'
                         f'<div class="map" style="height:150px;margin-top:10px;background-image:url({street_map(320, 150)})">{PIN}</div>'
                         '</div>' + keyboard())
    show = phone_frame("", "Outreach", event_head() +
                       f'<div class="row"><span class="ic">{ICON_PIN}</span><div style="flex:1"><b>{NAME}</b><span>{ADDRESS}</span><span style="margin-top:4px">{NOTE}</span></div>'
                       f'<span class="map" style="width:64px;height:64px;flex:none;background-image:url({street_map(64, 64, False)})"><svg viewBox="0 0 28 38" width="16" height="22" style="position:absolute;left:24px;top:12px"><path d="M14 37s12-13.4 12-22.5A12 12 0 0 0 2 14.5C2 23.6 14 37 14 37z" fill="var(--primary)" stroke="#fff" stroke-width="2"/></svg></span></div>'
                       '<div class="row"><span class="ic">✉</span><div><b>Letterboxing</b><span>Bring a water bottle and a hat.</span></div></div></div>')
    return page("A row, and a picker of its own", admin, choose, show)


def search_first():
    where = ('<div class="f"><span class="lbl">Where people meet</span><p class="desc">Printed on every route map and shown on the day\'s page.</p>'
             '<div class="seg"><span>At church</span><span class="on">Somewhere else</span></div>'
             f'<div style="position:relative"><div class="fld focus">{ICON_SEARCH}<span>640 south pine</span></div>'
             '<div style="position:absolute;left:0;right:0;top:50px;background:var(--page);border-radius:12px;box-shadow:0 10px 30px rgb(0 0 0 / .18);padding:6px 12px;z-index:2" class="res">'
             '<div class="cur" style="margin:0 -6px"><b>640 South Pine Road</b><span>Eatons Hill · 4 km west of church</span></div>'
             '<div><b>640 South Pine Road</b><span>Brendale · 6 km north-west</span></div><div style="border:0"><b>Eatons Hill Hotel</b><span>646 South Pine Road · 4 km west</span></div></div></div>'
             f'<div class="map" style="height:170px;margin-top:10px;background-image:url({street_map(540, 170)})">{PIN}<span class="attr">© OpenStreetMap</span></div>'
             '<p class="desc" style="margin-top:6px">Drag the pin to the exact spot.</p></div>'
             f'<div class="pair f"><div><span class="lbl">Name</span><div class="fld">{NAME}</div></div><div><span class="lbl">Note</span><div class="fld" style="white-space:normal;padding:8px 12px">{NOTE}</div></div></div>')
    choose = phone_frame("Settings", "Life Group", '<div class="sec"><p class="sech">When and where it meets</p>'
                         '<div class="f"><span class="lbl">When it meets</span><div class="fld">Tuesdays, 7:30pm</div></div>'
                         '<span class="lbl">Where it meets</span><div class="seg"><span>At church</span><span class="on">Somewhere else</span></div>'
                         f'<div class="fld focus">{ICON_SEARCH}<span>12 kenrick</span></div>'
                         '<div class="res" style="margin-top:6px"><div class="cur"><b>12 Kenrick Street</b><span>Albany Creek · 1 km south</span></div><div><b>12 Kenrick Close</b><span>Brendale · 5 km north-west</span></div></div>'
                         '</div>' + keyboard())
    show = phone_frame("", "Outreach", event_head() +
                       f'<div class="row"><span class="ic">{ICON_PIN}</span><div style="flex:1"><b>{NAME}</b><span>{ADDRESS}</span><span style="margin-top:4px">{NOTE}</span>'
                       '<div style="display:flex;gap:8px;margin-top:10px"><span class="btn q" style="height:36px">Show on the map</span><span class="btn q" style="height:36px">Directions</span></div></div></div>'
                       '<div class="row"><span class="ic">✉</span><div><b>Letterboxing</b><span>Bring a water bottle and a hat.</span></div></div></div>')
    return page("Search first, the map confirms", admin_form(where), choose, show)


def map_strip():
    where = ('<div class="f"><span class="lbl">Where people meet</span><p class="desc">Printed on every route map and shown on the day\'s page.</p>'
             '<div class="seg"><span>At church</span><span class="on">Somewhere else</span></div>'
             '<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">'
             f'<div><div class="fld f">{ICON_SEARCH}<span>640 South Pine Road, Eatons Hill</span></div>'
             f'<div class="f"><span class="lbl">Name</span><div class="fld">{NAME}</div></div>'
             f'<div><span class="lbl">Note</span><div class="fld" style="min-height:70px;align-items:flex-start;padding-top:10px;white-space:normal">{NOTE}</div></div></div>'
             f'<div class="map" style="height:262px;background-image:url({street_map(270, 262)})">{PIN}<span class="attr">© OpenStreetMap</span></div></div>'
             '<p class="desc" style="margin-top:6px">Search for the address, or drag the pin to the exact spot.</p></div>')
    choose = phone_frame("Where it meets", "Settings",
                         f'<div class="map" style="height:300px;border-radius:0;background-image:url({street_map(340, 300)})">{PIN}'
                         f'<div class="fld" style="position:absolute;left:12px;right:12px;top:12px;background:var(--page);box-shadow:0 4px 14px rgb(0 0 0 / .15)">{ICON_SEARCH}<span>12 Kenrick Street, Albany Creek</span></div></div>'
                         '<div style="padding:12px 16px 0"><div class="seg"><span>At church</span><span class="on">Somewhere else</span></div>'
                         '<div class="f"><span class="lbl">Name</span><div class="fld ph">Leave empty to show the address</div></div>'
                         '<div class="f"><span class="lbl">Note</span><div class="fld">Come round the side gate.</div></div>'
                         '<div class="f" style="display:flex;gap:12px;align-items:flex-start"><div style="flex:1"><span class="lbl">A home</span><p class="desc">Only the group\'s people see the street address; everyone else sees Albany Creek.</p></div><span style="flex:none;width:51px;height:31px;border-radius:999px;background:var(--live);position:relative;margin-top:4px"><i style="position:absolute;right:2px;top:2px;width:27px;height:27px;border-radius:50%;background:#fff"></i></span></div></div>')
    show = phone_frame("", "Outreach", event_head() + '</div><div class="sec">'
                       '<div style="border-radius:14px;overflow:hidden;background:var(--sand)">'
                       f'<div class="map" style="height:170px;border-radius:0;background-image:url({street_map(320, 170)})">{PIN}<span class="attr">© OpenStreetMap</span></div>'
                       f'<div style="padding:12px 14px"><b style="display:block;font-size:var(--text-row)">{NAME}</b><span style="display:block;font-size:var(--text-caption);color:var(--ink-2)">{ADDRESS}</span>'
                       f'<span style="display:block;font-size:var(--text-caption);color:var(--ink-2);margin-top:4px">{NOTE}</span>'
                       '<span class="btn q" style="margin-top:10px;height:36px">Open in Maps</span></div></div></div>')
    return page("The map strip", admin_form(where), choose, show)


for name, make in [("a-row-and-picker", row_and_picker), ("b-search-first", search_first), ("c-map-strip", map_strip)]:
    (HERE / f"{name}.html").write_text(make())
    print(name)
