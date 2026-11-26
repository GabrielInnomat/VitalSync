# Feedback and overlays

Part of the [Design System](./README.md).

## Live announcer

One place per page that speaks to screen readers. Everything that has to be announced without
moving focus — a toast, a banner, a message that appears later, a new field error, the start of a
button's loading when the button is not focused, a page's own "Rezepte geladen." — goes through
it, rather than each component bringing its own live region.

**Why one.** Live regions spread across components were read twice, once as the live region and once
as the visible message in front of or after it; a region with `role="alert"` in the first HTML was
read on page load, before the page's heading; and a region whose content is replaced announced only
the last of several messages raised in a row. One region, always present and visually hidden, is a
single place where those rules can be kept.

**Two regions, by urgency.** `LiveAnnouncerRegion` renders a polite one (`role="status"`) and an
assertive one (`role="alert"`), both empty at first. Each announcement is added as its own entry, so
every message of a burst is read, and removed again after five seconds, so the hidden text does not
pile up for someone reading the whole page. Nothing in them is focusable or visible.

**How it is used.** `LiveAnnouncer` is a scoped service: `AddScoped<LiveAnnouncer>()` in the host,
and `<LiveAnnouncerRegion @rendermode="InteractiveServer" />` once in each layout, next to the
toast and banner regions and for the same reasons. A page injects it and calls `Announce(text)` for
news that can wait or `AnnounceUrgent(text)` for news that cannot; a blank text throws. The
components use it on their own; a page only calls it for what is the page's to say, such as the end
of loading or the result of an action that shows no message of its own. Visible text on the page
stays where it is; the announcement is only the spoken copy.

## Messages

Three components tell the user something without being asked, and each has its own rules, because
each answers a different question:

- **`Alert`** belongs to the content. It sits where its subject is — in a card, above a form, in a
  section — and stays as long as the state it describes.
- **`AlertBanner`** belongs to the whole application. It runs across the top of the page for a
  state that affects everything, such as maintenance or being offline.
- **`Toast`** answers an action the user has just taken. It floats above the page, outside the
  content, and reports how the action went.

All three are built.

