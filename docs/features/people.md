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
| Staff with *Safeguarding Manager* | All of that, and link and unlink families, add a child for a parent, take back asks, make sign-in codes, set how a young person is reached, record and remove blue cards and training |

## How it's modelled

- **A child without a phone is an account without sign-in** (*default*): a row in `accounts.users` with no email and no
  passkey, `added_by_family` set. So a child has one id everywhere: Access can give them roles (a ministry's attendee
  in Stage 2), names show in history, the people picker finds them, and deleting works like any account. People
  makes and removes them through `IPeopleAccounts` (Platform.Api, implemented by Accounts); features never reference
  each other. The abandoned-sign-up clean-up skips them.
- **A sign-in code** (`Accounts`' `FamilySignIn` code: 8 letters and digits as `ABCD-EFGH`, one use, 24 hours, a new
  one replaces the last) is redeemed through the same `Accounts.Redeem` an invitation uses, then the child's phone
  makes a passkey for the account the parent made, already linked and aged. Signing in has *Got a code from your
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
is given**, owner, 2026-11-10). The period and the due dates are **placeholders until they come from Safe Ministry
Check**, which will decide them. **Entered by hand** by Safeguarding Managers on the person's Safeguarding tab, shown to
the person on their Account page (the card drawn as a card), and **flagged, never blocking**
(`SafeguardingFlags`):

- *Run out*: a card past its expiry or with a status other than Current or Exemption; training past its due date.
- *Due soon*: within 60 days (*default*).
- *Not recorded*: a card but no training, or training but no card. Someone with neither isn't flagged: Stage 2 adds
  "serves on a team that needs one".
- *No parent*: under 18 with no parent linked (with when they asked, if they have).

**Safeguarding** (People → Safeguarding, Safeguarding Viewer) lists everyone flagged, run out first, then by date;
chips filter by kind and count each. Anything run out and every young person without a parent also shows in Home's
*Needs you*.

**Shaped for an import later** (*default*): each row has a `source` (`hand`, `safe-ministry-check`, `elvanto`) and an
`external_id`, indexed together, so an import can fill and update rows it owns; saving by hand marks a row `hand`.
Safe Ministry Check has no published API yet (it runs an API-key MCP server at mcp.safeministrycheck.com.au and syncs
these fields into Elvanto); nothing is integrated.

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
  Safeguarding: flags, the card and training as fields, a preview of the card as the person sees it.
- **Admin, People → Safeguarding:** the flagged list.

## Open for the owner

- Stage 2: leaders setting families from their team and attendee lists (a leader role giving the `people.family.*`
  nodes for people in their ministry), and "this team needs a blue card" so *missing* can be flagged for people with
  nothing recorded.
- Getting blue card and training data from Safe Ministry Check (ask them for access) or Elvanto.
- The Simulator can't make a passkey against a local backend, so a child signing in with a code was checked up to the
  passkey (the code is accepted and a ticket issued); it wants a try on a real phone on dev.

Tests: `AgeTests`, `SafeguardingFlagTests`, `FamilyRulesTests`, `ReachTests`, `FamilyDeletionTests` (People) and
`FamilySignInTests` (Accounts).
