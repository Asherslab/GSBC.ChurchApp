---
version: 1
slug: "gsbc-churchapp-features-chats-admin-panel-8441cfee"
primary_target: "src/Features/Chats/GSBC.ChurchApp.Features.Chats.Admin/Panel"
related_targets: ["src/Features/Chats/GSBC.ChurchApp.Features.Chats.Client"]
---

# Chats on a computer: the chat panel (admin panel)

Scope: `/chats` and `/chats/{id}` in the admin panel: anyone signed in uses their own chats on a computer, as on the
phone. Mode: Operate. Owner's feedback #74 (2026-11-13). The old desk becomes *Chat safety* (`/chat-safety`).

- **Who and why:** staff and leaders at a desk, often (owner: "I would likely want and use that frequently"). Reading,
  replying, posting to a ministry's people; a few chats to a few dozen; 2 to 60 people; messages from a handful to
  hundreds.
- **Rules:** exactly the phone's: the panel shows the chats and messages Sync would give your phone (same rows), and
  writes through the same `gsbc.chats.Chats` methods. Reading anyone else's chat stays the Chat safety check.
- **Interview:** not held (owner asleep, "use best option this time"); answers assumed from the ticket and recorded as
  *default (owner to review)* in docs/features/chats.md.
- **Untouched:** the phone's screens, the check page, what a chat is.

## Direction contract

THESIS: Your chats as a desk tool: list, conversation and its people side by side, all three visible, so nothing is a
page away. Refuses the phone's stack of pages blown up to desktop width, and a ministry-first drill-down.

OWN-WORLD: The admin panel unchanged: navy sidebar (Chats near Home with an orange unread count), white work area, list
rows on Selected Fill when open, sand bubbles for others and navy for yours (the shared `MessageBubble`), the shared
`Composer` on sand, the people pane ruled off on the right with settings groups as sand cards.

STORY: Open Chats, see which chats have something new, open one, read and reply with the keyboard, react, paste a
picture, glance at who's in it, mute it, move to the next.

FIRST VIEWPORT: Sidebar; list column (title Chats, search, rows with ministry, preview, time, unread count);
conversation (chat name, ministry · audience · people, cog toggles the right pane; quiet staff line,
days, bubbles, composer with Enter/Shift+Enter hint); right pane (Chat settings head with ⋯ holding Delete for leaders, mute, people with roles, explanation). Starting a
chat stays in a ministry's Chats tab (amended 2026-11-13 after review: the server can't yet say where you may start
one, so a panel-wide New chat would offer ministries that refuse; docs/features/chats.md, default 3). Each message has
a ⋯ on hover or focus (and Enter on a focused message) opening its actions as a popover.

FORM: three panes, second on the ordered list (lead roll was ministry rail); seed: seed:26205211306c0b57

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
