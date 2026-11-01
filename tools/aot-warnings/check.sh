#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
mkdir -p out
rm -rf out/app src/Hosts/App/GSBC.ChurchApp.Hosts.App/obj/Release
dotnet publish src/Hosts/App/GSBC.ChurchApp.Hosts.App -c Release -o out/app > out/app.publish.log 2>&1 || { tail -50 out/app.publish.log; exit 1; }
python3 tools/aot-warnings/check.py out/app.publish.log tools/aot-warnings/app.txt --ours GSBC. Program. "$@"
