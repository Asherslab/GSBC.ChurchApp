# Words on screen

The rules for every label, description, placeholder, option, button, empty state and message, in the app and the
admin panel. Decided 2026-11-16 from UAT #18: the owner found fields that "don't make sense without context", text
that felt "too casual" or "like filler", and "in it": in what? DESIGN.md, *Words on screen*, has the short form.

Read every line aloud as a sentence before it ships. Write for someone opening the screen for the first time, who
doesn't know how the feature was built.

## Labels

A label names the thing in a noun or a short noun phrase, in sentence case, with no full stop.

| Not | But |
|---|---|
| In a line | Short description |
| Until (for several days) | Last day |
| Label | Line above the title |
| Who should see it? | Who can see this |
| Works for (days) | Days until it expires |

- No sentence fragments, questions or brackets. What the field means goes in its description.
- One label for one thing on every screen that shows it.

## Descriptions

A field whose purpose isn't obvious from its label has a description (`Hint`): one or two whole sentences saying
what it does and, when it changes what other people see, where they'll see it.

> **Line above the title**
> A word or two shown in small type above the event's title, in the calendar and on the event's page. Leave this
> empty to show the calendar's name, Services.

- It sits between the label and the field, in ink 2, so it's read before typing; a problem line stays under the field
  (owner's pick on the decision page, 2026-11-16, `.impeccable/mocks/field-descriptions/`). Fields without a label
  above their box (a colour, a font, a place) keep it after the control.
- What an empty field means is said here ("Leave this empty if you're away until further notice."), never as a
  placeholder and never as "Empty: no end".
- A description answers a question the label leaves open; it never repeats the label.

## Placeholders

A field is empty until someone types into it.

- No example placeholders: no "Like 15 Nov", no "Christmas break", nothing that could pass for a value already
  entered. Typed dates and times in the admin panel have none; the calendar or clock button, the read-back and the
  problem line ("Type a date such as 15 Nov, 15/11/2026 or next Sunday, or choose one from the calendar.") teach
  the format.
- A search box may have a placeholder only when it adds something the label doesn't.
- A field with a fixed shape may show it (`CodeField`'s ABCD-EFGH).

## Sentences

Descriptions, option details, menu details, empty states, leads and problem lines are whole sentences, with a
subject and a full stop.

- **Name the thing.** A pronoun is fine only when its noun is in the same sentence. "Only the team serves in it"
  becomes "This ministry has a team but no attendees." Headings too: "Where it sits" becomes "Where this ministry
  sits".
- **Plain, not casual.** No "Give it a …", "Like …", "Catching up…", "Take off", "come along". Contractions are fine.
- **No glued labels.** "Hidden: Shown is off" and "Shown at For phones" are labels pasted into a phrase; write the
  sentence ("Hidden from everyone").
- **An empty state** says what will appear and how to add the first one.

## Buttons and confirms

- A button is a verb and what it acts on: "Add the ministry", "Save the break", "Turn the code off".
- Both buttons in a confirm name the thing: "Delete the chat" and "Keep the chat", never "Delete it" / "Keep it".

## Problem and blocked lines

- A line that says why something can't be saved names the field and the fix: "Enter a name for the ministry."
- Messages the server sends to the screen follow the same rules: "You don't have permission to do that.", not
  "Not permitted.".

## One word for each thing

- The same verb set everywhere: **attend** and **attendee** for coming, **join the team** for serving, **approve**
  (not "says yes"), **remove** and **add** (not "take off", "put in").
- Nothing internal on screen: no ids, permission keys, feed names or words from the code ("Event layer",
  "flagged"). Say who or what in words.
- Never name a real ministry, group, room or person as an example (DESIGN.md, *Examples in copy*).

## The test

`WordingRules` (Architecture tests) reads every `.razor` and `.cs` file under `src` and fails on:

- a string starting "Like " (an example placeholder);
- "Give it a …";
- `Hint="Empty:`;
- "Delete it" or "Keep it" as a button;
- a `Label` with a bracket or ending in a question mark.

Its `WaitingForTheirStation` list held files other UAT stations were changing on 2026-11-16, until their wording was
fixed; it empties as they're done, and a second test fails when a listed file no longer breaks a rule.
