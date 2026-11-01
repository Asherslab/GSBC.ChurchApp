# GSBC Church App

The Good Shepherd Baptist Church app: iOS first, then Android, from one Blazor WebAssembly app in a
Capacitor shell, with a gRPC API behind a YARP gateway and a staff admin panel.

Start with [SPEC.md](SPEC.md), then [docs/](docs/README.md) for the architecture and the build order.

This repository is public. Never commit keys, tokens or personal data.

## Building

Needs the .NET SDK pinned in `global.json` (Homebrew's `dotnet`, 10.0.401) with the `wasm-tools`
workload, and Docker for local Postgres.

```sh
dotnet build GSBC.ChurchApp.slnx
dotnet test --solution GSBC.ChurchApp.slnx
dotnet run --project src/Hosts/GSBC.ChurchApp.Hosts.AppHost     # Postgres, migrator, API, gateway (localhost:5300)
tools/aot-warnings/check.sh                                     # AOT publish of the app + warning baseline, as CI does
```

## Testing on a phone

Dev builds talk to the backend on your Mac over Wi-Fi; no TestFlight needed.

1. Once: put your Apple team id in `src/Hosts/App/shell/.team` (gitignored), and pair the phone with
   this Mac (cable once, then it works over Wi-Fi). Keep the phone unlocked while it installs.
2. Start the backend: `dotnet run --project src/Hosts/GSBC.ChurchApp.Hosts.AppHost` (or the AppHost
   run configuration in Rider).
3. Deploy: `tools/ios/deploy.sh`, or **iPhone (dev)** in Rider's run configurations. About 40 seconds.

| Command | Builds |
|---|---|
| `tools/ios/deploy.sh` | Debug (interpreted, fastest) to the paired iPhone |
| `tools/ios/deploy.sh --release` | AOT, as shipped (about a minute) |
| `tools/ios/deploy.sh --simulator` | either of the above, to the iOS Simulator |

The script finds the phone, writes your team and this Mac's address (`<LocalHostName>.local:5300`) into
`ios/dev.xcconfig` (gitignored, so nothing machine-specific is committed), then builds, installs and
launches. Home shows whether the app reached the server. Override with `GSBC_GATEWAY=host:port` or
`GSBC_DEVICE=<udid>`.

To see sync working: open **Storage** in the app (it lists what's stored, the sync state and anything
waiting to send), then change the development feed from the Mac and watch it arrive:

```sh
tools/dev/feed.sh put hello "Hello from the Mac"     # add or edit a record
tools/dev/feed.sh delete hello                       # delete it
tools/dev/feed.sh list
```

Debug builds also have buttons there to queue a send that succeeds and one that fails, for the outbox.

How it reaches the Mac: app calls go through the native plugin (Swift, `URLSession`, gRPC-Web) to
the gateway, never from the WebView, so there's no CORS. iOS allows plain HTTP to local-network
addresses only (`NSAllowsLocalNetworking`), and asks for local-network permission the first time.
Debug logs from the plugin show with `xcrun simctl launch --console-pty booted au.com.baptist.churchapp`
on the simulator, or in Xcode's console on a phone.

Migrations: `dotnet ef migrations add <Name> -p <feature>.Api -s <feature>.Api -c <Context>` (the tool
is in `.config/dotnet-tools.json`; `dotnet tool restore` first).

## Layout

```
src/Platform/     what every feature stands on
src/Features/     one folder per feature: Contracts, Api, Client, App, Admin
src/Hosts/        Gateway, Api, Migrator, Admin, App (+ Capacitor shell and native plugin), AppHost
tests/            mirrors src/
docs/             architecture and features
spikes/           frozen reference
```

See [docs/architecture/repo-layout.md](docs/architecture/repo-layout.md).
