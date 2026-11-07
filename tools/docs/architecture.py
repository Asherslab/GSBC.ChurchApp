#!/usr/bin/env python3
import pathlib
import sys
from xml.sax.saxutils import escape

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "docs/architecture/architecture.svg"
PREFIX = "GSBC.ChurchApp."

NAVY, AQUA, BEIGE, ORANGE = "#2A3D5E", "#009C8F", "#D5C9B7", "#E7AE74"
PANEL, INK, MUTED = "#F7F5F1", "#1F2A3C", "#5B6577"
APP_FILL, SERVER_FILL, DEV_FILL = "#DDF2EF", "#E4E9F1", "#FBEBDD"
FONT = "Montserrat, -apple-system, 'Segoe UI', Helvetica, Arial, sans-serif"

WIDTH = 1240
HOSTS = {
    "Hosts.App": "Blazor WASM, AOT",
    "Hosts.Admin": "Blazor WASM, staff",
    "Hosts.AppHost": "Aspire: runs it all locally",
    "Hosts.Gateway": "YARP · passkeys · serves admin",
    "Hosts.Api": "gRPC services · Sync · tiers",
    "Hosts.Migrator": "applies every feature's migrations",
}
APP_PLATFORM = {"Platform.Common", "Platform.Permissions", "Platform.Contracts", "Platform.Client", "Platform.Ui"}
APP_PARTS = (".Contracts", ".Client", ".App", ".Admin")
PART_ORDER = ["Contracts", "Data", "Api", "Gateway", "Generator", "Client", "App", "Admin"]
PLATFORM_ORDER = ["Common", "Permissions", "Contracts", "Client", "Ui", "Api", "Data", "Storage"]


def projects():
    names = []
    for folder in ("src", "tests"):
        for path in (ROOT / folder).rglob("*.csproj"):
            if "node_modules" not in path.parts:
                names.append(path.stem.removeprefix(PREFIX))
    return sorted(names)


def is_test(name):
    return name.endswith(".Tests") or name == "Testing"


def kind(name):
    name = name.removeprefix(PREFIX)
    if name in HOSTS:
        return "dev" if name == "Hosts.AppHost" else ("app" if name in ("Hosts.App", "Hosts.Admin") else "server")
    if is_test(name):
        return "test"
    if name in APP_PLATFORM or (name.startswith("Features.") and name.endswith(APP_PARTS)):
        return "app"
    return "server"


FILL = {"app": APP_FILL, "server": SERVER_FILL, "dev": DEV_FILL, "test": "#FFFFFF"}
STROKE = {"app": AQUA, "server": NAVY, "dev": ORANGE, "test": BEIGE}


