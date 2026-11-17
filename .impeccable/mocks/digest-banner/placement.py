from sketch import page, side, rows, ITEMS, BAND

CSS = """.cols{display:grid;grid-template-columns:600px 1fr;flex:1;min-height:0}
.left{padding:24px 40px;border-right:1px solid var(--line)}.right{padding:24px 24px 0;display:flex;flex-direction:column;gap:14px;min-width:0;overflow:hidden}
.ph{display:flex;gap:14px;align-items:flex-start}.ph .hint{font-size:13px;color:var(--ink2);line-height:1.45}
.pv{display:flex;gap:16px;align-items:flex-start}.thumb{width:240px;height:91px;border-radius:12px;background:#7189be;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:3px;flex:none}
.thumb b{font-family:Georgia,serif;font-style:italic;font-size:23px;color:#fbf8f0;letter-spacing:-.03em}.thumb b span{color:#22325a}.thumb i{font-style:normal;font-size:7px;letter-spacing:.3em;color:#fbf8f0}
.acts{display:flex;flex-direction:column;gap:8px;font-size:13px;color:var(--ink2)}
.email{background:#e9eef4;border-radius:14px 14px 0 0;overflow:hidden;flex:1;min-height:0}
.ban{height:190px;background:#7189be;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px}.ban b{font-family:Georgia,serif;font-style:italic;font-size:46px;color:#fbf8f0;letter-spacing:-.04em}.ban b span{color:#22325a}.ban i{font-style:normal;color:#fbf8f0;font-size:15px;letter-spacing:.32em;font-weight:600}
.ecol{background:#fff;font-family:Arial,sans-serif;color:#1f2a3c}
.dl{padding:20px 26px 0}.dl span{display:block;font:700 11px Montserrat,Arial;letter-spacing:.14em;text-transform:uppercase;color:#2a3d5e;padding-bottom:10px;border-bottom:2px solid #2a3d5e}
.ei{padding:16px 26px}.ei .el{font-size:12px;font-weight:700;color:#007a70}.ei .et{font-size:18px;font-weight:700}.ei .ew{font-size:13px;font-weight:700;color:#2a3d5e;margin-top:2px}
.radio{display:flex;flex-direction:column;border-radius:12px;box-shadow:inset 0 0 0 1px var(--line);overflow:hidden}
.radio label{display:flex;gap:12px;padding:10px 14px;align-items:flex-start;border-top:1px solid var(--line)}.radio label:first-child{border-top:0}
.radio .dot{width:18px;height:18px;border-radius:50%;box-shadow:inset 0 0 0 2px var(--edge);flex:none;margin-top:1px}.radio .on .dot{box-shadow:inset 0 0 0 5px var(--accent)}
.radio b{display:block;font-size:14px;font-weight:600}.radio small{display:block;font-size:12.5px;color:var(--ink2);margin-top:1px}
.tog{display:flex;justify-content:space-between;gap:16px;align-items:center;padding:10px 0;border-top:1px solid var(--line)}.tog b{display:block;font-size:14px;font-weight:600}.tog small{font-size:12.5px;color:var(--ink2)}
.sw{width:44px;height:26px;border-radius:13px;background:var(--field);box-shadow:inset 0 0 0 1px var(--edge);position:relative;flex:none}.sw:after{content:"";position:absolute;width:20px;height:20px;border-radius:50%;background:#fff;top:3px;left:3px;box-shadow:0 1px 3px rgba(0,0,0,.25)}
.sw.on{background:var(--accent);box-shadow:none}.sw.on:after{left:21px}
.group{border-radius:14px;box-shadow:inset 0 0 0 1px var(--line);padding:14px 16px;display:flex;flex-direction:column;gap:12px;background:var(--surface)}
.group h3{font-size:15px;font-weight:700}.group .lead{font-size:13px;color:var(--ink2);margin-top:-8px}
.inline{display:flex;gap:22px;flex-wrap:wrap}.inline span{display:flex;gap:8px;align-items:center;font-size:14px;font-weight:600}.inline .dot{width:18px;height:18px;border-radius:50%;box-shadow:inset 0 0 0 2px var(--edge)}.inline .on .dot{box-shadow:inset 0 0 0 5px var(--accent)}
"""

HEAD = ('<div class="ph"><div class="seg" style="height:40px"><span>In the app</span><span class="on">Email</span></div>'
        '<span class="hint">Copy for email gives this as HTML. In Elvanto, start the email in the drag-and-drop builder…</span></div>')
PHOTO = ('<div class="f"><span class="l">Photo across the top</span><div class="pv"><span class="thumb"><b>What’s<span>Next</span></b><i>AT GOOD SHEPHERD</i></span>'
         '<div class="acts">The app shows the original · 1200×456<span class="btn s">Change the picture</span><span class="btn s">Remove the picture</span></div></div></div>')
EMAIL = ('<div class="email"><div class="ecol" style="width:100%"><div class="ban"><b>What’s<span>Next</span></b><i>AT GOOD SHEPHERD</i></div>'
         '<div class="dl"><span>Monday 14 – Sunday 27 December</span></div><div class="ei"><div class="el" style="color:#9a5a1c">Services on break</div>'
         '<div class="et">No Bible Study &amp; Prayer</div><div class="ew">The next two Wednesdays</div></div></div></div>')

LEFT = ('<div class="left"><div class="status"><b>✓ Saved</b> · not sent to the app yet</div><div class="choices"><div class="f"><span class="l">From</span><div class="i" style="width:180px">14 Dec 2026</div></div>'
        '<div class="f"><span class="l">How long</span><div class="seg"><span>1 week</span><span class="on">2 weeks</span><span>1 month</span><span>Other</span></div></div></div>'
        '<div class="range">Monday 14 – Sunday 27 December</div><div style="margin-top:14px">' + rows(ITEMS[:4]) + '</div></div>')


def sketch(name, controls):
    body = BAND.format(actions="") + f'<div class="cols">{LEFT}<div class="right">{HEAD}{controls}{EMAIL}</div></div>'
    page(name, body, CSS, dark=False)


RADIO = ('<div class="f"><span class="l">Under the photo</span><div class="radio">'
         '<label><span class="dot"></span><span><b>The title and the dates</b><small>What\'s Next at Good Shepherd, on navy</small></span></label>'
         '<label class="on"><span class="dot"></span><span><b>Just the dates</b><small>For a photo that already has the title</small></span></label>'
         '<label><span class="dot"></span><span><b>Nothing</b><small>Straight on to the first item</small></span></label></div></div>')
sketch("f-under-photo-rows.html", PHOTO + RADIO)

TOGGLES = ('<div><div class="tog"><span><b>Show the title</b><br><small>What\'s Next at Good Shepherd, on navy under the photo</small></span><span class="sw"></span></div>'
           '<div class="tog"><span><b>Show the dates</b><br><small>Monday 14 – Sunday 27 December, above the first item</small></span><span class="sw on"></span></div></div>')
sketch("g-two-switches.html", PHOTO + TOGGLES)

GROUP = ('<div class="group"><h3>Top of the email</h3><div class="lead">A photo across the top, and what goes under it.</div>' + PHOTO.replace('<span class="l">Photo across the top</span>', '')
         + '<div class="f"><span class="l">Under the photo</span><div class="inline"><span><i class="dot"></i>Title and dates</span><span class="on"><i class="dot"></i>Just the dates</span><span><i class="dot"></i>Nothing</span></div></div></div>')
sketch("h-top-group.html", GROUP)
