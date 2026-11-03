#!/usr/bin/env python3
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "src/Hosts/App/shell/ios/App/App/Assets.xcassets"
SOURCE = ASSETS / "AppIcon.appiconset/AppIcon-512@2x.png"
FONT = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"

ENVIRONMENTS = {
    "Dev": ("DEV", (232, 152, 40), (32, 24, 8)),
    "UAT": ("UAT", (79, 200, 205), (14, 36, 48)),
}


def banded(label: str, band: tuple[int, int, int], ink: tuple[int, int, int]) -> Image.Image:
    icon = Image.open(SOURCE).convert("RGB")
    size = icon.width
    top = int(size * 0.8)
    draw = ImageDraw.Draw(icon)
    draw.rectangle([0, top, size, size], fill=band)
    font = ImageFont.truetype(FONT, int(size * 0.13))
    left, upper, right, lower = draw.textbbox((0, 0), label, font=font)
    draw.text(((size - (right - left)) / 2 - left, top + (size - top - (lower - upper)) / 2 - upper),
              label, font=font, fill=ink)
    return icon


def main() -> None:
    for name, (label, band, ink) in ENVIRONMENTS.items():
        folder = ASSETS / f"AppIcon-{name}.appiconset"
        folder.mkdir(exist_ok=True)
        banded(label, band, ink).save(folder / "AppIcon.png")
        contents = json.loads((SOURCE.parent / "Contents.json").read_text())
        contents["images"][0]["filename"] = "AppIcon.png"
        (folder / "Contents.json").write_text(json.dumps(contents, indent=2) + "\n")
        print(f"wrote {folder.relative_to(ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
