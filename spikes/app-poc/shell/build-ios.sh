#!/bin/sh
set -eu
here=$(cd "$(dirname "$0")" && pwd)
team=$(cat "$here/.team")
device=${1:?usage: build-ios.sh <device-udid>}

dotnet publish "$here/../App" -c Release -o "$here/../out/app"
(cd "$here" && npx cap sync ios)
xcodebuild -project "$here/ios/App/App.xcodeproj" -scheme App -configuration Debug \
  -destination "id=$device" -derivedDataPath "$here/../out/ios-dd" \
  -allowProvisioningUpdates DEVELOPMENT_TEAM="$team" build
xcrun devicectl device install app --device "$device" "$here/../out/ios-dd/Build/Products/Debug-iphoneos/App.app"
xcrun devicectl device process launch --device "$device" au.com.baptist.churchapp.poc
