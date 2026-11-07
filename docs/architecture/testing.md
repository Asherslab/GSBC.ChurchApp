# Testing

How the tests get what they need, what keeps them isolated and repeatable, and what to do when one
fails only sometimes. What a test is *for* is in AGENTS.md: tests guard rules and real behaviour, one
test per real rule, never coverage for its own sake.

## A flake is a bug

A test that passes on a rerun found a bug: in the code, in the test, or in what the test runs on. Fix
it at the cause, in the change that found it or straight after. Never retry it, skip it, raise a
timeout, or put it down to load: load only changes the timing, and a test that depends on timing is
the bug. Write down what the cause was in the commit that fixes it.

How the causes found so far were fixed is the pattern to follow:

| What failed | The cause | The fix |
|---|---|---|
| During set-up: a connection timed out, was refused ("Failed to connect to 127.0.0.1:…"), got an HTTP reply ("unknown response H"), or an S3 readiness check gave up after 100 s | Every test started its own Postgres or S3 container; with 170 of them starting and stopping at once, connections to a new one timed out or were refused, a freed port could be handed to another container, and each Media test waited about 15 s for its own SeaweedFS (5 to 13 minutes for the project) | One Postgres and one S3 server per run, a database and buckets per test |
| An architecture rule failed on files nobody committed | It walked the disk and read a SwiftPM `.build` folder | Rules read the files git tracks or would add (`RepoFiles`), and `.build/` is ignored |
| A history test picked the wrong "newest" change | Two steps ran on the real clock and usually landed a few milliseconds apart; on a frozen clock they tie | Tests run on `TestClock` and move it on between steps that must be apart |
| "The ConnectionString property has not been initialized" in the output | Not a failure: the architecture tests started the real hosts, whose background services ran without a database | The rules start the hosts without the app's background services (`RoutesOnly`) |

## Unit tests and integration tests

**A unit test touches no I/O:** no database, no S3, no network, no container, no files beyond its own
sample data. It calls plain code (a function, a small class taking its inputs) and checks the result.
Most rules are like this: what a crop keeps, which series a sermon belongs to, who is in an audience,
whether two uploads are the same file, when a repeating event next falls.

**An integration test covers a database or storage rule,** and says which: the class carries
`[Integration("…")]` naming the behaviour, for example `[Integration("ON CONFLICT keeps the furthest
listening position when two phones save at once")]`. It is for what only the real store can show: sync
cursors, `NOTIFY`, pruning, advisory locks, raw SQL, `ON CONFLICT`, jsonb queries, content-addressed
blobs, transactions and concurrency, migrations. Everything else it reaches is incidental, so it keeps
its assertions to the storage rule and leaves the logic to unit tests.

When a rule sits inside a service that also saves things, pull the rule out into plain code the
service calls, and test that. Don't add seams only for tests: the extraction is the seam.

**Where they live:** in the same project, side by side: a feature part has one test project
(`GSBC.ChurchApp.Features.<Feature>.<Part>.Tests`). The attribute is also an xunit trait, so the unit
tests run on their own, with no Docker, in a couple of seconds:

```bash
dotnet test --solution GSBC.ChurchApp.slnx --no-build --filter-not-trait "Kind=Integration"
```

One project per part rather than separate unit and integration projects keeps a rule's two kinds of
test together, keeps the project map as it is, and the trait gives the fast run anyway.

`TestRules` checks it: a test class that reaches `TestDatabase` or `TestBuckets` (directly, or through
a host or helper in its project) must carry `[Integration]` with a reason, and a class that carries it
must actually reach storage.

## What a test gets

Everything shared lives in `tests/Testing/GSBC.ChurchApp.Testing`. A test never starts a container
itself, and `TestRules` fails if a test project references Testcontainers.

**A database.** `await TestDatabase.CreateAsync()` in `InitializeAsync`, and `DisposeAsync` drops it.
Every test class's tests each get a new, empty database on one shared Postgres server; the fixture
runs its migrations. The server is a `postgres:17` container on tmpfs with `fsync` off, started by
the first test process of a run and joined by the others through a file in the temp folder. It stops
itself once nothing has used it for 30 seconds, so a run leaves nothing behind, in CI or on a laptop.
It only joins a server that answers as `cluster_name = gsbc-churchapp-tests`, so it can never write
to a developer's or the AppHost's database.

**Buckets.** `await TestBuckets.CreateAsync()` names a public and a private bucket of the test's own
on one SeaweedFS server for the test process; the test creates them through the app's S3 client.
SeaweedFS takes about 15 seconds to start, so a project that needs S3 pays that once.

**The time.** `TestClock` (a `TimeProvider`) is registered wherever the code under test takes one. It
starts at a fixed instant, `TestClock.Start` or one the test chooses, and only moves when the test
moves it (`Advance`, or setting `UtcNow`). Rows a test writes take their times from the same clock.
Use `TestClock`, not `FakeTimeProvider` or a clock of the feature's own: one clock, so every test reads
the same way. Nothing here needs fake timers; if a test ever does, extend `TestClock`.

## What a test may not do

Enforced by `TestRules` in the architecture tests, with an allowlist that names each exception and why:

- **Read the real clock:** `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.UtcNow`,
  `TimeProvider.System`, `Stopwatch`. Use `TestClock`. When the code under test can only run on the
  real clock (the gateway's cookies do), read the time from the host's own `TimeProvider`, as
  `PasskeyCeremonyTests` does, so the test and the code agree.
- **Sleep or wait for time to pass:** `Task.Delay`, `Thread.Sleep`. Wait for the signal itself: a
  `TaskCompletionSource` the code completes, the next item on a channel, a second watcher hearing a
  later change before asserting that the first heard nothing (`LiveFeedTests`). A wait that has to
  give up uses one generous deadline only to stop a broken test hanging, never to decide the result.
- **Use unseeded randomness:** `Random.Shared`, `new Random()`. Seed it (`new Random(20261104)`) so
  a failure happens again on the next run. `Guid.NewGuid()` for names that keep tests apart is fine.
- **Start containers,** or reach anything outside the run: no network beyond the shared servers, no
  developer databases or AppHost volumes, no files outside the test's own temp folder.

And, by convention:

- **No state shared between tests.** Each test's rows, buckets and fakes are its own. A static that
  holds data between tests is a bug.
- **No order between tests.** Every test passes run alone, first or last.
- **No fixed ports.** Let the OS pick (`assignRandomHostPort`, port 0, `TestServer`).
- **Rules that scan the repository** use `RepoFiles`, never `Directory.EnumerateFiles`, so build
  output, SwiftPM's `.build` and `node_modules` never count.
- **Hosts started only to read their routes** use `RoutesOnly<TProgram>`, which leaves out the app's
  background services.

## Running them

Build once, then run without building:

```bash
dotnet build GSBC.ChurchApp.slnx
dotnet test --solution GSBC.ChurchApp.slnx --no-build                     # everything, about 35 s
dotnet test --project tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests --no-build
dotnet test --project <project> --no-build --filter-class '*WebsiteReadingTests'
dotnet test --project <project> --no-build --filter-method '*AStreamEndsOnceItsSignInHasEnded'
dotnet test --project <project> --no-build --output Detailed                  # each test's time
```

Docker must be running. CI runs the full suite on every push, the same way.

Projects run in parallel, and so do test classes within a project; tests within a class run one after
another. A project's time is roughly its slowest class, plus 15 seconds when it needs S3. A test that
takes more than a second or two is worth a look: usually it waits on something it could be told about.
