from make import CSS, FONT, HERE
from make2 import MORE, POST

POP = """
.desk3{position:relative;height:900px;border-right:1px solid var(--line);display:grid;grid-template-columns:1fr 380px}
.desk3 .veil{position:absolute;inset:0;background:rgba(8,12,22,.45)}
.dlg{position:absolute;left:50%;top:50%;transform:translate(-50%,-50%);width:520px;background:#fff;border-radius:22px;box-shadow:0 20px 60px rgba(0,0,0,.3);padding:24px 26px 22px}
.dlg h2,.sheet h2{font-size:21px;font-weight:700;letter-spacing:-.015em}.dlg .ld,.sheet .ld{font-size:14px;color:var(--ink2);margin-top:6px;line-height:1.45}
.acts{display:flex;justify-content:space-between;margin-top:20px;gap:10px}.acts span{height:44px;border-radius:22px;padding:0 20px;display:flex;align-items:center;font-size:14.5px;font-weight:600;background:#fff;box-shadow:inset 0 0 0 1px var(--edge)}.acts .p{background:var(--navy);color:#fff;box-shadow:none}
.stack span{width:100%;justify-content:center;height:50px;border-radius:14px;margin-top:10px;display:flex;align-items:center;font-size:15px;font-weight:600;box-shadow:inset 0 0 0 1px var(--edge)}.stack .p{background:var(--navy);color:#fff;box-shadow:none}
.big{display:flex;flex-direction:column;gap:10px;margin-top:18px}.big div{border-radius:14px;padding:14px 16px;box-shadow:inset 0 0 0 1px var(--edge);background:#fff}.big div.p{background:var(--navy);color:#fff;box-shadow:none}.big b{display:block;font-size:15px}.big small{font-size:13px;opacity:.85;display:block;margin-top:3px;line-height:1.35}
.toast{position:absolute;left:50%;bottom:110px;transform:translateX(-50%);background:var(--ink);color:#fff;border-radius:12px;padding:10px 16px;font-size:13.5px}
"""

SET = ('<div class="set"><div class="in"><h3>Who it\'s for and telling them <span>Fold ›</span></h3>'
       '<div class="grp"><h4>Who it\'s for</h4><div class="fi">Everyone</div></div>'
       '<div class="grp"><h4>Telling people</h4><p class="hint" style="margin:0">Its notification went Monday 9:02am.</p></div></div>'
       '<div class="foot"><p>It\'s published. Saving changes it for everyone now.</p><div class="sb"><span>Cancel</span><span class="p">Save</span></div></div></div>')


def page(title, dialog, sheet):
    phone = ('<div class="mast" style="padding-top:58px"><h1 style="font-size:24px">Change the post</h1></div>'
             f'<div class="dim"></div><div class="sheet" style="top:auto;padding-bottom:34px"><div class="grab"></div>{sheet}</div>')
    return (f'<!doctype html><html lang="en-AU"><head><meta charset="utf-8">{FONT}<title>{title}</title><style>{CSS}{MORE}{POP}</style></head>'
            f'<body><div class="desk3">{POST}{SET}<div class="veil"></div><div class="dlg">{dialog}</div></div>'
            f'<div class="phone"><div class="scr" style="position:relative">{phone}</div></div></body></html>')


def words(title, line):
    return (f'<div class="fl">Notification</div><div class="fi">{title}</div>'
            f'<div class="fl">More</div><div class="fi m">{line}</div>')


URG = '<div class="tg"><div><b>Urgent</b><small>Buzzes phones in quiet hours too</small></div><span class="sw"></span></div>'

a_lead = '<p class="ld">Saved. It went to everyone (about 140 phones) on Monday at 9:02am. Send it again, saying what changed? It replaces the earlier one in their notifications.</p>'
a_words = words("Updated: Working bee this Saturday", "Now starting at 9am, so families can come after swimming.")
a = page("Tell people again, saying what changed",
         '<h2>Tell people again?</h2>' + a_lead + a_words + URG + '<div class="acts"><span>Not now</span><span class="p">➤ Tell people again</span></div>',
         '<h2>Tell people again?</h2>' + a_lead + a_words + URG + '<div class="stack"><span class="p">➤ Tell people again</span><span>Not now</span></div>')

b_lead = '<p class="ld">A notification to everyone, opening the post. It went before on Monday at 9:02am.</p>'
b_words = words("Working bee this Saturday", "We're tidying the kids' rooms and the play area from 9am…")
b = page("The calendar's sheet, as it is",
         '<h2>Tell people?</h2>' + b_lead + b_words + '<div class="acts"><span>Not now</span><span class="p">➤ Tell people</span></div>',
         '<h2>Tell people?</h2>' + b_lead + b_words + '<div class="stack"><span class="p">➤ Tell people</span><span>Not now</span></div>')

c_choice = ('<p class="ld">Saved. Its notification went on Monday at 9:02am.</p><div class="big">'
            '<div class="p"><b>Tell people again…</b><small>Send its notification again to everyone (about 140 phones). You can change what it says first.</small></div>'
            '<div><b>Just save</b><small>Nobody is told. It changes quietly in News.</small></div></div>')
c = page("Ask first, then the words", '<h2>Tell people about the change?</h2>' + c_choice, '<h2>Tell people about the change?</h2>' + c_choice)

(HERE / "t-a-what-changed.html").write_text(a)
(HERE / "t-b-calendar-sheet.html").write_text(b)
(HERE / "t-c-ask-first.html").write_text(c)
