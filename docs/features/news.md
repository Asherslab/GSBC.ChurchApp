# News

Status: interviewed 2026-11-13; being built on `feature/church-news`.

News for the whole church, not under a ministry, in its own place in the app and the admin panel; ministry posts put into
it; and one post editor and renderer, with full formatting and media, for church news and ministry news alike. The owner,
2026-11-13, the last piece before UAT: "News, currently only a feature under ministries. i also want an app wide News tab
specifically for the church as a whole (i.e. not under a ministry), it should allow (by staff) posting a news post from a
ministry's news to the church wide one if wanted. news will be expanded to allow full formatting and media to be
embedded/etc in them".

## The interview (owner, 2026-11-13)

1. **Church news replaces announcements.** "announcements was a way to send a pure notification. now news posts should be
   used for those, notifications center and everything still exists, but now what would have been an announcement with no
   link to anything, will now just be a news post, with a notification sent out linking to the news post." The
   notifications list, push, quiet hours and preferences stay as they are; what staff send is a church news post, and
   posting one can send a notification that opens it.
2. **The Announcements page goes** (owner's pick: "Page goes; posts link out"). Church news takes its place in the admin
   panel's sidebar. A post can carry a link to something else (an event, a sermon); its notification opens the post.
   Announcements already sent stay as read-only history, and those can still be recalled.
3. **A post's notification is on by default.** Its title and line come from the post and can be changed, it can be
   urgent (skipping quiet hours), it goes when the post is published (now or at a scheduled time), and it can be tried on
   your own phone first. Deleting a post recalls its notification.
4. **Service cancellations are urgent on the day** (owner, in the same answer: "service cancellations should be urgent if
   it's same day, always").
5. **Each post chooses who it's for**, Everyone by default: everyone (signed out too), everyone signed in, or people in
   chosen roles, with the audience picker Home's tiles and announcements use.
6. **Video and audio, linked or uploaded.** A YouTube or Vimeo link shows as a player (it needs the internet to play); a
   video or audio file can be uploaded into the Media library. **Up to 100 MB for anyone who can post; up to 2 GB with a new
   role for staff** ("allow a certain role that allows larger upload sizes for longer videos and such, nothing crazy, but
   staff can be more responsible with those things"). **Larger videos download on demand and don't stay offline**
   (anything over 25 MB plays from the internet when tapped); smaller ones are kept on the phone like pictures.
7. **Leaders ask, staff share.** A ministry post's *Ask to send it to the whole church* becomes *Ask for church news*; staff
   who publish church news see the asks and share a post with one tap, or say no. They can also share any for-everyone post
   without an ask.
8. **The News page** (decision page, 2026-11-13; the owner re-rolled the first hand with a steer): "pictures lead like
   front page for top post, other post move to image on side. include tab for your ministries (combined feed), hide tabs if
   person has no ministries". The newest post leads with its picture; the rest are rows with their picture at the side; a
   *Your ministries* tab gathers the news of every ministry you're in, and the tabs aren't shown to someone in no ministry.
   **Picked** (the owner's pick on the re-rolled hand): *What's urgent today first, then the lead*. A post sent as urgent
   today sits above everything on a marker band; then the newest post leads with its picture; then rows with the
   picture at the right (`.impeccable/mocks/church-news/r-c-today-first.html`).
