#!/usr/bin/env python3
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(subprocess.check_output(["git", "rev-parse", "--show-toplevel"], text=True).strip())
DOCS = [ROOT / n for n in ("SPEC.md", "AGENTS.md", "DESIGN.md", "README.md", "PRODUCT.md")]
DOCS += sorted((ROOT / "docs").rglob("*.md")) + sorted((ROOT / "tools").rglob("README.md"))

LINK = re.compile(r"\[[^\]]*\]\(([^)\s]+)\)")
CODE = re.compile(r"`([^`\n]+)`")
PATH_ROOTS = ("src/", "tests/", "tools/", "docs/", "changelog/", "spikes/", ".claude/", ".github/", ".impeccable/")
FILE_SUFFIXES = (".cs", ".razor", ".md", ".sh", ".py", ".yml", ".yaml", ".json", ".svg", ".csproj", ".props", ".css", ".js", ".swift", ".proto", ".slnx", ".txt")


def heading_slugs(path):
    slugs = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        m = re.match(r"#+\s+(.*)", line)
        if m:
            s = re.sub(r"[^\w\s-]", "", m.group(1).lower().replace("`", "")).strip()
            slugs.add(re.sub(r"\s", "-", s))
    return slugs


def exists(path):
    if "*" in path or "…" in path or "<" in path or "{" in path:
        return True
    return (ROOT / path.rstrip("/")).exists()


problems = []
for doc in DOCS:
    if not doc.exists():
        continue
    text = doc.read_text(encoding="utf-8")
    rel = doc.relative_to(ROOT)
    in_fence = False
    for n, line in enumerate(text.splitlines(), 1):
        if line.lstrip().startswith("```"):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        for target in LINK.findall(CODE.sub('', line)):
            if re.match(r"[a-z]+:", target) or target.startswith("mailto:"):
                continue
            file_part, _, anchor = target.partition("#")
            dest = doc if not file_part else (doc.parent / file_part)
            if not dest.exists():
                problems.append(f"{rel}:{n}: broken link {target}")
            elif anchor and dest.suffix == ".md" and anchor not in heading_slugs(dest):
                problems.append(f"{rel}:{n}: missing anchor {target}")
        for token in CODE.findall(line):
            token = token.strip()
            if " " in token:
                continue
            if token.startswith(PATH_ROOTS) and (token.endswith(FILE_SUFFIXES) or token.endswith("/")):
                if not exists(token):
                    problems.append(f"{rel}:{n}: no such path {token}")

for p in problems:
    print(p)
sys.exit(1 if problems else 0)
