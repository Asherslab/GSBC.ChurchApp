# Chats

Detail behind [DESIGN.md](../../DESIGN.md); its rules win. The desk's panes, the *Chat settings* arrow, the narrow steps, a message's ⋯ and the composer are in its *Pattern map*.

## Chats desk (admin)
`/chats`, `/chats/{id}`: your own chats, the same as the phone's, written through the same methods. Chats sits under Home in the sidebar, outside the areas, with its count of unread chats (`INavCounts`); checking other people's chats is *Chat safety* (`/chat-safety`, Safety area, `AppIcons.ChatSafety`).
- **Panes:** full height, ruled off by hairlines; settings start open above 1360px wide.
- **One head line:** the list's title, the chat's `PaneHead` and the settings head share one band, so the three heads line up. The chat's lead names its ministry, what kind of chat it is and how many people ("{Ministry} · Team · group chat · 3 people").
- **The list:** a sticky head of the title and search, then rows flat on the page, grouped under each ministry's name, the ministry with the newest message first (owner, 2026-11-13: "make sure it's clear which chats belong to what ministry"). Each row's second line starts with the kind of chat ("Team · group chat", "Parents · leaders post", "Asking to join") before the latest message. The time sits top right with the unread count under it in a marker capsule; a muted chat shows the bell-off icon instead. A hint at the foot says where chats come from and where to start one. With nothing open, the middle says "Choose a chat".
- **The conversation:** dragged files outline it in dashed deep aqua. A message's ⋯ stays beside its bubble on the outer side, always shown on touch screens; its popover keeps to the bubble's side.
- **Chat settings:** the same `ChatPeople` as the phone's sheet: *Mute this chat* as a toggle row in a settings group, *Who's in it* as people with "Leader" at the right, then the hints. For a leader the head's ⋯ holds *Delete the chat…*, red, asking in a `Sheet`.
- **At 900px and under:** below the narrow window's bar, the list, or the chat with a quiet "Chats" back button above its head, or its settings in place of the conversation; the heads drop the shared band.

## Checking a chat (admin)
`/chat-safety/check/{id}`, in *Chat safety*: one chat folded down to what you're looking for, in the shared workbench under a title band.
- **Logged note:** first in the conversation and sticky at its top, a masthead-navy box with a marker icon, saying the reading is logged and who can see the log.
- **Messages:** sand tiles: who, the time, any quiet pills and a ⋯ menu; a quote of what it answers; the text; its photos (opening in the picture viewer); its reactions as disabled chips. A match has the found words marked in marker. The message before and after a match is unfilled with a hairline ring. A reported message carries a danger ring. Days divide the conversation with a centred line.
- **Fold line (signature):** whatever doesn't match folds into one full-width Link button, "9 messages · Show", between dashed hairlines; it opens in place.
- **Reports:** in the record pane, each report a card with its title and reporter, then a field and its own save bar to mark it handled; the checks of this chat are listed under them.
- **At 1360px and under:** the rail becomes a band of three columns above the conversation (search across the top, the Who list scrolling); at 900px and under, one column.
