---
version: 1
slug: "chats-gsbc-churchapp-features-chats-admin-81ff7e36"
primary_target: "src/Features/Chats/GSBC.ChurchApp.Features.Chats.Admin"
related_targets: ["src/Features/Chats/GSBC.ChurchApp.Features.Chats.Client"]
---

# Chats: checking a chat (admin panel)

Scope: the full page for checking one chat, `/chats/check/{id}` in the admin panel, reached from the Chats desk (choose a
chat, say why, Open the chat). Mode: Operate. Owner's feedback #49 (2026-11-11); interview and pick 2026-11-12.

- **Who and why:** a ministry's leaders and the church's chat auditors, after a report or a worry. They are usually
  *finding something*: a person, a word, a day, then reading around it (owner's answer).
- **Tools (owner):** search; Who, When, Has photos or files, Edited or deleted (and Reported); photos and files through
  the picture viewer; the chat's reports handled on the page; Pause a sender from a message's ⋯ menu (auditors).
- **Range:** a few messages to thousands; 2 to 60 people; reports 0 to a handful. Every check is logged with its reason;
  the reason is asked on the desk before the page opens (opening the URL directly asks there too).
- **Untouched:** what a check is (the server's `Oversight.Check`, every version, the log); the desk's tabs.

## Direction contract

seed: seed:0d5cdc54667f1588 (surface, operate; the dealt lead, "folded", chosen by the owner)

THESIS: The chat folded down to what you're looking for. Matches sit in the conversation with the message before and
after; everything else folds into a quiet "9 messages · Show" line. Refuses a flat search-results list torn out of the
conversation, and an endless unfiltered scroll.

OWN-WORLD: The admin panel's world unchanged: navy sidebar, white work area, the TitleBand, the Workbench (sand filter
rail with FilterChips Rail and the search field; the conversation in the middle; the record pane ruled off on the
right). Messages are sand tiles (radius tile), matches on Selected Fill with the found words marked in marker orange,
the reported message ringed in danger red, the logged note on navy.

STORY: Arrive on the logged note and the newest messages; type a word or pick a person and the chat folds to the
matches; open a fold to read around one; look at its photos bigger; handle the report on the right; pause a sender
from the message's ⋯.

FIRST VIEWPORT: TitleBand (back to Chats, the chat's name, its ministry, audience, people and message count, a pill
for waiting reports, the ⋯ menu); under it the Workbench: rail (search, Who with counts, When from/to, Only chips with
counts), middle (logged note, "3 messages from Jordan say Sunday" with Clear, folds and matches by day), right (Reports
with Mark as handled, then Checks of this chat).

SIGNATURE: the fold line: a dashed rule with "N messages · Show" that opens in place, so context is one click, never
lost.

GUARDRAILS: Pattern map components only (Workbench, FilterChips Rail, SearchField, DateField, PaneHead, Menu, Sheet,
PictureOpen + PictureViewer, TitleBand). No coloured left borders on matches; no new spacing values outside tokens;
works at 1360 and under (rail above) and in dark mode.
