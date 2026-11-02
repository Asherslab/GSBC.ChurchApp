# Accounts

Signing up, signing in, passkeys, sessions, invitations and recovery, for everyone, in the browser
and in the app. The rules are in [identity.md](../architecture/identity.md); this is how they're
built. Permissions (who may do what) are the Access feature, next.

## What people can do (v1)

| Who | Can |
|---|---|
| Anyone | Sign up with a name, an email and a passkey; sign in; confirm their email |
| Signed in | See and remove their passkeys, add another, see and end their sessions, sign out everywhere, change their name |
| Forgot / lost device | Ask for a recovery code by email (confirmed email, no sensitive permissions) and register a new passkey |
| Staff with `accounts.manage` | Invite someone by email; give someone a recovery code (QR or email); see and end anyone's sessions; remove a passkey; disable an account |

## Flows

Every non-gRPC step is one of the approved passkey ceremonies at the gateway (AGENTS.md).

**Sign up.** `Accounts.StartSignUp(name, email)` (gRPC, public, rate-limited) creates an unconfirmed
account and returns a sign-up ticket (single-use, 15 minutes). The client runs the *register a
passkey* ceremony with the ticket; on success the gateway starts a session. A 6-digit code is emailed;
`Accounts.ConfirmEmail(code)` marks the email confirmed. Until then the account works but can't
recover by email.

**Sign in.** The *sign in* ceremony with a discoverable passkey (no username to type; the browser's
autofill offers passkeys). On success the gateway starts a session.

**Add a passkey.** Signed in, the *register a passkey* ceremony with the session instead of a ticket.

**Invitation.** `Accounts.Invite(name, email)` (`accounts.manage`) emails a link to
`<site>/welcome?code=…`, a client-side route of the app or admin panel (static files, not an API path).
The page calls `Accounts.Redeem(code)`, which returns a registration ticket for that account, then runs
the *register a passkey* ceremony.

**Recovery.** `Accounts.RequestRecovery(email)` (public, rate-limited, always answers the same so it
doesn't reveal who has an account) emails a code when allowed. Staff can issue one with
`Accounts.IssueRecoveryCode(userId)`. Either way, `Accounts.Redeem(code)` and the ceremony, as above.
Recovering never removes existing passkeys; the person removes lost ones afterwards.

## Sessions and tokens

- **Server-side sessions** in `accounts.sessions`: id, user, client (browser or app, device name),
  created, last seen, expires. Ending one deletes the row, and it stops working at once.
- **Browser:** the cookie holds only the session id (ASP.NET Core's cookie `SessionStore`, backed by
  that table). HttpOnly, Secure, SameSite=Strict.
- **App:** the native layer keeps a session secret in the Keychain and sends it as a header; it never
  reaches the WebView. Needs Associated Domains, so it waits for the enrolment.
- **The gateway swaps either for an API token** on every proxied call: a JWT signed by the gateway
  (ES256), 5 minutes, carrying user id and session id only. The API checks issuer, audience, signature
  and expiry, and the session is checked again live for anything sensitive.
- Sessions last 90 days from last use in the app and 14 days in the browser, then need a passkey again.

## Where it lives

| Project | Holds |
|---|---|
| `Features.Accounts.Contracts` | `Session` and `Accounts` services, messages, `accounts.*` permissions |
| `Features.Accounts.Data` (new part) | `AccountsDbContext` (schema `accounts`): Identity users and passkeys, sessions, tickets, codes, audit log; the session store |
| `Features.Accounts.Api` | The gRPC services |
| `Features.Accounts.Gateway` (new part) | The ceremony endpoints, session cookie and header handling, the session-to-token transform |
| `Features.Accounts.Client` / `.App` / `.Admin` | Sign-in and account screens; staff screens in Admin |

`Data` and `Gateway` are new optional parts: features whose state the gateway needs (only Accounts, so
far) put it in `Data`, shared by `Api` and `Gateway`.

## Robustness

- ASP.NET Core Identity's passkey support (.NET 10), user verification required, attestation none.
- Every ceremony and public method is rate-limited per IP; sign-up and recovery also per email.
- Codes and tickets are random, hashed at rest, single-use, short-lived, bound to one account.
- Audit log: sign-ups, sign-ins, passkeys added and removed, recoveries, invitations, sessions ended.
- An integration test drives the real ceremonies with a software authenticator: sign up, sign in,
  add a passkey, recover, end a session, and the API rejecting the ended session's token.

## Build order

1. Data, the gRPC services, and the browser ceremonies on the gateway; tested locally (`localhost`
   is a valid passkey domain in the browser), then through `dev.baptist.com.au`.
2. The admin panel's sign-in and account screens; the token transform; the API's token check.
3. The app's account screens, read-only signed out.
4. After the enrolment: Associated Domains, native passkeys in the app, the Keychain session.

## Status (2026-11-01)

**Built, in the browser:** sign-up, sign-in, adding and removing passkeys, confirming an email,
recovery by email link, invitation links, seeing and ending sessions, signing out everywhere, the
session-to-token swap, and the API checking tokens. Tested by `Features.Accounts.Gateway.Tests`
(real ceremonies, software authenticator, Postgres) and checked end to end through
`dev.baptist.com.au`. Try it at https://dev.baptist.com.au/account with a real passkey; in
development the emails (confirmation codes, links) appear in the API's log in the Aspire dashboard.

**Waiting on something else:**

- ~~*The Access feature*~~: built. Staff with `accounts.manage` can invite, give recovery links,
  disable accounts and read the audit log (the admin panel's People page), and people holding a
  sensitive permission can't recover by email. Abandoned sign-ups (no passkey after 7 days) are
  cleaned up, and email is queued and retried.
- ~~*The shared mailbox*~~: done 2026-11-02. Email goes out as `app@baptist.com.au` through Graph
  ([email.md](../architecture/email.md)); a staff invitation was sent, accepted and signed in end to
  end in development. Production needs the email secret in the cluster, later
  ([email.md](../architecture/email.md), *Going to production*).
- *The Apple enrolment:* Associated Domains, then passkeys and the Keychain session in the app.
- *Production keys and relying party:* the token signing key and `Accounts:Passkeys` for
  `baptist.com.au`, as cluster secrets and config through `gsbc.argo`.

## Open

- The token signing key in production: a cluster secret through `gsbc.argo` (owner).
