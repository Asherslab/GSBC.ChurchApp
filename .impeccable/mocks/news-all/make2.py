from make import CSS, FONT, HERE

MORE = """
.desk2{display:grid;grid-template-columns:1fr 380px;height:900px;border-right:1px solid var(--line)}
.wl{background:var(--sand);display:flex;flex-direction:column;overflow:hidden}
.tb{display:flex;justify-content:space-between;align-items:center;padding:20px 28px;background:#fff;border-bottom:1px solid var(--line)}
.tb h1{font-size:24px;font-weight:700;letter-spacing:-.02em}.tb small{display:block;font-size:13px;color:var(--accent);font-weight:600;margin-bottom:4px}
.tabs2{display:flex;gap:20px;padding:12px 28px 0;font-size:14px;font-weight:600;color:var(--ink3)}.tabs2 span{padding-bottom:9px}.tabs2 .on{color:var(--ink);box-shadow:inset 0 -3px 0 var(--marker)}
.paper{margin:16px 28px;background:#fff;border-radius:14px;padding:24px 30px;box-shadow:0 0 0 1px var(--line);flex:1;overflow:hidden}
.paper h2{font-size:26px;font-weight:700;letter-spacing:-.02em;padding-bottom:12px;border-bottom:1px solid var(--line)}
.paper .bar{display:flex;gap:16px;color:var(--ink3);font-weight:700;padding:12px 0;border-bottom:1px solid var(--line);font-size:14px}.paper .bar b{margin-left:auto;background:var(--navy);color:#fff;border-radius:999px;padding:4px 14px;font-size:13px}
.paper p{font-size:15px;line-height:1.6;color:var(--ink2);margin-top:14px}.paper .pic{height:160px;border-radius:12px;background:linear-gradient(135deg,#4a7355,#84a87d);margin-top:14px}
.set{background:#fff;border-left:1px solid var(--line);display:flex;flex-direction:column;overflow:hidden}
.set .in{padding:16px 18px;flex:1;overflow:hidden;display:flex;flex-direction:column;gap:12px}
.set h3{font-size:15px;font-weight:700;display:flex;justify-content:space-between}.set h3 span{font-size:12.5px;font-weight:600;color:var(--ink3)}
.grp{border-radius:14px;box-shadow:0 0 0 1px var(--line),0 1px 2px rgba(24,35,58,.05);padding:14px 14px 12px}
.grp h4{font-size:14px;font-weight:700;margin-bottom:8px}
.fl{font-size:12.5px;font-weight:600;color:var(--ink2);margin:10px 0 5px}.fi{background:var(--field);box-shadow:inset 0 0 0 1px var(--edge);border-radius:12px;padding:10px 12px;font-size:14px}.fi.m{min-height:58px;line-height:1.4}
.hint{font-size:12px;color:var(--ink3);margin-top:4px}
.tg{display:flex;justify-content:space-between;align-items:flex-start;gap:12px;padding:8px 0}.tg b{font-size:14px;font-weight:600;display:block}.tg small{font-size:12.5px;color:var(--ink2);line-height:1.35;display:block;margin-top:2px}
.sw{width:44px;height:26px;border-radius:13px;background:rgba(98,106,121,.4);position:relative;flex:none}.sw:after{content:"";position:absolute;top:3px;left:3px;width:20px;height:20px;border-radius:50%;background:#fff;box-shadow:0 1px 2px rgba(0,0,0,.2)}
.sw.on{background:var(--accent)}.sw.on:after{left:21px}
.sent{background:var(--sand);border-radius:12px;padding:10px 12px;font-size:13px;color:var(--ink2);line-height:1.45}.sent b{color:var(--ink);font-weight:600}
.sent ol{list-style:none;margin-top:6px;border-top:1px solid var(--line);padding-top:6px}.sent li{display:flex;justify-content:space-between;font-variant-numeric:tabular-nums;padding:2px 0}
.qb{display:inline-flex;height:36px;align-items:center;border-radius:20px;padding:0 14px;background:var(--field);box-shadow:inset 0 0 0 1px var(--edge);font-size:13.5px;font-weight:600;margin-top:10px}.qb.on{background:var(--navy);color:#fff;box-shadow:none}
.lock{background:linear-gradient(160deg,#3b4d6e,#22334f);border-radius:16px;padding:14px 12px;margin-top:8px}
.note{background:rgba(255,255,255,.88);border-radius:14px;padding:9px 11px;font-size:12.5px;line-height:1.35;color:#18233a}.note b{display:flex;justify-content:space-between;font-size:12px;font-weight:700}.note b small{font-weight:400;color:#626a79}.note i{font-style:normal;font-weight:600;display:block;margin-top:2px}
.lnk{color:var(--accent);font-weight:600;font-size:13.5px;margin-top:8px;display:inline-block}
.disc{display:flex;justify-content:space-between;font-size:13.5px;font-weight:600;padding:10px 0 2px;border-top:1px solid var(--line);margin-top:6px}
.foot{background:var(--sand);padding:12px 18px;border-top:1px solid var(--line)}.foot p{text-align:right;font-size:12.5px;color:var(--ink2);margin-bottom:8px}
.sb{display:flex;justify-content:space-between}.sb span{height:42px;border-radius:22px;padding:0 18px;display:flex;align-items:center;font-size:14px;font-weight:600;background:#fff;box-shadow:inset 0 0 0 1px var(--edge)}.sb .p{background:var(--navy);color:#fff;box-shadow:none}
.sheet{position:absolute;left:0;right:0;bottom:0;top:70px;background:#fff;border-radius:22px 22px 0 0;box-shadow:0 -10px 30px rgba(0,0,0,.25);padding:10px 20px 20px;overflow:hidden}
.sheet .grab{width:38px;height:5px;border-radius:3px;background:var(--sand2);margin:0 auto 10px}
.sheet .sh{display:flex;justify-content:space-between;align-items:center;margin-bottom:6px}.sheet .sh b{font-size:20px;font-weight:700}.sheet .sh span{color:var(--accent);font-weight:600;font-size:15px}
.dim{position:absolute;inset:0;background:rgba(8,12,22,.45)}
.ins{background:var(--sand);border-radius:14px;padding:4px 14px;margin-top:10px}.ins .tg{border-bottom:1px solid var(--line);padding:12px 0}.ins .tg:last-child{border-bottom:0}
.lbl{font-size:13px;font-weight:700;color:var(--ink3);margin:16px 0 6px}
"""

