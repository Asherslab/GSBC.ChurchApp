# Identity and passkeys

One sign-in for everyone: members, Life Group leaders and staff, in the app and the admin panel
(requirement B). **Everyone can sign in from v1** (decided 2026-11-01), so members create their own
accounts as well as being invited. Being staff is a set of permissions ([permissions.md](permissions.md)), not a
different kind of account. Reading public content still needs no account (SPEC §3).

## Decided

- **Passkeys only.** No passwords, so nothing to phish, reuse or reset.
- **Relying party: `baptist.com.au`** (decided 2026-11-01). It covers the admin panel, the app and
  any future site on any subdomain. It is permanent: a passkey can never move to another domain.
  The RP ID and the allowed origins are configuration, never read from a request header.
- **Dev and UAT have their own relying parties,** each its app host: `dev-app.baptist.com.au` (decided
  2026-11-01 as `dev.baptist.com.au`, renamed 2026-11-03) and `uat-app.baptist.com.au` (2026-11-03), so
  test passkeys never sit beside real ones in people's keychains. Dev is reached through the dev proxy
  ([tools/dev-proxy](../../tools/dev-proxy/README.md)); on a dev Mac with no tunnel, the browser accepts
  `localhost` as the RP. Passkeys made on `dev.baptist.com.au` don't work after the rename: recover the
  account by email on `dev-app` and add one. See [environments.md](environments.md).
- **Bundle ids** (owner, 2026-11-03): `au.com.baptist.app.dev` for dev builds and `au.com.baptist.app.uat`
  for TestFlight's UAT app, beside production's below; each environment's association file names its own.
- **Production's bundle id: `au.com.baptist.app`** (owner, 2026-11-03), so the app id in the association files is
  `<team id>.au.com.baptist.app`. It replaced `au.com.baptist.churchapp` (decided 2026-11-01), which
  is stuck on the owner's expired personal developer team: Xcode registered it there while that
  membership was paid, and an expired team can't remove an App ID, so the church's team can't
  register it.
- **The ceremonies are the approved non-gRPC paths** in AGENTS.md: begin/complete for sign-in and for
  registering a passkey, at the gateway, called by the browser's WebAuthn API and by the app's
  native layer (the platform passkey APIs produce the same JSON).
- **ASP.NET Core Identity's passkey support** at the gateway, as in MealPlanner: user verification
  required, no attestation, challenges single-use and short-lived, every POST checks its origin.

## Apple (the church's developer account)

**The church is enrolled as an organisation** (owner, 2026-11-03: the church is the seller), team id
`YNT9A4LGVA`. The team id isn't a secret (every association file publishes it), but it stays out of
tracked files: it lives in the gitignored `src/Hosts/App/shell/.team`, which `tools/ios/deploy.sh`
puts in the build and the AppHost passes to the dev gateway as `Apple:TeamId`.

- **Associated Domains** (`App.entitlements`): `webcredentials:` and `applinks:` on
  `$(GSBC_ASSOCIATED_DOMAIN)`, set per environment in `ios/environments/*.xcconfig`: `dev-app.baptist.com.au`,
  `uat-app.baptist.com.au`, and `baptist.com.au` for release builds.
- **Each gateway's association file** is written at start-up from `Apple:TeamId` and `Apple:BundleId`
  (`au.com.baptist.app.dev` in development, `.uat` in UAT's `config.yaml`, production's by default) into
  a temporary folder, and served as a static file at `/.well-known/apple-app-site-association`: the same
  bytes on every request, `application/json`, no redirect (`no-store` in development, `no-cache` elsewhere).
  Apple's CDN fetches it through Cloudflare (checked 2026-11-03). `AppAssociation.Json` writes it; a
  gateway test checks it names the configured team and bundle.
- **Applinks:** `/welcome` (invitations and recovery links) opens the app.

**Needs setting up outside this repo (owner):**

- `https://baptist.com.au/.well-known/apple-app-site-association` for production, at the apex in
  front of the website (a Cloudflare rule or Worker), with the same content as dev's but for
  `baptist.com.au`: `{"applinks":{"details":[{"appIDs":["YNT9A4LGVA.au.com.baptist.app"],
  "components":[{"/":"/welcome"},{"/":"/welcome/*"}]}]},"webcredentials":{"apps":["YNT9A4LGVA.au.com.baptist.app"]}}`,
  as `application/json`, with no redirect. Production's `Accounts:Passkeys:AllowedOrigins` must include
  `https://baptist.com.au`, the origin the phone's passkeys report.
- Android's `assetlinks.json` at the same apex, when Android is built.

