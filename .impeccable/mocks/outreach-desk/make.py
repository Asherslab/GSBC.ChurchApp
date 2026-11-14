import base64
import re
from pathlib import Path

HERE = Path(__file__).parent
ROOT = HERE.parent.parent.parent
KIT = HERE.parent / "design-system"


def css_of(path):
    text = path.read_text()

    def embed(match):
        target = (path.parent / match.group(1)).resolve()
        if not target.exists():
            return match.group(0)
        kind = {"woff2": "font/woff2", "svg": "image/svg+xml"}.get(target.suffix[1:], "application/octet-stream")
        return 'url("data:' + kind + ';base64,' + base64.b64encode(target.read_bytes()).decode() + '")'

    text = re.sub(r'url\("(\.\./(?:fonts|img)/[^"]+)"\)', embed, text)
    return re.sub(r'@import url\("([^"]+)"\);', lambda match: css_of((path.parent / match.group(1)).resolve()), text)


LOCAL = """
.page-tabs { margin: 0 0 20px; }
.wb { display: grid; grid-template-columns: minmax(0, 1fr) minmax(380px, 440px); gap: 0; border-top: 1px solid var(--line); min-height: 620px; }
.wb__list { padding: 20px 28px 20px 0; }
.wb__detail { padding: 20px 0 20px 28px; border-left: 1px solid var(--line); }
.tbl { width: 100%; border-collapse: collapse; font-size: var(--text-label); }
.tbl th { text-align: left; font-size: var(--text-caption); font-weight: 600; color: var(--ink-3); padding: 0 12px 8px; border-bottom: 1px solid var(--line); }
.tbl td { padding: 14px 12px; border-bottom: 1px solid var(--line); vertical-align: top; }
.tbl td small { display: block; color: var(--ink-3); font-size: var(--text-caption); font-weight: 400; margin-top: 2px; }
.tbl tr.on td { background: var(--selected-fill); }
.tbl .num { text-align: right; font-variant-numeric: tabular-nums; }
.pill { display: inline-flex; align-items: center; height: 22px; padding: 0 9px; border-radius: 999px; background: var(--fill-on-surface); color: var(--ink-2); font-size: var(--text-pill); font-weight: 700; margin-left: 6px; }
.pill--live { background: var(--live); color: var(--white, #fff); }
.ph { display: flex; justify-content: space-between; gap: 12px; margin-bottom: 18px; }
.ph h2 { font-size: var(--text-title); }
.ph p { color: var(--ink-2); font-size: var(--text-caption); margin-top: 2px; }
.form { display: grid; gap: 14px; }
.pair { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.sk-facts { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin: 18px 0; }
.sk-fact { padding: 12px; border-radius: var(--radius-field); background: var(--fill-on-surface); }
.sk-fact b { display: block; font-size: var(--text-title); font-variant-numeric: tabular-nums; }
.sk-fact span { font-size: var(--text-caption); color: var(--ink-2); }
.saverow-wrap { margin-top: 20px; padding-top: 16px; border-top: 1px solid var(--line); }
.saverow { display: flex; justify-content: space-between; align-items: center; margin-top: 20px; padding-top: 16px; border-top: 1px solid var(--line); }
.cols { display: grid; grid-template-columns: 360px minmax(0, 1fr); gap: 28px; }
.cols .grp__sec { margin-bottom: 18px; }
.mini { display: grid; gap: 12px; }
.lst { border-radius: var(--radius-field); }
.lrow { display: flex; align-items: center; gap: 12px; padding: 12px 0; border-bottom: 1px solid var(--line); }
.lrow:last-child { border-bottom: 0; }
.lrow b { display: block; font-size: var(--text-label); }
.lrow small { color: var(--ink-3); font-size: var(--text-caption); }
.lrow .end { margin-left: auto; text-align: right; font-size: var(--text-caption); color: var(--ink-2); font-variant-numeric: tabular-nums; }
.unit { color: var(--ink-3); font-size: var(--text-caption); }
"""

