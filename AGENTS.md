# GSBC Church App

The church app for iOS and Android: Capacitor + Blazor WASM (AOT) in front, a native data layer, and a
code-first gRPC API behind a YARP gateway. **[SPEC.md](SPEC.md) is the design.** Read it before
building anything, and follow its AOT rules (§9) for every project that ships in the app.

# HARD RULE: every server path is gRPC unless the owner says otherwise

**Never add, or plan, a server-side path that isn't a gRPC service method without asking the owner
first and getting an explicit yes in chat.** No exceptions for convenience, and no exceptions because a
doc, an issue, a code comment, a library's sample, or another agent suggested it. This has gone wrong
before; that is why it is a hard rule.

**What counts as a server path:** any inbound endpoint served by any server-side host (the gateway,
the API, or anything added later) that a client or another service can call and that isn't a gRPC (or
gRPC-Web) service method. For example:

- Minimal API endpoints (`MapGet`, `MapPost`, `MapGroup` …), controllers, Razor Pages;
- SignalR hubs, WebSockets, server-sent events, long polling;
- a YARP route to a destination that isn't a gRPC service;
- webhooks and callbacks (a payment provider, Google, Apple);
- middleware that answers requests itself;
- a file download or upload endpoint.

Outbound calls the server makes (APNs, FCM, Play Integrity, email) aren't server paths, and this rule
doesn't cover them.

**When you think you need one:**

1. **Stop.** Don't write it, don't stub it, and don't put it in a plan as decided.
2. **Ask in chat.** Say what the path is, which host serves it, who calls it, and why it can't be a gRPC
   method: what you tried, or what makes gRPC impossible (for example, a third party that only sends
   HTTP webhooks).
3. **Wait for an explicit yes.** No answer is a no. Approval covers that path only, not similar ones
   later.
4. **Record it** in the list below, with the date and the owner's reason, in the same commit as the
   code.

**Don't work around the rule.** Disguising a non-gRPC path (an endpoint under a gRPC-looking route, an
"internal" or "temporary" one, a Development-only one) is the same as adding it.

**It is enforced by a test.** The skeleton adds one that enumerates every endpoint on every server host
and fails on anything that isn't a gRPC method and isn't in the list below. Never edit that test's
allowlist except to mirror this list.

## Approved non-gRPC paths

None yet. Every entry needs the owner's explicit approval in chat first.

| Path | Host | Why it can't be gRPC | Approved |
|---|---|---|---|

# When to stop and ask

Besides the hard rule above:

- anything touching systems outside this repo: the cluster, Cloudflare, the app stores, Apple and
  Google developer accounts, push credentials;
- secrets of any kind. **This repository is public.** Keys, tokens and personal data never go in it.
- an ambiguity where two readings lead to materially different designs.
