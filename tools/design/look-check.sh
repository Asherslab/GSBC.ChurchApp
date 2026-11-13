#!/usr/bin/env bash
set -euo pipefail

usage="Usage: tools/design/look-check.sh [--base http://localhost:5400] [--out DIR] [--widths 1440,1024,390]
                                    [--schemes light,dark] [--only REGEX] [--extra path,path] [--jobs 6]
Signs in to this worktree's admin panel with a test Owner (e2e+look@example.com, made in this worktree's database
only), visits every page in the sidebar with its tabs and a record open, and flags content too close to an edge,
spilling or cut off, overlapping, sideways overflow and bars that aren't flush. Writes a report with screenshots to
--out and exits 1 if anything was flagged. Run it from a worktree whose AppHost runs beside main's (AGENTS.md)."

case "${1:-}" in -h|--help) echo "$usage"; exit 0 ;; esac

here="$(cd "$(dirname "$0")" && pwd)"
top="$(git -C "$here" rev-parse --show-toplevel)"
main="$(git -C "$here" worktree list --porcelain | awk '/^worktree /{print substr($0, 10); exit}')"
[ "$top" != "$main" ] || { echo "This makes a test account an Owner, so it only runs from a worktree (its own database), never the main checkout."; exit 2; }

[ -d "$here/node_modules/playwright-core" ] || (cd "$here" && npm ci --no-audit --no-fund --silent)
browsers="${PLAYWRIGHT_BROWSERS_PATH:-$HOME/Library/Caches/ms-playwright}"
ls -d "$browsers"/chromium_headless_shell-* >/dev/null 2>&1 || (cd "$here" && npx --no-install playwright-core install chromium-headless-shell)

exec node "$here/look-check.mjs" "$@"