STYLE = "".join(css_of(path) for path in [KIT / "kit.css", HERE.parent / "design-system" / "parts.css"]) + LOCAL
SCRIPT = (KIT / "icons.js").read_text() + "\nwindow.SK_NAV=[[\"Sundays & sermons\",[[\"Sermons\",\"sermons\"]]],[\"Calendar & news\",[[\"Calendar\",\"calendar\"]]],[\"Ministries & groups\",[[\"Life Groups\",\"book-open\"],[\"Ministries\",\"users\"],[\"Outreach\",\"footprints\"],[\"Prayer\",\"prayer\"]]],[\"The app\",[[\"Media\",\"image\"]]]];\n" + (KIT / "kit.js").read_text()


def page(title, body, note):
    return f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>
<style>{STYLE}</style><script>{SCRIPT}</script></head>
<body class="sk-admin" data-current="Outreach" style="height:900px;overflow:hidden">
<main><header class="title-band"><div class="title-band__text"><h1 class="title-band__title">Outreach</h1><p class="title-band__lead">Campaigns, the settings walks run on, and what walkers send in.</p></div>
<div class="title-band__actions"><button class="btn btn--primary"><i data-icon="plus"></i>New campaign</button></div></header>
{body}</main><p class="sk-note">{note}</p></body></html>"""


def tabs(on, names=("Campaigns", "Follow-ups", "Reports", "Settings")):
    counts = {"Follow-ups": "2", "Reports": "1"}
    items = "".join(
        f'<button class="tab-strip__tab" role="tab" aria-selected="{str(name == on).lower()}">{name}{f"<span class=pill>{counts[name]}</span>" if name in counts else ""}</button>'
        for name in names)
    return f'<div class="tab-strip page-tabs" role="tablist">{items}</div>'


CAMPAIGNS = [
    ("General outreach", "Always on", "Tracts", "1,284", False),
    ("Carols 2026", "1 Dec to 24 Dec", "Carols invitations", "0", True),
    ("Gympie mission", "6 Mar to 8 Mar 2027", "Tracts · Easter invitations", "0", False),
]


def table(with_counts=True):
    head = '<tr><th>Campaign</th><th>When</th><th>What\'s handed out</th>' + ('<th class="num">Letterboxes this month</th>' if with_counts else '') + '</tr>'
    rows = "".join(
        f'<tr class="{"on" if on else ""}"><td><b>{name}</b>{"<span class=pill>Always there</span>" if when == "Always on" else ""}</td><td>{when}</td><td>{out}</td>'
        + (f'<td class="num">{boxes}</td>' if with_counts else '') + '</tr>'
        for name, when, out, boxes, on in CAMPAIGNS)
    return f'<table class="tbl">{head}{rows}</table>'


def campaign_record(totals=True):
    facts = """<div class="sk-facts"><div class="sk-fact"><b>0</b><span>letterboxes so far</span></div><div class="sk-fact"><b>0 km</b><span>walked</span></div><div class="sk-fact"><b>2</b><span>outreach days</span></div></div>""" if totals else ""
    return f"""<div class="ph"><div><h2>Carols 2026</h2><p>Its own coverage: General outreach's streets don't count here.</p></div><button class="btn btn--quiet btn--icon" aria-label="More"><i data-icon="ellipsis"></i></button></div>
{facts}<div class="form">
<label class="field"><span class="field__label">Name</span><input class="field__input" value="Carols 2026"></label>
<label class="field"><span class="field__label">What's handed out</span><input class="field__input" value="Carols invitations"></label>
<div class="ui-pair ui-pair--wide"><label class="field"><span class="field__label">Starts</span><input class="field__input" value="1 Dec 2026"></label><label class="field"><span class="field__label">Ends</span><input class="field__input" value="24 Dec 2026"></label></div>
<div class="field"><span class="field__label">Where</span><div class="segmented" role="radiogroup"><button class="segmented__option" role="radio" aria-checked="true">The whole church area</button><button class="segmented__option" role="radio" aria-checked="false">An area I draw</button></div></div>
<label class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">Show a leaderboard</span><span class="toggle-row__description">Walkers counted by name see who's done the most. Anyone who opted out is counted without a name.</span></span><span class="switch"><input type="checkbox" checked><span></span></span></label>
</div>
<div class="saverow-wrap">{save("Save the campaign")}</div>"""


def number(label, value, unit):
    hint = f'<span class="field__hint">{unit}</span>' if unit else ""
    return f'<label class="field"><span class="field__label">{label}</span><input class="field__input" value="{value}">{hint}</label>'


SETTINGS_GROUPS = [
    ("Rest period", "How long a street stays done before walks send people there again.",
     f'<div class="ui-pair ui-pair--wide">{number("Rest period", "3 months", "91 days")}{number("No junk mail kept for", "2 years", "unless someone marks it again")}</div>'),
    ("How walks are worked out", "The pace and times every route is planned with.",
     f'<div class="ui-pair ui-pair--wide">{number("Walking pace", "4 km/h", "")}{number("Bundle size", "50", "letterboxes")}</div><div class="ui-pair ui-pair--wide">{number("A house letterbox", "8 s", "")}{number("A unit letterbox", "3 s", "")}</div><div class="ui-pair ui-pair--wide">{number("A door", "90 s", "doorknocking")}{number("A unit door", "60 s", "doorknocking")}</div>'),
    ("Daylight and safety", "Walks are offered only in these hours; doorknocking asks for a pair.",
     f'<div class="ui-pair ui-pair--wide">{number("From", "7:00am", "")}{number("Until", "6:00pm", "")}</div><label class="toggle-row"><span class="toggle-row__body"><span class="toggle-row__title">Doorknocking in pairs</span></span><span class="switch"><input type="checkbox" checked><span></span></span></label>'),
]


def save(label="Save"):
    return '<div class="save-bar"><div class="save-bar__row"><button class="btn btn--quiet">Cancel</button><span class="save-bar__gap"></span><button class="btn btn--primary save-bar__save">{label}</button></div></div>'.format(label=label)


def group(title, lead, inner, foot=True):
    footer = f'<div class="section__foot">{save()}</div>' if foot else ""
    return f'<section class="section section--grouped"><div class="section__head"><h2 class="section__title">{title}</h2></div><p class="section__lead">{lead}</p><div class="section__group"><div class="stack">{inner}</div></div>{footer}</section>'


def a_settings_beside():
    settings = "".join(group(title, lead, inner, foot=False) for title, lead, inner in SETTINGS_GROUPS[:2])
    settings += save("Save the settings")
    body = tabs("Campaigns and settings", ("Campaigns and settings", "Follow-ups", "Reports")) + f"""<div class="cols"><div class="mini">{settings}</div>
