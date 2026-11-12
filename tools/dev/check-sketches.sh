#!/usr/bin/env bash
set -euo pipefail

usage="Usage: tools/dev/check-sketches.sh <payload.json>
Checks every HTML sketch an impeccable decision-page payload declares is self-contained, before serve-question --start or --update.
The page serves each sketch alone at /sketch/<n>, so a stylesheet, script, font or picture it links by a relative path
never loads and the sketch shows unstyled."

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
print(f"{sum(1 for c in cards if c.get('html'))} sketches are self-contained.")
EOF
