#!/usr/bin/env bash
set -euo pipefail
for candidate in /opt/homebrew/bin/cloudflared /usr/local/bin/cloudflared "$(command -v cloudflared || true)"; do
  [ -x "$candidate" ] && exec "$candidate" tunnel run gsbc-dev
done
echo "cloudflared isn't installed: brew install cloudflared, then follow tools/dev-proxy/README.md."
exit 1
