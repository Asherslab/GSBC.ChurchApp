# Access

Who may do what: roles, assignments, the permission catalogue, and the evaluator every gRPC method's
`[Requires]` uses. The model is in [permissions.md](../architecture/permissions.md); this is how it's
built.

## What people can do (v1)

| Who | Can |
|---|---|
| Anyone signed in | See their own permissions (the app and admin panel use them to show or hide tools) |
| `access.view` | See roles, the permission catalogue, and who holds which role |
| `access.roles.edit` | Create, change and delete custom roles (system roles are locked) |
| `access.assignments.edit` (at a scope) | Give or take away roles at that scope: globally, or within one ministry |

## Roles

- **System roles** are declared in code by features and reconciled into the database at start-up;
  they can be assigned but not edited. Access declares **Owner** (`*`), **Account manager**
  (`accounts.*`) and **Access manager** (`access.*`). Ministries will declare its own.
- **Custom roles** are made in the admin panel: a name, a description, the scope kind they're assigned
  at, patterns (`ministries.news.*`, `-ministries.roster.edit`) and up to one level of included roles.
- The permission catalogue is every node the running features declare, reconciled at start-up, so the
  role editor can list and explain them. Unknown patterns are allowed (a feature may come later) but
  flagged.

## Assignments

User + role + scope object (for a scoped role) + optional end date + source (`manual`, `bootstrap`,
later `derived`). The last remaining Owner assignment can't be removed, so the church can't lock itself
out.

**The first owner** is made once per environment with `tools/access/make-owner.sh <email>` after that
person has signed up. It's an operator step against the database, not an API, so nothing in the app
can grant Owner to itself.

## Checking

