from make import phone, pic, write

BACK = '<header class="topbar topbar--solid"><div class="topbar__start"><button class="icon-btn icon-btn--back"><i data-icon="chevron-left" data-size="md"></i>News</button></div><span class="topbar__title"></span><div class="topbar__end"><button class="icon-btn" aria-label="Share"><i data-icon="share" data-size="md"></i></button></div></header>'
CLEAR = BACK.replace("topbar topbar--solid", "topbar sk-over")

META = '<p class="sk-post-meta"><span class="sk-from"><i data-icon="church"></i>Church office</span><span>Thursday 13 November</span></p>'

BODY = """<div class="rich-text sk-post-body">
<p>Christmas is close. Here's every service and gathering, so you can plan and bring someone along.</p>
<h2>Carols on the lawn</h2>
<p>Saturday 20 December from 6pm. Bring a picnic rug and a torch; the kids' choir starts at 7.</p>
<div class="rich-text-gallery" data-count="3">
<figure class="rich-text-gallery__item">{g1}<figcaption>Last year's carols</figcaption></figure>
<figure class="rich-text-gallery__item">{g2}</figure>
<figure class="rich-text-gallery__item">{g3}</figure>
</div>
<h2>Christmas Eve and Day</h2>
<ul><li><strong>Christmas Eve</strong>, 7pm: candles and carols</li><li><strong>Christmas Day</strong>, 9am: one family service</li></ul>
<blockquote><p>For to us a child is born, to us a son is given.</p></blockquote>
<figure class="rich-text-figure rich-text-figure--embed"><div class="video-embed"><span class="video-embed__poster"><span class="video-embed__play"><i data-icon="play" data-size="md"></i></span></span></div><figcaption>A message from our pastor</figcaption></figure>
<figure class="rich-text-figure"><a class="rich-text-file"><span class="rich-text-file__icon"><i data-icon="file"></i></span><span class="rich-text-file__text"><span class="rich-text-file__name">Christmas service times</span><span class="rich-text-file__weight">PDF · 240 KB</span></span><i data-icon="download"></i></a></figure>
</div>
<div class="sk-post-link"><button class="btn btn--primary btn--block"><i data-icon="calendar"></i>See it in the calendar</button></div>"""


def body(small=True):
    g = lambda t: f'<span class="sk-g">{pic(t)}</span>'
    return BODY.format(g1=g("a"), g2=g("b"), g3=g("c"))


TITLE = "Christmas at Good Shepherd: every service, and what's on"

strip = f"""<div class="sk-strip">{pic("a")}<span class="sk-strip__count">1 of 5</span></div>
<div class="sk-strip-dots"><span class="on"></span><span></span><span></span><span></span><span></span></div>"""
r2a = f"""<div class="sk-post">{strip}<h1 class="sk-post-title">{TITLE}</h1>{META}{body()}</div>"""
write("p-a-strip.html", phone("The post's pictures lead as a strip", r2a, topbar=BACK, tab="News"))

cover = f"""<div class="sk-cover">{pic("a")}<div class="sk-cover__text"><h1>{TITLE}</h1><p><span>Church office</span><span>Thursday 13 November</span></p></div></div>"""
r2b = CLEAR + f"""<div class="sk-post sk-post--cover">{body()}</div>"""
write("p-b-cover.html", phone("The picture as its cover", cover + f'<div class="sk-post sk-post--cover">{body()}</div>', topbar=CLEAR, tab="News"))

r2c = f"""<div class="sk-post"><div class="sk-post-pic">{pic("a")}</div><h1 class="sk-post-title">{TITLE}</h1>{META}{body()}</div>"""
write("p-c-article.html", phone("An article, picture first", r2c, topbar=BACK, tab="News"))

