from make import CSS, SIDE, ADD, HERE

EXTRA = """.mk{color:var(--ink3);font-weight:400}.mk.f{opacity:.45}
.md{font-family:Montserrat,sans-serif;font-size:15.5px;line-height:1.75;color:var(--ink);white-space:pre-wrap}
.colhead{display:flex;justify-content:space-between;align-items:center}
.pin{width:32px;height:32px;border-radius:9px;display:grid;place-items:center;color:var(--ink2);border:1px solid var(--line)}
.hint{font-size:12.5px;color:var(--ink3);margin-top:6px}.bar b.on{background:var(--nav);color:#fff;border-color:var(--nav)}
.tip{position:absolute;margin-top:-34px;margin-left:2px;background:var(--ink);color:#fff;font-size:11.5px;font-weight:600;border-radius:7px;padding:3px 8px}
"""
BAR = '<div class="bar"><b>B</b><b><i>I</i></b><b>☰</b><b>1.</b><b>“</b><b>⌁</b></div>'
GAL = '<div class="blk"><span class="hdl">⋮⋮</span><div><p class="lbl">▣ Gallery · 3 pictures</p><div class="gal" style="margin-top:8px"><i></i><i></i><i></i><em>+</em></div><p class="cap">Carols on the lawn last year</p></div><span class="more">⋯</span></div>'
HEAD = '<div class="blk"><span class="hdl">⋮⋮</span><div class="h2">Helping on the day</div><span class="more">⋯</span></div>'

WYSIWYG = ('<div class="txt"><p>Christmas is a few weeks away, and there\'s a place for everyone at every service. <strong>Bring a friend</strong>: invitation cards are at the <a>Welcome desk</a>.</p>'
           '<ul><li><strong>Carols on the lawn</strong>, Saturday 20 December, 6pm.</li><li><strong>Christmas Eve</strong>, 7pm, with the kids\' nativity.</li><li><strong>Christmas Day</strong>, 9am, one service only.<span class="caret"></span></li></ul></div>')
MARKDOWN = ('<div class="md">Christmas is a few weeks away, and there\'s a place for everyone at every service. **Bring a friend**: invitation cards are at the [Welcome desk](/places/welcome).\n\n'
            '- **Carols on the lawn**, Saturday 20 December, 6pm.\n- **Christmas Eve**, 7pm, with the kids\' nativity.\n- **Christmas Day**, 9am, one service only.<span class="caret"></span></div>'
            '<p class="hint">Preview on a phone shows how it looks.</p>')
LIVE = ('<div class="txt"><p>Christmas is a few weeks away, and there\'s a place for everyone at every service. <strong>Bring a friend</strong>: invitation cards are at the <a>Welcome desk</a>.</p>'
        '<ul><li><strong>Carols on the lawn</strong>, Saturday 20 December, 6pm.</li><li><strong>Christmas Eve</strong>, 7pm, with the kids\' nativity.</li>'
        '<li><span class="mk f">**</span><strong>Christmas Day</strong><span class="mk f">**</span>, 9am, one service only.<span class="caret"></span></li></ul></div>'
        '<p class="hint">Bold, lists, quotes and links are in the bar, or ⌘B, ⌘I and ⌘K. Pasting from Word or Google Docs keeps them. Typing - or # starts a list or heading, for those who know.</p>')

SETTINGS = ('<div class="grp"><h3>Who it\'s for</h3><div class="seg"><span class="on">Everyone</span><span>Signed in</span><span>Some roles</span></div><p class="note">Anyone with the app, signed in or not, sees it in News.</p></div>'
            '<div class="grp"><div class="line">Send a notification<span class="sw"></span></div><div class="fld"><small>What it says</small>Christmas at Good Shepherd</div><div class="line">Urgent<span class="sw off"></span></div></div>'
            '<div class="grp"><h3>When</h3><div class="seg"><span class="on">Now</span><span>Later</span></div></div>')


def page(title, text, bar=BAR):
    blocks = f'<div class="blk on" style="background:var(--field)"><span class="hdl">⋮⋮</span><div>{bar}{text}</div><span class="more">⋯</span></div>' + GAL + HEAD
    work = ('<div class="band"><div><p class="crumb">‹ Church news</p><h1>New post</h1></div><span class="ib">⋯</span></div>'
            '<div style="flex:1;overflow:hidden;display:grid;grid-template-columns:1fr 340px;background:var(--sand)">'
            '<div style="overflow:hidden;padding:20px 40px 0"><div style="max-width:760px;margin:0 auto;background:var(--page);border-radius:14px 14px 0 0;box-shadow:0 1px 0 var(--line),0 8px 28px rgba(42,61,94,.08);padding:4px 36px 28px;height:100%">'
            '<div class="tabs"><span class="on">Write</span><span>Preview on a phone</span></div><div style="display:flex;flex-direction:column;gap:12px;padding-top:14px"><div class="title" style="border:0">Christmas at Good Shepherd: every service</div>'
            + blocks + ADD + '</div></div></div>'
            '<aside style="background:var(--page);border-left:1px solid var(--line);display:flex;flex-direction:column;overflow:hidden"><div style="padding:18px 20px 16px;display:flex;flex-direction:column;gap:12px;flex:1;overflow:hidden">'
            '<div class="colhead"><p class="lbl" style="font-size:13.5px">Who it\'s for and telling them</p><span class="pin" title="Hide the settings">⇥</span></div>' + SETTINGS + '</div>'
            '<div style="border-top:1px solid var(--line);padding:12px 20px;display:flex;justify-content:space-between;align-items:center"><span class="btn q">Cancel</span><span class="btn p">Post for everyone</span></div></aside></div>')
    return f'<!doctype html><html lang="en-AU"><head><meta charset="utf-8"><link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700&display=swap" rel="stylesheet"><title>{title}</title><style>{CSS}{EXTRA}</style></head><body>{SIDE}<main class="work">{page_body(work)}</main></body></html>'


def page_body(work):
    return work


for name, title, text in [("t-a-formatted.html", "Formats as you type", WYSIWYG),
                          ("t-b-markdown.html", "A Markdown editor", MARKDOWN),
                          ("t-c-live.html", "Live Markdown", LIVE)]:
    bar = BAR.replace("<b>B</b>", '<b class="on" title="Bold is on here (⌘B)">B</b>') if name.startswith("t-c") else BAR
    (HERE / name).write_text(page(title, text, bar))
