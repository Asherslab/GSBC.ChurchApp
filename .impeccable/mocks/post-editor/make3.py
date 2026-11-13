import sys
from pathlib import Path

HERE = Path(__file__).parent
sys.path.insert(0, str(HERE.parent / "church-news"))
from make import phone, pic

TOP = '<header class="topbar topbar--solid"><div class="topbar__start"><button class="topbar__text-btn">Cancel</button></div><span class="topbar__title">New post</span><div class="topbar__end"></div></header>'
STYLE = """<style>
.pe{padding:12px 16px 0;display:grid;gap:10px}
.pe-title{font-size:21px;font-weight:700;line-height:1.25;letter-spacing:-.01em;padding:4px 0 8px;border-bottom:1px solid var(--line)}
.pe-text{font-size:16px;line-height:1.55;color:var(--ink)}.pe-text p{margin:0 0 10px}.pe-text ul{margin:0;padding-left:22px}.pe-text a{color:var(--accent);text-decoration:underline;text-underline-offset:3px}
.pe-md{font-size:16px;line-height:1.6;white-space:pre-wrap;color:var(--ink)}.pe-mk{color:var(--ink-3)}.pe-faint{color:var(--ink-3);opacity:.5}
.pe-caret{display:inline-block;width:2px;height:19px;background:var(--accent);vertical-align:-4px;margin-left:1px}
.pe-thumbs{display:grid;grid-template-columns:repeat(4,1fr);gap:6px}.pe-thumbs>span{aspect-ratio:1;border-radius:9px;overflow:hidden;display:block}.pe-thumbs .sk-pic{width:100%;height:100%}
.pe-label{font-size:13px;font-weight:600;color:var(--ink-2);display:flex;gap:6px;align-items:center}
.pe-dock{position:absolute;left:0;right:0;bottom:0;z-index:20}
.pe-bar{display:flex;gap:4px;align-items:center;padding:6px 8px;background:var(--surface);border-top:1px solid var(--line)}
.pe-bar b{width:44px;height:40px;border-radius:10px;display:grid;place-items:center;color:var(--ink-2);font-size:16px;font-weight:700}
.pe-bar b.on{background:var(--primary);color:var(--on-primary)}.pe-bar .gap{flex:1}
.pe-bar .pe-add{width:auto;padding:0 12px;gap:6px;display:inline-flex;background:var(--field);font-size:14px;font-weight:600;border-radius:20px}
.pe-kb{height:291px;background:#d1d4db;padding:8px 3px 0;display:grid;grid-template-rows:repeat(4,42px);gap:11px}
.pe-row{display:flex;gap:6px;justify-content:center}.pe-row i{display:block;width:32px;height:42px;border-radius:5px;background:#fff;box-shadow:0 1px 0 rgba(0,0,0,.3)}.pe-row i.w{width:42px;background:#abb0ba}.pe-row i.sp{width:180px}
.pe-hint{font-size:12.5px;color:var(--ink-3);line-height:1.4}
</style>"""
KB = '<div class="pe-kb">' + "".join('<div class="pe-row">' + "".join(f'<i class="{c}"></i>' for c in row) + "</div>" for row in [[""] * 10, [""] * 9, ["w"] + [""] * 7 + ["w"], ["w", "w", "sp", "w"]]) + "</div>"


def bar(bold_on=False, marks=False):
    keys = ["B", "<em>I</em>", '<i data-icon="list"></i>', "1.", "“", '<i data-icon="link"></i>'] if not marks else ["**", "*", "-", "1.", "&gt;", "[ ]"]
    cells = "".join(f'<b class="{"on" if bold_on and i == 0 else ""}">{k}</b>' for i, k in enumerate(keys))
    return f'<div class="pe-dock"><div class="pe-bar">{cells}<span class="gap"></span><b class="pe-add"><i data-icon="plus"></i>Add</b></div>{KB}</div>'


GALLERY = f'<div><p class="pe-label"><i data-icon="images"></i>Gallery · 3 pictures</p><div class="pe-thumbs" style="margin-top:8px"><span>{pic("a")}</span><span>{pic("b")}</span><span>{pic("c")}</span></div></div>'
TITLE = '<div class="pe-title">Christmas at Good Shepherd</div>'

A = ('<div class="pe-text"><p>There\'s a place for everyone at every service. <strong>Bring a friend</strong>: invitation cards are at the <a>Welcome desk</a>.</p>'
     '<ul><li><strong>Carols on the lawn</strong>, Saturday 20 December, 6pm</li><li><strong>Christmas Eve</strong>, 7pm</li><li><strong>Christmas Day</strong>, 9am<span class="pe-caret"></span></li></ul></div>')
B = ('<div class="pe-md">There\'s a place for everyone at every service. <span class="pe-mk">**</span>Bring a friend<span class="pe-mk">**</span>: invitation cards are at the <span class="pe-mk">[</span>Welcome desk<span class="pe-mk">](/places/welcome)</span>.\n\n'
     '<span class="pe-mk">-</span> <span class="pe-mk">**</span>Carols on the lawn<span class="pe-mk">**</span>, Saturday 20 December, 6pm\n<span class="pe-mk">-</span> <span class="pe-mk">**</span>Christmas Eve<span class="pe-mk">**</span>, 7pm\n<span class="pe-mk">-</span> <span class="pe-mk">**</span>Christmas Day<span class="pe-mk">**</span>, 9am<span class="pe-caret"></span></div>')
C = ('<div class="pe-text"><p>There\'s a place for everyone at every service. <strong>Bring a friend</strong>: invitation cards are at the <a>Welcome desk</a>.</p>'
     '<ul><li><strong>Carols on the lawn</strong>, Saturday 20 December, 6pm</li><li><strong>Christmas Eve</strong>, 7pm</li><li><span class="pe-faint">**</span><strong>Christmas Day<span class="pe-caret"></span></strong><span class="pe-faint">**</span>, 9am</li></ul></div>')


def screen(title, text, **bar_args):
    body = STYLE + f'<div class="pe">{TITLE}{text}{GALLERY}</div>' + bar(**bar_args)
    return phone(title, body, topbar=TOP, tabs="")


for name, title, text, args in [("t-a-formatted-phone.html", "Formats as you type, on the phone", A, {}),
                                ("t-b-markdown-phone.html", "A Markdown editor, on the phone", B, {"marks": True}),
                                ("t-c-live-phone.html", "Live Markdown, on the phone", C, {"bold_on": True})]:
    (HERE / name).write_text(screen(title, text, **args))
