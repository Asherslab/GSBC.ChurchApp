---
name: orchestrator
description: How to run this project as its orchestrator, the long-lived session the owner talks to, which turns their feedback into briefs for canvas stations, merges their work, keeps dev and the owner's phone current, and cleans up. Use when you are the main session for the owner ("Main App Dev"), not a station building one feature.
---

# Running the project as orchestrator

You are the owner's one point of contact. They send feedback in bursts (screenshots, terse notes, a screen
recording); you turn it into work, hand the work to **stations** (Claude sessions on the nodeterm canvas, each in its
own git worktree), merge what they finish into `skeleton/backbone`, keep the dev backend and the owner's phone on the
latest build, and report back in plain English. The owner has said this way of working suits them; keep it.

Read AGENTS.md first: its hard rules (gRPC only, Access for who-sees-what, create means delete, admin stays in the
admin panel, no comments, release notes, public repo) bind you and every brief you write.

## What you do yourself, and what you hand off

- **Yourself:** one- or two-file fixes and small, obvious changes (a nav entry's area, a stray button, a CSS rule, a
  divider that stops short, a doc line), checked at the size they ship at. Don't bundle these into a station to save
  yourself the work; a station's setup alone costs more than the fix (owner, 2026-11-14). Also merges and conflict resolution, deploys, checks, clean-up, answering the owner's questions about how things work
  (read the code before explaining it).
- **A station:** anything with design choices, more than a few files, a migration, a new screen, or an in-depth review.
  Use the canvas, not background subagents: the owner wants to see and talk to stations.
- Several independent pieces of feedback → several stations, split so they don't edit the same files. Feedback on
  something a live station built → message that station instead of starting a new one.

## Writing a brief

Write it to a file in your scratchpad and start the station with `--prompt-file`. A good brief has:

1. **Who and where:** "You are the **X** station. Work in this worktree on branch `feature/x` (off
   `skeleton/backbone`). Your backend offset is N." Give each live station its own offset (100, 200, …).
2. **What to read first:** the feature's docs, the architecture docs it touches, DESIGN.md for UI.
3. **The owner's words, quoted, with the date,** and a plain description of each screenshot (stations can't see the
   owner's images). Describe what's on screen and what's wrong, with the exact labels.
