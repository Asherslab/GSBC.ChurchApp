import pathlib
K = {
 "light": dict(page="#ffffff", ink="#18233a", ink2="#545e70", navy="#2a3d5e", beige="#d5c9b7", sand="#f4efe7", aqua="#00776d", line="rgba(42,61,94,.12)", btn="#2a3d5e", btnink="#ffffff", seg="#ffffff", tab="#ffffff"),
 "dark": dict(page="#141d31", ink="#f2ede4", ink2="#b2b8c5", navy="#283c62", beige="#d5c9b7", sand="#26334f", aqua="#2ec4b6", line="rgba(255,255,255,.09)", btn="#d5c9b7", btnink="#141d31", seg="#3a4766", tab="#141d31"),
}
CHECK = '<svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>'
FEET = '<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 16v-2.4C4 11.5 3 10.5 3 8c0-2.7 1.5-6 4.5-6C9.4 2 10 3.8 10 5.5c0 3.1-2 5.7-2 8.7V16a2 2 0 1 1-4 0"/><path d="M20 20v-2.4c0-2.1 1-3.1 1-5.6 0-2.7-1.5-6-4.5-6C14.6 6 14 7.8 14 9.5c0 3.1 2 5.7 2 8.7V20a2 2 0 1 0 4 0"/></svg>'
CHEV = '<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="m9 18 6-6-6-6"/></svg>'

def ask(k):
    return f'''<div class="ask" style="background:{k["sand"]}"><p class="askt">Got an hour? Walk from your door</p>
<p class="askb" style="color:{k["ink2"]}">The app makes a loop of streets the church hasn't walked lately. Put your phone away and walk: it marks the streets as you pass.</p>
<div class="seg"><span>30 min</span><span class="on" style="background:{k["seg"]}">1 hr</span><span>2 hr</span><span>Distance</span></div>
<div class="btn" style="background:{k["btn"]};color:{k["btnink"]}">{FEET} Plan my walk</div></div>'''

def rows(k):
    r = lambda t, s: f'<div class="row" style="border-color:{k["line"]}"><span class="ico" style="background:{k["sand"]}">{FEET}</span><span class="rt"><b>{t}</b><span style="color:{k["ink2"]}">{s}</span></span>{CHEV}</div>'
    return f'<div class="rows" style="border-color:{k["line"]}">{r("My outreach","Your walks and your totals")}{r("Offline maps","The maps kept on this phone")}</div>'

def phone(s, look):
    k = K[s]
    note = "Letterboxing with the church, at your own pace"
    top = ""; toast = ""
    if look == "line":
        top = f'''<div class="saved" style="background:{k["sand"]}"><span class="tick" style="color:{k["aqua"]}">{CHECK}</span><span class="st"><b>Your walk is saved</b><span style="color:{k["ink2"]}">1.4 km · 62 letterboxes · thank you</span><a style="color:{k["aqua"]}">See it in My outreach</a></span></div>'''
    if look == "toast":
        toast = f'''<div class="toast" style="background:{k["navy"]};color:#fff"><span style="color:{k["beige"]}">{CHECK}</span><span>Walk saved · 62 letterboxes</span><a style="color:{k["beige"]}">My outreach</a></div>'''
    if look == "mast":
        note = f'<span class="mnote">{CHECK}<span>Walk saved: 1.4 km, 62 letterboxes. Thank you.</span></span>'
    return f'''<figure class="phone" style="background:{k["page"]};color:{k["ink"]}">
<div class="mast" style="background:{k["navy"]}"><p class="back">‹ More</p><p class="mt">Outreach</p><p class="mn" style="color:{k["beige"]}">{note}</p></div>
<div class="body">{top}{ask(k)}{rows(k)}</div>{toast}
<div class="tabs" style="background:{k["tab"]};border-color:{k["line"]};color:{k["ink2"]}"><span>Home</span><span>Sermons</span><span>Calendar</span><span>Ministries</span><span style="color:{k["aqua"]}">More</span></div>
<figcaption>{s}</figcaption></figure>'''

CSS = '''*{box-sizing:border-box;margin:0}body{font-family:Montserrat,-apple-system,system-ui,sans-serif;background:#e9e4da;padding:24px;display:flex;gap:28px;flex-wrap:wrap;justify-content:center}
.phone{width:375px;height:780px;border-radius:36px;overflow:hidden;position:relative;box-shadow:0 10px 30px rgba(0,0,0,.18);display:flex;flex-direction:column}
figcaption{position:absolute;top:8px;right:18px;font-size:10px;opacity:.5}
.mast{padding:54px 20px 22px;color:#fff}.back{font-size:16px;margin-bottom:12px}.mt{font-size:38px;font-weight:700;letter-spacing:-.5px}.mn{font-size:15px;font-weight:600;margin-top:6px;line-height:1.35}
.mnote{display:flex;gap:8px;align-items:flex-start}.mnote svg{flex:none;width:18px;height:18px;margin-top:1px}
.body{padding:20px;flex:1;display:flex;flex-direction:column;gap:20px;overflow:hidden}
.ask{border-radius:20px;padding:18px}.askt{font-size:19px;font-weight:700;line-height:1.25}.askb{font-size:14px;line-height:1.45;margin:8px 0 12px}
.seg{display:flex;justify-content:space-between;font-size:14px;font-weight:600;margin-bottom:12px}.seg span{padding:8px 12px;border-radius:10px}
.btn{border-radius:16px;padding:14px;text-align:center;font-weight:700;font-size:16px;display:flex;gap:8px;justify-content:center;align-items:center}
.rows{border-top:1px solid}.row{display:flex;gap:14px;align-items:center;padding:12px 0;border-bottom:1px solid}.ico{width:36px;height:36px;border-radius:10px;display:grid;place-items:center}.rt{flex:1;display:flex;flex-direction:column;font-size:14px;gap:2px}.rt b{font-size:16px}
.saved{display:flex;gap:12px;padding:14px 16px;border-radius:16px}.tick{flex:none;margin-top:1px}.st{display:flex;flex-direction:column;gap:3px;font-size:14px}.st b{font-size:16px}.st a{font-weight:600;margin-top:4px}
.toast{position:absolute;left:16px;right:16px;bottom:84px;border-radius:14px;padding:12px 14px;display:flex;gap:10px;align-items:center;font-size:14px;font-weight:600;box-shadow:0 8px 24px rgba(0,0,0,.25)}.toast span:nth-child(2){flex:1}.toast a{font-weight:700}
.tabs{display:flex;justify-content:space-around;font-size:11px;font-weight:600;padding:12px 0 26px;border-top:1px solid}'''

for look in ["line", "toast", "mast"]:
    html = f'<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><style>{CSS}</style></head><body>{phone("light", look)}{phone("dark", look)}</body></html>'
    pathlib.Path(f"saved-{look}.html").write_text(html)
