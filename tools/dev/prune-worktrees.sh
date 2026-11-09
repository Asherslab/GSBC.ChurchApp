#!/usr/bin/env bash
set -euo pipefail

usage="Usage: tools/dev/prune-worktrees.sh [--dry-run]
Removes every worktree whose branch is merged into the main checkout's branch and has no local changes,
then removes the Postgres volumes no remaining checkout uses. Skips any worktree a running process is working in. Branches are kept."

dry_run=false
case "${1:-}" in
  --dry-run) dry_run=true ;;
  "") ;;
  *) echo "$usage"; exit 1 ;;
esac

main="$(git worktree list --porcelain | awk '/^worktree /{print substr($0, 10); exit}')"
base="$(git -C "$main" symbolic-ref --short HEAD)"

# Aspire names a data volume after the SHA-256 of the AppHost's project path, lower-cased.
postgres_volume_for() {
  local project="$1/src/Hosts/GSBC.ChurchApp.Hosts.AppHost/GSBC.ChurchApp.Hosts.AppHost.csproj"
  local hash
  hash="$(printf %s "$project" | tr '[:upper:]' '[:lower:]' | shasum -a 256 | cut -c1-10)"
  echo "gsbc.churchapp.hosts.apphost-$hash-postgres-data"
}

in_use="$(lsof -d cwd -Fn 2>/dev/null | sed -n 's/^n//p' | sort -u)"
docker_up=false
docker info >/dev/null 2>&1 && docker_up=true

before="$(df -k "$main" | awk 'NR==2{print $4}')"
git -C "$main" worktree list --porcelain | awk '/^worktree /{path=substr($0, 10)} /^branch /{print path "\t" substr($0, 19)} /^detached/{print path "\t"}' |
while IFS=$'\t' read -r path branch; do
  [ "$path" != "$main" ] || continue
  if [ -z "$branch" ]; then
    echo "keep  $path: detached, nothing to compare"; continue
  fi
  ahead="$(git -C "$main" rev-list --count "$base..$branch")"
  if [ "$ahead" != 0 ]; then
    echo "keep  $branch: $ahead commits not in $base"; continue
  fi
  if [ -n "$(git -C "$path" status --porcelain)" ]; then
    echo "keep  $branch: uncommitted changes"; continue
  fi
  if printf '%s\n' "$in_use" | grep -qF -- "$path"; then
    echo "keep  $branch: a process is working in it"; continue
  fi
  echo "prune $branch ($(du -sh "$path" | cut -f1))"
  $dry_run && continue
  git -C "$main" worktree remove "$path"
done
git -C "$main" worktree prune
if $docker_up; then
  owned="$(git -C "$main" worktree list --porcelain | sed -n "s/^worktree //p" | while read -r checkout; do postgres_volume_for "$checkout"; done)"
  docker volume ls -q --filter "name=gsbc.churchapp.hosts.apphost-" | { grep -- "-postgres-data$" | grep -vxF -- "$owned" || true; } |
  while read -r volume; do
    echo "prune volume $volume"
    $dry_run || docker volume rm "$volume" >/dev/null || echo "      in use, kept"
  done
else
  echo "Docker isn't running, so unused Postgres volumes weren't checked; run this again with Docker up."
fi
after="$(df -k "$main" | awk 'NR==2{print $4}')"
echo "Freed $(( (after - before) / 1024 / 1024 )) GB."