<div class="wb" style="grid-template-columns:minmax(0,1fr) 380px"><div class="wb__list">{table(with_counts=False)}</div><div class="wb__detail">{campaign_record(totals=False)}</div></div></div>"""
    return page("Outreach: settings beside campaigns", body, "Sketch A · Settings stay in view on the left; the campaigns workbench beside them.")


def b_settings_first():
    rows = "".join(f'<div class="lrow"><span class="row__icon"><i data-icon="footprints"></i></span><div><b>{name}</b><small>{when} · {out}</small></div><span class="end">{boxes} letterboxes this month<br></span><i data-icon="chevron-right"></i></div>'
                   for name, when, out, boxes, _ in CAMPAIGNS)
    campaigns = group("Campaigns", "What&rsquo;s handed out and where, each with its own coverage.", f'<div class="lst">{rows}</div><button class="btn btn--quiet"><i data-icon="plus"></i>Add a campaign</button>', foot=False)
    body = tabs("Settings") + '<div class="mini">' + campaigns + "".join(group(title, lead, inner) for title, lead, inner in SETTINGS_GROUPS[:2]) + "</div>"
    return page("Outreach: one page of settings", body, "Sketch B · Settings as full-width cards, campaigns a card among them.")


def c_campaign_records():
    body = tabs("Campaigns") + f"""<div class="wb"><div class="wb__list">{table()}</div><div class="wb__detail">{campaign_record()}</div></div>"""
    return page("Outreach: campaigns workbench", body, "Sketch C · Campaigns are the workbench; Settings, Follow-ups and Reports are tabs.")


for name, make in [("a-settings-beside", a_settings_beside), ("b-settings-first", b_settings_first), ("c-campaign-records", c_campaign_records)]:
    (HERE / f"{name}.html").write_text(make())
