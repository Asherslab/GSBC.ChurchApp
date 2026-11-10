# Access

Who may do what: roles, assignments, the permission catalogue, and the evaluator every gRPC method's
`[Requires]` uses. The model is in [permissions.md](../architecture/permissions.md); this is how it's
built.

## What people can do (v1)

| Who | Can |
|---|---|
| Anyone signed in | See their own permissions (the app and admin panel use them to show or hide tools) |
| `access.view` (Roles Manager, Owner) | See everyone's roles and what each person can do |
| `access.assignments.edit` (at a scope) | Give or take away any role at that scope whose abilities they have themselves |
| A role's **assignable-by** roles (at a scope) | Give or take away that role at that scope ([below](#who-can-give-which-role-assignable-by)): a calendar's Editors add its Editors |

Nobody makes or changes roles any more: they're all built in (owner, 2026-11-09).

## Roles by feature, with levels (owner, 2026-11-09)

The owner: roles "feel painful to use … role names just don't feel like they're easy to understand. Split
roles more or less by feature, with levels of what most people will need." Decided with the owner the same
day (mocks and review in `.impeccable/mocks/decision/roles/`):

- **Built in only.** Features declare every role in code (`SystemRole`); staff can't make or change one,
  and `access.roles.edit` is gone. Roles made here before were removed on the first start after this
  (`AccessReconciler.RemoveMadeHereRolesAsync`), unless something was still aimed at one (below).
- **Named *Feature Level*, ticked independently.** Each role names its feature (`SystemRole.Feature`,
  the group heading, in `RoleFeatures`) and is listed in declaration order. A person can hold any
  combination; a higher level already includes the lower one's abilities.
- **A role brings what its job needs from elsewhere** through a **kit** the owning feature declares
  (`AddRoleKit`): Media's `pictures` (see the library, upload) and People's `people-names` (see names and
  emails). Kit patterns are stored as *everywhere* patterns (`role_patterns.everywhere`), so a role given
  for one calendar still lets its holder pick a picture. The escalation guard counts them: you can't give
  a role whose kit you don't hold.
- **Bookkeeping roles are hidden** (`SystemRole.Hidden`): Sermon follower, Series follower, Calendar
  added, Calendar turned off and App reviewer. The app gives them; they never show on People, in "what can
  they do", in the audience picker, and `Assign` refuses them.