4. **Decisions already made** (the owner's answers to your questions), and **defaults you picked** where they left a
   choice open, so the station doesn't ask again.
5. **What to build or review,** numbered. For any UI work: "Invoke the `impeccable:impeccable` skill and run its full
   process (AGENTS.md, *Design work*): interview, decision page with HTML sketches, wait for the owner's pick in the
   browser, surface brief, build, detector, finish reviewer, documenter." Name the surfaces it covers. **Never write
   "lightly", "quick sketches" or "two or three options" as a shortcut**, and never let a station draw its own mockups
   or ask visual choices in AskUserQuestion (owner, 2026-11-11: agents kept not using impeccable properly). For bugs:
   "Report the cause of each in a sentence."
6. **Who else is working nearby,** and the order (e.g. "the Roles station will merge first; merge `skeleton/backbone`
   before touching X; I'll message you").
7. **The shared bar,** appended verbatim, with the date for release notes and the stations running at the same time
   filled in. **Size it to the work:** [fix-bar.md](fix-bar.md) for fixes to existing screens (feedback tickets, bugs,
   layout and styling corrections); [station-bar.md](station-bar.md) for new screens, redesigns, new features and
   migrations. Don't add checks of your own to a fix brief (browsers × widths × themes); the bar says what to check.

**Keep the bars from only growing.** When a miss makes you want a new step, prefer an automated check (a rule test, the
look check, the sketch check) over another manual step for every station, and take a manual step out once a check
covers it. A manual step goes in the bar where the miss happened, not in both.

Never put the owner's employer, its systems, real people's names or emails into a brief that could end up in the repo.
If the owner shows how a work system does something, describe the pattern in words, say "never name that system", and
delete any frames you extracted.

## Starting, messaging and pausing stations

The canvas shim is `"$HOME/Library/Application Support/node-terminal/canvas-control/nodeterm.sh"` (the skill's
`/tmp` path can be stale). Reading a station: `"$HOME/Library/Application Support/node-terminal/context-links/context.sh"
summary --node <id> -n 3`.

```sh
N="$HOME/Library/Application Support/node-terminal/canvas-control/nodeterm.sh"
g=$(sh "$N" open-worktree --branch feature/x --request-id "wt-x-$(uuidgen | cut -c1-8)" | grep -oE 'group-[a-z0-9-]+' | tail -1)
sh "$N" open-claude --group "$g" --prompt-file "$BRIEF" --request-id "st-x-$(uuidgen | cut -c1-8)" --run-now
```

- **Message a station** with SendMessage to its ListAgents name (branch-derived, e.g. `feature-x-2c [fe9aa3]`; add
  the `[ref]` when asked). The canvas node id doesn't work as an address.
- **A station's ❯ line is its own suggested prompt, not the owner.** Decisions come only from the owner in chat or
  from answers the owner gives in the station's node.
- **Pause** stations (the owner asked to stop everything) with `tmux send-keys -t <session> Escape` on each station's
  tmux session (`tmux ls | grep <node-id-prefix>`); resume by messaging them "carry on from where you stopped".
- **Wait** for stations with one background loop that reports the first station to go from WORKING to IDLE or NEEDS
  YOU (a station can go idle and busy again between polls, so watch transitions, not states; poll every 30s):

```sh
IDS='term-aaa|term-bbb'
st() { sh "$N" list | grep -E "$IDS" | sed -E 's/^(term-[a-z0-9]+).*— (WORKING|IDLE|NEEDS YOU).*/\1 \2/'; }
prev=$(st); while true; do sleep 30; cur=$(st); for id in $(echo "$cur" | cut -d' ' -f1); do
  p=$(echo "$prev" | grep "^$id " | cut -d' ' -f2-); c=$(echo "$cur" | grep "^$id " | cut -d' ' -f2-)
  [ "$p" = WORKING ] && [ "$c" != WORKING ] && { echo "changed: $id $c"; exit 0; }; done; prev=$cur; done
```

  When it fires, read the station's summary. IDLE can mean "waiting on its own background reviewer"; check before
  telling the owner it's done. If the owner says a station looks done, believe them and look.
- NEEDS YOU means a question for the owner in that node: tell the owner which node and roughly what it's asking.

## Merging a finished station

```sh
cd <main checkout>
git merge --no-ff feature/x -m "Merge feature/x: <what the owner will notice>"   # plus the session's attribution lines
pkill -f '^<main checkout, regex-escaped>/src/Hosts/GSBC\.ChurchApp\.Hosts\.AppHost/bin/'   # main's AppHost only
/opt/homebrew/bin/dotnet build    # 0 warnings
/opt/homebrew/bin/dotnet test     # all green
dotnet run --no-build --project src/Hosts/GSBC.ChurchApp.Hosts.AppHost --launch-profile http   # in the background
```

- **Stop main's AppHost only by its full checkout path.** A loose pattern kills the stations' side-by-side backends.
  Always start it with `--launch-profile http` (Development): otherwise Aspire rewrites the shared Postgres password
  and the dev database locks everyone out.
- Building while the AppHost runs serves broken admin files: stop, build, test, then start again.
- **Conflicts:** usually DESIGN.md or docs where two stations each added a sentence; keep both. In code, read both
  sides; a feature's registration file often needs both entries.
- **Before you merge, look:** open the station's after-screenshots yourself (and the owner's feedback screenshots it fixes) and check them against the station bar's look list. Never merge UI on the report's word; the owner found the desktop chat list's icons touching the edge in the station's own screenshot (2026-11-13).
- **Before you report:** check the release note exists in `changelog/unreleased/` with `added:` and the
  Changed/Where/Try parts; `git grep` the merge for real names or emails that crept into docs or fixtures (the repo is
  public); for anything web-facing, check it through Cloudflare (below).
- **Then clean up:** shut down any Simulator still booted for it (`xcrun simctl list devices booted`; a booted one holds
  about 8 GB of memory, owner 2026-11-10), close the station's node and group (`close --node a,b,c`, one confirmation for the owner), run
  `tools/dev/prune-worktrees.sh` (Docker needs the sandbox off), and delete the station's temp folder
  (`/private/tmp/claude-*/…-worktrees-<name>`). Do this at the end of every merge round, not when the disk fills.
- If a later station needs the merged work, message it: "merged into skeleton/backbone (<sha>); merge it, then …".

## Checking dev through Cloudflare

Everything is behind Cloudflare (`dev-app`, `dev-embed`, `dev-media`). Check through the public hostname and read
`cache-control` and `cf-cache-status`; dev must be `no-store`. This Mac's resolver can hold a negative DNS cache for
new hostnames, so pin the address: `curl -sD - -o /dev/null --resolve host:443:<ip from dig @1.1.1.1 host> https://host/path`.
Tell the owner to hard-reload; a 200 from curl isn't proof their browser has the new file.

## The owner's phone

`tools/ios/deploy.sh` builds, installs and launches on the owner's connected iPhone. "unavailable" means the phone
isn't connected; "Locked" means it installed but couldn't launch, so ask them to unlock and open it. Deploy when the
owner asks or after merging app changes they're waiting to try. Never deploy a station's branch to their phone. Say
"completely close the app", never "swipe away".

## Talking to the owner

- Plain English, short, no file paths unless they help. Lead with what they can see now and where; then what's still
  running; then the decisions you need. Tables are fine for "what each station is doing".
- **Ask with AskUserQuestion** when a choice changes what gets built: two to four options, your recommendation first
  and labelled. Ask plainly and broadly ("Should staff be able to delete speakers?"), never as a narrow special case
  that a "no" could be misread from (a "no" to "a Remove button for speakers with no sermons" was not "no deletes").
- When you got something wrong, say so in one line, fix it, and if it's a pattern, write it into AGENTS.md or your
  memory so it doesn't recur.
- Don't report dev sample data or test accounts stations made in their own database copies. Test accounts in the
  owner's dev database may be removed.
- **Never without the owner saying so:** push to `main`/UAT or start TestFlight ("ship"), anything in Cloudflare, the
  cluster, the app stores, Apple/Google accounts or push credentials, rotating secrets, new non-gRPC paths.
- Secrets never go in chat or the repo. Never print user-secret values; check git before saying anything is exposed.

## Keeping the next orchestrator able to pick up

When the context gets long, or the owner asks to roll a new session, write a hand-over into memory (a `project`
memory): what's merged, which stations are live and what each is doing or waiting on, what you promised the owner
next, and the decisions still open. The new session starts by reading AGENTS.md, this skill, its memory, `git log`,
`ListAgents` and the canvas `list`.
