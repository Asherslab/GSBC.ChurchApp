# Rich text

News, event details, ministry About pages and sermon notes need formatted text. This is the one format
they all use. Decided by the owner on 2026-11-02: a restricted Markdown subset, edited with a toolbar
and a live preview (a structured block model and a JavaScript WYSIWYG editor were the alternatives).

## What it allows

**Posts allow more** (2026-11-13, [news.md](../features/news.md)): a media line may name any file from the library, with an
optional caption, `![alt](media:<id> "caption")`; pictures on consecutive lines are a gallery; a YouTube or Vimeo link on
its own line, `![title](https://youtu.be/…)`, is a player rebuilt from the video's id alone. Each use says which kinds it
takes (`RichTextMedia`): everything else still takes only pictures. Posts are written as blocks (`PostBlocks`,
`PostEditor`) over the same text, so what is stored stays this format.

| Allowed | Written as |
|---|---|
| Paragraphs, with single line breaks kept | Text separated by a blank line |
| Two heading levels (the page title is the one above) | `## Heading`, `### Subheading` |
| Bold and italic | `**bold**`, `*italic*` or `_italic_` |
| Bulleted and numbered lists, nested one level | `- item`, `1. item`, indented two spaces to nest |
| Block quotes (scripture) | `> quoted` |
| Links | `[text](target)` |
| Images from the media library, each on its own line | `![description](media:<media id>)` |

Nothing else: no tables, colours, fonts, code, or HTML. Anything outside the subset (HTML included) is
shown as the plain text it is, so it can never run. `\` escapes a mark (`\*`).

**Links** go only to `https://`, `mailto:`, `tel:` or a page in the app (`/events/12`). Anything else
(`javascript:`, `data:`, `http:`, `//host`) is refused when it's saved, and shown as plain text if it
somehow got stored. Web links open with `rel="noopener noreferrer"`.

## How it's stored and synced

`RichText` (Platform.Contracts.Content) is a small message: the Markdown text, plus the `MediaRef`s its
images use. It goes **inside a feature's own record**, like any `MediaRef`, so it reaches the phone
through that feature's feed and audience, and the phone never looks an image up. As a Configuration
setting (`RichTextSetting`) it's stored as JSON.

An older app meeting syntax added later shows it as text, which is acceptable; nothing is lost.

## Checked on the server, on every save

`RichTextRules.ProblemsWith` parses the text and refuses unsafe links, images that aren't in the text's
own media, files that aren't images, inline images, and text over the length limit (50,000 characters,
or the setting's own). The feature then passes the text's media through `IMediaReferences.VerifyAsync`
([media.md](../features/media.md)), which replaces every `MediaRef` with the library's own; unused ones
are dropped (`RichTextRules.WithOnlyUsedMedia`). So what is stored is always safe and always real.
Configuration does all of this for `RichTextSetting`; a feature storing rich text in its own records
does the same in its save.

The parser is linear in practice: unclosed marks don't make it rescan (tested with a hostile 48,000
characters).

## Showing it

`RichTextView` (Platform.Ui) renders the parsed document straight into Blazor's render tree: elements,
text and `MediaImage` components. There is no `MarkupString`, no `innerHTML` and no JavaScript, so the
app and the admin panel show exactly the same thing, under AOT. The phone's offline images come through
`MediaImage` as everywhere else.

## Editing it

`RichTextEditor` (Platform.Ui): a toolbar (bold, italic, heading, subheading, bulleted and numbered
lists, quote, link, picture), a plain text area, and a live preview made by the same `RichTextView`,
with the server's problems listed as you type. Staff never need to know the syntax:

- Bold and italic wrap the selection (trimming spaces a double-click picked up), or insert a
  placeholder to type over; pressing them again removes the marks. ⌘B, ⌘I and ⌘K work.
- Heading, lists and quote apply to every line in the selection, and toggle off.
- Link opens a box for the target and won't add one the server would refuse.
- Picture opens the media library (`MediaChooser`, below) and puts the image on its own line.

The only JavaScript is `js/rich-text-editor.js`, which reads and sets the text area's selection and turns
the shortcuts and Enter in the link box into toolbar clicks. The edits themselves are C#
(`RichTextEdits`).

`MediaChooser` shows whichever component is registered as the `MediaPicker` (Platform.Client). Media
registers `MediaChoice` (the picture sheet), so Platform.Ui and other features can offer "choose an
image" without referencing Media. With no picker registered, the Picture button isn't shown.

Styling uses the design system's tokens (`css/rich-text.css` in Platform.Ui), so it follows the
theme and dark mode.

## Status (2026-11-02)

**Built:** the model, parser and rules; the renderer, editor and chooser; `RichTextSetting` in
Configuration. Tested by `RichTextTests` (Platform.Contracts.Tests) and `PlacementEditorTests` (unsafe
rich text is never stored). The editor was checked in headless Chrome against the admin panel's build:
the toolbar's edits, the shortcuts, the link box refusing `javascript:`, and the preview.

**Not yet:** no feature stores rich text yet (News, Events and Ministries will); the editor hasn't
been tried on a phone; the Picture button hasn't been tried against the running API.
