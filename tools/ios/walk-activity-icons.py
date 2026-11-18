import json
import re
import shutil
import sys
from pathlib import Path

USAGE = """Usage: python3 tools/ios/walk-activity-icons.py
Writes the Live Activity's icons into the widget extension's asset catalog from the app's own icons
(src/Platform/GSBC.ChurchApp.Platform.Ui/IconPaths.cs), so both draw the same glyph for each meaning."""

ICONS = ["arrow-left", "arrow-right", "arrow-up", "rotate-ccw", "circle-off", "message-square", "pause", "play", "check",
         "door-open", "footprints", "flag"]

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "src/Platform/GSBC.ChurchApp.Platform.Ui/IconPaths.cs"
CATALOG = ROOT / "src/Hosts/App/shell/ios/App/WalkActivity/Icons.xcassets"


def paths():
    text = SOURCE.read_text()
    found = dict(re.findall(r'\["([a-z0-9-]+)"\] = """(.*?)"""', text, re.S))
    missing = [name for name in ICONS if name not in found]
    if missing:
        sys.exit(f"Not in IconPaths.cs: {', '.join(missing)}")
    return found


def svg(body):
    return ('<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" '
            'stroke="#000000" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' + body.strip() + '</svg>\n')


def main():
    if len(sys.argv) > 1:
        sys.exit(USAGE)
    found = paths()
    if CATALOG.exists():
        shutil.rmtree(CATALOG)
    CATALOG.mkdir(parents=True)
    (CATALOG / "Contents.json").write_text(json.dumps({"info": {"author": "xcode", "version": 1}}, indent=2) + "\n")
    for name in ICONS:
        folder = CATALOG / f"{name}.imageset"
        folder.mkdir()
        (folder / f"{name}.svg").write_text(svg(found[name]))
        (folder / "Contents.json").write_text(json.dumps({
            "images": [{"filename": f"{name}.svg", "idiom": "universal"}],
            "info": {"author": "xcode", "version": 1},
            "properties": {"preserves-vector-representation": True, "template-rendering-intent": "template"},
        }, indent=2) + "\n")
    print(f"Wrote {len(ICONS)} icons to {CATALOG.relative_to(ROOT)}")


main()
