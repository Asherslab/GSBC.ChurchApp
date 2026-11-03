#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."

since="${1:-$(git describe --tags --abbrev=0 --match 'testflight/uat/*' 2>/dev/null || true)}"
if [ -n "$since" ]; then
  files="$(git diff --name-only --diff-filter=AM -M "$since" HEAD -- 'changelog/*/*.md')"
else
  files="$(git ls-files 'changelog/*/*.md')"
fi
dotnet run tools/release-notes/WhatToTest.cs -- $files