class Svg:
    def __init__(self):
        self.parts = []

    def add(self, markup):
        self.parts.append(markup)

    def text(self, x, y, value, size=13, weight=400, fill=INK, anchor="start", italic=False):
        style = ' font-style="italic"' if italic else ""
        self.add(f'<text x="{x}" y="{y}" font-size="{size}" font-weight="{weight}" fill="{fill}" text-anchor="{anchor}"{style}>{escape(value)}</text>')

    def panel(self, x, y, w, h, title, dashed=False, title_at_bottom=False):
        dash = ' stroke-dasharray="6 4"' if dashed else ""
        self.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="14" fill="{PANEL}" stroke="{BEIGE}" stroke-width="1.5"{dash}/>')
        self.text(x + 16, y + h - 16 if title_at_bottom else y + 26, title, size=14, weight=700, fill=NAVY)

    def box(self, x, y, w, h, title, subtitle, k, project=None):
        data = f' data-project="{escape(project)}"' if project else ""
        self.add(f'<g{data}><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="10" fill="{FILL[k]}" stroke="{STROKE[k]}" stroke-width="1.5"/>')
        self.text(x + w / 2, y + h / 2 - 3, title, size=14, weight=700, anchor="middle")
        if subtitle:
            self.text(x + w / 2, y + h / 2 + 15, subtitle, size=11, fill=MUTED, anchor="middle")
        self.add("</g>")

    def chip(self, x, y, w, label, project):
        k = kind(project)
        self.add(f'<g data-project="{escape(project)}"><rect x="{x}" y="{y}" width="{w}" height="28" rx="14" fill="{FILL[k]}" stroke="{STROKE[k]}" stroke-width="1.2"/>')
        self.text(x + w / 2, y + 18.5, label, size=12, weight=600, anchor="middle")
        self.add("</g>")

    def cylinder(self, x, y, w, h, title, subtitle):
        self.add(f'<path d="M{x},{y + 10} a{w / 2},10 0 0,1 {w},0 v{h - 20} a{w / 2},10 0 0,1 {-w},0 z" fill="{SERVER_FILL}" stroke="{NAVY}" stroke-width="1.5"/>')
        self.add(f'<path d="M{x},{y + 10} a{w / 2},10 0 0,0 {w},0" fill="none" stroke="{NAVY}" stroke-width="1.5"/>')
        self.text(x + w / 2, y + h / 2 + 4, title, size=14, weight=700, anchor="middle")
        self.text(x + w / 2, y + h / 2 + 21, subtitle, size=11, fill=MUTED, anchor="middle")

    def arrow(self, points, label=None, label_at=None, dashed=False, colour=NAVY):
        dash = ' stroke-dasharray="5 4"' if dashed else ""
        path = " L".join(f"{px},{py}" for px, py in points)
        self.add(f'<path d="M{path}" fill="none" stroke="{colour}" stroke-width="1.8"{dash} marker-end="url(#arrow)"/>')
        if label:
            lx, ly = label_at
            self.text(lx, ly, label, size=11, weight=600, fill=colour)

    def render(self, height):
        head = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {WIDTH} {height}" width="{WIDTH}" height="{height}" '
                f'font-family="{FONT}" role="img" aria-label="GSBC Church App architecture">'
                f'<defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
                f'<path d="M0,0 L10,5 L0,10 z" fill="{NAVY}"/></marker></defs>'
                f'<rect width="{WIDTH}" height="{height}" rx="18" fill="#FFFFFF"/>')
        return head + "".join(self.parts) + "</svg>\n"


