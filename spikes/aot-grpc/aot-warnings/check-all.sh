#!/usr/bin/env bash
set -uo pipefail
cd "$(dirname "$0")/.."
OUR_NAMESPACES_AND_TOP_LEVEL_TYPES=(Contracts. Server. Client. Wasm. Program. EventsService ServerServices AppClients)
status=0
for host in server client wasm; do
  echo "== $host"
  python3 aot-warnings/check.py "out/$host.publish.log" "aot-warnings/$host.txt" --ours "${OUR_NAMESPACES_AND_TOP_LEVEL_TYPES[@]}" "$@" || status=1
done
exit $status
