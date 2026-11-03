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
  account on `dev-app` and add one (proven on the owner's iPhone, 2026-11-03). See
  [environments.md](environments.md).
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

- **Associated Domains** (`App.entitlements`): `webcredentials:` on `$(GSBC_ASSOCIATED_DOMAIN)` and
  `applinks:` on `$(GSBC_LINKS_DOMAIN)`, set per environment in `ios/environments/*.xcconfig`. Both are
  `dev-app.baptist.com.au` and `uat-app.baptist.com.au` in those builds. Release builds use `baptist.com.au`
  for passkeys and `app.baptist.com.au` (the gateway, which the phone accepts links from) for links
  (2026-11-03, [destinations.md](destinations.md)).
- **Each gateway's association file** is written at start-up from `Apple:TeamId` and `Apple:BundleId`
  (`au.com.baptist.app.dev` in development, `.uat` in UAT's `config.yaml`, production's by default) into
  a temporary folder, and served as a static file at `/.well-known/apple-app-site-association`: the same
  bytes on every request, `application/json`, no redirect (`no-store` in development, `no-cache` elsewhere).
  Apple's CDN fetches it through Cloudflare (checked 2026-11-03). `AppAssociation.Json` writes it; a
  gateway test checks it names the configured team and bundle.
- **Applinks:** `/welcome` (invitations and recovery links) and `/open/*` (shared links to a place in the
  app, [destinations.md](destinations.md)) open the app.

**Needs setting up outside this repo (owner):**

- `https://baptist.com.au/.well-known/apple-app-site-association` for production, at the apex in
  front of the website (a Cloudflare rule or Worker), with the same content as dev's but for
  `baptist.com.au`: `{"applinks":{"details":[{"appIDs":["YNT9A4LGVA.au.com.baptist.app"],
  "components":[{"/":"/welcome"},{"/":"/welcome/*"}]}]},"webcredentials":{"apps":["YNT9A4LGVA.au.com.baptist.app"]}}`,
  as `application/json`, with no redirect. Production's `Accounts:Passkeys:AllowedOrigins` must include
  `https://baptist.com.au`, the origin the phone's passkeys report. Only its `webcredentials` part matters
  now: since 2026-11-03 production links open the app from `app.baptist.com.au`, whose gateway serves its own
  file ([destinations.md](destinations.md)).
- Android's `assetlinks.json` at the same apex, when Android is built.

## Sessions

The gateway owns sessions, stored server-side, so any one can be ended at once.

| Client | Holds | Sent as |
|---|---|---|
| Admin panel (browser) | A cookie: HttpOnly, Secure, SameSite=Strict | The cookie |
| App (native layer) | A session secret in the iOS Keychain, never in the WebView | A header on each gRPC call |
| A connected tool (a terminal on a computer) | A session secret in the macOS Keychain | The same header |

On every proxied call the gateway swaps either one for a signed API token that lives for minutes
(SPEC §6). The API never sees a cookie or a session secret. Removing a person, a passkey or a
session, or "sign out everywhere", ends the gateway session immediately; a token already issued
expires within minutes, and permissions are checked live anyway.

## Connected tools (built 2026-11-03)

A developer can connect a tool, such as Claude on their computer, to their own account, so it can read
App health from a terminal (owner, 2026-11-03; [analytics.md](../features/analytics.md), *Reading App
health from the terminal*). It's a third kind of session, not an API key (decided with the owner,
2026-11-03):

- **Who:** people holding `accounts.tools.connect` (sensitive): the Developer role (Accounts adds it to
  Analytics' role with `AddToSystemRole`) and Owner. Account managers hold `accounts.view`, `.manage` and
  `.review.manage` by name, so not this.
- **Connecting** is `Accounts.ConnectTool(name)` in the admin panel's Account page; the secret is shown once.
  The phone's native layer refuses the method, so the WebView never receives one.
- **It acts as its account but never with anything sensitive.** The gateway marks the API token
  (`tool: True`), and Access adds a deny for every sensitive permission to a tool's grants, wherever they're
  read: `[Requires]`, sync audiences, drafts, role editing. A tool can't give or take away roles (even
  through an assigner role) and can't add a passkey, so a leaked secret can't become a full sign-in.
- **It ends** after **30 days unused** (each use moves it on), when it's signed out under *Where you're
  signed in*, or with *sign out everywhere*. Connecting one is audited.

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

## Review sign-in codes (built 2026-11-03)

Apple's reviewers need a way in and can't make a passkey, and the owner wants no passwords (owner,
2026-11-03): a **sign-in code** that signs in to a **demo account** only. What goes in App Store Connect
is in [environments.md](environments.md), *Sign-in for Apple's reviewers*.

- **A demo account is an ordinary account holding the App reviewer system role** (`app-reviewer`,
  declared by Accounts, granting nothing). There's no flag on the account: Access says who is a demo
  account, as for everything else. Staff with `accounts.review.manage` (sensitive; Account managers hold
  it) make one in the admin panel, and Accounts gives it the role as a feature.
- **A code only ever reaches a demo account:** it must hold the role, have no passkey (a passkey means
  a real person) and hold nothing sensitive (so giving it Owner by mistake stops its codes rather than
  exposing Owner). The checks run on every sign-in, not only when the code is made.
- **A demo account never gets a passkey:** sign-up, invitations and recovery refuse its email, and the
  gateway's *register a passkey* ceremony refuses a session that came from a code. The Account screen
  says it's a demo account and offers no passkeys.
- **Codes:** 12 characters from `ACDEFGHJKMNPQRTUVWXY34679` (no 0/O, 1/I/L, 2/Z, 5/S, 8/B), shown as
  `ABCD-EFGH-JKMN`; typing ignores case, spaces and dashes. Only the SHA-256 is stored, and the code is
  shown once. A code lasts 30 days by default (1 to 180), works any number of times, and can be turned off.
- **Signing in** is a gRPC method, `Accounts.SignInWithCode(email, code)`, `[Public]` and rate-limited, so
  it needs no new API path. It answers with an app session secret, the same as the passkey ceremonies
  with `app: true`. The phone's native layer makes the call itself (`CodeSignIn`) and keeps the secret in
  the Keychain; the WebView only hears the outcome, and the native `unary` refuses that method so the
  WebView can never receive a secret. It's the app only: there's no cookie path, so the admin panel
  doesn't offer it (Apple only reviews the app).
- **A code session lives only as long as its code:** turning a code off deletes its sessions, and the
  gateway stops honouring a session whose code expired or was turned off.
- **Throttled:** besides the per-IP rate limit, five refusals for one account, or ten from one address,
  in 15 minutes refuse every attempt (right or wrong) until the window passes. Counted from the audit log,
  so it holds across API instances.
- **Audited:** making a demo account, making and turning off a code, every sign-in with a code (with the
  code's label and the address) and every refusal.

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
