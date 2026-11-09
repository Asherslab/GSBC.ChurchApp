# Import from the website (temporary)

A one-off admin page that brings every sermon on the old website (baptist.com.au, a Tithe.ly site) into the app, so
Sermons can live only in the app ([sermons.md](sermons.md), *Sermons live in the app*). It is its own module, run once
per environment at go-live and then **deleted** (below).

## What the owner decided (2026-11-09)

- "We do need an import tool for when we do go to production, but it needs to pull *all* the sermons/etc off the site
  (not the 100 or so that the rss feed allows)."
- An **admin page in its own removable module**, *Import from the website*. Staff run it on production themselves, see
  its progress, and can run it again safely: it skips what's already there.
- It **copies everything** into our storage (audio, series pictures, sermon pictures, speaker photos), deduplicated like
  all media, so nothing depends on the old website afterwards.
- **Dev's website sermons are wiped and imported again** with this tool, as production will be (the migration
  `SermonsAppOnly`, sermons.md).
- **Guest speakers' titles (owner, 2026-11-09):** "split them, keep the honourific, as another optional field … for now
  don't include, just store the data for later." See *What comes across*.

## How the website was read (research, 2026-11-09)

There's no Tithe.ly API (checked 2026-11-04) and the RSS feed (`/podcasts/media.rss`) holds only the last year (155
items). The site's own pages hold everything:

- **`/podcasts/media?page=N`** lists the sermons ten a page, newest first, each as `<a class="row d-sm-flex"
  href="/media/<slug>">` with its title. Page 1's pagination names the last page.
- **`/podcasts/media/<slug>`** (where `/media/<slug>` redirects) is a sermon's page: the title (`h1.header-heading`),
  the service label (`.header-subheading`), the speaker's name, photo and the day (`.header-content-middle`), the series
  (the breadcrumb's `/series/<slug>`), the audio (`<audio src>`, an mp3 on Tithe.ly's CloudFront), the YouTube video
  (`<iframe src=…/embed/<id>>`), the Bible passage (*Reference*), the picture and any notes (`.article`), and the site's
  own id (`main.media_<n>`).
- **`/series/<slug>`** has the series' picture (`img` with the series' name) and description (`.pageheading`).
- **How we know we got them all:** the listing's sidebar counts sermons by year and by speaker. On 2026-11-09 the
  year counts sum to **887**, the 30 speakers' counts sum to 887, and the 89 listing pages hold 88 × 10 + 7 = 887
  distinct sermons, each with its own site id. The import shows the website's own count beside what it found.
