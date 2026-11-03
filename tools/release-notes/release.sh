#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."

version="${1:-}"
[[ "$version" =~ ^[0-9]+\.[0-9]+(\.[0-9]+)?$ ]] || { echo "usage: tools/release-notes/release.sh <version>, like 1.1 or 1.1.1"; exit 2; }
[ ! -e "changelog/$version" ] || { echo "changelog/$version already exists."; exit 1; }
notes=(changelog/unreleased/*.md)
[ -e "${notes[0]}" ] || { echo "changelog/unreleased/ has no notes to release."; exit 1; }

mkdir -p "changelog/$version"
git mv "${notes[@]}" "changelog/$version/"
sed -i '' "s/^MARKETING_VERSION = .*/MARKETING_VERSION = $version/" src/Hosts/App/shell/ios/environments/Version.xcconfig
echo "Moved ${#notes[@]} notes into changelog/$version/ and set the app's version to $version. Check, then commit."
