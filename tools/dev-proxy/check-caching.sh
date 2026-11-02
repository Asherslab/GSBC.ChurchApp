#!/usr/bin/env bash
set -uo pipefail

site="${1:-https://dev.baptist.com.au}"
page="$([[ -n "${2:-}" ]] || curl -s "$site/")"
assets="$(printf '%s' "$page" | grep -oE '(_framework|_content)/[^"]+\.(js|css)' | sort -u)"
failures=0

check() {
  local path=$1 expected=$2
  local headers cache status
  headers="$(curl -s -o /dev/null -D - -H 'Accept-Encoding: br, gzip' "$site/$path")"
  status="$(printf '%s' "$headers" | head -1 | tr -d '\r')"
  cache="$(printf '%s' "$headers" | grep -i '^cache-control:' | cut -d' ' -f2- | tr -d '\r')"
  printf '%-90s %-12s %s [%s]\n' "$path" "${status#HTTP/* }" "${cache:-(none)}" \
    "$(printf '%s' "$headers" | grep -i '^cf-cache-status:' | cut -d' ' -f2- | tr -d '\r')"
  if ! printf '%s' "$cache" | grep -qE "$expected"; then
    echo "   ✗ expected cache-control matching: $expected"
    failures=$((failures + 1))
  fi
}

if [[ -n "${2:-}" ]]; then
  for _ in 1 2; do check "$2" 'immutable'; done
elif [[ "$site" == *dev.* ]]; then
  for path in "" $assets "a-file-that-does-not-exist.js"; do check "$path" '^no-store$'; done
else
  check "" 'no-cache|no-store'
  for path in $assets; do
    if [[ "$path" =~ \.[a-z0-9]{10}\.(js|css)$ ]]; then check "$path" 'immutable'; else check "$path" 'no-cache|no-store'; fi
  done
fi

[ "$failures" -eq 0 ] && echo "OK: Cloudflare serves the headers we expect." || { echo "$failures problem(s)."; exit 1; }