- **What's there (2026-11-09):** 887 sermons from 1998 to 2026 (most from 2021 on), every one with mp3 audio; 34
  series covering 249 sermons; 129 with a YouTube video, 118 with a passage. 661 name their speaker (30 people); 226
  older ones (mostly 2016–2021, imported into Tithe.ly from SoundCloud) say "Guest Speaker" and put the preacher in the
  title ("Bro Sam Sample - Hope In A Storm"). With those split out there are 75 speakers. The audio comes
  to about **49 GB** (887 mp3s, the largest 114 MB; measured from the files' sizes on 2026-11-09).
- **Read politely:** one request at a time, a second between pages, and a User-Agent naming the app.

## What comes across

| From the website | Becomes |
|---|---|
| Title | The title; a guest speaker's name is taken out of it (below) |
| The day | The day preached (midnight in church time, as the RSS feed had it) |
| Speaker | Lands on the speaker with that name (ignoring case and spaces), or a new one, with the photo the page shows |
| *Guest Speaker* with "Bro Sam Sample - Hope In A Storm" | Speaker *Sam Sample*, title *Hope In A Storm*, and the speaker's **honorific** *Bro* stored on the speaker (`speakers.honorific`, not shown anywhere yet). Honorifics: Bro, Brother, Pastor, Ps, Evangelist, Missionary, Mrs, Mr, Dr, Sis, Sister, Rev; the name can come first or last ("Things I've learned from John - Bro Sam Sample"). On 2026-11-09, 209 of the 226 split; the other 17 ("Missions Panel", or a name with no honorific before the dash) keep their title and have no speaker |
| Series | Lands on the series with the website's id or name, or a new one, with its picture and description |
| Service label | The same rules the website reader had (`ServiceLabels`): "Sun AM" is *Sunday Morning*, "Sun PM - Sam Sample" *Sunday Evening*, conference sessions stay as written. Old labels map by their day: "Morning Service" on a Sunday is *Sunday Morning*, "Evening Service" or "#Sundaynight" on a Sunday *Sunday Evening*, "Midweek Service" on a Wednesday *Wednesday Night* |
| Reference | The Bible passage |
| Notes under the picture | *About this sermon*, unless it only repeats the service or is SoundCloud's "… by Good Shepherd Baptist Church" |
| YouTube embed | The video |
| Picture | The larger of the page's picture and the original behind its share image, kept in Media's *Sermon artwork* folder |
| Audio | Downloaded, hashed, stored under its SHA-256 in the public bucket and kept in Media's *Sermon audio* folder |

Imported sermons arrive **already announced** (no notification to anyone) and **already checked** (not in *Needs a
look*). Speakers that end up with two spellings (on 2026-11-09 two people each appear once under a misspelling) are
fixed by staff: delete one and move its sermons to the other.

### Ids

Each sermon, series and speaker the import makes gets the id the website reader used to give it (UUIDv5 over the
website's address, the series' slug, or the speaker's name; `LastingIds`). On dev and UAT that means listening progress,
series follows and Hub links made while the website was read find their sermon again after the wipe and import.

### Running again

The import's own table (`sermons_import.sermons`) keeps each website address it has seen, the sermon it became, and
what went wrong. Sermons knows nothing of the website.

- **Start import** reads the listing pages, then brings across each sermon not yet brought across, newest first.
- **Run it again** reads the listing again (new sermons on the website are picked up), skips everything it brought
  across before, and tries the failed ones again. A sermon brought across and since deleted by staff is not brought
  back.
- A sermon whose audio is a recording already in the app (the same bytes) isn't added twice: it counts as already
  there.
- **Try again** on one failure retries just that one.
- It runs on the server (`ImportRunner`, a background service holding a Postgres advisory lock, so one API replica at
  a time); closing the page doesn't stop it, and an API restart carries on where it was.
- Progress is the `sermons.import` feed (a marker, about once a second while audio downloads), audience
  `sermons.import`; the page is `LiveData` on it.

## Time and storage

- **It takes a few hours:** about 7 to 10 seconds a sermon (mostly the audio download), so around two and a half hours
  for 887, longer on a slow connection. Leave it running; it carries on after an API restart.
- **Storage:** the audio adds about 49 GB to the public bucket (R2: $0.015/GB-month past the free 10 GB, so about $0.60
  a month; Cloudflare's price list, checked 2026-11-04). Pictures are small and kept once.
- **In development** the media store is SeaweedFS in Docker, so the import needs about 50 GB free on Docker Desktop's
  disk. When it runs out, every sermon after that fails with *Something went wrong*; free space and *Run it again*.
- **The trial run (2026-11-09, a copy of dev's database):** found 887; brought across 887 (884 sermons, since 3 were
  recordings already there, plus 34 series and 75 speakers); 0 failed in the end. About 2¾ hours of importing in all
  (the newest ~500 at about 6 a minute, the older ~390 at about 14 a minute), with 45 GiB in the media store. Running it
  again took 3 minutes: 887 found, 0 brought across, 887 skipped.

## Permission

`sermons.import` (*Import every sermon from the old website*), in no system role: the Owner holds it through `*`.

## Removing it after go-live

Once production has been imported and checked:

1. Delete `src/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Import.Contracts`, `.Import.Api`, `.Import.Admin`
   and `tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Import.Api.Tests`.
2. Remove their four lines from `GSBC.ChurchApp.slnx`.
3. `Hosts.Api`: the project reference, `AddSermonsImportApi` and `MapSermonsImportApi` in `Program.cs`.
   `Hosts.Migrator`: the project reference and `AddSermonsImportData`. `Hosts.Admin`: the project reference and
   `AddSermonsImportAdmin`.
4. `Sermons.Api`'s csproj: the two `InternalsVisibleTo` lines for `…Sermons.Import.Api` and its tests.
5. The project map: the four projects' nodes, arrows and table rows in
   [repo-layout.md](../architecture/repo-layout.md), then `python3 tools/docs/architecture.py`.
6. This doc, and its links from sermons.md.
7. Optional, in each database: `DROP SCHEMA sermons_import CASCADE;` (nothing reads it any more). `speakers.honorific`
   stays: it's Sermons' data.

The build and `ProjectMapRules` catch anything missed.