- **No Roles page, no Who can do what.** Global roles are given from the person; roles for one thing
  from the thing ([Admin screens](#admin-screens)).

| Feature | Role | Gives | Brings |
|---|---|---|---|
| Sermons | Sermons Uploader | `sermons.upload` | pictures |
| | Sermons Editor | `sermons.edit`, `sermons.upload` | pictures |
| Calendar | Calendar Editor | `events.edit` in every calendar | pictures |
| | Calendar Publisher | `events.edit`, `events.publish` everywhere | pictures |
| | Calendar Manager | `events.calendars.manage`, edit and publish everywhere | pictures |
| | Digest Maker | `events.digest` | |
| Announcements | Announcements Writer | `notifications.announcements.write` | |
| | Announcements Sender | write and `send` | |
| App | App Editor | `config.{hub,appearance,tabs}.edit`, `config.drafts.preview` everywhere | pictures |
| | App Publisher | `config.*` | pictures |
| Media | Media Viewer | `media.view` | |
| | Media Editor | view, upload, organise, delete | |
| | Media Manager | Media Editor and `media.fonts.upload` | |
| People | People Viewer | `accounts.view` | |
| | People Manager | `accounts.view`, `accounts.manage` | |
| | Roles Manager | `access.view`, `access.assignments.edit` | people-names |
| | App Review Manager | `accounts.review.manage` | |
| | Safeguarding Viewer | `people.family.view`, `people.safeguarding.view` | people-names |
| | Safeguarding Manager | `people.*` except `people.family.act` | people-names |
| Insights and health | Insights Viewer | `analytics.view` | |
| | Developer | `analytics.health.view`, `accounts.tools.connect` | |
| Everything | Owner | `*` | |

**Roles for one thing** have no feature; they're given on the thing: a calendar's **Editor** (assignable by
its Editors, Calendar Editors, Publishers and Managers; brings pictures) and **Publisher** (by Calendar
Publishers and Managers; brings pictures), a regular event's **Organiser** (by Calendar Editors, Publishers
and Managers), and a draft's **Reviewer** (given by Configuration when a draft is shared). Ministries'
Member, Leader and Admin will be the same.

**Nobody gains or loses access by accident** (the mapping, 2026-11-09). A role renamed in code keeps its
record (`SystemRole.Replaces`), so its id, its holders and every Home tile, tab, calendar or announcement
aimed at "people in this role" carry across; a role split off another starts with its holders, once, when
it's first made (`StartsWithHoldersOf`).

| Before | After |
|---|---|
| Access manager (`access.*`, `accounts.view`) | Roles Manager: loses making roles, which no one can do now |
| Account manager | People Manager (same record), and the new App Review Manager for the same people |
| Announcer | Announcements Sender |
| App editor (`config.*`) | App Publisher, and gains pictures |
| Calendars manager | Calendar Manager, and gains pictures |
| Calendar editor, Calendar publisher (one calendar) | Editor, Publisher of that calendar, and gain pictures |
| Event organiser, Draft reviewer | Organiser, Reviewer |
| Media editor, Insights viewer, Developer, Digest maker, Owner | the same, renamed to the new capitals |
| Made here | removed |

**A role nothing can be aimed at once it's gone** (the owner, 2026-11-09, through the main station): a
feature that aims things at "people in this role" implements `IRoleUses` (Platform.Api) and says what still
is: Configuration (Home tiles, tabs, colours), Events (calendars) and Notifications (announcements not
yet sent). A made-here role in use is kept, and the reconciler logs what to change, rather than leaving
its tile or announcement reaching only the Owners.

## Assignments

User + role + scope object (for a scoped role) + optional end date + source (`manual`, `bootstrap`,
or `derived` when a feature gives it on its own rules, such as Join through `GiveAsFeatureAsync`). The last remaining Owner assignment can't be removed, so the church can't lock itself
out.

**The first owner** is made once per environment with `tools/access/make-owner.sh <email>` after that
person has signed up. It's an operator step against the database, not an API, so nothing in the app
can grant Owner to itself.

## Checking

- `AccessEvaluator` (the API's `IAccessEvaluator`) compiles a user's grants (assignments → roles →
  patterns, includes expanded, expired assignments dropped) and caches them per user. Any change to
  roles or assignments sends a Postgres `NOTIFY`, and every API instance drops its cache.
- The admin panel gets its grants from `Access.GetMine` after signing in. The phone gets them from
  the private **`access.grants` feed** (built 2026-11-03): Access compiles each person's grants and
  publishes a row for them whenever roles or assignments change (`GrantsFeed`, through
  `AccessChanges`), and every 5 minutes for assignments that expire. The feed's audience is
  *own records*, so Sync sends each person only their own row and a signed-out phone nothing. So the
  phone knows its permissions offline, and a role given in the admin panel shows its tools on the
  phone within a second. The shared `IAccess` / `<Can>` use them either way; the server checks every
  call anyway. Tested in `Features.Access.Api.Tests`, and proven on the owner's iPhone (a role given
  in the admin panel showed its tool on the phone live).
- Navigation entries can name a permission, and are hidden from people who don't hold it.

## Sensitive permissions

Permission nodes can be marked sensitive (`accounts.manage`, `access.*`). People who hold any of them
can't recover their account by email; a staff member issues them a recovery code instead
([identity.md](../architecture/identity.md)).

**A connected tool never uses a sensitive permission,** even when its account holds it
([identity.md](../architecture/identity.md), *Connected tools*). The API token says the session is a tool,
and the evaluator adds a deny for every sensitive node to the grants it hands out for that caller in that
request, so `[Requires]`, sync audiences and every feature reading `GrantsForAsync` refuse alike.
`AssignmentRules` also refuses a tool outright, since an assigner role isn't a permission.

## Admin screens

- **A person** (`/people/{id}`, People's page, Accounts): Roles, What they can do, and Account tabs.
  Access contributes its two tabs through `PersonTab` (Platform.Client), so the features stay apart.
  **Roles** lists every role as a tick, grouped by feature in two columns; a tick the viewer can't give is
  greyed with the reason. The (i) beside each fills the sand panel beside the list: what it lets them do,
  then "Also, from Media, because the job needs it", then who gave it and when. Under the ticks, *For one
  thing* lists their roles on single things ("Editor and Publisher of the Kids calendar") with a link
  there. **What they can do** is the explanation by place, with the role that gives each.
- **People on a thing**: the shared `PeopleRow` and `PeopleSheet` (Platform.Ui) over `IPlacePeople`
  (Platform.Client). A row names who's on it and opens the sheet: search to add someone (as the thing's
  first role you may give), each person's roles for that thing as ticks with the same (i), Take off.
  Access implements it for any role kind (`IPlacePeopleSource.For(kind, id)`), using `Access.ListPlace`
  and `Access.FindPeople` (both signed in, for people who may give a role there or hold `access.view`;
  found people show a masked email, or the full one to people who can see People, through Accounts' `people-names` kit). Built on a calendar's settings, a regular event's *Organisers*, and
  a draft's Share (Configuration's own adapter, so sharing still notifies).
- **Roles: developer tool** (`/access/roles/advanced`, not in the navigation, `access.view`): every role
  with its raw patterns (everywhere ones marked), its feature and who gives it, and the catalogue. Read only.

## Status (2026-11-09)

**Built 2026-11-09 (roles, redone):** roles by feature with levels, kits, hidden bookkeeping roles, built in
only; renames and splits carry holders across; made-here roles removed unless aimed at (`IRoleUses`); the
person page's Roles and What they can do tabs; the shared People row and sheet on a calendar, a regular
event's organisers and a draft's Share. The Roles page, the role editor and Who can do what are gone.
Tested in `Features.Access.Api.Tests` (a kit given everywhere from a role for one thing, the guard
counting a kit, a renamed role keeping its holders, a split role starting with its holders once, a
made-here role kept while aimed at) and checked in the admin panel against a copy of the dev database.


**Built 2026-11-02 (the design below, minus Ministries itself):** plain-language descriptions and
areas in the catalogue (`Permission.Area`), assignable-by and applies-to-children on roles
(`SystemRole.AssignableBy`, `AppliesToChildren`, migration `RoleAssignersAndTeams`), the assigning
rules with refusals in words (`AssignmentRules`), grants compiled onto child ministries through
`IScopeHierarchy`, `Access.Explain` ("what can this person do?") and `Access.PreviewRole`, the
in-process `IRoleAssignments` for Ministries, and the new Roles, role, copy-editor and Who-can-do-what
screens. Tested in `Features.Access.Api.Tests`: leaders add members but only staff make leaders, the
escalation guard's words, a leader covering a ministry's teams, the preview and the explanation. The
screens have since been redesigned and checked in the admin panel on `dev-app.baptist.com.au`
(2026-11-03).

**Built 2026-11-01:** system roles and the catalogue reconciled at start-up, custom roles, assignments with
scopes and end dates, the cached evaluator behind every `[Requires]`, the escalation and last-owner
guards, grants loaded into the admin panel's `IAccess` (navigation and `<Can>`), the Roles and
Who-holds-what pages, and the bootstrap script. Tested by `Features.Access.Api.Tests` (Postgres)
and checked end to end through `dev-app.baptist.com.au`: a new member holds nothing and every staff
method refuses them.

## Usability comes first (owner, 2026-11-01)

The people managing access are church staff and volunteers, not administrators. The owner's earlier
system never let users make custom roles for exactly this reason. Custom roles can stay, but only
behind a good experience. The first Roles page (a pattern textarea and the raw catalogue) was a
developer's tool, not that experience. The design below (agreed with the owner, 2026-11-02) replaces
it.

## How staff see access (agreed 2026-11-02)

### The words on screen

Nothing on a staff screen shows a node, a pattern, a wildcard or a deny. What's underneath is
translated like this:

| Underneath | On screen |
|---|---|
| A permission node (`ministries.news.post`) | An **ability**, written as what someone can do: "Post news" |
| The node's feature or area | A heading the abilities are grouped under: News, Roster, Team, People, Access |
| A scope (`global`, `ministry:kids`) | A **place**: "Everywhere", "In Children's Ministry", "In Music and its teams" |
| A role | A **role** with a plain name and a one-line description: Leader, "Runs a ministry and is shown as its leader" |
| An assignment | **Give** a role, **take away** a role, "until 21 Dec" for an end date |
| A wildcard role (`ministries.news.*`, `*`) | The abilities it covers today, listed one by one ("Everything" for the Owner) |
| A sensitive node | "Needs extra care: people with this can't reset their sign-in by email" |
| The escalation guard, assignable-by, the last-Owner guard | A sentence saying why, and who to ask (below) |

Each feature declares its abilities' descriptions in those words: a short phrase that starts with a
verb and fits after "Can" ("Post news", "Accept join requests", "See everyone's name and email"). A
permission may name its area; when it doesn't, the area is its feature (the node's first part).

