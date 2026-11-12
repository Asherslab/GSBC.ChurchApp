#!/usr/bin/env bash
set -euo pipefail

usage="Usage: tools/dev/check-sketches.sh <payload.json>
Run straight after every impeccable serve-question --start or --update. Checks every HTML sketch the payload declares is
self-contained, then marks each as just written.
The page serves each sketch alone at /sketch/<n>, so a stylesheet, script, font or picture it links by a relative path
never loads and the sketch shows unstyled. It also only serves a sketch written after the round was served or updated;
one written before stays on \"sketching\" forever."

[ $# -eq 1 ] && [ -f "$1" ] || { echo "$usage"; exit 1; }

python3 - "$1" <<'EOF'
import json, re, sys
from pathlib import Path

payload = json.loads(Path(sys.argv[1]).read_text())
cards = payload.get("options", []) + ([payload["canonCard"]] if payload.get("canonCard") else [])
linked = re.compile(
    r'<link\b[^>]*\bhref\s*=\s*["\'](?!data:|https?:)[^"\']+'
    r'|<(?:script|img|source|iframe)\b[^>]*\bsrc\s*=\s*["\'](?!data:|https?:|#)[^"\']+'
    r'|@import\s+(?:url\()?["\']?(?!data:|https?:)[^"\')\s;]+'
    r'|url\(\s*["\']?(?!data:|https?:|#)[^"\')\s]+',
    re.IGNORECASE)

problems = []
for card in cards:
    path = card.get("html")
    if not path:
        continue
    file = Path(path)
    if not file.is_file():
        problems.append(f"{card.get('id')}: {path} doesn't exist")
        continue
    for match in linked.finditer(file.read_text()):
        problems.append(f"{card.get('id')}: {path} links {match.group(0)[:90]}")

if problems:
    print("Sketches that will show unstyled on the decision page (inline these into the file):")
    print("\n".join(f"  {p}" for p in problems))
    sys.exit(1)
sketches = [Path(c["html"]) for c in cards if c.get("html")]
for sketch in sketches:
    sketch.touch()
print(f"{len(sketches)} sketches are self-contained and marked as just written.")
EOF
