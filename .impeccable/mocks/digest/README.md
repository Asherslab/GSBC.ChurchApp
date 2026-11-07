# The weekly digest: proof of concept

A quick, throwaway proof of concept to show church staff what "send the digest" from the calendar could look
like (events.md, decision 18). Static HTML only, with made-up events, people and places; no product code. It may
be scrapped quickly, so nothing here is settled design.

- **admin.html** (1440×900): the admin panel's *Weekly digest* page, under Calendar. It's pre-filled from the
  calendar's next two weeks of one-off events, specials and breaks. Staff tick or untick items, drag to reorder,
  and reword one for the digest without changing the event (the Outreach morning is open, reworded). Beside it,
  a preview of the app's digest, **Copy for email** and **Send to the app**.
- **app.html** (390×844): the digest in the app, opened from the notification that *Send to the app* posts. It's
  a page of the app in its own design language (masthead, feature cards, a callout for a break, Add to my
  calendar), never a copy of the email.
- **email.html** (600px wide): the plain HTML email that *Copy for email* produces, close in spirit to today's
  *What's Next at Good Shepherd*: a picture, a label, a when line, a description with the place and a contact,
  a Register button where there's a link, and a button to all events. The coloured blocks stand in for pictures.

Each file is self-contained (inline CSS, no JavaScript). Open one in a browser at the size given.
