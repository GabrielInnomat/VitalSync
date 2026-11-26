# Data

Part of the [Design System](./README.md).

## Table

`Table` shows items of one kind in rows and columns, such as the nutrients of a few foods. It only
displays; filtering, sorting and paging belong to the [`DataTable`](#data-table) built on it, not
to this component. It is our own markup, not QuickGrid, so it carries the system's structure and
styling without a second set of conventions.

**A real table.** A `<table>` with `<caption>`, a `<th scope="col">` per column and one row header
per row. A screen reader therefore announces the column and the row with every cell (1.3.1).

**The caption is visible and mandatory.** It names the table and the scroll area around it; a
blank caption throws. It sits above the table, larger and semibold.

**One column names the rows.** By default the first one; `RowHeader="true"` on another column moves
it there, and `RowHeader="false"` on the first column leaves the table without row headers, for
tables whose rows have no name. Two row-header columns throw. The row header keeps the cell's
appearance with medium weight and the header's text colour, so it reads as the row's name without
looking like a second header.

**Numbers align on the right.** A `Numeric` column aligns its header and its cells to the end and
uses tabular figures, so digits of the same place stand above each other. Numbers do not wrap.
Every other column aligns to the start, and a column always shares one alignment from header to
last row.

**Only horizontal lines.** A thick line in `--color-border-default` under the header, thin lines in
`--color-border-subtle` between the rows. No zebra stripes and no row hover: rows are not clickable,
so nothing should suggest they are. Cells may hold links, buttons and badges; those carry their own
interaction.

**One density.** There is no compact or spacious variant.

**Empty.** Caption and header stay, and one cell across all columns says "Keine Einträge
vorhanden." in the secondary text colour. `EmptyText` replaces the sentence; a blank one throws.

**Too wide scrolls sideways.** The table never squeezes its columns below their content. When it
is wider than its space — a phone, 400 % zoom — it scrolls inside its own area, not the page
(1.4.10). That area is then a region named by the caption and a Tab stop, so the keyboard can
scroll it (2.1.1); while the table fits, it is neither, so there is no empty Tab stop. Before the
page is interactive the area is always a Tab stop, because there is nothing yet to measure whether
it overflows. The caption's text stays in view while the table scrolls under it.

**The scroll edges show.** A soft shadow on the side where more content is hidden, in the new
semantic role `--color-scroll-shadow`; it disappears at the end. The shadow is decoration only —
the scrollbar is the real cue, and its thumb is measured at 3:1 against the page and the card
(1.4.11). Both sides mirror in right-to-left text.

**Focus rings are not clipped.** The first and last cells keep a padding at the table's edges as
wide as the focus ring and its offset, so a focused link or button in an edge cell shows its full
ring inside the scroll area.

**No sticky header.** A header row that stays on top while the page scrolls cannot work inside an
area that scrolls sideways — the browser ties it to the nearest scrolling ancestor, which is the
table's own area. It is left out until a real use case shows how long tables get.

**How it is used.** `Table` takes the mandatory `Items` and `Caption` and one `TableColumn` per
column. Each `TableColumn` has a mandatory `Title` and a template that gets the item as `context`;
`Numeric` and `RowHeader` are optional. A table without columns throws, as does a `TableColumn`
outside a table. Columns may be shown conditionally; they keep the order they have in the markup,
and a template that changes is shown at once.

**It measures itself.** The table sits in an inline-size container, so it never widens a grid or
flex parent but scrolls instead; a parent that sizes itself to its content (an inline block, an auto-width grid or flex
item) would therefore collapse it and has to give it a width.

**Script.** One JavaScript module measures whether the area overflows and where it is scrolled,
and sets the edge shadows. It tells the component only whether the area overflows; the component
renders the region role, the name and the Tab stop itself.

**Tokens and rules.** The new semantic role `--color-scroll-shadow` in both themes. The `--table-*`
set: the text size and colour; the caption's size, weight, colour and spacing; the header's weight,
colour and line; the row header's weight; the row line; the cell padding; the edge padding; the
empty text's colour; the scrollbar thumb; and the shadow's colour and size. Ten rules, in both
themes: the cell text, the header, the caption and the empty text, each on the page and on a card,
and the scrollbar thumb on both.

## Data table

`DataTable` is a [`Table`](#table) that filters, sorts and pages: a list of foods, workouts or
measurements too long to read at once. It renders the same table — caption, row header, numbers on
the right, sideways scrolling — and adds a filter area above it and page links below it. It lives in
`Patterns/Data/` next to `Table`.

**One filter per column, no general search.** Only a column that opts in gets a filter, and every
filled filter applies together (AND). A single search box would leave the user guessing which
columns it searches; a field per column says it.

**What a filter understands depends on the column.**

- **Text** finds every row that contains the entry, ignoring case. There are no wildcards.
- **Numbers** take `<`, `<=`, `>`, `>=` and `=` (also `≤` and `≥`), a range like `10-20` (with a
  hyphen or an en dash), or a number alone, which means "equal". They are written the Swiss way:
  `7.5`, `10’000`. A range given backwards is read the right way round.
- **Dates** take the same comparisons and ranges, written as `25.11.2026`.

**The filter area is a form.** A `<fieldset>` whose legend is the caption plus "filtern", with one
[`Input`](./forms.md#input) per filter and its visible label, laid out in as many columns as fit and stacked
on a narrow screen. Number and date fields carry a [`Toggletip`](./forms.md#toggletip) with examples, because
the syntax cannot be guessed. When any filter has text, a link "Alle Filter zurücksetzen" follows.

**Filtering waits until the user stops typing.** A filter applies 800 ms after the last keystroke,
and at once on Enter or when focus leaves the field. A table that rebuilds with every letter makes
a screen reader talk over the user and moves the rows under the pointer; waiting longer than a
typing pause avoids both. The delay was confirmed by hand with JAWS.

**A wrong entry suspends only its own filter.** It shows the error text under the field the way
[form validation](./forms.md#form-validation) does, with `aria-invalid`, and names an example of what works.
The other filters keep applying. While the user is still typing, a new error stays hidden; it
appears on Enter or when focus leaves the field, so the field does not scold a half-written entry.

**The count is announced, once.** After filters apply, the [`LiveAnnouncer`](./feedback-and-overlays.md#live-announcer) says
politely "{n} Treffer" or "Keine Treffer" — only when the count changed, so refining a filter that
does not change the result stays silent.

**No results is not an empty table.** When filters exclude every row, the one row across all
columns says "Keine Treffer." with the link "Alle Filter zurücksetzen". The table's own empty text
is kept for a table without data. Resetting clears every filter, returns to page 1 and moves focus
to the caption, because the link that had focus is gone.

**Sorting by one column.** A sortable column's header is a button; pressing it sorts ascending,
pressing it again descending, and another column starts ascending again. The sorted header carries
`aria-sort` and shows the direction as an arrow with the word "aufsteigend" or "absteigend", never
as an icon alone (1.3.1, 1.4.1). The other sortable headers show a neutral double arrow, so they
read as operable before they are used. The word is hidden from screen readers and nothing is
announced: the screen reader already reports the change of `aria-sort` in its own words, and
saying it again — once from the button text, once from the announcer — made JAWS repeat the
direction three times. Sorting returns to page 1.

**Pages of 25.** Below the table, a `<nav>` named after the caption lists the first and last page,
the current one and its neighbours. A gap is an ellipsis hidden from screen readers; a gap of a
single page shows that page instead, since the number takes no more room than the ellipsis. Each
link reads "Seite n": the word is visually hidden text inside the link rather than an `aria-label`,
because JAWS announces an accessible name that differs from the visible text as additional content.
The hidden word keeps its trailing space, so it is read as "Seite 3" and not as "Seite3".
The current one has `aria-current="page"` and is marked by fill and
weight, not by colour alone; in Windows high contrast it uses the system highlight. "Vorherige" and
"Nächste" are text with an arrow and appear only where they lead somewhere. Every page link is at
least 44 px in both directions. With more than one page, the caption ends with the page, such as
"Lebensmittel, Seite 3 von 10", so a sighted reader sees it at the top of the rows. After a page
change, focus moves to the caption, so a screen reader reads that page as confirmation and the
next Tab starts at the top of the new rows rather than at the bottom of the page. The caption is
rendered as a new element whenever its text changes: when only the text of the focused caption
changed, JAWS kept reading the previous page from its own copy of the document. A page beyond
the last is read as the last.

**The address keeps the state.** Sort, order, page and filters are query parameters prefixed by
the table's `QueryName`: `{name}.sort`, `{name}.order=desc`, `{name}.page`, `{name}.filter.{key}`.
They are replaced in place, without a history entry per keystroke; reloading or coming back
restores them, and other parameters on the page are left alone. A sort key the table does not know
is ignored. Page links and the reset link are real links to that address, so they work before the
page is interactive; once it is, a script catches the click and the component changes the page
without a navigation. Filters need the page to be interactive.

**Two ways to get rows.**

- **`Items`** hands over everything at once; the component filters, sorts and pages in memory.
- **`ItemsProvider`** asks for one page at a time, for data that stays at the BFF. It receives a
  `DataTableRequest` — the parsed filters by column key, the sort, the page, the page size and the
  rows to skip — and a cancellation token, and returns a `DataTableResult` with the rows and the
  total count. A request superseded by a newer one is cancelled. While the first page loads, the
  empty row shows a [`Spinner`](./forms.md#spinner); while a later page loads, the spinner stands above the
  rows that are still shown. The first request is made once the page is interactive.
  `RefreshAsync()` asks again.

Exactly one of the two is set; neither or both throws.

**How it is used.** `DataTable` takes the mandatory `Caption`, `Items` or `ItemsProvider`, and its
columns; `EmptyText` and `QueryName` (default `table`, needed per table when a page has two) are
optional. There are two kinds of column, both with a mandatory `Title` and an optional
`RowHeader`:

- **`DataTableColumn`** shows a `Value` taken from the item. `Sortable` and `Filterable` switch the
  features on, and either needs a `Key`, unique in the table, which names the column in the address
  and in the request. The value's type decides the filter: text, numbers or dates; any other type
  cannot be filtered, and a value that cannot be compared cannot be sorted — both throw. Numbers
  are aligned to the right on their own and shown as `#,0.##`, dates as `dd.MM.yyyy` and date-times
  as `dd.MM.yyyy HH:mm`; `Format` replaces the pattern, `Numeric` the alignment, and a template
  replaces the cell while the value keeps sorting and filtering.
- **`DataTableTemplateColumn`** shows only a template — a link, a button, a badge — and neither
  sorts nor filters.

**Script.** One JavaScript module catches clicks on the page and reset links and writes the state
into the address. The table's own module still handles the scroll area.

**Tokens and rules.** The `--data-table-*` set: the gaps; the minimum width of a filter; the
legend's size, weight and colour; the link colours; the sort button's colour, hover colour, state
text and icons; and the page links' size, border, radius, colours, current fill and weight, icon
and gap colour. Twenty-five rules, in both themes: the legend, the reset link, the sort button
and its state text, each also hovered, the idle sort icon, and the page links' text and border, also
hovered — each on the page and on a card — plus the current page's fill against both and its number
on the fill.

## Vitals card

`VitalsCard` shows one measurement at a glance: today's steps, the resting pulse, last night's
sleep. It is a card of its own, made for the [dashboard grid](./layout-and-maintenance.md#dashboard-grid), and lives in
`Patterns/Data/` next to the table, because it presents data rather than a control.

**Title, value, unit, in that order.** The title is a real heading that names the measurement, so
heading navigation finds every card on a dashboard (1.3.1); only the page knows its outline, so the
page sets the level. Below it stands the value, large, followed by its unit in the secondary text
colour. Unit and value are one paragraph, so a screen reader reads them as one phrase. The number
uses tabular figures and the [Swiss format](./foundations.md#tone-of-voice).

**At most one link, and the whole card is its target.** When the card leads to a detail page, the
title becomes the link. Its area is stretched over the whole card, so a pointer hits it anywhere,
while the keyboard and a screen reader meet a single link named by the title — one Tab stop per
card, not one per line of text. The focus ring is drawn around the card rather than around the
title, where the browser supports `:has()`; elsewhere it stays on the title, which is still
visible. Hovering the card raises its shadow. Because the whole card is the link, it holds no other
control: a second button inside would sit under the link's area and could not be reached by pointer.

**The trend is text.** "1’200 Schritte mehr als gestern" says the direction and the amount in
words. An arrow in front of it repeats the direction and is `aria-hidden`. The arrow is neutral
grey, never green or red: whether fewer kilocalories or a higher pulse is good depends on the person
and their goal, and a colour that judges would mislead exactly the users who read it most closely.
A trend needs a value; without one it throws.

**The goal is a sentence; the bar only repeats it.** With a goal, the card states it — "Ziel:
10’000 Schritte, 75% erreicht" — and below it a bar shows the same share. The bar is `aria-hidden`
and is not a `progressbar`: the sentence already says everything, and a second announcement of the
same number would only add noise. The share is rounded down, so the card never reports 100 % for a
goal that is not quite reached. Above the goal, the sentence keeps the real share — 111 % — while
the bar stops at full. The goal sits at the bottom of the card, so in a row of cards the bars line
up.

**The bar's track is the card, outlined.** A filled track in a neutral shade looked calmer, but the
domain colours could not reach 3:1 against any shade that still read as a track — three of them
fell below it. The track is therefore the card's own background with a thin border in
`--color-border-interactive`, which holds 3:1 against the card like every other component boundary.
The fill and the outline are then both measured against the card, which the data colours were
already chosen for (see [Data visualisation](./foundations.md#data-visualisation)). In forced-colours mode the fill
takes the text colour, so the bar does not vanish.

**The domain colour marks, it does not explain.** `Category` colours the icon and the bar with the
area's data colour — activity, nutrition, heart rate or sleep. That helps a reader who scans a
dashboard, but the title already names the measurement, so nothing depends on the colour (1.4.1).
Without a category, both are grey. The icon is decoration and `aria-hidden`.

**No data, said in words.** Without a value the card says "Noch keine Daten" where the number
would be. A goal stays as "Ziel: 8.0 Stunden", without a share and without a bar; a bar at zero
would suggest a measurement of zero.

**Nothing is announced.** Values change when the page loads, not while the user reads it; the card
carries no live-region role. A value that updates on its own would be announced through the
[live announcer](./feedback-and-overlays.md#live-announcer) by the page, not by the card.

**Long titles break.** In a narrow column the title hyphenates where the browser has a dictionary
for German and otherwise breaks within the word, so it never overflows the card (1.4.10).

**How it is used.** `Title`, `HeadingLevel` (2 to 6), `Value` and `Unit` are mandatory; `Value`
may be `null` for no data. `Decimals` sets the decimal places, `Goal` the goal, `Trend` a
`VitalsTrend` with a direction (`Up`, `Down`, `Steady`) and its sentence, `Icon` a
`SystemIconName`, `Category` the domain colour and `Href` the detail page. A blank title, unit or
link, a level outside the range, negative decimals, a goal of zero or less and a trend without a
value throw. Further attributes, including `class`, are passed on to the card.

**Tokens and rules.** No new semantic role. The `--vitals-card-*` set takes the card's background,
border, radius, padding and shadows from `--card-*`, and adds the gap, the title's and the link's
type and colours, the icon's size, the accent colours, the value's and the unit's type and colour,
the detail text, the trend arrow's size and colour, and the bar's height, radius, track and border.
Eighteen rules, in both themes, all on the card: the title, the link and its hover colour, the
value, the unit, the detail text and the trend arrow; the neutral and the four domain accents, once
on the card and once as bar fill on the track; and the track's border.

## Metric chart

`MetricChart` shows one measurement over consecutive days: the steps of the last week, the resting
pulse of the last month. Like the [vitals card](#vitals-card) it is a card of its own, sized for a
wide entry in the [dashboard grid](./layout-and-maintenance.md#dashboard-grid), and lives in `Patterns/Data/`. The SVG is
rendered on the server, without JavaScript, and stretches to the width of the card.

**Two kinds, one series.** Bars are for daily totals — steps, kilocalories — where each day stands
for itself and the height from zero is the message; bars therefore always start at zero. A line is
for measurements — pulse, weight, sleep — where the course matters more than the distance from
zero; its scale fits the values. Several series in one chart are deliberately not offered: a second
series would need a second carrier besides colour, and a dashboard reads better with two cards.

**The picture is a repetition, not the source.** The chart itself is `aria-hidden`. Everything it
shows is said in text first, so a reader who cannot see it, or cannot tell its shapes apart, loses
nothing (1.1.1, 1.4.1):

- The latest value stands large under the title, with its unit and its date — the same reading
  order as the vitals card. A screen reader hears it as one sentence instead, "Neuester Wert am
  Mittwoch, 25.11.: 7’500 Schritte.", and skips the visible lines: read on their own, the date line
  would name the latest value without saying it.
- A visible sentence summarises the period: its dates, the average or the range, the highest day,
  and how often the goal was reached. It names how many days have data when some are missing.
- A disclosure "Als Tabelle anzeigen" opens the same data as a [table](#table), one row per day,
  including the days without data and, for bars with a goal, whether the goal was reached.

The disclosure is a native `details` element. It works without JavaScript, is reachable by
keyboard, announces its state on its own, and changes its label to "Tabelle ausblenden" when open.

**No tooltips.** A value that appears on hover cannot be reached by keyboard, is hard to hit with a
finger, gets cut off when zoomed and is announced inconsistently (1.4.13, 2.1.1). The exact values
live in the table, which every user can open.

**The goal is labelled in words.** An optional goal draws a dashed line across the plot. Its label
"Ziel: 10’000 Schritte" stands in the legend below the chart rather than on the line, where it would
cover the bars; the summary and the table say it again. The line takes the primary text colour, so
it is not mistaken for a data series.

**A missing day is never drawn as zero.** A zero would look like a measurement — zero steps — and
mislead exactly the reader who looks closely. For bars, a day without data gets a short dashed
outline at the baseline instead of a bar; for a line, the line breaks. Each case has a legend entry
that says it in words — "Tag ohne Daten" or "Lücke in der Linie: Tag ohne Daten" — so neither the
dash nor the gap carries the meaning alone. The table writes "Keine Daten". Every value on a line
gets a dot, so a single value between two gaps does not disappear.

**Axes that never overlap.** The y-axis shows three to five round values in the
[Swiss format](./foundations.md#tone-of-voice), and faint grid lines at the same heights. The x-axis names
weekdays — "Mo", "Di" — for a week, and dates — "25.11." — for longer periods, always counted back
from the last day so the most recent day is labelled. How many labels fit depends on the card, not
the window: container queries on the card choose between three densities, so a chart in a narrow
column shows fewer labels instead of letting them collide (1.4.10). The labels are HTML, not SVG
text, so they keep their size when the plot stretches and follow the user's text settings (1.4.4,
1.4.12).

**Too little data, said in words.** With fewer than two values a chart has no course to show. The
card then keeps its title and says "Noch zu wenig Daten" with a sentence on when the chart appears,
instead of an empty or a one-point plot. The link, if any, stays.

**The period is the page's choice.** The chart shows the days it is given; switching between seven
and thirty days belongs to the page, for example with [tabs](./feedback-and-overlays.md#tabs). The chart does not know today's
date, so its summary names the period by its dates, "Vom 19.11. bis 25.11.", rather than "in den
letzten sieben Tagen".

**The domain colour marks, it does not explain.** `Category` colours the series with the area's
data colour, as on the vitals card; without one it is grey. The title already names the
measurement, so nothing depends on the colour. In forced-colours mode the series, the goal and the
missing-day outlines take the text colour and the grid takes the disabled-text colour, so the chart
stays visible.

**Nothing is announced.** Like the vitals card, the chart carries no live-region role; it is drawn
when the page loads.

**How it is used.** `Title`, `HeadingLevel` (2 to 6), `Kind` (`Bars` or `Line`), `Unit` and
`Points` are mandatory. `Points` holds one `MetricChartPoint` per consecutive day, with a `null`
value for a day without data. `Decimals` sets the decimal places, `Goal` the goal, `Category` the
domain colour, and `Href` with `LinkText` an optional link at the bottom, to a detail page. A blank
title, unit or link, a level outside the range, negative decimals, a goal of zero or less, a link
and its text given apart, a missing list, days that are not consecutive and negative values in a
bar chart throw; an empty list shows the too-little-data state. Further attributes, including
`class`, are passed on to the card.

**Tokens and rules.** No new semantic role. The `--metric-chart-*` set takes the card's background,
border, radius, padding and shadow from `--card-*`, and adds the gap, the type and colours of the
title, the value, the unit, the detail text and the axis labels, the plot's height, the series
colours, the line's width and the dot's size, the missing-day and goal strokes with their dash
patterns, the grid and baseline, the legend swatch and the colours of the toggle and the link.
Sixteen rules, in both themes, all on the card: the title, the value, the unit, the detail text,
the axis labels, the toggle and the link with their hover colours as text; the neutral and the four
domain series colours, the missing-day outline and the goal line as non-text. The grid lines have no
rule on purpose: they are a reading aid, and every value they mark is written on the axis.
