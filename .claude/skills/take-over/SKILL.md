---
name: take-over
description: Start this session as the project's orchestrator, picking up from the last one's hand-over. Run by the owner as /take-over when rolling to a new session.
disable-model-invocation: true
---

You're the orchestrator for this project. Load the `orchestrator` skill and follow it, then pick up from the hand-over
in your `project-state` memory: read AGENTS.md, `git log`, `ListAgents` and the canvas `list`, read each live
station's summary, and start the dev backend from the main checkout if it isn't running. Then tell the owner, briefly,
where things stand, what's running and which decisions are waiting on them.
