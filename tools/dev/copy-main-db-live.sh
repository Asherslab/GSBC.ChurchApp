#!/usr/bin/env bash
set -euo pipefail

usage="Usage: tools/dev/copy-main-db-live.sh
Copies the main checkout's dev database into this worktree's while both backends run (this worktree's AppHost started
with Dev__PortOffset). Reads main's with pg_dump only; replaces this worktree's churchapp database, gives it its own sync
origin and forgets every phone's push registration."

[ $# -eq 0 ] || { echo "$usage"; exit 1; }

here="$(git rev-parse --show-toplevel)"
main="$(git worktree list --porcelain | awk '/^worktree /{print substr($0, 10); exit}')"
[ "$here" != "$main" ] || { echo "This is the main checkout; run it from a worktree."; exit 1; }

# Aspire names a data volume after the SHA-256 of the AppHost's project path, lower-cased.
volume_for() {
  local project="$1/src/Hosts/GSBC.ChurchApp.Hosts.AppHost/GSBC.ChurchApp.Hosts.AppHost.csproj"
  echo "gsbc.churchapp.hosts.apphost-$(printf %s "$project" | tr '[:upper:]' '[:lower:]' | shasum -a 256 | cut -c1-10)-postgres-data"
}

container_for() {
  local found
  found="$(docker ps --filter "volume=$1" --format '{{.Names}}' | head -1)"
  [ -n "$found" ] || { echo "No running Postgres uses $1; start that checkout's AppHost first." >&2; exit 1; }
  echo "$found"
}

from="$(container_for "$(volume_for "$main")")"
to="$(container_for "$(volume_for "$here")")"
[ "$from" != "$to" ] || { echo "Both checkouts resolve to $from; refusing."; exit 1; }

psql_in() { docker exec -i "$1" sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" exec "$0" -h 127.0.0.1 -U postgres "$@"' "${@:2}"; }

psql_in "$to" psql -q -d postgres -c "drop database if exists churchapp with (force)" -c "create database churchapp"
psql_in "$from" pg_dump -Fc -d churchapp | psql_in "$to" pg_restore --no-owner -d churchapp 2>&1 | tail -3 || true
psql_in "$to" psql -q -d churchapp -c "
  DO \$\$ BEGIN
    IF to_regclass('sync.origin') IS NOT NULL THEN UPDATE sync.origin SET id = gen_random_uuid(); END IF;
    IF to_regclass('notifications.devices') IS NOT NULL THEN DELETE FROM notifications.devices; END IF;
  END \$\$;"
echo "Copied main's database ($from) into this worktree's ($to), with its own sync origin and no phones registered for push."