def main():
    names = projects()
    unknown_hosts = [n for n in names if n.startswith("Hosts.") and n not in HOSTS]
    if unknown_hosts:
        sys.exit(f"New host(s) {unknown_hosts}: give them a place in tools/docs/architecture.py first.")

    svg = Svg()
    svg.text(40, 46, "GSBC Church App", size=22, weight=700, fill=NAVY)
    svg.text(232, 46, "architecture: how the pieces fit", size=15, fill=MUTED)

    y = 70
    svg.panel(40, y, 470, 170, "iPhone · Capacitor shell (Android later)")
    svg.box(60, y + 46, 200, 64, "Hosts.App", HOSTS["Hosts.App"], "app", PREFIX + "Hosts.App")
    svg.box(290, y + 46, 200, 64, "data-layer plugin", "Swift: store · sync · outbox", "app")
    svg.arrow([(260, y + 78), (288, y + 78)])
    svg.text(60, y + 136, "Screens read the phone's SQLite store;", size=11, fill=MUTED)
    svg.text(60, y + 152, "calls and sync go through native code.", size=11, fill=MUTED)

    svg.panel(540, y, 300, 170, "Browser · staff")
    svg.box(570, y + 46, 240, 64, "Hosts.Admin", HOSTS["Hosts.Admin"], "app", PREFIX + "Hosts.Admin")

    svg.panel(870, y, 330, 170, "Dev Mac", dashed=True)
    svg.box(890, y + 46, 290, 64, "Hosts.AppHost", HOSTS["Hosts.AppHost"], "dev", PREFIX + "Hosts.AppHost")
    svg.text(890, y + 140, "deploy.sh puts dev builds on the phone.", size=11, fill=MUTED)

    sy = 300
    svg.panel(40, sy, 1160, 190, "Server · gsbc.argo cluster, behind Cloudflare", title_at_bottom=True)
    svg.box(70, sy + 56, 250, 70, "Hosts.Gateway", HOSTS["Hosts.Gateway"], "server", PREFIX + "Hosts.Gateway")
    svg.box(400, sy + 56, 250, 70, "Hosts.Api", HOSTS["Hosts.Api"], "server", PREFIX + "Hosts.Api")
    svg.cylinder(730, sy + 46, 180, 92, "Postgres", "schema per feature")
    svg.box(970, sy + 56, 210, 70, "Hosts.Migrator", HOSTS["Hosts.Migrator"], "server", PREFIX + "Hosts.Migrator")
    svg.add(f'<rect x="400" y="{sy + 142}" width="250" height="32" rx="8" fill="#FFFFFF" stroke="{ORANGE}" stroke-dasharray="5 4"/>')
    svg.text(525, sy + 163, "R2 (S3 API) · Dropbox · APNs: planned", size=11, fill=MUTED, anchor="middle")

    svg.arrow([(390, y + 112), (390, y + 190), (170, y + 190), (170, sy + 54)], "gRPC-Web", (178, y + 216))
    svg.arrow([(690, y + 112), (690, y + 205), (250, y + 205), (250, sy + 54)], "gRPC-Web", (698, y + 196))
    svg.arrow([(322, sy + 91), (398, sy + 91)], "/gsbc.* only", (324, sy + 82))
    svg.arrow([(652, sy + 80), (728, sy + 80)])
    svg.arrow([(728, sy + 108), (652, sy + 108)], "NOTIFY", (664, sy + 126), dashed=True, colour=AQUA)
    svg.arrow([(968, sy + 91), (912, sy + 91)], "migrate", (918, sy + 82))
    svg.arrow([(1160, y + 112), (1160, sy - 2)], "runs locally", (1080, y + 200), dashed=True, colour=ORANGE)

    cy = sy + 230
    features = {}
    for n in names:
        if n.startswith("Features."):
            _, feature, part = n.split(".", 2)
            features.setdefault(feature, []).append(part)
    platform = [n.removeprefix("Platform.") for n in names if n.startswith("Platform.") and not n.endswith(".Tests")]
    tests = [n for n in names if is_test(n)]

    chip_w, gap = 104, 8
    feature_h = 60 + max(1, len(features)) * 44 + 24
    svg.panel(40, cy, 1160, feature_h, "src/Features · one folder per feature")
    fy = cy + 44
    for feature, parts in sorted(features.items()):
        svg.text(60, fy + 19, feature, size=13, weight=700)
        for i, part in enumerate(sorted(parts, key=lambda p: PART_ORDER.index(p) if p in PART_ORDER else 99)):
            svg.chip(170 + i * (chip_w + gap), fy, chip_w, part, f"{PREFIX}Features.{feature}.{part}")
        fy += 44
    svg.text(60, cy + feature_h - 14, "Features never reference each other, only Platform.", size=11, fill=MUTED, italic=True)

    py = cy + feature_h + 20
    platform_h = 88
    svg.panel(40, py, 1160, platform_h, "src/Platform · what every feature stands on")
    ordered = sorted(platform, key=lambda p: PLATFORM_ORDER.index(p) if p in PLATFORM_ORDER else 99)
    for i, part in enumerate(ordered):
        svg.chip(60 + i * (chip_w + 34), py + 44, chip_w + 26, part, f"{PREFIX}Platform.{part}")
    code_h = feature_h + 20 + platform_h

    ty = cy + code_h + 20
    rows = [[]]
    x = 60
    for t in tests:
        w = 14 + 7.4 * len(t)
        if x + w > 1180 and rows[-1]:
            rows.append([])
            x = 60
        rows[-1].append((x, w, t))
        x += w + 12
    tests_h = 76 + (len(rows) - 1) * 36
    svg.panel(40, ty, 1160, tests_h, "tests/ · rules and real behaviour")
    for r, row in enumerate(rows):
        for x, w, t in row:
            svg.chip(x, ty + 36 + r * 36, w, t, PREFIX + t)

    ly = ty + tests_h + 30
    for i, (k, label) in enumerate([("app", "ships in the app or admin (AOT-checked)"), ("server", "server side"), ("dev", "local development only")]):
        lx = 40 + i * 330
        svg.add(f'<rect x="{lx}" y="{ly}" width="22" height="14" rx="4" fill="{FILL[k]}" stroke="{STROKE[k]}"/>')
        svg.text(lx + 30, ly + 12, label, size=12, fill=MUTED)

    OUT.write_text(svg.render(ly + 40))
    print(f"Wrote {OUT.relative_to(ROOT)} with {len(names)} projects.")


if __name__ == "__main__":
    main()
