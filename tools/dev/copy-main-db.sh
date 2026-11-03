#!/usr/bin/env bash
set -euo pipefail

here="$(git rev-parse --show-toplevel)"
main="$(git worktree list --porcelain | awk '/^worktree /{print substr($0, 10); exit}')"
[ "$here" != "$main" ] || { echo "This is the main checkout; run it from a worktree."; exit 1; }

# Aspire names a data volume after the SHA-256 of the AppHost's project path, lower-cased.
volume_for() {
  local project="$1/src/Hosts/GSBC.ChurchApp.Hosts.AppHost/GSBC.ChurchApp.Hosts.AppHost.csproj"
  local hash
  hash="$(printf %s "$project" | tr '[:upper:]' '[:lower:]' | shasum -a 256 | cut -c1-10)"
  echo "gsbc.churchapp.hosts.apphost-$hash-postgres-data"
}

from="$(volume_for "$main")"
to="$(volume_for "$here")"
docker volume inspect "$from" >/dev/null 2>&1 || { echo "The main checkout has no database yet ($from)."; exit 1; }

in_use="$(docker ps -q --filter "volume=$from" --filter "volume=$to")"
[ -z "$in_use" ] || { echo "Stop the AppHost first: a Postgres container is using one of the volumes."; exit 1; }

docker volume create "$to" >/dev/null
docker run --rm -v "$from:/from:ro" -v "$to:/to" alpine sh -c 'rm -rf /to/* /to/..?* /to/.[!.]* 2>/dev/null; cp -a /from/. /to/'
echo "Copied the main checkout's database into this worktree's ($to)."
