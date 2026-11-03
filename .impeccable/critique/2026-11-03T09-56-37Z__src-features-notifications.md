---
target: this round's screens (notifications, announcements, analytics, app review, drafts notices)
total_score: 23
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 5
target_identity: "file:/Users/asherp/Personal-Projects/Git/GSBC.ChurchApp.worktrees/feature-design-review/src/Features/Notifications"
timestamp: 2026-11-03T09-56-37Z
slug: src-features-notifications
---
Method: dual-agent (A: canvas station term-mw0i29ja · B: canvas station term-mw0hw2h1). Round reviewed: notifications inbox/bell/settings, Announcements, App health, Insights, Help improve, App review, sign-in code, drafts live notices; What's new for regressions.

Heuristics: 1 Visibility 2 · 2 Match 3 · 3 Control 2 · 4 Consistency 2 · 5 Error prevention 2 · 6 Recognition 2 · 7 Flexibility 2 · 8 Aesthetic 3 · 9 Recovery 2 · 10 Help 3 = 23/40 Acceptable.

Specificity: copy authored throughout; Announcements and Insights category-interchangeable in composition; App health and App review on-system. Detector: 0 markup findings (16 targets); browser: ink-3 on sand 4.16:1 (Announcements headers, hint, placeholder), Insights line length 92ch, Drafts h1→h3, pulsing layer dot without reduced motion, Insights summaries 20px tall. overused-font montserrat is a false positive.

Priority issues:
- [P1] Church-wide Send now: no confirm, no sent feedback; Send to my phone and Delete silent (AnnouncementsWorkbench.razor:52,161).
- [P1] Staff-only kinds listed in every member's notification settings (NotificationPreferences.cs:27).
- [P1] Announcements absent from office week, Needs you, sidebar count (NotificationsAdminExtensions.cs:13).
- [P1] Composer doesn't autosave; edits lost on navigation.
- [P1] Review code shown once without Copy (MakeReviewCode.razor:8; also PersonDetail.razor:28).
- [P2] TrendLine text scales with width; empty axes; unnamed focus stops; comparison line 2.3:1.
- [P2] Two screens named Notifications; refused state has no Open Settings.
- [P2] Keep mine doesn't save; "someone published or deleted it" vague.
- [P2] Pill tones invert the Marker Rule (Crash, Expired, Turned off).
- [P2] App health/Insights/App review sidebar groups; App health window vs detail mismatch.
- [P2] Opens field takes raw routes; Send at is the raw datetime control.

Personas: office administrator double-sends, loses text, loses code; older member finds staff switches, no way to Settings; Sam: unnamed chart stops, "Back to Back"; Alex: no shortcuts, inert focusable rows.
