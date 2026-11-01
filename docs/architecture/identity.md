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
- **Dev has its own relying party: `dev.baptist.com.au`** (decided 2026-11-01), reached through the dev
  proxy ([tools/dev-proxy](../../tools/dev-proxy/README.md)). Test passkeys never sit beside real ones
  in people's keychains. On a dev Mac with no tunnel, the browser accepts `localhost` as the RP.
- **Bundle id: `au.com.baptist.churchapp`** (decided 2026-11-01), so the app id in the association
  files is `<team id>.au.com.baptist.churchapp`.
- **The ceremonies are the approved non-gRPC paths** in AGENTS.md: begin/complete for sign-in and for
  registering a passkey, at the gateway, called by the browser's WebAuthn API and by the app's
  native layer (the platform passkey APIs produce the same JSON).
- **ASP.NET Core Identity's passkey support** at the gateway, as in MealPlanner: user verification
  required, no attestation, challenges single-use and short-lived, every POST checks its origin.

## Needs setting up outside this repo (owner)

- `https://baptist.com.au/.well-known/apple-app-site-association`, served at the apex in front of the
  website (a Cloudflare rule or Worker), with `webcredentials` (passkeys) and `applinks` (invitation
  and recovery links open the app). It needs the Apple team id, so it waits for enrolment.
- The app's Associated Domains entitlement: `webcredentials:baptist.com.au`, `applinks:baptist.com.au`
  (and `dev.baptist.com.au` for dev builds). Associated Domains needs the paid developer account, so
  passkeys in the app wait for the enrolment; passkeys in the browser can be built and tested first.
- Dev's association file is served by the dev gateway as a static file at
  `dev.baptist.com.au/.well-known/apple-app-site-association`, written from the dev Mac's team id
  (gitignored), so it needs nothing from the apex website.
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

## Open

- Email goes out from a Microsoft 365 shared mailbox through Microsoft Graph (decided 2026-11-01), the
  same way GSBC.Accounting sends claims: an app registration with client credentials, permission to
  send granted in Exchange and scoped to that one mailbox (never `Mail.Send` in Entra, which would let
  it send as anyone), and an outgoing-email table with a background sender so an outage delays mail
  but never loses it. In development, emails show in the Aspire dashboard instead. Owner: which
  mailbox, and the app registration and Exchange scoping (GSBC.Accounting's
  `docs/modules/infrastructure/email.md` has the steps).