POST = ('<div class="wl"><div class="tb"><div><small>‹ Church news</small><h1>Change the post</h1></div><span class="ib">⋯</span></div>'
        '<div class="tabs2"><span class="on">Write</span><span>Preview on a phone</span></div>'
        '<div class="paper"><h2>Working bee this Saturday</h2><div class="bar">¶ &nbsp;B &nbsp;I &nbsp;S &nbsp;• &nbsp;1. &nbsp;❝ &nbsp;🔗<b>+ Insert</b></div>'
        '<p>We\'re tidying the kids\' rooms and the play area from <s>8am</s> <u>9am</u> — a later start so families can come after swimming.</p>'
        '<p>Bring gloves if you have them. Morning tea is on us.</p><div class="pic"></div></div></div>')


def desk(telling, save="Save and notify again", line="It's published. Saving changes it for everyone now."):
    return (f'<div class="desk2">{POST}<div class="set"><div class="in"><h3>Who it\'s for and telling them <span>Fold ›</span></h3>'
            '<div class="grp"><h4>Who it\'s for</h4><div class="fi">Everyone</div></div>'
            f'<div class="grp"><h4>Telling people</h4>{telling}</div></div>'
            f'<div class="foot"><p>{line}</p><div class="sb"><span>Cancel</span><span class="p">{save}</span></div></div></div></div>')


def fields(compact=False):
    return ('<div class="fl">What it says</div><div class="fi">Working bee this Saturday — now 9am</div>'
            '<div class="fl">Its line</div><div class="fi m">A later start so families can come after swimming. Bring gloves if you have them.</div>'
            + ('' if compact else '<div class="tg"><div><b>Urgent</b><small>Buzzes phones in quiet hours too</small></div><span class="sw"></span></div>'))


def phone(body):
    return ('<div class="mast" style="padding-top:58px"><h1 style="font-size:24px">Change the post</h1></div>'
            f'<div class="dim"></div><div class="sheet"><div class="grab"></div><div class="sh"><b>Who it\'s for and telling them</b><span>Done</span></div>{body}</div>')


def page(title, d, p):
    return (f'<!doctype html><html lang="en-AU"><head><meta charset="utf-8">{FONT}<title>{title}</title><style>{CSS}{MORE}</style></head>'
            f'<body>{d}<div class="phone"><div class="scr" style="position:relative">{p}</div></div></body></html>')


history = ('<div class="sent"><b>Its notification went today at 3:58pm</b>, to everyone (about 140 phones).'
           '<ol><li><span>Sent again</span><span>Today 3:58pm</span></li><li><span>First sent</span><span>Mon 9:02am</span></li></ol></div>')
a_desk = history + '<span class="qb on">✓ Notify people again when I save</span>' + fields()
a_phone = ('<div class="lbl">Telling people</div>' + history + '<span class="qb on">✓ Notify people again when I save</span>' + fields(compact=True))

b_tell = ('<p class="hint" style="margin:0 0 4px">Its notification went today at 3:58pm (sent twice).</p>'
          '<div class="tg"><div><b>Notify people again</b><small>Everyone it went to gets it again, and it replaces the earlier one in their list</small></div><span class="sw on"></span></div>'
          '<div class="lock"><div class="note"><b>GOOD SHEPHERD <small>now</small></b><i>Working bee this Saturday — now 9am</i>A later start so families can come after swimming…</div></div>'
          '<span class="lnk">Change the words</span>')
c_tell = ('<p class="hint" style="margin:0 0 4px">Its notification went today at 3:58pm (sent twice).</p>'
          '<div class="tg"><div><b>Notify people again</b><small>To everyone, about 140 phones. Off unless you turn it on.</small></div><span class="sw on"></span></div>'
          '<div class="disc"><span>Its words: Working bee this Saturday…</span><span style="color:var(--accent)">Change ⌄</span></div>')

(HERE / "n-a-sent-box.html").write_text(page("Sends listed, then Notify again", desk(a_desk), phone(a_phone)))
(HERE / "n-b-preview.html").write_text(page("A switch and how it will look", desk(b_tell), phone('<div class="lbl">Telling people</div>' + b_tell)))
(HERE / "n-c-folded.html").write_text(page("A switch, the words folded", desk(c_tell), phone('<div class="lbl">Telling people</div>' + c_tell)))
