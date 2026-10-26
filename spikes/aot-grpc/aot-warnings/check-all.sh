#!/usr/bin/env bash
# Check each host's publish log against its baseline. Run after publishing to out/ (see ../README.md).
#   aot-warnings/check-all.sh            # check
#   aot-warnings/check-all.sh --update   # regenerate the baselines; review the diff before committing
set -uo pipefail
cd "$(dirname "$0")/.."
# Our own code: namespaces, plus the spike's top-level (namespace-less) types.
OURS=(Contracts. Server. Client. Wasm. Program. EventsService ServerServices AppClients)
status=0
for host in server client wasm; do
  echo "== $host"
  python3 aot-warnings/check.py "out/$host.publish.log" "aot-warnings/$host.txt" --ours "${OURS[@]}" "$@" || status=1
done
exit $status
