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

## Open

- A private sync feed for your own grants, so permission-gated tools work offline in the app: with the
  first phone feature that needs it.
