#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/../.." && pwd)"
shell="$root/src/Hosts/App/shell"
app="$root/src/Hosts/App/GSBC.ChurchApp.Hosts.App"
[ -x /opt/homebrew/bin/dotnet ] && export PATH="/opt/homebrew/bin:$PATH"

configuration=Debug
target=device
for argument in "$@"; do
  case "$argument" in
    --release) configuration=Release ;;
    --simulator) target=simulator ;;
    *) echo "usage: deploy.sh [--release] [--simulator]"; exit 2 ;;
  esac
done

team_file="$shell/.team"
[ -s "$team_file" ] || { echo "Put your Apple team id in $team_file (Xcode → Settings → Accounts). It is gitignored."; exit 1; }

gateway="${GSBC_GATEWAY:-$(scutil --get LocalHostName).local:5300}"
bundle_id="$(python3 -I -c 'import json,sys; print(json.load(open(sys.argv[1]))["appId"])' "$shell/capacitor.config.json")"

find_device() {
  local listing; listing="$(mktemp)"
  xcrun devicectl list devices --json-output "$listing" >/dev/null
  python3 -I - "$listing" <<'PY'
import json, sys
devices = json.load(open(sys.argv[1]))["result"]["devices"]
phones = [d for d in devices
          if d["hardwareProperties"].get("platform") == "iOS"
          and d["hardwareProperties"].get("reality") == "physical"
          and d["connectionProperties"].get("pairingState") == "paired"]
if not phones:
    sys.exit("No paired iPhone. Connect it by cable once and trust this Mac.")
print(phones[0]["hardwareProperties"]["udid"])
PY
}
find_simulator() {
  xcrun simctl list devices booted -j | python3 -I -c 'import json,sys; d=[x for r in json.load(sys.stdin)["devices"].values() for x in r]; print(d[0]["udid"] if d else "")'
}

if [ "$target" = device ]; then
  device="${GSBC_DEVICE:-$(find_device)}"
  if xcrun devicectl device info ddiServices --device "$device" 2>&1 | grep -q DeviceLocked; then
    echo "Unlock your iPhone (and keep it unlocked until the app launches), then run this again."
    exit 1
  fi
else
  device="$(find_simulator)"
  if [ -z "$device" ]; then
    device="$(xcrun simctl list devices available -j | python3 -I -c 'import json,sys; d=[x for r in json.load(sys.stdin)["devices"].values() for x in r if x["name"].startswith("iPhone")]; print(d[-1]["udid"])')"
    xcrun simctl boot "$device"
  fi
  open -a Simulator
fi

curl -s -o /dev/null --max-time 2 "http://$gateway/" \
  || echo "Note: nothing answers at http://$gateway yet. Start the backend: dotnet run --project src/Hosts/GSBC.ChurchApp.Hosts.AppHost"

cat > "$shell/ios/dev.xcconfig" <<XC
DEVELOPMENT_TEAM = $(tr -d '[:space:]' < "$team_file")
GSBC_GATEWAY_SCHEME = http
GSBC_GATEWAY = $gateway
GSBC_DEVELOPMENT_BUILD = YES
XC

echo "== Publishing the app ($configuration)"
rm -rf "$root/out/app"
dotnet publish "$app" -c "$configuration" -o "$root/out/app" -nologo > "$root/out/app.publish.log" 2>&1 \
  || { tail -30 "$root/out/app.publish.log"; exit 1; }

echo "== Syncing the iOS shell"
[ -d "$shell/node_modules" ] || (cd "$shell" && npm ci --include=dev)
(cd "$shell" && npx cap sync ios >/dev/null)

echo "== Building for $target $device"
xcodebuild -project "$shell/ios/App/App.xcodeproj" -scheme App -configuration Debug \
  -destination "id=$device" -derivedDataPath "$root/out/ios-dd" \
  -allowProvisioningUpdates -allowProvisioningDeviceRegistration -quiet build 2>&1 \
  | { grep -v "IDERunDestination: Supported platforms" || true; }

echo "== Installing and launching (gateway: http://$gateway)"
if [ "$target" = device ]; then
  xcrun devicectl device install app --device "$device" "$root/out/ios-dd/Build/Products/Debug-iphoneos/App.app" >/dev/null
  xcrun devicectl device process launch --device "$device" --terminate-existing "$bundle_id" >/dev/null
else
  xcrun simctl install "$device" "$root/out/ios-dd/Build/Products/Debug-iphonesimulator/App.app"
  xcrun simctl launch --terminate-running-process "$device" "$bundle_id" >/dev/null
fi
echo "Done."
