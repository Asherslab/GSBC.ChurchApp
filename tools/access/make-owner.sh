#!/usr/bin/env bash
set -euo pipefail

email="${1:?usage: make-owner.sh <email of someone who has already signed up>}"

if [ -n "${DATABASE_URL:-}" ]; then
  psql() { command psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -q "$@"; }
else
  container="$(docker ps --format '{{.Names}}' | grep -m1 '^postgres' || true)"
  [ -n "$container" ] || { echo "No Postgres container, and DATABASE_URL isn't set. Start the AppHost, or point DATABASE_URL at the database."; exit 1; }
  psql() { docker exec -i "$container" sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" exec psql -h localhost -U postgres -d churchapp -v ON_ERROR_STOP=1 -q "$@"' psql "$@"; }
fi

result="$(psql -t -A -v email="$email" <<'SQL'
WITH person AS (SELECT id FROM accounts.users WHERE normalized_email = upper(:'email')),
     owner AS (SELECT id FROM access.roles WHERE system_key = 'owner'),
     made AS (
       INSERT INTO access.assignments (id, user_id, role_id, scope_object_id, source, expires_at, created_at, created_by)
       SELECT gen_random_uuid(), person.id, owner.id, NULL, 'Bootstrap', NULL, now(), NULL FROM person, owner
       WHERE NOT EXISTS (SELECT 1 FROM access.assignments a WHERE a.user_id = person.id AND a.role_id = owner.id)
       RETURNING 1)
SELECT (SELECT count(*) FROM person) || ' ' || (SELECT count(*) FROM owner) || ' ' || (SELECT count(*) FROM made);
NOTIFY gsbc_access_changes;
SQL
)"

read -r people owners made <<< "$result"
if [ "$people" = 0 ]; then echo "Nobody has signed up with $email yet."; exit 1; fi
if [ "$owners" = 0 ]; then echo "The Owner role doesn't exist yet: start the API once so it can set up roles."; exit 1; fi
if [ "$made" = 0 ]; then echo "$email is already an Owner."; else echo "$email is now an Owner."; fi
