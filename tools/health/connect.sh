#!/usr/bin/env bash
set -euo pipefail

environment="${1:-}"
case "$environment" in
  dev|uat) ;;
  *) echo "usage: tools/health/connect.sh dev|uat" >&2; exit 2 ;;
esac
service="gsbc-tool-$environment"

read -rsp "Paste the session secret from the $environment admin panel's Account page: " secret
echo
if [[ ! "$secret" =~ ^[A-Za-z0-9_-]{40,}$ ]]; then
  echo "That doesn't look like a session secret. Copy it again with the Copy button." >&2
  exit 1
fi

printf 'add-generic-password -U -a %s -s %s -w %s\n' "$USER" "$service" "$secret" | security -i >/dev/null
unset secret
echo "Saved in your login keychain as $service. Try: tools/health/errors.sh --env $environment"
