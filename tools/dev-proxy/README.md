# Dev proxy: dev-app.baptist.com.au → a dev Mac

Dev builds and the admin panel reach the backend running on a dev Mac through Cloudflare, over real
HTTPS, from anywhere. `cloudflared` on the Mac mini holds a tunnel to Cloudflare and forwards
`dev-app.baptist.com.au` to the dev Mac's gateway on the local network. Nothing here is a secret except
the tunnel's credentials file, which stays on the Mac mini.

Why this shape: passkeys need HTTPS on a real domain, and the phone's sync must be proven through
Cloudflare (SPEC §5, open point 5). gRPC-Web is ordinary HTTP, so the tunnel needs no gRPC settings;
`Watch` sends a heartbeat every 30 seconds, inside Cloudflare's 100-second idle limit.

## Once, on the Mac that runs the tunnel

Either the Mac mini (always on) or the dev Mac itself. On the dev Mac, `service` is
`http://localhost:5300` and the tunnel only runs while you do.


```sh
brew install cloudflared
cloudflared tunnel login                       # pick the baptist.com.au zone
cloudflared tunnel create gsbc-dev             # prints the tunnel id and writes its credentials file
cloudflared tunnel route dns gsbc-dev dev-app.baptist.com.au
cloudflared tunnel route dns gsbc-dev dev-media.baptist.com.au
```

Copy `cloudflared.yml` to `~/.cloudflared/config.yml` and fill in:

- `credentials-file`: the path `tunnel create` printed;
- `service`: `http://localhost:5300` on the dev Mac, or from the Mac mini the dev Mac's Bonjour name
  (`scutil --get LocalHostName` on the dev Mac, plus `.local`).
- `dev-media.baptist.com.au` is the dev media store (SeaweedFS, port 5310), so uploads from
  `https://dev-app.baptist.com.au` and media on a phone work ([media.md](../../docs/features/media.md)).
  The store listens on localhost only, so this hostname works only when the tunnel runs on the dev Mac.

Then nothing else on the dev Mac: **the AppHost starts the tunnel with the backend** whenever
`~/.cloudflared/config.yml` names `gsbc-dev` and `cloudflared` is installed (it shows as `tunnel` in
the Aspire dashboard). To run only the tunnel, use **Cloudflare tunnel** in Rider or
`tools/dev-proxy/run.sh`. On the Mac mini, run it as a service instead: `sudo cloudflared service install`.

## Using it

Start the backend on the dev Mac as usual, then deploy a build that goes through Cloudflare:

**iPhone (dev)** and **iPhone (AOT, as shipped)** in Rider deploy builds that use
`https://dev-app.baptist.com.au`; **iPhone (dev, Wi-Fi only)** goes straight to the laptop when the
tunnel is down. From a terminal: `GSBC_GATEWAY=https://dev-app.baptist.com.au tools/ios/deploy.sh`. The
admin panel is at https://dev-app.baptist.com.au.

Proven 2026-11-01: gRPC-Web calls and a `Watch` stream through Cloudflare (HTTP/2 at the edge), changes
arriving within a second, heartbeats keeping the stream open.

## Notes

- Nothing is cached in dev: the dev gateway sends `Cache-Control: no-store` on every response, because
  Cloudflare otherwise replaces `no-cache` with its 4-hour browser cache (it once kept a 404 alive in
  Safari). If a browser still shows something stale, empty its cache once (Safari: Develop → Empty Caches).
  `check-caching.sh` checks every asset as Cloudflare serves it; run it after changing anything the
  gateway serves.
- `dev-app.baptist.com.au` is also dev's passkey domain (docs/architecture/identity.md), kept apart from
  production's `baptist.com.au` so test passkeys never sit beside real ones.
- If the dev Mac changes, change `service` and restart: `sudo launchctl kickstart -k system/com.cloudflare.cloudflared`.
