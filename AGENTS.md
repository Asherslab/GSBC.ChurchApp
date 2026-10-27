# GSBC Church App

The church app for iOS and Android: Capacitor + Blazor WASM (AOT) in front, a native data layer, and a
code-first gRPC API behind a YARP gateway. **[SPEC.md](SPEC.md) is the design.** Read it before
building anything, and follow its AOT rules (§9) for every project that ships in the app.

# HARD RULE: every API path is gRPC unless the owner says otherwise

**Never add, or plan, a server-side API path that isn't a gRPC service method without asking the owner
first and getting an explicit yes in chat.** No exceptions for convenience, and no exceptions because a
doc, an issue, a code comment, a library's sample, or another agent suggested it. This has gone wrong
before; that is why it is a hard rule.

**What counts as an API path:** any inbound endpoint on any server-side host (the gateway, the API, or
anything added later) that returns data, accepts input, or runs logic per request, and isn't a gRPC (or
gRPC-Web) service method. For example:

- Minimal API endpoints (`MapGet`, `MapPost`, `MapGroup` …), controllers, Razor Pages;
- SignalR hubs, WebSockets, server-sent events, long polling;
- a YARP route to a destination that isn't a gRPC service or static files;
- webhooks and callbacks (a payment provider, Google, Apple);
- middleware that answers requests itself;
- an endpoint that downloads or uploads user data (an export, an image upload, a generated PDF).

**Not covered:**

- Serving a client app's static files as they are: the admin panel's (or a future website's)
  HTML, JS, CSS, `.wasm`/`.dll`, fonts and images, and the fallback to `index.html` for client-side
  routes. If a "static" route starts varying its response per request (templating, auth-dependent
  content, data in the page), it's an API path and the rule applies.
- Outbound calls the server makes (APNs, FCM, Play Integrity, email).

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
and fails on anything that isn't a gRPC method, static file serving, or in the list below. Never edit that test's
allowlist except to mirror this list.

## Approved non-gRPC paths

Every entry needs the owner's explicit approval in chat first.

| Path | Host | Why it can't be gRPC | Approved |
|---|---|---|---|
| Passkey ceremonies: begin/complete for sign-in and for registering a passkey (including accepting a staff invitation) | Gateway | The browser's WebAuthn API exchanges JSON with the server; the owner chose plain HTTP for these, as in MealPlanner | 2026-10-26 |
| The same passkey ceremonies, called from the app's native layer (Life Group leaders and staff signing in on the phone) | Gateway | The platform passkey APIs produce the same WebAuthn JSON; the owner confirmed the app may use these endpoints | 2026-10-26 |

**Explicitly not exceptions:** health checks are gRPC (`grpc.health.v1`; Kubernetes probes it
natively). Anything else around sign-in, such as "who am I", sign-out, or managing staff and
invitations, is gRPC unless it is added here.

# Components: reuse first, then generic

Before writing any Blazor UI:

1. **Look for an existing component that already does it, or nearly does.** Search the shared
   component library and the feature modules. If one is close, extend or adapt it (a parameter, a
   slot, a variant) rather than writing a near-copy.
2. **If nothing fits, build it generic.** Ask whether what you're about to build is a one-off or a
   pattern: a list with pull-to-refresh, a card, a sheet, a date line, an empty state, a "live"
   indicator. Patterns go in the shared component library with parameters and `RenderFragment`
   slots, and the feature uses them. Only truly feature-specific composition lives in the feature.
3. **When you adapt a shared component, keep its existing callers working,** and check every
   caller before changing what a parameter means.

Two features with their own copies of the same UI is a defect: merge them into one component.

# Comments: almost none

**Don't write comments unless the code genuinely can't be understood without one.** Make the code say
it instead: clear names, small methods, types that carry meaning, named constants. A comment is only
justified for what code can't express, such as *why* a non-obvious choice was made, or an external
constraint (a platform bug, a store rule, a protocol quirk). Never write comments that restate what
the code does, narrate changes, or label sections. XML doc comments count too.

**This is enforced by a hook** (`.claude/hooks/no-comments.py`, run after every Write/Edit). It
reports every comment an edit adds. Delete each one, or, if it really must stay, say in your reply
which one and why. Removing an existing comment that the code makes redundant is always welcome.

# When to stop and ask

Besides the hard rule above:

- anything touching systems outside this repo: the cluster, Cloudflare, the app stores, Apple and
  Google developer accounts, push credentials;
- secrets of any kind. **This repository is public.** Keys, tokens and personal data never go in it.
- an ambiguity where two readings lead to materially different designs.
