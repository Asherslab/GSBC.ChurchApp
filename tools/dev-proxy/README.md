# Dev proxy: dev.baptist.com.au → your laptop

Dev builds and the admin panel reach the backend running on a dev Mac through Cloudflare, over real
HTTPS, from anywhere. `cloudflared` on the Mac mini holds a tunnel to Cloudflare and forwards
`dev.baptist.com.au` to the dev Mac's gateway on the local network. Nothing here is a secret except
the tunnel's credentials file, which stays on the Mac mini.

Why this shape: passkeys need HTTPS on a real domain, and the phone's sync must be proven through
Cloudflare (SPEC §5, open point 5). gRPC-Web is ordinary HTTP, so the tunnel needs no gRPC settings;
`Watch` sends a heartbeat every 30 seconds, inside Cloudflare's 100-second idle limit.

## Once, on the Mac mini

```sh
brew install cloudflared
cloudflared tunnel login                       # pick the baptist.com.au zone
cloudflared tunnel create gsbc-dev             # prints the tunnel id and writes its credentials file
cloudflared tunnel route dns gsbc-dev dev.baptist.com.au
```

Copy `cloudflared.yml` to `~/.cloudflared/config.yml` and fill in:

- `credentials-file`: the path `tunnel create` printed;
- `service`: the dev Mac's Bonjour name (`scutil --get LocalHostName` on the dev Mac, plus `.local`).

Then run it as a service, so it survives restarts:

```sh
sudo cloudflared service install
```

## Using it

Start the backend on the dev Mac as usual, then deploy a build that goes through Cloudflare:

```sh
GSBC_GATEWAY=https://dev.baptist.com.au tools/ios/deploy.sh
```

or **iPhone (dev, through Cloudflare)** in Rider. The admin panel is at https://dev.baptist.com.au.

## Notes

- Nothing is cached: gRPC calls are POSTs, and the admin panel's file names are fingerprinted.
- `dev.baptist.com.au` is also dev's passkey domain (docs/architecture/identity.md), kept apart from
  production's `baptist.com.au` so test passkeys never sit beside real ones.
- If the dev Mac changes, change `service` and restart: `sudo launchctl kickstart -k system/com.cloudflare.cloudflared`.