EDIT_TOP = '<header class="topbar topbar--solid"><div class="topbar__start"><button class="topbar__text-btn">Cancel</button></div><span class="topbar__title">New post</span><div class="topbar__end"></div></header>'
TOOLS = '<div class="rich-text-toolbar sk-tools"><button>B</button><button><em>I</em></button><button>H</button><button>H<small>2</small></button><button><i data-icon="list"></i></button><button>“”</button><button><i data-icon="link"></i></button><button class="sk-add"><i data-icon="plus"></i>Add</button></div>'
TEXT = """<div class="sk-area">Christmas is close. Here's every service and gathering, so you can plan and bring someone along.

<span class="sk-md">##</span> Carols on the lawn
Saturday 20 December from 6pm. Bring a picnic rug…

<span class="sk-md">![Last year's carols](picture)</span></div>"""
WHO = """<p class="grp-head">Who it's for</p><section class="grp"><div class="sk-choice"><b>Everyone</b><span>Anyone with the app, signed in or not</span><i data-icon="chevron-right"></i></div></section>"""
TELL = """<p class="grp-head">Tell people</p><section class="grp"><div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">Send a notification</span><span class="toggle-row__description">About 410 phones. It opens the post.</span></span><label class="switch"><input type="checkbox" checked><span></span></label></div><label class="field"><span class="field__label">What it says</span><input class="field__input" value="Christmas at Good Shepherd: every service"></label><div class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">Urgent</span><span class="toggle-row__description">Buzzes phones in quiet hours too</span></span><label class="switch"><input type="checkbox"><span></span></label></div></section>"""
WHEN = """<p class="grp-head">When</p><section class="grp"><div class="segmented" role="radiogroup"><button class="segmented__option" role="radio" aria-checked="true">Now</button><button class="segmented__option" role="radio" aria-checked="false">Later</button></div></section>"""
SAVE = '<div class="sk-savebar sk-foot"><button class="btn btn--quiet">Cancel</button><button class="btn btn--primary">Post for everyone</button></div>'

e_a = '<div class="sk-grouped">' + WHO + TELL + WHEN + f"""<p class="grp-head">The post</p><section class="grp"><label class="field"><span class="field__label">Title</span><input class="field__input" value="Christmas at Good Shepherd"></label>{TOOLS}{TEXT}</section>""" + SAVE + "</div>"
write("e-a-settings-first.html", phone("Who, when and telling first, then the words", e_a, topbar=EDIT_TOP, tab="News", scroll=0))

BLOCKS = f"""<div class="sk-blocks">
<div class="sk-block"><span class="sk-block__grip"><i data-icon="grip"></i></span><div class="sk-block__body"><p>Christmas is close. Here's every service and gathering, so you can plan and bring someone along.</p></div></div>
<div class="sk-block"><span class="sk-block__grip"><i data-icon="grip"></i></span><div class="sk-block__body"><p class="sk-block__h">Carols on the lawn</p></div></div>
<div class="sk-block sk-block--on"><span class="sk-block__grip"><i data-icon="grip"></i></span><div class="sk-block__body"><div class="sk-block__label"><i data-icon="images"></i>Gallery · 3 pictures</div><div class="sk-thumbs"><span>{pic("a")}</span><span>{pic("b")}</span><span>{pic("c")}</span><span class="sk-thumbs__add"><i data-icon="plus"></i></span></div><input class="field__input sk-caption" value="Last year's carols"></div><button class="icon-btn"><i data-icon="more"></i></button></div>
<div class="sk-block"><span class="sk-block__grip"><i data-icon="grip"></i></span><div class="sk-block__body"><div class="sk-block__label"><i data-icon="video"></i>Video from YouTube</div><p class="sk-block__sub">A message from our pastor</p></div></div>
</div>
<div class="sk-addbar"><span>Add</span><button><i data-icon="notebook-pen"></i>Text</button><button><i data-icon="image"></i>Picture</button><button><i data-icon="video"></i>Video</button><button><i data-icon="mic"></i>Audio</button><button><i data-icon="file"></i>File</button></div>"""
e_b = f"""<div class="page sk-editpage"><label class="field"><span class="field__label">Title</span><input class="field__input" value="Christmas at Good Shepherd"></label>{BLOCKS}<div class="sk-settings-row"><i data-icon="users"></i><span><b>Everyone</b> · Notification on · Now</span><i data-icon="chevron-right"></i></div>{SAVE}</div>"""
write("e-b-blocks.html", phone("The post as blocks you add and move", e_b, topbar=EDIT_TOP, tab="News"))

NEXT = EDIT_TOP.replace('<div class="topbar__end"></div>', '<div class="topbar__end"><button class="topbar__text-btn">Next</button></div>')
e_c1 = f"""<div class="page sk-editpage"><p class="sk-step">Step 1 of 2 · Write it</p><label class="field"><span class="field__label">Title</span><input class="field__input" value="Christmas at Good Shepherd"></label><div class="segmented" role="radiogroup"><button class="segmented__option" role="radio" aria-checked="true">Write</button><button class="segmented__option" role="radio" aria-checked="false">See it</button></div>{TOOLS}{TEXT}</div>"""
write("e-c-two-steps.html", phone("Two steps: write it, then send it", e_c1, topbar=NEXT, tab="News"))
