# Media library

Detail behind [DESIGN.md](../../DESIGN.md) for the media library in the admin panel. Read DESIGN.md first; its rules win.

## Media library
Two shelves: the Library, which staff organise in folders, and Made by the app, which the app fills. Reached from the sidebar's The app area. Its bars stay above the split view (the Still Frame Rule).
- **Shelf switch:** a `Segmented` of Library and Made by the app (a lock), beside the type chips (All, Pictures, PDFs and files, Not used, with counts) and a Grid/List `Segmented`.
- **Path row:** breadcrumbs to the open folder, each a drop target. A staff folder adds its ⋯ menu after the last crumb: Rename…, Move to… and, last and red after a rule, Delete folder…, which opens the confirm sheet. Searching shows "Results for “…” in every folder" and Clear search instead.
- **Folder tiles:** name over a count ("3 files · 1 folder", "Empty", or "Shared with others" for one the viewer can't open); app folders show a lock; a tile under a drag is tinted deep aqua. A folder holding only folders says "No files here, only the folders above…" rather than empty.
- **Picture grid:** 4:3 thumbnails over the name and how it's used. Hover and chosen are marker rings; a corner tick shows on hover, focus, when chosen and on every card while two or more are chosen. Files drag onto folder tiles and crumbs. List view is the data table.
- **Detail:** the split view's detail; its Folder fact links to the folder (after a lock when the app owns it), and Move to… leads its actions.
- **Move notice:** an info notice above the grid: "3 files moved to Sermons › 2026 series. Everything that uses them still shows them." A failure is a warning there.
- **Already in the library:** a file the library already has replaces the upload form with a sand box: "Already in the library", a lead saying it is exactly the same and one copy is easier to look after, and the existing file ("In <folder> · Used in N places"). Use the existing one is the only primary; Add it again anyway is quiet. The box takes focus without a ring, scrolls into view and announces its heading and lead. Using the existing one opens it in its folder, chosen, under "“Romans series.jpg” was already in the library, so nothing new was added. Here it is." Adding it again returns the form with Add a second copy.
- **Folder picker (Move to…):** a sheet titled for what moves ("Move 3 files", "Move “Sermons”"), lead "Things that use them keep working: moving only changes where you find them here.", then the folder tree; the current folder says "Here now"; it starts with nothing chosen; the chosen row takes Selected Fill; a folder it can't go into (itself or one inside it) is ink-3 and can't be chosen. Move here stays disabled until a different folder is chosen. Deleting a folder with things in it uses the same sheet ("Delete “Sermons”?", Move them and delete); an empty one is a plain confirm.
- **New folder, Rename:** a sheet with one Name field (up to 80 characters).
- **Made by the app:** a caption with a lock under the path row says what the folder holds and that its files stay where the app put them. Empty: "Nothing made by the app yet".

**The App's Own Rule.** Wherever the app owns something rather than staff (the Made by the app shelf, its folders and crumbs, a file's folder, the shelf's empty state), it carries the lock glyph and one plain caption saying what it is and what staff can't do to it. The lock is ink-2 or the surrounding text colour; ownership takes no colour or badge of its own.