### Roles

Roles are jobs people already know, declared in code by features. The staff roles are now by feature
([above](#roles-by-feature-with-levels-owner-2026-11-09)); the ministry roles below are still to come.

| Role | Given | Declared by | Can |
|---|---|---|---|
| **Member** | per ministry | Ministries | Read the ministry's members-only side, chats and roster; reply to their roster |
| **Leader** | per ministry, applies to its teams too | Ministries | Everything a Member can, plus run the ministry: post, start chats, roster, settings, add and remove members. Shown as a leader on the ministry's About |
| **Admin** | per ministry, applies to its teams too | Ministries | Exactly what a Leader can, without the title: not shown as a leader. For the person who does a ministry's tech (owner, 2026-11-02) |
| **Staff** | everywhere | Ministries | Everything a Leader can, in every ministry, plus message all serving teams |
| **Life Groups planner** | everywhere | Ministries | Plan the Life Groups' sets of Sundays, their time and when they show (`ministries.lifegroups.plan`; Staff have it too). Added 2026-11-10 |
| **Owner** | everywhere | Access | Everything |

**Applies to its teams too** is a property of the role: a Leader of Music is a Leader of Choir, Vocal
Groups, Song Leading and Production. A Member of Music is not a Member of Choir. (A Choir member
reading Music's members-only items is a Ministries feed rule, not a grant; see
[ministries.md](ministries.md#the-model).)

~~Custom roles: copy a template~~ (owner, 2026-11-02): replaced 2026-11-09 by built-in roles only.

### Who can give which role: "assignable by"

Every role lists the roles whose holders may give and take it away. Someone may give role R in place
P when either:

- they hold `access.assignments.edit` in P (Roles Managers and the Owner hold it everywhere); or
- they hold, in P or everywhere, one of R's **assignable-by** roles (counting a role on a parent
  ministry that applies to its teams);

and, in both cases, **the escalation guard**: they hold every ability R gives, in P, themselves. Taking
a role away follows the same rule, plus the last-Owner guard.

| Role | Assignable by (owner, 2026-11-02) |
|---|---|
| Member | Leader, Admin (of that ministry or its parent), Staff |
| Leader, Admin | Staff |
| A calendar's Editor | Its Editors, Calendar Editors, Calendar Publishers, Calendar Managers (2026-11-09) |
| A calendar's Publisher | Calendar Publishers, Calendar Managers |
| A regular event's Organiser | Calendar Editors, Calendar Publishers, Calendar Managers |
| Every role by feature, Staff, Owner | Only through `access.assignments.edit` (Roles Managers, Owners), and the escalation guard: they must hold everything the role gives, its kit included |

Leaders add members; only staff make Leaders or Admins. Admins are staff-made too: an Admin can do
everything a Leader can, so a Leader making Admins would be a Leader making co-leaders.

### Where people are given roles

- **A ministry's team list** (Ministries, in the app and the admin panel alike): **Add to team**
  (Member), **Remove from team**, and for staff **Make a leader**, **Make an admin**, **Stop being a
  leader**. Accepting a join request and an Open ministry's **Join** give Member too. Membership lives
  in the app, for every ministry (owner, 2026-11-02): Access assignments are the team list.
- **A person** (People → a person → Roles, staff only): tick or untick any role you're allowed to give.
- **The thing itself**, through its People button: a calendar, a regular event's organisers, a draft's
  reviewers.

### "What can this person do?"

Opening a person shows their abilities in sentences, grouped by place, each with the reason:

> **Everywhere** · Invite people · See everyone's name and email · *because they're an Account manager*
>
> **In Children's Ministry** · Post news · Manage the roster · Add and remove members · *because
> they're an Admin of Children's Ministry*
>
> **In Music and its teams** · … · *because they're a Leader of Music*

An end date shows as "until 21 Dec". Denied abilities simply don't appear. The same view, for yourself,
is "What can I do?" (with the first phone feature that needs it).

### Before giving a role

Every role's (i) says what it lets the person do, and what it brings from other features, before it's
ticked. (Previewing a changed role went with making roles, 2026-11-09.)

### Guard rails, in words

| When | The message |
|---|---|
| A Leader tries to make a Leader | "Only someone with the Staff role can give the Leader role. Ask one of them." |
| The escalation guard | "You can't give the Leader role, because it lets people do things you can't do yourself: Post news, …" |
| A role you can't give | The tick is greyed, and its (i) says why, in the guard's words |
| Taking away the last Owner | "This is the church's last Owner. Make someone else an Owner first." |
| Giving a hidden role by hand | "The app gives the Calendar added role by itself; it isn't given by hand." |
| A person has no roles | "Nothing beyond what everyone can do." |

### Scope hierarchy

One level, as in Ministries (Music over Choir). A role that applies to its teams is compiled into a
grant at the ministry and at each of its child ministries, so the server, the phone and the admin
panel check with the same `GrantSet` and no extra lookups. Ministries tells Access the children of a
ministry through `IScopeHierarchy` (Platform.Api); until it exists, there are none. When a ministry's
parent changes, Ministries announces it and every grant cache drops.

### What Ministries must call

Ministries is the first feature whose people give roles. Built 2026-11-02 for it to use (all in place
and tested; Ministries isn't built, so nothing calls them yet):

- **Its system roles**, declared with `AddSystemRole`: Member, Leader, Admin, Staff, each with its
  assignable-by keys and whether it applies to child ministries.
- **`IRoleAssignments`** (Platform.Api, implemented by Access), called in-process from Ministries'
  own gRPC methods (`IMinistryMembershipService`):
  - `GiveAsync(actor, user, roleKey, ministryId, expiresAt)` and `TakeAwayAsync(...)`, which apply the
    rules above and return an `AssignmentOutcome` with the refusal in words instead of throwing; the
    team list's buttons call these, and show the refusal as it is.
  - `MayGiveAsync(actor, roleKey, ministryId)`, to decide which buttons to show.
  - `GiveAsFeatureAsync` / `TakeAwayAsFeatureAsync`, for Ministries acting on its own rules: Join on
    an Open ministry, accepting a request, Leave. The caller has already checked who may do it.
  - `HoldersAsync(roleKey, ministryId)`, for the team list and the About's leaders.
- **`IScopeHierarchy`**, implemented by Ministries (`ChildrenOfAsync`, `ParentOfAsync`), registered
  to replace the platform's flat default, and `IRoleAssignments.ScopesChangedAsync` when a ministry's
  parent changes.
- **Finding someone to add** to a team shouldn't need the global directory (`accounts.view` is
  sensitive): a Ministries method that, for someone who may give Member there, finds people by name
  and returns names with a masked email to tell two Sams apart. It needs an in-process people lookup
  from Accounts, the server-side twin of `IPeopleDirectory`.
- **Ministry names** for "In Children's Ministry": Ministries registers an `IScopeNamer` for
  `ScopeKind.Ministry` in Platform.Client, as Configuration's `DraftScopeNames` does for drafts.

## Open

- Whether a Leader should be able to give their ministry an Admin after all (the owner chose staff
  only, 2026-11-02); revisit after the first ministries use it.