## Sessions

The gateway owns sessions, stored server-side, so any one can be ended at once.

| Client | Holds | Sent as |
|---|---|---|
| Admin panel (browser) | A cookie: HttpOnly, Secure, SameSite=Strict | The cookie |
| App (native layer) | A session secret in the iOS Keychain, never in the WebView | A header on each gRPC call |

On every proxied call the gateway swaps either one for a signed API token that lives for minutes
(SPEC §6). The API never sees a cookie or a session secret. Removing a person, a passkey or a
session, or "sign out everywhere", ends the gateway session immediately; a token already issued
expires within minutes, and permissions are checked live anyway.

## Robust, not just working

What makes passkeys survive real people losing real phones:

1. **More than one passkey.** Enrolment encourages a second one (another device, or a computer).
   iCloud Keychain and Google Password Manager sync passkeys, so a replaced phone usually just works.
2. **Recovery with no password**, through the same path as an invitation: a one-time code that is
   single-use, expires in hours, and is bound to one account. Redeeming it is a gRPC call
   (`Accounts.Redeem`) that returns a short-lived registration ticket, and registering is the
   approved ceremony, so recovery adds no new API path. Codes come from:
   - a staff member (shown as a QR code in person, or sent by email);
   - the person themselves by email, unless they hold a sensitive permission (the pastoral inbox,
     editing roles); those accounts need a staff member to recover them.
3. **Manageable.** Settings lists your passkeys (device, created, last used) and sessions, and can
   remove any of them. Staff can do the same for others with `accounts.manage`.
4. **Audited.** Sign-ins, registrations, recoveries and removals are logged with who and when.
5. **Rate-limited** per IP and per account on every ceremony and on code redemption.
6. **Clear failures.** The UI explains the common ones (no passkey on this device, cancelled, a
   device without passkey support) rather than showing a WebAuthn error.

## Signing up

Anyone can create an account (decided 2026-11-01). `Accounts.StartSignUp` (gRPC: name and email) returns
a short-lived, single-use sign-up ticket and sends a code to confirm the email; the first passkey is
then registered through the approved ceremony with that ticket, the same path as accepting an
invitation, so sign-up adds no API path (AGENTS.md). Rate limits apply per IP and per email, and an
account's email must be confirmed before it can recover by email.

## Minimal personal data

An account has a display name, an email (for recovery and invitations) and its passkeys. It is not a
church directory (SPEC §1's non-goals). A link to an Elvanto person can be added when rosters need it.

## Signing in on the phone (built 2026-11-03)

The native layer runs the approved ceremonies itself, with the platform passkey API
(`ASAuthorizationPlatformPublicKeyCredentialProvider`), and sends `app: true`, so the gateway answers
with a session secret instead of setting a cookie. The secret goes straight into the iOS Keychain
(`SessionKeychain`, this device only, after first unlock) and is sent as `X-Gsbc-Session` on every
gRPC call; the gateway authenticates it like the browser's cookie, strips it, and issues the usual
API token. The WebView only ever hears "signed in" or the reason it failed (`SessionSecretRules` and
the native `PasskeyCeremonyTests` hold this).

- **Sign in, sign up, add a passkey, accept an invitation, recover:** the shared `SignIn`,
  `Welcome` and `AccountDetails` components, through `IPasskeyCeremonies` (the browser's WebAuthn in
  the admin panel, the native layer in the app).
- **Signing out** ends the session on the server and forgets the secret; **a session ended elsewhere**
  (sign out everywhere, staff ending it, a disabled account) comes back as `X-Gsbc-Session-Ended` on
  the next call, and the phone forgets it too. Either way, and on signing in, the phone clears its
  private feeds and private media.
- App sessions last 90 days from last use.
- *Link this phone*, the development-only stopgap, was removed (owner, 2026-11-03).

**Proven on the owner's iPhone, with the owner, 2026-11-03** (through `dev-app.baptist.com.au`): signing in
with a passkey made earlier in Safari, the session surviving the app being completely closed, a Hub
role given in the admin panel showing More → Drafts on the phone live, previewing a draft while
signed in, signing out clearing drafts and staff tools, and an invitation link opening the app and
creating its passkey there.

## Open

- ~~Email~~: done 2026-11-02. It goes out as `app@baptist.com.au`, a Microsoft 365 shared mailbox,
  through Microsoft Graph with a certificate, permission to send granted in Exchange and scoped to that
  one mailbox (never `Mail.Send` in Entra), behind a queue so an outage delays mail but never loses it.
  See [email.md](email.md).
