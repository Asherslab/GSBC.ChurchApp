import base64
import re
from pathlib import Path

HERE = Path(__file__).parent.parent / "church-news"
MOCKS = HERE.parent
ROOT = MOCKS.parent.parent


def _css(path):
    text = path.read_text()

    def embed(m):
        target = (path.parent / m.group(1)).resolve()
        if not target.exists():
            return m.group(0)
        kind = {"woff2": "font/woff2", "svg": "image/svg+xml"}.get(target.suffix[1:], "application/octet-stream")
        return 'url("data:' + kind + ';base64,' + base64.b64encode(target.read_bytes()).decode() + '")'

    text = re.sub(r'url\("(\.\./(?:fonts|img)/[^"]+)"\)', embed, text)
    return re.sub(r'@import url\("([^"]+)"\);', lambda m: _css((path.parent / m.group(1)).resolve()), text)


def _inline():
    css = "".join(_css(f) for f in [
        MOCKS / "design-system" / "kit.css",
        ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/css/rich-text.css",
        ROOT / "src/Features/Ministries/GSBC.ChurchApp.Features.Ministries.Client/wwwroot/css/ministries.css",
        HERE / "news.css"])
    js = (MOCKS / "design-system" / "icons.js").read_text() + "\n" + (MOCKS / "design-system" / "kit.js").read_text()
    return "<style>" + css + "</style><script>" + js + "</script>"


TABS = "Home:home,News:newspaper,Sermons:sermons,Calendar:calendar,More:menu"


def head(title):
    css = _inline().replace("body.sk-phone", ".sk-phone")
    return (f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title>{css}'
            '<style>html,body{margin:0;background:#e4e0d8}body{width:800px;height:844px;overflow:hidden;display:flex;gap:20px}.sk-phone{background:var(--page);flex:none}</style></head>')


def phone(body, tab="News", tabs=TABS, topbar=""):
    return (f'<div class="sk-phone" data-tab="{tab}" data-tabs="{tabs}"><div class="sk-screen"><div class="page-scroll">{topbar}'
            f'<div class="page">{body}</div></div></div>{chrome(tab, tabs)}</div>')


def chrome(on, tabs):
    items = "".join(f'<span class="{"on" if label == on else ""}"><i data-icon="{icon}"></i>{label}</span>' for label, icon in (t.split(":") for t in tabs.split(",")))
    return f'<div class="sk-status"><span>9:41</span><span>\u25cf\u25cf\u25cf \u25ae</span></div><nav class="sk-tabbar">{items}</nav>'
