# Release notes

One file per change people would notice, written with the change. The design is in
[docs/architecture/changelog.md](../docs/architecture/changelog.md).

```markdown
---
title: Sign in without a password          # a few words
audience: everyone                   # everyone, or staff for staff-only changes (admin screens, tools)
added: 2026-10-26                    # the day you wrote it: What's new lists the newest first
highlight: true                      # optional: shown once after the update, in What's new
icon: key                            # optional: an icon name from Platform.Ui's IconPaths
link: /account                       # optional: the app screen "Show me" opens
image: passkeys.webp                 # optional: a 16:9 picture in changelog/images/
area: Account                        # optional: where in the app, shown beside the note
---
Use your face, your fingerprint or your phone's passcode to sign in, and stay signed in.

## For staff
Changed: What changed, in plain words.
Where: More → Account
Try: What to try, step by step.
```

- New notes go in `unreleased/`. `tools/release-notes/release.sh <version>` moves them into `<version>/`.
- The line under the front matter is for everyone: one short sentence (160 characters at most) in the
  church's plain voice, nothing technical.
- `## For staff` is for UAT testers: still plain words, no class names or PR talk. Each part is optional.
- Refactors, tests and docs get no note.
