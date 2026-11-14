import base64, sys
from pathlib import Path

SHOTS = Path(sys.argv[1])
HERE = Path(__file__).parent
LOOKS = {
    "pockets": ("What bounds a walk", "Parks in sage, schools and shops in a warm beige, main roads with a firm brown edge: the things that end a walk's pocket are visible, so “streets near you run out at 4.8 km” has its reason on the map."),
    "sand": ("One calm sand page", "Everything is the bulletin's sand with white streets: no park or land-use colour, no buildings. The quietest base; the route and coverage are the only colour. Prints cleanly on an A4 handout."),
    "homes": ("The houses themselves", "Buildings in a warm beige over a near-white ground, so you see where letterboxes are. Here most houses aren't mapped as buildings in OpenStreetMap, so blocks of homes look empty."),
}
PANELS = [("light", "16.3", "Walking, light"), ("dark", "16.3", "Walking, dark"), ("light", "14.3", "Planning, light"), ("dark", "14.3", "Planning, dark")]

def pic(look, scheme, zoom):
    data = base64.b64encode((SHOTS / f"map-{look}-{scheme}-z{zoom}.webp").read_bytes()).decode()
    return f"data:image/webp;base64,{data}"

for look, (title, line) in LOOKS.items():
    panels = "".join(
        f'<figure><img src="{pic(look, s, z)}" alt="{c}"><figcaption>{c}</figcaption></figure>' for s, z, c in PANELS)
    html = f"""<!doctype html><html><head><meta charset="utf-8">
<link rel="preconnect" href="https://fonts.googleapis.com"><link href="https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700&display=swap" rel="stylesheet">
<style>
*{{box-sizing:border-box}}html,body{{margin:0;width:1440px;height:900px;overflow:hidden}}
body{{font-family:Montserrat,system-ui,sans-serif;background:#f4efe7;color:#18233a}}
header{{background:#2a3d5e;color:#fff;padding:28px 64px 24px}}
header p{{margin:0 0 6px;font-size:12px;font-weight:600;letter-spacing:.12em;text-transform:uppercase;color:#d5c9b7}}
header h1{{margin:0;font-size:30px;font-weight:700;letter-spacing:-.01em}}
.lead{{max-width:1040px;margin:20px auto 0;font-size:15px;line-height:1.55;color:#545e70}}
.row{{display:flex;gap:24px;justify-content:center;margin-top:20px}}
figure{{margin:0;width:260px}}
img{{display:block;width:260px;height:400px;border-radius:14px;box-shadow:0 1px 2px rgb(42 61 94/.12),0 6px 18px rgb(42 61 94/.10)}}
figcaption{{margin-top:10px;font-size:13px;font-weight:600;color:#545e70}}
.key{{display:flex;gap:28px;justify-content:center;margin-top:22px;font-size:13px;color:#545e70}}
.key span{{display:flex;align-items:center;gap:8px}}
.swatch{{display:inline-block;width:28px;height:6px;border-radius:3px}}
.dot{{display:inline-block;width:14px;height:14px;border-radius:50%;background:#e7ae74;border:1px solid #8a5422;box-shadow:0 0 0 3px #fff}}
</style></head><body>
<header><p>Outreach map · the same Albany Creek streets in each look</p><h1>{title}</h1></header>
<p class="lead">{line}</p>
<div class="row">{panels}</div>
<div class="key"><span><i class="swatch" style="background:#00776d"></i>Your walk</span><span><i class="swatch" style="background:#626a79;opacity:.45"></i>Walked recently</span><span><i class="dot"></i>You</span><span>Streets and addresses © OpenStreetMap contributors, State of Queensland</span></div>
</body></html>"""
    (HERE / f"{look}.html").write_text(html)
    print(look, len(html) // 1024, "KB")
