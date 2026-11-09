# Accounts

Signing up, signing in, passkeys, sessions, invitations and recovery, for everyone, in the browser
and in the app. The rules are in [identity.md](../architecture/identity.md); this is how they're
built. Permissions (who may do what) are the Access feature ([access.md](access.md)).

## What people can do (v1)

| Who | Can |
|---|---|
| Anyone | Sign up with a name, an email and a passkey; sign in; confirm their email |
| Signed in | See and remove their passkeys, add another, see and end their sessions, sign out everywhere, change their name, delete their account, pin admin pages to the top of the sidebar |
| Forgot / lost device | Ask for a recovery code by email (confirmed email, no sensitive permissions) and register a new passkey |
| Staff with `accounts.manage` | Invite someone by email; give someone a recovery code (QR or email); see and end anyone's sessions; remove a passkey; disable an account; delete an account |
| Staff with `accounts.review.manage` | Make a demo account and sign-in codes for Apple's reviewers; turn a code off ([identity.md](../architecture/identity.md), *Review sign-in codes*) |
| Apple's reviewers | Sign in to a demo account with its email and a sign-in code, in the app |
| Staff with `accounts.tools.connect` (Developer, Owner) | Connect a tool, such as Claude on a computer, to their own account ([identity.md](../architecture/identity.md), *Connected tools*) |

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

**Invitations not yet accepted** (2026-11-04). `Accounts.ListInvitations` (`accounts.view`) lists every
unused invitation whose account has no passkey yet: who, who invited them (from the audit log), when, and
when the link stops working (expired ones stay listed until the cleanup removes the account).
`Accounts.ResendInvitation` (`accounts.manage`) replaces the link with a new 7-day one and emails it;
`Accounts.WithdrawInvitation` (`accounts.manage`) stops the link and deletes the account that was never
set up, as the cleanup would a week later. The admin panel's People shows them under the *Invited*
filter, with both actions on the person's panel, and an invitation still waiting after two days, or
expired, reaches Home's *Needs you* (`InvitationsOfficeWeek`, for holders of `accounts.manage`).

**Review sign-in.** In the app, signed out: **Have a sign-in code?** → email and code →
`Accounts.SignInWithCode` (gRPC, public, rate-limited and throttled) answers with an app session secret,
which the native layer keeps. Staff make demo accounts and codes on the admin panel's **App review** page
(`/app-review`): `Accounts.CreateDemoAccount`, `CreateReviewCode`, `TurnOffReviewCode` and
`GetReviewAccess`, all needing `accounts.review.manage`. Codes are in `accounts.review_codes`; a session
started with one names it (`sessions.review_code_id`).

