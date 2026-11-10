# People: families, young people and safeguarding

Status: built 2026-11-10 as Stage 1 of [Ministries](ministries.md) (the owner's interview, 2026-11-10), overnight while
the owner slept: every decision below marked *default* is Claude's and the owner can change it. Sketches, the
alternatives and why each screen was chosen: `.impeccable/mocks/people/decision.html`.

What the later stages (ministries, rosters, chats with safeguarding) rely on: **who is under 18, who their parents
are, how to reach them, and who holds a current blue card and Safe Ministry training.**

## What people can do

| Who | Can |
|---|---|
| Anyone signed in | Give their birth date once (or say they're 18 or older); see their family, blue card and training on their Account page |
| A parent | Add a child (name and birth date, no email or phone); make a child a sign-in code for their own phone; change a child's name (if the parent added them) and birth date; choose how ministries reach the child; ask another parent to link too; say yes or no to a young person's ask; stop looking after a child; remove a child who can't sign in themselves |
| A young person (under 18) | Ask a parent by email to link to them; take an ask back; see their parents. Can't unlink a parent while under 18 |
| An adult with a parent linked | Unlink them |
| Staff with *Safeguarding Viewer* | See anyone's birth date, family, blue card and training, and the Safeguarding page |
| Staff with *Safeguarding Manager* | All of that, and link and unlink families, add a child for a parent, take back asks, make sign-in codes, set how a young person is reached, record and remove blue cards and training, link and unlink Safe Ministry Check records, Sync now |

## How it's modelled

- **A child without a phone is an account without sign-in** (*default*): a row in `accounts.users` with no email and no
  passkey, `added_by_family` set. So a child has one id everywhere: Access can give them roles (a ministry's attendee
  in Stage 2), names show in history, the people picker finds them, and deleting works like any account. People
  makes and removes them through `IPeopleAccounts` (Platform.Api, implemented by Accounts); features never reference
  each other. The abandoned-sign-up clean-up skips them.
- **A sign-in code** (`Accounts`' `FamilySignIn` code: 8 letters and digits as `ABCD-EFGH`, one use, 24 hours, a new
  one replaces the last) is redeemed through the same `Accounts.Redeem` an invitation uses, then the child's phone
  makes a passkey for the account the parent made, already linked and aged. The code is spent when the passkey is made,
  not when it's redeemed, so a cancelled passkey prompt can be tried again. The field (`CodeField`) adds the capitals
  and the dash as it's typed. Signing in has *Got a code from your
  parent?*. A parent can also make a code for a child who already signs in (a new phone, a lost passkey).
- **Everything else is the People feature** (schema `people`): `people` (birth date, *I'm 18 or older*, how to
  reach), `links` (parent, child, how and by whom), `asks` (a child and a parent's email, who asked) and
  `safeguarding` (one row per person).
- **A young person asks by email** (*default*): the ask is stored against the email, so it waits for a parent who
  hasn't signed up yet; the answer never reveals whether the email has an account. A parent with an account gets a
  notification (*Someone asks you to look after them in the app*, waits until answered). Up to 5 asks wait per child.
  A parent asks another parent the same way for their child.
- **Sign-up asks for a birth date** (owner, 2026-11-10), in the app and the admin panel, with *I'm 18 or older* (no
  date kept) instead. Someone under 18 also names a parent's email and goes the young person's way: once their passkey
  is made, the ask is sent as if from the Account page. Accounts doesn't know about ages: People adds the question to
  the form through `ISignUpQuestion` (Platform.Client) and saves it with its own gRPC calls once the new account is
  signed in, so an abandoned sign-up keeps nothing and asks nobody. If saving fails, the Account page asks again.
- **People who signed up before that are asked once** on the Account page, the same way. Someone sets their own birth
  date once; after that a parent or the office changes it, so a young person can't type their way out of being a
  minor.

## Who sees what: Access only

No list of user ids in People decides anything. **Being someone's parent is a role**: linking gives the parent the
hidden **Parent or guardian** role at `person:<child>` (a new scope kind, `ScopeKind.Person`; Accounts registers the
scope objects), **ending at midnight on the child's 18th birthday** in Brisbane, so all of a parent's powers stop at 18
without a job. Changing a birth date moves the end. The nodes:

| Node | Scope | Who |
|---|---|---|
| `people.family.view` | person | Parent at the child; everyone at global through Safeguarding Viewer and Manager |
| `people.family.edit` | person | Parent at the child; Safeguarding Manager |
| `people.family.signin` | person | Parent at the child; Safeguarding Manager |
| `people.family.act` | person | **Parent at the child only** (Safeguarding Manager is denied it): act for the child, shown as the parent |
| `people.families.manage` | global, sensitive | Safeguarding Manager: link and unlink anyone, add a child for a parent |
| `people.safeguarding.view` | person, sensitive | Safeguarding Viewer and Manager |
| `people.safeguarding.edit` | person, sensitive | Safeguarding Manager |

The parent's nodes are **not sensitive** on purpose: holding any sensitive grant stops someone recovering their account
by email, and that must not happen to every parent. People refuses connected tools on every family call itself.
You see your own family through your own records (the `people.mine` feed, own records); the office's tabs read the
`people.family` and `people.safeguarding` feeds, scoped per person.

## Young people on a team: how they're reached (owner, 2026-11-10)

From the owner's decision *Young people on a team, reached through their parents* ([ministries.md](ministries.md)):
each linked young person has a way to be reached, **Directly**, **Through their parents** or **Both**, chosen by the
family on the child's sheet (or staff on the Family tab). **Only a parent or the office changes it, not the young
person** (owner, 2026-11-10). The rule (`Reach.Effective`): an adult, or someone with no
parent linked, is reached directly; a child who can't sign in is reached through their parents; otherwise what the
family chose, **Both** until they choose (*default*). A ministry requiring one comes with Stage 2.

For later stages, `IFamilies` (Platform.Api) answers **how to reach someone** (`PersonReach`: whether directly, and
which parents) and **whether someone may act for them** (`MayActForAsync`: themselves, or a parent holding
`people.family.act` at them, which ends at 18). `ActingFor` names who acted for whom, for showing "Sarah, for Tom".
Rosters and chats themselves are Stages 3 and 4.

## Blue cards and Safe Ministry training

Per person: blue card number, status (Current, Exemption card, Applied for, Suspended, Cancelled, Expired), expiry and
when the office last checked it; Safe Ministry training done and due again (**a year after it was done unless a date
is given**, owner, 2026-11-10). They come **from Safe Ministry Check** for everyone linked to a record there (below), and
are **entered by hand** by Safeguarding Managers on the person's Safeguarding tab for everyone else. They're shown to the
person on their Account page (the card drawn as a card), and **flagged, never blocking** (`SafeguardingFlags`):

- *Run out*: a card past its expiry or with a status other than Current or Exemption; training past its due date.
- *Due soon*: within 60 days (*default*).
- *Not recorded*: a card but no training, or training but no card; training Safe Ministry Check says isn't finished.
  Someone with neither isn't flagged: Stage 2 adds "serves on a team that needs one".
- *No parent*: under 18 with no parent linked (with when they asked, if they have).
- *Check Safe Ministry Check*: something there needs looking at (below).

**Safeguarding** (People → Safeguarding, Safeguarding Viewer) lists everyone flagged, run out first, then by date;
chips filter by kind and count each. Anything run out and every young person without a parent also shows in Home's
*Needs you*.

## Safe Ministry Check (owner, 2026-11-10)

The church has Safe Ministry Check API access ([docs](https://documenter.getpostman.com/view/13693981/2sB2x9hqEa)):
`GET https://<subdomain>.safeministrycheck.com.au/api/people?page=&limit=` with an `X-API-Key` header, 5 requests a
second (429 above that), no webhooks. The owner's decisions:

1. **Read only.** "we'll just record their safe ministry id against their person on our side, instead of writing it to
   their other system id side. so do read only". The app never creates or updates anything in Safe Ministry Check; the
   `smc_id` is kept on our side (`people.safeguarding`, `source = safe-ministry-check`, `external_id`).
2. **Matching by email, or by name and birth date, both automatically.** A link, once made, is kept (a changed email
   doesn't break it). Safeguarding Managers see, fix or unlink a match on the person's Safeguarding tab and link an
   unmatched record by hand there or on **People → Safe Ministry Check**, which lists the records still to match and why.
3. **Only the blue card and training come across:** `wwcc_id`, `wwcc_expiry`, `wwcc_status` (mapped to our statuses),
   `wwcc_checked`, `course_progress`, `next_action_date` (training due), `other_training_name` and
   `other_training_completed_date`, and the overall `status` and `status_description`. Every other field is read only to
   decide whether to raise **one plain flag, "Check Safe Ministry Check"**, with short generic reasons ("their police
   check isn't cleared"); their values and the admin notes are never stored.
4. **Their due date wins** over the yearly rule: `next_action_date` is the training's due date.
5. **Synced values replace hand-entered ones** for linked people, and the screens say where they came from ("From Safe
   Ministry Check, 3 hours ago").
6. Who sees it is Access: the Safeguarding Viewer and Manager roles. Deleting an account forgets its link and synced
   data (`IPersonData`), and a link can be removed (create means delete).

**How it runs.** Nightly at 2am Brisbane (`SafeMinistrySchedule`), once on start if a night was missed, and when a
Safeguarding Manager presses **Sync now**: all outbound, so no new inbound path. A Postgres advisory lock keeps one sync
at a time across API instances; requests are spaced 250 ms apart and a 429 is retried after its `Retry-After`. A failed
sync changes nothing and the page says why. Everyone's name, email and birth date from Safe Ministry Check are kept in
`people.smc_people` (to match and to list the unmatched), dropped when they leave Safe Ministry Check unless linked.

**Defaults (Claude's, the owner can change them):**

- **Synced fields can't be edited** in the app (the tab shows them read-only and points to Safe Ministry Check; the
  server refuses a hand save). Hand entry stays for anyone not linked.
- **Unlinking clears** the synced blue card and training, and that record **isn't matched automatically again**; it waits
  under *Still to match* ("Unlinked by staff") until someone links it by hand. Linking by hand reads that one record
  straight away.
- **Ambiguity is left for staff:** an email two people in the app share, a name and birth date two people share, or two
  records that match the same person are all listed as still to match, never guessed.
- **Someone who leaves Safe Ministry Check** keeps their link and last values, flagged "they're no longer in Safe
  Ministry Check".
- **What counts as a problem** (calibrated 2026-11-10 against the church's 117 records, counted in memory, nothing
  kept; the owner confirmed the first rules flagged the owner's own cleared record wrongly): an overall `status` other than
  *Cleared* (they're *Cleared*, *Pending* or *Error*; *Pending* says why in its description, which is shown); a
  `wwcc_status` it couldn't map (*NOTFOUND*: the card couldn't be verified; *CLEARED* maps to Current, *EXPIRED* to
  Expired); a police check status other than cleared or not required; a referee answer other than *positive* or
  *positive_admin*; a disclosure on someone not yet cleared; working with children with no blue card there. **Overdue
  training is flagged, but less severely** (owner, 2026-11-10: "overdue training should technically still be flagged
  regardless. but it is less severe"): *Pending* while their training is overdue or unfinished is left to the training
  flag, and "Check Safe Ministry Check" sorts above run-out, due-soon and not-recorded flags on Safeguarding. **Not
  problems:** `wwcc_status_description` ("WWCC Status should be verified again" is on most cleared people), a *Pending*
  blue card photo (also common among cleared people), `paperwork_process_status` (*4* is "Linked to portal").
- **The person doesn't see the flag or the status** on their Account page, only their card and training and that they
  came from Safe Ministry Check; following up is the office's.

**Configuration.** `People:SafeMinistryCheck:Subdomain` and `People:SafeMinistryCheck:ApiKey` are secrets: never in the
repo, chat or logs (the HTTP client logs nothing, as the address names the church's subdomain). Without both, the page
says *Not connected* and nothing runs. **Development:** the AppHost's user-secrets, which it passes to the API:

```bash
dotnet user-secrets --id gsbc-churchapp-apphost set "People:SafeMinistryCheck:Subdomain" "<subdomain>"
dotnet user-secrets --id gsbc-churchapp-apphost set "People:SafeMinistryCheck:ApiKey" "<key>"
```

**UAT and production:** a cluster secret through `gsbc.argo`, as `People__SafeMinistryCheck__Subdomain` and
`People__SafeMinistryCheck__ApiKey`, given to the API as `envFrom` with `optional: true`.

## Deleting

Through `IPersonData` when an account is deleted:

- Their birth date, links (as parent and as child), asks and safeguarding row go, and every *Parent or guardian*
  role at them.
- **A child who can't sign in and has no other parent goes too** (*default*): nobody would be left to look after their
  details, and the church shouldn't keep a minor's record with no guardian. The deletion sheet says so. A child with
  their own sign-in keeps their account and shows as *No parent* on Safeguarding. A child with another parent stays.
- **Create means delete:** a parent removes a child they added who can't sign in (the account is deleted the same
  way), stops looking after one who can; the office unlinks anything; a blue card or training is removed on its own.
- A parent can't unlink a child who can't sign in if they're the last parent (remove the child instead, or link
  another parent first).

## Screens

- **App and admin Account page** (`AccountPeople`, an `AccountSection` after *You*): *Your family* (age question,
  birth date, asks to answer, children with age, sign-in and how they're reached, *Add a child*), *Your parents* (for
  a young person, or anyone with parents or asks), *Blue card and training*. A child opens a sheet: sign-in code, how
  to reach them, details, another parent, remove or stop looking after.
- **Admin, a person's page:** *Family* and *Safeguarding* tabs (`PersonTab`) beside Roles and Account. Family: birth
  date, how ministries reach a young person, what's waiting, children, parents, the shared `PersonFinder` to link.
  Safeguarding: flags, the card and training as fields (read-only when they come from Safe Ministry Check, with the
  record, how it was linked, its status and Unlink), linking to a Safe Ministry Check record, a preview of the card.
- **Admin, People → Safeguarding:** the flagged list, with a line saying when Safe Ministry Check last synced.
- **Admin, People → Safe Ministry Check:** when it last synced, Sync now, and the records still to match.

## Open for the owner

- Stage 2: leaders setting families from their team and attendee lists (a leader role giving the `people.family.*`
  nodes for people in their ministry), and "this team needs a blue card" so *missing* can be flagged for people with
  nothing recorded.
- The Simulator can't make a passkey against a local backend, so a child signing in with a code was checked up to the
  passkey (the code is accepted and a ticket issued); it wants a try on a real phone on dev.

Tests: `AgeTests`, `SafeguardingFlagTests`, `FamilyRulesTests`, `ReachTests`, `FamilyDeletionTests`, `SmcValuesTests`,
`SmcMatchingTests`, `SafeMinistryClientTests`, `SafeMinistryScheduleTests`, `SafeMinistrySyncTests` (People) and
`FamilySignInTests` (Accounts).
