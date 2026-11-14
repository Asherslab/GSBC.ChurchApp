# Saving

Detail behind [DESIGN.md](../../DESIGN.md) for the two kinds of form, where the save goes, instant changes and leaving with changes. Read DESIGN.md first; its rules win.

## Saving
A form with unsaved changes always asks before they're lost (owner, 2026-11-10). A person can always tell which of the two kinds of form they're on.
- **Saves by itself:** long work with its own publish or send step (App styling drafts, an announcement, the digest). `SaveStatus` by its title: "Saving…", "Saved" ("Saves as you type" before the first save), "Couldn't save. Trying again by itself." with *Try now*, or why it can't yet ("Give it a title so it can save"). No Save button. It asks before leaving only while a save is waiting, running, failed or not possible.
- **Saves with a button:** everything else (a ministry's details, an event, a sermon). `SaveBar`, placed as DESIGN.md's *Pattern map* says, its primary named for what it does ("Save", "Add the regular event"). Status: "You have changes that aren't saved" (a pencil, ink-3), and "Saved" with a check only once saved. It always asks before leaving with changes.
- **Can't save yet:** the button is disabled and the reason, in words, takes the status line ("Give it a title to add it."), never only a tooltip.
- **Where the save goes:** a desktop editor with a navy header: the header's action row at the right, as the on-masthead button. The workbench's record pane: docked at its foot. A settings group: its sand foot. A sheet: its actions. Elsewhere: the sticky editor foot or the form's last row. Phone: the editor foot, Save full width (`Block`) beside Cancel, status centred above.
- **Instant changes** (a switch, a role tick, linking a person) apply at once with no Save or status, in their own group below any Save form, its foot saying "These change straight away".
- **Leaving:** `UnsavedGuard` covers another page, a sidebar link, Back and closing the tab. Screens that swap what's open without navigating (calendar, sermons, series, speakers) call `UnsavedChanges.MayLeaveAsync()`. The sheet says what would be lost and offers *Save and go on* (when it can), *Discard the changes* and *Stay here*.