**Recovery.** `Accounts.RequestRecovery(email)` (public, rate-limited, always answers the same so it
doesn't reveal who has an account) emails a code when allowed. Staff can issue one with
`Accounts.IssueRecoveryCode(userId)`. Either way, `Accounts.Redeem(code)` and the ceremony, as above.
Recovering never removes existing passkeys; the person removes lost ones afterwards.

**Deleting an account** (owner, 2026-11-09: "yes staff can delete, also let people delete their own account? no reason
not to"). `Accounts.DeleteAccount(userId)` (`accounts.manage`, from the person's panel on People, never your own) and
`Accounts.DeleteMyAccount` (signed in, at the end of the Account page in the app and the admin panel). Both ask first and
say it can't be undone (`AccountDeletionWords`).

- **Only the name stays** (`AccountDeletion`): the account's row is kept with its display name, so the history of what
  they did still names them (draft changes, Live history, the People activity log). Its email, user name and phone are
  cleared (the email is free to sign up again, and the audit details that were the email are cleared), it's marked
  `deleted_at` and disabled, its pinned pages are cleared, and its passkeys, Identity claims, logins and tokens, sessions, codes and review codes are
  deleted in one transaction, so it can't sign in and every device is signed out. People and the people picker no
  longer list it; `GetPeople` still answers its name, with `Deleted`. The abandoned-account clean-up never removes it.
- **Every feature forgets its part** through `IPersonData` (Platform.Api, registered by each feature's API like
  `IMediaHolder`): Access deletes their role assignments; Sermons their listening progress (deletions on the progress
  feed); Notifications their inbox, preferences and pending pushes, and their phones stay registered as signed-out
  phones; Events their private calendar link, and takes them off any event, regular event or date they're a contact
  for. A feature that fails is asked again by the clean-up (every 6 hours) until `forgotten_at` is set. Withdrawing an
  invitation and the clean-up of abandoned sign-ups forget the same way before the account goes.
- **A feature can say no** (`IPersonData.WhyKeepAsync`): Access refuses while the account holds the church's last
  Owner assignment ("Make someone else an Owner first").
- Phones held roles of their own (calendars added, follows) stay with the phone, as when someone signs out.
- Tests: `AccountDeletionTests`, `InvitationTests`, `AssignmentPolicyTests`.

**Pinned pages** (owner, 2026-11-09: pins "with the account"). The admin sidebar's *Pinned* list
([repo-layout.md](../architecture/repo-layout.md), *The admin sidebar*) is kept on the account, so it follows the person to
any computer: `Accounts.GetPinnedPages` and `Accounts.SetPinnedPages` (signed in, your own only; up to 12 page addresses),
stored as `accounts.users.pinned_pages`. Saving marks the own-records `accounts.pins` feed, so another computer's sidebar
reloads them; `AccountPinnedPages` is the admin panel's `IPinnedPages`. Deleting the account clears them.

## Sessions and tokens

- **Server-side sessions** in `accounts.sessions`: id, user, client (browser or app, device name),
  created, last seen, expires. Ending one deletes the row, and it stops working at once.
- **Browser:** the cookie holds only the session id (ASP.NET Core's cookie `SessionStore`, backed by
  that table). HttpOnly, Secure, SameSite=Strict.
- **App:** the native layer keeps a session secret in the Keychain and sends it as a header; it never
  reaches the WebView ([identity.md](../architecture/identity.md), *Signing in on the phone*).
- **The gateway swaps either for an API token** on every proxied call: a JWT signed by the gateway
  (ES256), 5 minutes, carrying user id and session id only. The API checks issuer, audience, signature
  and expiry, and the session is checked again live for anything sensitive.
- Sessions last 90 days from last use in the app, 30 days for a connected tool and 14 days in the browser,
  then need a passkey again.
- **A connected tool** is a session with client `Tool` and a secret, sent as the app's header; its API
  token carries `tool`, and Access refuses it anything sensitive.

## Where it lives

| Project | Holds |
|---|---|
| `Features.Accounts.Contracts` | `Session` and `Accounts` services, messages, `accounts.*` permissions |
| `Features.Accounts.Data` | `AccountsDbContext` (schema `accounts`): Identity users and passkeys, sessions, tickets, codes, audit log; the session store |
| `Features.Accounts.Api` | The gRPC services |
| `Features.Accounts.Gateway` | The ceremony endpoints, session cookie and header handling, the session-to-token transform |
| `Features.Accounts.Client` / `.App` / `.Admin` | Sign-in and account screens; staff screens in Admin |

`Data` and `Gateway` are optional parts: features whose state the gateway needs (only Accounts, so
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
   is a valid passkey domain in the browser), then through `dev-app.baptist.com.au`.
2. The admin panel's sign-in and account screens; the token transform; the API's token check.
3. The app's account screens (More → Account).
4. After the enrolment: Associated Domains, native passkeys in the app, the Keychain session (built
   2026-11-03).

## Status

**Built, in the browser:** sign-up, sign-in, adding and removing passkeys, confirming an email,
recovery by email link, invitation links, seeing and ending sessions, signing out everywhere, the
session-to-token swap, and the API checking tokens. Tested by `Features.Accounts.Gateway.Tests`
(real ceremonies, software authenticator, Postgres) and checked end to end through
`dev-app.baptist.com.au`. Try it at https://dev-app.baptist.com.au/account with a real passkey; in
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
- ~~*The Apple enrolment*~~: done 2026-11-03 (the church, as an organisation). Built the same day:
  Associated Domains, native passkeys in the app (sign in, sign up, add a passkey, invitation and
  recovery links), the Keychain session, signing out clearing private data, and `/welcome` links
  opening the app. Tested by `Features.Accounts.Gateway.Tests` (the phone gets a secret, not a cookie,
  until the session ends; the association file) and the native `PasskeyCeremonyTests`, and proven on
  the owner's iPhone ([identity.md](../architecture/identity.md), *Signing in on the phone*).
- *Production keys and relying party* (future work: production doesn't exist yet): the token signing key and `Accounts:Passkeys` for
  `baptist.com.au`, as cluster secrets and config through `gsbc.argo`.

## Open

- The token signing key in production (future work): a cluster secret through `gsbc.argo` (owner).
