---
title: The app puts itself right after a server restore
audience: everyone
icon: refresh
area: Testing
---
If the church's server is restored or rebuilt, the app notices and reloads what it shows, so nothing appears twice or out of date.

## For staff
Changed: Sermon series, events or anything else showing two or three times after a server change now sorts itself out. The app sees that it's talking to a different database and fetches everything again, once. The admin panel does the same in an open tab.
Where: The app and the admin panel, on the next sync after this update reaches your phone.
Try: Open Sermons with a signal: each series should show once. If you saw duplicates before, they're gone after the app has finished syncing.
