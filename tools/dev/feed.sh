#!/usr/bin/env bash
set -euo pipefail

usage="usage: feed.sh put <id> <text> | delete <id> | list"
container="$(docker ps --format '{{.Names}}' | grep -m1 '^postgres' || true)"
[ -n "$container" ] || { echo "No Postgres container. Start the AppHost first."; exit 1; }

psql() { docker exec -i "$container" sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" exec psql -h localhost -U postgres -d churchapp -v ON_ERROR_STOP=1 -q "$@"' psql "$@"; }
feed="dev.echo"

case "${1:-}" in
  put)
    [ $# -eq 3 ] || { echo "$usage"; exit 2; }
    psql -v feed="$feed" -v id="$2" -v text="$3" <<'SQL'
INSERT INTO sync.feed_changes (feed, record_id, version, deleted, payload, sort_key)
SELECT :'feed', :'id', COALESCE(MAX(version), 0) + 1, false, convert_to(:'text', 'UTF8'), :'id'
FROM sync.feed_changes WHERE feed = :'feed' AND record_id = :'id';
SQL
    ;;
  delete)
    [ $# -eq 2 ] || { echo "$usage"; exit 2; }
    psql -v feed="$feed" -v id="$2" <<'SQL'
INSERT INTO sync.feed_changes (feed, record_id, version, deleted, payload)
SELECT :'feed', :'id', COALESCE(MAX(version), 0) + 1, true, ''::bytea
FROM sync.feed_changes WHERE feed = :'feed' AND record_id = :'id';
SQL
    ;;
  list)
    psql -v feed="$feed" -c "SELECT cursor, record_id, version, deleted, convert_from(payload, 'UTF8') AS text FROM sync.feed_changes WHERE feed = '$feed' ORDER BY cursor"
    ;;
  *) echo "$usage"; exit 2 ;;
esac
