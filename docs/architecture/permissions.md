# Permissions

Decided 2026-11-01: evolve the owner's earlier permissions model rather than port it. Its core idea stays:
permissions are defined in code, roles can be scoped to one object, and an assignment carries that
object's id. What changes is listed at the end.

## The model

**Permission node.** A dot path a feature declares in its Contracts, with a description and the kind
of scope it applies to:

An example (Ministries isn't built; the real ones are in each feature's `*Permissions` class):

```csharp
public static class MinistryPermissions
{
    public static readonly Permission PostNews =
        new("ministries.news.post", ScopeKind.Ministry, "Post news to a ministry");
}
```

Nodes are typed objects, not strings with placeholders, so the phone can check them with no
reflection, regex or template rendering (AOT-safe). On start-up the API reconciles every registered
node into the database, so the admin panel can list and describe them.

**Scope.** Either global, or one object: `ministry:42`. Life Groups are a kind of ministry
([ministries decisions](../features/ministries.md)), so one scope kind covers both. A grant at
global covers every object of that kind. New scope kinds are added in Platform.Permissions when a
feature needs one.

**Role.** A named set of patterns, with the scope kind it is assigned at:

| Pattern | Means |
|---|---|
| `ministries.news.post` | exactly this node |
| `ministries.news.*` | every node under `ministries.news` |
| `*` | everything (the owner role only) |
| `-ministries.roster.edit` | deny this node, even if another pattern allows it |

The most specific matching pattern wins; when two are equally specific, deny wins. This is the old
Minecraft permissions plugins' rule, and it means a role can be "everything in Ministries except
rosters" in two lines. A role may include other roles, one level deep, so "Ministry leader" can be
"Ministry member" plus more.

**Roles are data** (requirement G). Holders of `access.roles.edit` create and change roles in the
admin panel. Features also seed **system roles** in code (today Owner, Access manager, Account manager, App editor,
Media editor, Announcer, Insights viewer, Developer, Draft reviewer and App reviewer; Staff and the
ministry roles come with Ministries), reconciled on start-up, as the earlier model's role creator did; they can be assigned but not edited, so
code can rely on them. A role also says **who can give it** (assignable by: Leaders give Member) and
whether it **applies to a ministry's teams**; staff see all of it in plain words
([access.md](../features/access.md#how-staff-see-access-agreed-2026-11-02)).

**Assignment.** User + role + scope object (for a scoped role), with an optional end date (a Life
Group round) and a source:

- **manual:** someone with `access.assignments.edit` at that scope made it;
- **derived:** a feature made it from something else (joining a ministry's team makes you its member),
  and that feature owns it. Elvanto team membership can feed this later.

**Holding a role** (2026-11-04). Compiling someone's grants also gives them a global
`roles.held.<role id>` grant for each role they hold and each role it includes (`RoleMembership`), so
"people in a role" can be an audience checked like any permission (Home's tiles use it). The node isn't
in the catalog, so it never shows as an ability; `*` matches it, so the owner is in every role's audience.

**Sensitive oversight nodes** (2026-11-04). Where a feature's records have their own audience (a
chat's files are for that chat), a separate global node may let chosen staff see them all for
oversight, held by no role but the Owner's by default: `media.system.view` sees inside every system
folder in the media library ([media.md](../features/media.md), *System folders*). It is marked
sensitive, and a system folder's own audience is still checked through `IAccessEvaluator` like any
grant.

## Checking, on the server

- **Tokens carry who, not what.** The API token holds the user id and session id. Permissions are
  never rendered into claims, so tokens stay small and a revoked role stops working on the next call,
  not when a token expires.
- **Effective grants are compiled once per user** (patterns resolved per scope, includes expanded),
  cached in each API instance, and dropped when Postgres `NOTIFY` reports a role or assignment change.
  This is the same `LISTEN` connection Sync uses (SPEC §5).
- **Every gRPC method declares its tier**, checked by a server interceptor that sees the deserialised
  request:

  | Attribute | Allows |
  |---|---|
  | `[Public]` | anyone; rate-limited |
  | `[AppInstall]` | a genuine installed app (App Attest; SPEC §6) |
  | `[SignedIn]` | any signed-in user; the method checks anything finer itself |
  | `[Requires("ministries.news.post")]` | users holding the node at the request's scope |

  For a scoped node the request message implements `IScopedRequest`, so the interceptor knows which
  object is being acted on. `Architecture.Tests` (`TierRules`) fails on a method with no tier, more than one, or a
  node no feature declares. With no attribute the default is deny (SPEC §6: fail closed).

## Checking, on the phone and in the browser

- `Access.GetMine` returns the signed-in user's compiled grants. They also arrive as a private sync
  feed, so permission-gated UI works offline.
- `IAccess.Can(MinistryPermissions.PostNews, ministryId)` uses the same matcher as the server
  (Platform.Permissions is AOT and shared). This is how management UI appears in the app for a leader
  and in the admin panel for staff, from one component (requirement A).

## Feeds respect permissions

A feed declares its audience: public, signed in, or a scope plus a node (a ministry's private news
needs `ministries.private.view` at that ministry). Sync only sends what the caller may see. When a
user's grants change, the feeds whose audience depends on them reset for that user on the next sync
(built 2026-11-02: Sync keeps a fingerprint per session and feed in `sync.audience_marks`),
so the phone never keeps rows it is no longer allowed to read.

## What changed from the earlier model, and why

| Earlier model | Here | Why |
|---|---|---|
| Templated permission strings, rendered at run time for each object | Typed node + scope kind | AOT on the phone; no template rendering or regex |
| Rendered permissions in role claims | Grants cached server-side, keyed by user | Small tokens; revocation is immediate |
| Roles only in code | System roles in code, custom roles as data | Staff shape roles without a release (requirement G) |
| Exact match, all key subsets tried (2ⁿ renders) | Wildcards, negation, most-specific-wins | Fewer, clearer roles; one lookup per check |
| Multi-slot objects (an object and its owner) | One scope object | Church data nests simply; add a scope kind if that changes |
| `[Authorize(Policy = ":permission:…")]` before the body is read | Interceptor after deserialising | The scope is in the request |

## Open

- Whether role includes need more than one level. Start with one.
- Elvanto-derived assignments: shape them when rosters are built.