**What they share.** Every message has a mandatory title and a mandatory text. The kind is written
out at the start of the title, followed by a dash — "Fehler – Speichern fehlgeschlagen" — so it
reaches everyone in words and never depends on colour or icon (1.4.1). The words are "Information",
"Erfolg", "Warnung" and "Fehler". Each kind has a fixed icon from the [Icons](./foundations.md#icons) set: a check in
a circle for success, a triangle for a warning and an X in a circle for an error, as on `Badge`, and
an i in a circle for information. The icon cannot be replaced and stays `aria-hidden`,
because the title already says what it shows. Tone follows [Tone of voice](./foundations.md#tone-of-voice): calm
at every kind.

### Alert

**Four kinds:** `Info`, `Success`, `Warning`, `Critical`.

**The title is a real heading.** It looks like one, so 1.3.1 requires it to be one, and it makes
the message reachable by heading navigation. Only the page knows its outline, so the page sets the
level.

**At most one action.** A message that names a problem should offer the way out where the problem
is described: a link such as "Zutaten ergänzen" or a button such as "Erneut versuchen", below the
text. Kept inside the message, it stays next to its problem for everyone, including a screen reader
reading the message as one piece. Its name has to make sense together with the title (2.4.4), and
it has the 44×44 px target of every other control. One, because a message with a choice of actions
has become a form. An action that solves the problem removes the message, and the focused action
with it, so the page then moves focus itself, as it names the target after closing.

**Subtle fill, coloured border and icon, ordinary text.** Each kind has a subtle fill, a border and
an icon in its colour; title and text keep the normal text colour, which is measured against every
subtle fill at 4.5:1. Icon and border are held to 3:1. The bold fills `Badge` uses would turn a
paragraph of text into a block of colour.

**Information has its own colour.** The blue of actions would make an information message look
clickable. The new information colour joins the separation check for status text described under
[Data visualisation](./foundations.md#data-visualisation). Success and warning gain the subtle fill, border and
icon roles that so far only critical had.

**Only a message that appears later is announced.** A message that is part of the page when it
loads is not announced; it is found by reading and by heading navigation, like the rest of the
content, and announcing it on top of the page would interrupt the page's own announcement. A message
that appears later — after saving, after a check — is announced once, and the page states that it
does: the title and text are handed to the [`LiveAnnouncer`](#live-announcer), `Warning` and
`Critical` interrupting, `Info` and `Success` waiting until the screen reader is idle. The message
itself carries no live-region role, so a re-render cannot repeat it, and a screen reader reading the
page meets it once, with its title read once.

**When the user's own action brings the message, focus can go to it.** The title has
`tabindex="-1"` and is described by the text, so `FocusAsync()` moves focus there and a screen
reader reads title and text together. That suits a message that answers a button the user has just
pressed and that may stay where it is, such as a failed retry; the message then needs no
`Announce`, because moving focus already reads it.

**It can be closable, and is not by default.** Closing suits a message that is no longer needed
once read, such as a success. A message about a problem that still exists is never closable; it
disappears when the problem is solved. The close button is named "Schliessen" together with the
title, has the 44×44 px target of every other control, and on closing moves focus to an element the
page must name. Without that, focus would fall back to the start of the document (2.4.3), and only
the page knows what comes next.

**How it is used.** `Variant` defaults to `Info`. `Title`, `HeadingLevel` (2 to 6) and the text as
`ChildContent` are mandatory, and a blank title, a missing text or a level outside that range
throws. The single action is passed as `Action`. `Announce` marks a message that appears later and
has it announced once, only from an interactive render, never from prerendering. `OnClose` adds the
close button; it requires `CloseFocusTarget`, a
function that returns the element to focus, and throws without it. Focus moves first and `OnClose`
runs after, so the page can remove the message without focus being lost on the way. A message about
a problem that still exists must not be given `OnClose`; the component cannot tell, so that stays
the page's responsibility.

**The close button.** It shows the bare X described under [Icons](./foundations.md#icons), in the secondary text
colour and the primary one on hover, with an invisible 44×44 px hit area centred on it, like
[`Toggletip`](./forms.md#toggletip)'s help button. Its visually hidden name is "Schliessen:" followed by the
full title, so two closable messages never share a name.

**Information is sky, not the action blue.** `sky-700` in light and `sky-400` in dark — still
bluish, as information is expected to be, but apart from the `blue-600` of links and buttons.

**Narrow space breaks words, never the border.** With large text in a narrow column, long words in
the title, the text and the action break inside the message instead of running past its edge
(1.4.4, 1.4.10).

**Tokens and rules.** New semantic roles: `--color-text-info`, and a subtle fill, border and icon
colour for information, success and warning, matching the existing critical ones. The `--alert-*`
set: padding, gaps, radius, a thin border, icon and close-icon size and colour, the text colour,
the title weight, and fill, border and icon colour per kind. Twelve rules per kind, in both themes:
the text and the icon on the fill, the border against page and card, the close icon at rest and on
hover, a link at rest and on hover, a secondary button's text and border, a primary button's fill,
and the focus ring, all on the fill.

### Alert banner

**Built like `Alert`.** Same subtle fill per kind, coloured icon, title format, text and ordinary
text colour, but across the full width of the viewport: no radius, and only a bottom border, which
separates it from the page below. The text keeps a readable line length of about 70 characters and
stays left-aligned; a wide screen leaves the rest of the bar empty rather than stretching the line.

**Three kinds:** `Info`, `Warning`, `Critical`. A success is never news for the whole application.

**The title is a fixed second-level heading.** It is reachable by heading navigation like
`Alert`'s, but a banner sits outside every page, above the page's own `<h1>`, so no page could set
its level. Level 2 places it below the document's top without claiming to be the page's title.

**At most one action, and it is a link.** Such as "Mehr erfahren", leading to where the state is
explained. A banner lives outside every page, so a button there would run code that belongs to no
page; a link only needs a target. It has the 44×44 px target of every other control, and its name
has to make sense together with the title (2.4.4).

**Announced through the [`LiveAnnouncer`](#live-announcer), as with `Toast`.** A banner present
when the region first renders is not announced; it is found by reading, by heading and by landmark
navigation. One that appears later, such as "Du bist offline", is: `Warning` and `Critical`
interrupt, `Info` waits until the screen reader is idle. Showing a banner again with exactly the
same content is not news and is not announced again; showing it with a changed content is. The
banner region holds no live region of its own, so the message exists once on the page, with its
close button, and not a second time without it. A banner shown while the page itself is still
starting up follows the same urgency: `Warning` and `Critical` are announced, an `Info` is not, as
the manual test with JAWS showed. Such an `Info` was there when the
page opened, so it is no news; it is found in the "Systemmeldungen" landmark and by its heading. The
component gallery has a demo that shows a banner during start-up.

**At the very top, in its own landmark.** Banners sit above the header and the navigation, in a
region named "Systemmeldungen", which screen readers list among the landmarks and can jump to. The
region exists only while there is a banner.

**It scrolls away.** A bar fixed to the top would cover too much of the page at high zoom and on
small screens (1.4.10).

**Closable like `Alert`.** Optional, and never for a problem that still exists; the component
cannot tell, so that stays the caller's responsibility. The close button is `Alert`'s: the bare X
with an invisible 44×44 px hit area and the visually hidden name "Schliessen:" followed by the full
title. Closing moves focus to the main content, as closing the last toast does. A closed banner
stays closed until the page is reloaded, even when it is shown again; nothing is stored.

**Several at once stack.** One under the other, the most severe first — `Critical`, `Warning`,
`Info` — and within a kind in the order they first appeared. There is no limit: each banner stands
for a state that is true right now, and hiding one would hide that state.

**Raised through a service, by key.** Pages inject the scoped `AlertBannerService` and call
`ShowInfo`, `ShowWarning` or `ShowCritical` with a key, a title and a text, and optionally an
`AlertBannerLink` (text and target) and `closable`. Key, title, text and both link values must not
be blank. The key names the state the banner describes: showing again under the same key replaces
the banner in place, and `Remove(key)` takes it away once the state is over — "offline" is shown
when the connection drops and removed when it is back. A removed banner can be shown again; only one
the user closed cannot. The service may be called from any thread. No page renders a banner itself;
`AlertBanner` is public only because Razor makes every component public, and exists for the region
and the gallery.

**The region is an interactive island in a static layout,** for the same reasons as `Toast`'s:
`AlertBannerRegion` is placed once in each layout with its own `@rendermode="InteractiveServer"`,
shares the circuit and so the service with every interactive page, keeps its banners across
enhanced navigation, and is prerendered. The same
limit follows: a banner can only be raised from an interactive page. Nothing connects the service to
the backend yet; deciding where states such as maintenance or being offline come from is left to
the feature that needs them.

**Narrow space breaks words, never the bar.** Long words in the title, the text and the link break
inside the banner instead of running past its edge (1.4.4, 1.4.10).

**Tokens and rules.** No new semantic role. The `--alert-banner-*` set: padding, gaps, the line
length, a thin border, icon and close-icon size and colour, the text colour, the title weight, the
link colour at rest and on hover, and fill, border and icon colour per kind — all pointing at the
same semantic roles as `Alert`'s. Nine rules per kind, in both themes: the text, the icon, the close
icon at rest and on hover, the link at rest and on hover and the focus ring, each on the fill, and
the border against the page and against a card's surface, since the bar can sit above either.

### Toast

**Three kinds:** `Success`, `Info`, `Critical`. Success confirms an action. Information reports
an outcome that is neither success nor failure, such as "Keine Änderungen – Es gab nichts zu
speichern." Critical reports a failure that has no place on the page, such as a request that ran in
the background or an action whose form is already gone; a failure that belongs to a form or a
section is an `Alert` there. There is no warning toast: a warning about an action's result belongs
next to that result.

**Icon, title and text, nothing else.** No action. The close button is the only control.

**The title is not a heading.** It is set apart as emphasised text. Toasts come and go, and as
headings they would keep changing the page's outline; they sit outside the content, in their own
landmark, and are found through that.

**It stays until it is closed.** No time limit, so 2.2.1 is met without a setting to extend or turn
off a timeout, and a message can be read at any pace.

**Announced through the [`LiveAnnouncer`](#live-announcer).** Showing a toast hands its title and
text to the announcer, which is in the page from the start; a live region created together with its
text is not announced reliably. `Success` and `Info` wait until the screen reader is idle, so a
confirmation does not cut into what the user is doing; `Critical` interrupts. Each toast is its own
announcement, so several raised in a row are all read, not only the last one. The toast region
itself is not a live region: it would be a second copy of the same message, and a screen reader
reading the page would find the message twice.

**It never takes focus.** Toasts sit in a region named "Benachrichtigungen" at the end of the page,
reached with Tab and by landmark navigation. The region exists only while there is a toast. Inside
it the toasts form a list in the order they appeared, oldest at the top and newest at the bottom,
next to the corner — the order on screen is the order in the document, so reading and tabbing
follow what is seen (1.3.2, 2.4.3). Closing a toast moves focus to the close button of the toast
below it, or of the one above when it was the newest, or to the main content when none is left.
For that, `<main>` carries `tabindex="-1"` in every layout, which closing an `AlertBanner` relies
on too.

**Bottom right; on narrow screens full width, and only the newest.** Three full-width toasts would
cover too much of the page, and high zoom makes every screen narrow (1.4.10). The others wait in the
stack and show again once the newest is closed. Wide screens give the region 24rem; below the `md`
breakpoint it spans the viewport. Because the region is fixed to the viewport, this is the one
place where a viewport media query is right rather than a container query. When toasts outgrow the
viewport's height, the region scrolls.

**At most three.** A fourth makes the oldest give way, never the one that holds focus. A toast only
reports on an action and carries nothing the user needs later, so giving way loses nothing.

**Looks like `Alert`, and floats.** The same subtle fill per kind, coloured border and icon and
ordinary text colour, plus a shadow that lifts it off the page. Long words break inside the toast,
never past its edge. The close button is `Alert`'s: the bare X with an invisible 44×44 px hit area
and the visually hidden name "Schliessen:" followed by the full title.

**Raised through a service.** Pages inject the scoped `ToastService` and call `ShowSuccess`,
`ShowInfo` or `ShowCritical` with a title and a text, both plain strings; a blank one throws. The
service may be called from any thread, such as when a background request finishes. No page renders
a toast itself. `Toast` has to be public, because Razor generates every component as a public
class, but it exists for the region and the gallery only.

**The region is an interactive island in a static layout.** Layouts render statically, and only
pages choose `InteractiveServer`, so a region rendered as part of the layout could never change
after the page loaded. `ToastRegion` is therefore placed once in each layout with its own
`@rendermode="InteractiveServer"`. All interactive components on a page share one circuit and so
one `ToastService`, which is how a page's call reaches the region. The circuit survives enhanced
navigation, so open toasts stay when the user moves to another page. The region is prerendered.
Two limits follow. A toast can only be raised
from an interactive page; a statically rendered page has no circuit to call the service in. And a
modal [Dialog](#dialog) sits in the browser's top layer, above everything including toasts, and
makes the rest of the page inert while it is open; a toast raised then is announced but can be
reached only once the dialog closes.

**Tokens and rules.** No new semantic role. The `--toast-*` set: width, the region's margin, the
gap between toasts, padding, gaps, radius, a thin border, the shadow, the z-index, icon and
close-icon size and colour, the text colour, the title weight, and fill, border and icon colour per
kind — all pointing at the same semantic roles as `Alert`'s. Seven rules per kind, in both themes:
the text, the icon, the close icon at rest and on hover and the focus ring, each on the fill, and
the border against the page and against a card, since a toast can float over either.

## Empty state

`EmptyState` stands where an area that works has no data yet: no recipes, no workouts, too little
data for an analysis. It says what is missing, how the area fills, and offers at most one way to
start. It lives in `Patterns/Feedback/` next to the messages, but is not one of them: it reports no
event and no problem, only that nothing has been created yet.

**Only "nothing created yet".** A search or filter that finds nothing is a different situation —
the data exists, the user narrowed it — and belongs to the [`DataTable`](./data.md#data-table). Data that
failed to load is a problem and is shown with an [`Alert`](#alert).

**It replaces, it does not sit inside.** A [`Table`](./data.md#table) keeps its own empty row for a table
that happens to be empty for a moment. When a whole area is empty because nothing has been created
yet, the page shows `EmptyState` instead of the table. The component cannot tell the two apart, so
the choice stays with the page.

**The title is a real heading.** It names what is missing — "Noch keine Rezepte" — and looks like a
heading, so 1.3.1 requires it to be one, and heading navigation finds it. Only the page knows its
outline, so the page sets the level.

**A sentence says how it fills.** Below the title, in the secondary text colour, at most about 70
characters per line. It names the step that fills the area, not only that it is empty.

**At most one action.** A link or a button below the text, such as "Rezept anlegen". One, because
the empty state points at the first step, not at a menu. Its name has to make sense together with
the title (2.4.4); a link gets the 44×44 px target of every other control, as in `Alert`.

**The icon is decoration.** An optional icon from the [Icons](./foundations.md#icons) set, larger than the text and
in the secondary text colour, `aria-hidden`, because the title already says everything. No
illustrations: they would need their own colours for both themes and add nothing for a screen
reader.

**Quiet and centred.** No border and no fill, so it fits on the page and in a card alike. Icon,
title, text and action stand centred one below the other. Its padding is vertical only: the page or
the card already gives the side margin, and a second one would leave too little width at 400 %
zoom (1.4.10). Long words break rather than overflowing in a narrow column.

**Nothing is announced.** The empty state is part of the page's content when the page loads and is
found by reading and by heading navigation, like the rest of it. It carries no live-region role.

**How it is used.** `Title`, `HeadingLevel` (2 to 6) and the text as `ChildContent` are mandatory;
a blank title, a missing text or a level outside that range throws. `Icon` takes a
`SystemIconName`, and the single action is passed as `Action`. Further attributes, including
`class`, are passed on to the outer element.

**Tokens and rules.** No new semantic role. The `--empty-state-*` set: vertical padding and gap,
the icon's size, colour and spacing, the title's size, weight, line height and colour, the text's
size, maximum width and colour, and the space before the action. Six rules, in both themes: the
title, the text and the icon, each on the page and on a card.

## Dialog

A modal dialog: the page behind it cannot be used until it is closed. `Dialog` is the ordinary
dialog, `ConfirmDialog` the confirmation; both are built on one shared frame.

**Built on the native `<dialog>`.** Opened with `showModal()`, the browser makes the rest of the page
inert, so neither Tab nor a screen reader's reading cursor can leave the dialog. It also places the
dialog above everything else and closes it on Esc. A self-built overlay would have to imitate all of
that in script and would get it wrong in places. `showModal()` cannot be called from markup, so the
dialog brings the library's first JavaScript module.

**Two kinds.** The ordinary dialog holds content or a form. The confirmation asks one question
before an action, such as "Eintrag löschen?", and is exposed as `role="alertdialog"`, so screen
readers announce it as a question that needs an answer.

**The title is mandatory.** It names the dialog, and the dialog is announced with it.

**Ways out.** The ordinary dialog always has the X at the top right, the bare X described under
[Icons](./foundations.md#icons), and may also have an "Abbrechen" button among its buttons. The confirmation has no
X; its "Abbrechen" button does that job. Esc always closes and counts as cancelling. A click on the
dimmed background does not close: a click beside the dialog is often a slip, and it would discard
what was typed. With the X, "Abbrechen" and Esc there are enough ways out, and all of them are
reachable by keyboard.

**Unsaved input.** Every way of closing an ordinary dialog first asks the page. The page may refuse
and open a confirmation on top instead, such as "Änderungen verwerfen?". That is the only dialog
allowed over another one; nothing else nests. Esc then closes only the confirmation. A
[`Toggletip`](./forms.md#toggletip) open inside a dialog takes Esc first; the next Esc closes the dialog.

**Focus on opening is fixed.** In the ordinary dialog with a form it moves to the first field, so
typing can start at once. A dialog whose content has no field — text to read, perhaps with a link
or a button in it — puts focus on its title instead, which takes `tabindex="-1"` for that, so a
screen reader starts reading at the top rather than in the middle or at the buttons, and Tab from
there follows the order on screen. In the confirmation it moves to "Abbrechen", so an Enter
pressed too early never confirms. The page cannot change either.

**Focus on closing** returns to the element that opened the dialog. When that element is gone —
after a deletion, say — the page must name the target, as it does when an [`Alert`](#alert) closes;
otherwise focus would fall back to the start of the document (2.4.3).

**Sizes.** The confirmation is narrow. The ordinary dialog is medium, and can be set to fill the
whole screen.

**Small screens and zoom.** The dialog takes nearly the full width and is never taller than the
screen. Only its content scrolls, so the title and the buttons stay in view, and the scrolling area
can be reached with the keyboard even when it holds nothing operable. When the screen is too low for
that, the whole dialog scrolls instead, so the content never shrinks to nothing (1.4.10).

**Buttons.** At the bottom, aligned right, the main action last and furthest right. When space runs
short they stack, in the same order. What is seen first is always reached first with Tab (1.3.2,
2.4.3).

**The page behind.** It is dimmed with `--color-scrim` and does not scroll while the dialog is
open.

**Border and shadow.** A thin border as well as the shadow, so the dialog stays clearly separate in
the dark theme, where shadows barely show, and in forced colours, where they are removed.

**No animation.** The dialog appears and disappears at once.

**How it is used.** Both are controlled by the page through `Open`; the dialog never closes itself.
`Title` and the content as `ChildContent` are mandatory, and a blank title or a missing content
throws. The title is always an `<h2>`, because a dialog starts its own outline above the page.

- `Dialog` requires `OnClose`, which the X, "Abbrechen" and Esc all call. The page closes the
  dialog by setting `Open` to `false`, or refuses and opens a confirmation instead. `CancelButton`
  adds "Abbrechen"; the page's own buttons go into `Actions`, the main one last. `Fullscreen` fills
  the screen. A form inside keeps its submit button in `Actions` and links it with the `form`
  attribute, so Enter in a field still submits.
- `ConfirmDialog` requires `ConfirmLabel`, `OnConfirm` and `OnCancel`; Esc calls `OnCancel`.
  `ConfirmVariant` is `Primary` by default and `Critical` for a destructive action; `Secondary`
  throws, because it is taken by "Abbrechen".
- `CloseFocusTarget` is a function that returns the element to focus once the dialog has closed. It
  is required when the opener no longer exists afterwards; the component cannot know that, so it
  stays the page's responsibility.

**The close button.** The bare X described under [Icons](./foundations.md#icons), in the secondary text colour and
the primary one on hover, with an invisible 44×44 px hit area, like the close button of the
[messages](#messages). Its visually hidden name is just "Schliessen": the dialog's own name already
says what it closes.

**The scrolling area.** When the content scrolls and holds nothing operable, it becomes a Tab stop
of its own, exposed as a region named by the title, so it can be scrolled with the arrow keys
(2.1.1). This is checked again whenever the dialog changes size.

**Script.** One JavaScript module opens and closes the dialog, sets the focus, locks page scrolling
and reports Esc and the browser's own close to the component. Everything else, from the inert page
to the top layer, is the browser's.

**Tokens and rules.** A new semantic role, `--color-scrim`: `--neutral-950` at `--opacity-scrim`.
The `--dialog-*` set: the fill, which is `--color-background-elevated-3`, the text colour, a thin
border in `--color-border-default`, radius, shadow, padding, gaps, outer margin, the two widths,
the title size and weight, and the close icon's size and colours. Twelve rules, in both themes, all
on the dialog's fill: the text, the close icon at rest and on hover, the focus ring, a link at rest
and on hover, a secondary button's text and border, a primary and a critical button's fill, and the
borders of a field and of a checkbox or radio button. A link in the action colour first fell short
on the dark fill, so dark `--color-background-elevated-3` was lowered to `#2a3755`. The dimmed page
behind is not measured; it is not something to read while the dialog is open.

## Tabs

`Tabs` splits one page into a few views of the same subject, such as the days of a week plan, and
shows one at a time. It follows the ARIA tabs pattern; it is not page navigation, which stays with
ordinary links.

**Two to five tabs.** Fewer is not a choice, more no longer fit on a phone and turn into a list
nobody scans. Both limits throw, as do a blank label, key or title, a tab without content and two
tabs with the same key.

**Every tab has a title.** It may also have an icon in front and a badge after it. The icon only
supports the title and is hidden from screen readers. The badge is short text, such as a count; it
is read as part of the tab's name, so a bare number gets a hidden description from the page ("3"
becomes "3 offen" for a screen reader), and the visible text still starts the name (2.5.3). It is
a small text pill of its own, not the `Badge` component, which is a full-height status label with
a mandatory icon.

**Automatic activation.** Moving to a tab shows its panel at once; there is no second key press to
open it. Switching costs nothing here: all panels are already on the page.

**Keyboard.** The row of tabs is a single Tab stop, landing on the selected tab. The arrow keys
move between tabs and wrap from the last to the first and back; Home and End jump to the first and
the last. Space and Enter select as well; before the page is interactive only Enter does, because
the tab is then a plain link. The next Tab moves into the panel.

**The panel is always a Tab stop.** Even when it holds a button or a field, so the step from the
tabs into the content always lands on the same place and a screen reader announces which panel
that is.

**Inactive panels stay on the page, hidden.** Their state survives switching: a half-typed field
is still there when the user comes back.

**The selected tab** has a thick bar in the action colour along its bottom edge, semibold text and
the action colour. The bar and the weight carry the state without colour (1.4.1); the bar alone is
measured at 3:1 against the page (1.4.11). The other tabs use the secondary text colour and the
primary one on hover. Every tab reserves the width of its semibold title, so switching never moves
its neighbours.

**Only horizontal, and nothing hidden.** When the row no longer fits — a narrow phone, 400 % zoom —
the tabs stack, each one full width, with the bar on the start side; Up and Down then work as well
as Left and Right. There is no scrolling row and no "more" menu, both of which hide tabs (1.4.10).
The row never widens the area it sits in; it is measured against it, so a grid or flex parent
cannot hide the overflow that should make it stack.

**One size.** At least 44 px high, the same touch target as `Button`.

**No disabled tabs**, and tabs cannot be closed or reordered.

**The selected tab is in the address.** The page binds the selected key with `@bind-Active`; the
component also keeps it in the query string, `?tab=…` by default. A reload or a shared link
therefore opens the same tab. Switching replaces the address rather than adding to the history, so
"Back" leaves the page instead of stepping through tabs.

**It works without the live connection.** Every tab is a real link to its own address. Before the
page is interactive, or after the connection has dropped, a click simply loads that address. Once
interactive, the click is taken over and the tab switches without a reload; a middle click or
Ctrl+click still opens the tab in a new browser tab.

**No animation.** The panel changes at once.

**How it is used.** `Tabs` takes the mandatory `Label`, the accessible name of the row, and one
`Tab` per view. Each `Tab` has a mandatory `Key`, which is what ends up in the address and in
`@bind-Active`, a mandatory `Title` and its content; `Icon`, `Badge` and `BadgeDescription` are
optional, and a description without a badge throws. Without a bound value or a key in the address,
the first tab is selected, and the component reports it back through `@bind-Active`. A key in the
address wins over the bound value when the page loads; a later change of the bound value wins over
the address. An unknown key falls back to the first tab. `QueryName` changes the name in the query
string; two `Tabs` on one page need two names, or they would share one address entry.

**Script.** One JavaScript module handles the keys, the click on a tab, the check whether the row
still fits and the address. Blazor renders every role and state itself; the module never sets
`aria-selected`. The address is replaced in the browser's history directly, because a navigation
through Blazor would reload the page that is not yet interactive.

**Tokens and rules.** The `--tabs-*` set: minimum height, padding, gap, icon size, the two text
weights, the text colours at rest, on hover and selected, the bar's colour and width, the divider's
colour and width, the badge's fill, text colour, size, weight, padding and radius, and the space
above the panel. No new semantic role. Nine rules: the tab text at rest, on hover and selected, and
the bar, each on the page and on a card, in both themes, plus the badge text on the badge fill.
