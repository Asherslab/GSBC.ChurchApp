# People and families

Detail behind [DESIGN.md](../../DESIGN.md) for people, safeguarding and families. Its rules win; safeguarding's colours are in its *Colors* (Danger).

## A person's page and roles
`/people/{id}`: a title band with a back link to People, only their name, and the account state pill when it needs staff (turned off, not invited yet, invitation expired); then tabs: Profile first, then Roles, What they can do, and Activity for people who manage accounts (owner, 2026-11-13, #81).
- **Profile:** two `Section Grouped` cards of facts. *Their details*: name, email, then each `PersonFact` a feature adds (Platform.Client, with its order and the permission it needs). *Their account*: state, passkey, joined, the invitation and the recovery link, with its actions (turn off, delete, the invitation's) in the card head's ⋯ menu.
- **Roles:** the ticks, one unbroken block per feature, beside a sticky sand explanation panel, capped together at 1280px; at 1328px the ticks go to one column and the panel narrows so it stays beside them, dropping below only under 680px. Then *For one thing*: role and place, with "Change it there".
- **Role tick:** a checkbox, the role name and an (i) that explains it; the tick stays the stronger mark than a chosen (i). A tick the viewer can't give is disabled, with the reason as its title and in the explanation.
- **Explanation:** the role's name, then a lead ("Announcements Writer lets {name}:"), a bulleted list, and for a kit "Also, from Media, because the job needs it:"; under a rule, who gave it and when.
- **Family and Safeguarding tabs:** laid out as Roles, the panel dropping under the content (no longer sticky) at 928px. Groups are headed "Waiting", "Children {name} looks after · 2", "{Name}'s parents and guardians".
- **Birth date:** the first `PersonFact` under *Their details*, "Birth date" ("3 Dec 2001 · 24 years old", ", under 18" for a young person, "Says they're 18 or older", or "Not given"), shown to those who may see families; those who may change them get *Change* (or *Add it*), a sheet "{Name}'s birth date" saying what the date decides, with Cancel, Save and Clear it. It isn't a family fact (#37, 2026-11-12).
- **Family:** how ministries reach a young person (Segmented, with a hint); Waiting asks on the warm tile with small Take it back and Link them; children and parents led by their Avatar with small quiet Open and Unlink; "No parent linked yet" on the warm tile while a young person has none; and accent add rows ("Link a child", "Link a parent") that open the person finder in place (for a child, also a name and birth date to add one who isn't in the app), closed with Done. The panel says what linking does.
- **Safeguarding:** any flag first, as a notice (due soon in marker, run out in danger). Then *Blue card* and *Safe Ministry training*, with Save beside quiet "Remove the blue card" and "Remove the training" and a hint of who recorded it and when. The panel says the record is entered by hand and never blocks anyone, and shows the blue card as the person sees it.

## People on a thing
The shared way to put people on one thing (a calendar, a regular event, a draft). Its panel has a **People row**: the names on one line, a count ("2 people") and a *People* button. It opens the wide **People sheet**: a lead saying what each role does; the person finder ("Add someone: find them by name", results with "Add as Editor"); then each person with "Added by …, 9 Nov", a small quiet *Take off*, and their roles for this thing as role ticks. The (i) opens the explanation inline under that person. Done closes it.

## Safeguarding page
`/safeguarding` (People area): a lead saying nothing here stops anyone serving, then a table. Filter chips with counts: Everything, Run out, Due in 60 days, Not recorded, Young people without a parent. Columns: the person, what it is, a status pill (Danger for run out, Marker for due soon and without a parent, Quiet for not recorded), and when, soonest first. A row opens the person's Safeguarding tab (Family tab for a young person without a parent). With nothing flagged, a shield-check empty state, "Nothing to follow up". Run-out items and young people without a parent are the Needs-you items: rows in the office week's Needs you ("Record the new one", "Link a parent"), and the only ones the sidebar's count on Safeguarding counts; due soon and not recorded wait on the page.

## Person finder
`PersonFinder` (Platform.Ui) finds someone to do one thing to: the People sheet's "Add someone", linking a child or parent. Its label and placeholder say who ("Find their parent or guardian by name"); it searches from two letters, and each match has a small primary button naming the act ("Link as their child", "Add as Editor"). People already there are left out. With no match: "No one matches “…”."

## Your family (Account)
On Account (both sides), *Your family* is one settings group, its lead saying what it's for ("Children you look after in the app.", or for a young person "Your parents see your details in the app while you're under 18."). The add row sits inside the group with the children it adds (#60, 2026-11-12). One list, in this order:
- **Asks of you** first, on the warm tile with a people icon (who asks, the child's age and when), with a quiet "Not mine" (or "Not my child") and a small primary "Yes, link us".
- **Children** you look after, led by their `Avatar`, with age and how they're reached, opening the child sheet.
- **Parents**, led by their Avatar, "Your parent · linked …", with a small quiet Unlink for an adult.
- **Your own asks** on the quiet tile with a clock ("Waiting for …"), with a small quiet *Take back*.
- **The add row** on the accent tile: "Add a child" for an adult, "Ask a parent to link to you" for a young person; a sheet whose primary names the result ("Add {name}"), and Not now.

Under the list, for a young person with a parent, a hint saying how they're reached. Your birth date lives in the **You** group after your email (and its code while unconfirmed), as a fact about you; features add there with an `AccountYouPart` (Platform.Client), or a section after You with an `AccountSection`. Until you've answered: a birth date field ("Like 4 Mar 2011"), its hint saying why the church asks and who sees it, a quiet "I'm 18 or older" and *Save birth date*; answered, "Born 4 March 2011" and who sees it; after "I'm 18 or older" nothing shows, and a demo account has none of it. Then *Blue card and training*, for anyone with either recorded: the blue card, and Safe Ministry training as a row (accent tile with a shield check when current; warm tile with an alert and a pill when not: Marker "Due in 12 days", Danger "Overdue").

## Child sheet
A child opens a sheet titled with their name:
- **Sign-in code:** a sand card ("Sign in on their own phone", or "A new sign-in for their phone"). Before: when to make one and "Make a sign-in code". After: the code, tabular and spoken digit by digit, a copy button, and how long it lasts.
- **How to reach them:** Segmented (Directly, Through you, Both) once the child can sign in, its lead saying what each means.
- **Details:** name (for a child the family added who can't sign in yet) and birth date, with a small quiet *Save details*.
- **Another parent:** waiting asks as clock rows with *Take back*, then an email field and a small quiet *Ask them*.
- **Remove:** a small quiet "Remove …" or "Stop looking after …" that confirms inline: a warning saying what happens, then Keep beside the small Danger action.
- Done (primary, block) closes it.

## Blue card (signature)
A Queensland blue card drawn as the card in your pocket (`BlueCard`): masthead navy with on-masthead text and the ring at its top right (see DESIGN.md, *Shapes*). Its head row: "QLD blue card" small and uppercase in the masthead note colour, and the status capsule: **current** in the live colour ("Current"), **due soon** in the marker ("Expires in 12 days"), **run out** white with danger text ("Expired", or the status the office recorded, such as "Suspended"). Then the number (tabular) and Expires and Checked. It shows on the person's own Account page and, as "How {name} sees it", in the Safeguarding tab's panel.