- `AccessEvaluator` (the API's `IAccessEvaluator`) compiles a user's grants (assignments → roles →
  patterns, includes expanded, expired assignments dropped) and caches them per user. Any change to
  roles or assignments sends a Postgres `NOTIFY`, and every API instance drops its cache.
- The phone and the admin panel get their grants from `Access.GetMine` after signing in, and the
  shared `IAccess` / `<Can>` use them to show tools. The server checks every call anyway.
- Navigation entries can name a permission, and are hidden from people who don't hold it.

## Sensitive permissions

Permission nodes can be marked sensitive (`accounts.manage`, `access.*`). People who hold any of them
can't recover their account by email; a staff member issues them a recovery code instead
([identity.md](../architecture/identity.md)).

## Admin screens

- **Roles:** the list, and an editor for custom roles with the catalogue beside it.
- **People** (Accounts): find someone, invite someone, give a recovery code, disable an account, see the
  audit log; and (Access) their roles: assign one, end one.

The two features meet through a small `IPeopleDirectory` in Platform.Client, implemented by Accounts,
so Access can show names without referencing Accounts.

## Status (2026-11-01)

**Built:** system roles and the catalogue reconciled at start-up, custom roles, assignments with
scopes and end dates, the cached evaluator behind every `[Requires]`, the escalation and last-owner
guards, grants loaded into the admin panel's `IAccess` (navigation and `<Can>`), the Roles and
Who-holds-what pages, and the bootstrap script. Tested by `Features.Access.Api.Tests` (Postgres)
and checked end to end through `dev.baptist.com.au`: a new member holds nothing and every staff
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

Roles are jobs people already know. Features declare them in code (system roles), and they double as
the **templates** for anything custom.

| Role | Given | Declared by | Can |
|---|---|---|---|
| **Member** | per ministry | Ministries | Read the ministry's members-only side, chats and roster; reply to their roster |
| **Leader** | per ministry, applies to its teams too | Ministries | Everything a Member can, plus run the ministry: post, start chats, roster, settings, add and remove members. Shown as a leader on the ministry's About |
| **Admin** | per ministry, applies to its teams too | Ministries | Exactly what a Leader can, without the title: not shown as a leader. For the person who does a ministry's tech (owner, 2026-11-02) |
| **Staff** | everywhere | Ministries | Everything a Leader can, in every ministry, plus message all serving teams |
| **Account manager** | everywhere | Accounts | Invite people, help them back in, disable accounts, read the audit log |
| **Access manager** | everywhere | Access | Manage roles and who holds them |
| **Owner** | everywhere | Access | Everything |

**Applies to its teams too** is a property of the role: a Leader of Music is a Leader of Choir, Vocal
Groups, Song Leading and Production. A Member of Music is not a Member of Choir. (A Choir member
reading Music's members-only items is a Ministries feed rule, not a grant; see
[ministries.md](ministries.md#the-model).)

**Custom roles: copy a template, Access managers only** (owner, 2026-11-02). There's no blank role.
An Access manager opens a role and chooses **Make a copy**, renames it, ticks abilities on or off by
area, and saves after the preview. Copies store the abilities by name, never wildcards, so a feature
added later never widens a custom role without someone choosing it. The raw editor (patterns,
includes, the catalogue) stays as a developer's tool at `/access/roles/advanced`, not in the
navigation, needing `access.roles.edit`.

### Who can give which role: "assignable by"

Every role lists the roles whose holders may give and take it away. Someone may give role R in place
P when either:

- they hold `access.assignments.edit` in P (Access managers and the Owner hold it everywhere); or
- they hold, in P or everywhere, one of R's **assignable-by** roles (counting a role on a parent
  ministry that applies to its teams);

and, in both cases, **the escalation guard**: they hold every ability R gives, in P, themselves. Taking
a role away follows the same rule, plus the last-Owner guard.

| Role | Assignable by (owner, 2026-11-02) |
|---|---|
| Member | Leader, Admin (of that ministry or its parent), Staff |
| Leader, Admin | Staff |
| Staff, Account manager, Access manager, Owner | Only through `access.assignments.edit`, and the escalation guard means the giver must hold everything the role gives: in practice the Owner |
| A custom role | Chosen in the editor; by default, only people with `access.assignments.edit` |

Leaders add members; only staff make Leaders or Admins. Admins are staff-made too: an Admin can do
everything a Leader can, so a Leader making Admins would be a Leader making co-leaders.

### Where people are given roles

- **A ministry's team list** (Ministries, in the app and the admin panel alike): **Add to team**
  (Member), **Remove from team**, and for staff **Make a leader**, **Make an admin**, **Stop being a
  leader**. Accepting a join request and an Open ministry's **Join** give Member too. Membership lives
  in the app, for every ministry (owner, 2026-11-02): Access assignments are the team list.
- **A person** (Access → People, staff only): what they can do, and give or take away any role you're
  allowed to.
- The Roles screen never assigns; it shows who holds a role and links to them.

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

### Preview before saving

Saving a role, or deleting one, first shows its effect: "Saving changes what 4 people can do. They
gain: Post news (in their ministry). They lose: Accept join requests." Giving a role shows what it will
let that person do in that place before the button: "Sam will be able to: …".

### Guard rails, in words

| When | The message |
|---|---|
| A Leader tries to make a Leader | "Only staff can make someone a leader. Ask the church office." |
| The escalation guard | "You can't give Access manager, because it lets people manage roles and you can't do that yourself." |
| A custom role with an ability you lack | The tick is greyed: "You can't do this yourself, so you can't give it." |
| Taking away the last Owner | "This is the church's last Owner. Make someone else an Owner first." |
| Changing a system role | "Built-in roles come from the app. Make a copy to change it." |
| A person has no roles | "Nothing beyond what everyone can do." |

### Scope hierarchy

One level, as in Ministries (Music over Choir). A role that applies to its teams is compiled into a
grant at the ministry and at each of its child ministries, so the server, the phone and the admin
panel check with the same `GrantSet` and no extra lookups. Ministries tells Access the children of a
ministry through `IScopeHierarchy` (Platform.Api); until it exists, there are none. When a ministry's
parent changes, Ministries announces it and every grant cache drops.

### What Ministries must call

Ministries is the first feature whose people give roles. Built in this pass for it to use:

- **Its system roles**, declared with `AddSystemRole`: Member, Leader, Admin, Staff, each with its
  assignable-by keys and whether it applies to child ministries.
- **`IRoleAssignments`** (Platform.Api, implemented by Access), called in-process from Ministries'
  own gRPC methods (`IMinistryMembershipService`):
  - `GiveAsync(actor, user, roleKey, ministryId)` and `TakeAwayAsync(...)`, which apply the rules
    above and return a refusal in words instead of throwing; the team list's buttons call these.
  - `GiveAsFeatureAsync` / `TakeAwayAsFeatureAsync`, for Ministries acting on its own rules: Join on
    an Open ministry, accepting a request, Leave. The caller has already checked who may do it.
  - `HoldersAsync(roleKey, ministryId)`, for the team list and the About's leaders.
- **`IScopeHierarchy`**, implemented by Ministries (the children of a ministry), and an announcement
  when it changes.
- **Finding someone to add** to a team shouldn't need the global directory (`accounts.view` is
  sensitive): a Ministries method that, for someone who may give Member there, finds people by name
  and returns names with a masked email to tell two Sams apart. It needs an in-process people lookup
  from Accounts, the server-side twin of `IPeopleDirectory`.
- **Ministry names** for "In Children's Ministry": Ministries implements `IScopeNames` in
  Platform.Client; until then the place shows the ministry's id.

## Open

- A private sync feed for your own grants, so permission-gated tools work offline in the app: with the
  first phone feature that needs it.
- Whether a Leader should be able to give their ministry an Admin after all (the owner chose staff
  only, 2026-11-02); revisit after the first ministries use it.
