# Layout and maintenance

Part of the [Design System](./README.md).

## App navigation

`AppNavigation` is the app's main navigation: the product name and one link per area of the app.
It lives in `Layouts/`, because it frames every page rather than sitting inside one. It is page
navigation made of ordinary links, not a menu in the ARIA sense: there is no `role="menu"` and no
arrow-key handling, and Tab moves from link to link.

**Two forms, chosen by the window width.** From `--breakpoint-lg` (1024 px) on, it is a sidebar on
the start side that is always visible, with an icon and a text per entry. Narrower, it is a bar
across the top with the product name and a "Menü" button. The button discloses the entries below
the bar and pushes the content down; it does not cover the page. There is therefore no overlay, no
focus trap and nothing that needs to be closed before the page can be used. The bar is not fixed
while scrolling, so at 400 % zoom it does not take a share of the screen.

**The button says "Menü".** A visible word rather than an icon alone, because a bare hamburger icon
is not understood by everybody in this user base. Its icon changes between a list and an X with
the state, as a second cue next to `aria-expanded`. Escape closes the entries and returns the focus
to the button; following a link closes them as well.

**The landmarks are named.** The whole component is a `<header>`; the entries are a `<nav>` named
"Hauptnavigation", with a list inside, so a screen reader announces how many entries there are.
The product name is a link to the start page.

**The current page** has `aria-current="page"`. It is shown by a bar on the start side, semibold
text and the action colour. The bar and the weight carry the state without colour (1.4.1); the bar
is measured at 3:1 against the navigation's background (1.4.11). The other entries use the
secondary text colour, and the primary one with an underline on hover. An entry counts as current
for its own address and for every address below it. `Match="NavLinkMatch.All"` limits that to the
exact address, which the start page always needs, because otherwise every address would be below
it.

**Every entry has a visible text.** The icon only supports it and is hidden from screen readers.
There are no icon-only entries, not even in the narrow form.

**One size.** Every entry, the button and the product name are at least 44 px high.

**It works without the live connection.** The navigation is rendered statically, like the layout
around it; the entries are plain links, and the current page is decided on the server for every
address. Only the button needs script.

**Script.** The page around the navigation is static, so there is no component instance to call
into. The design system therefore ships a JavaScript initializer, `VitalSync.DesignSystem.lib.module.js`,
which Blazor loads by itself for every app that references the library. It imports the navigation's
module, which listens once on the document: a click on the button toggles `aria-expanded`, a click
on an entry and Escape collapse it. The CSS shows or hides the entries from that attribute alone.
Because the listener sits on the document, it keeps working after Blazor's enhanced navigation
replaces the page.

**How it is used.** `AppNavigation` takes the mandatory `Title`, the product name, and one
`AppNavigationLink` per area. Each `AppNavigationLink` has a mandatory `Href`, the page's path
(empty for the start page), and a mandatory `Title`. `Icon` and `Match` are optional. A blank title,
a missing navigation title or entries, an address with a query or a fragment and an entry outside
`AppNavigation` all throw. The app's layout puts a "Zum Inhalt springen" link in front of it, so a
keyboard user does not have to pass every entry on every page.

**Where the focus goes.** After a page change inside the app, the app moves the focus to the new
page's `<h1>`, so a screen reader announces the page and reading continues in its content. A
change of the query string alone is not a page change and leaves the focus where it is. A fresh
load or a reload moves nothing: the page starts at the top like any other web page, and the first
Tab reaches the skip link. Blazor's `FocusOnNavigate` cannot do the second part, because it also
focuses the heading on a fresh load; the app uses its own JavaScript initializer instead.

**The focus survives an interactive start.** Blazor does not take over prerendered markup: when an
interactive page starts, it removes that markup and renders the page anew, which replaces the
`<h1>` the focus was just moved to, and the focus falls back to the document body. After a page
change, the script therefore keeps following the heading: if the one it focused is removed while
the focus sits on the body, it focuses the new `<h1>`. It stops at the user's first key press or
pointer press and at the next page change, so it never takes the focus from where the user has put
it. A page can therefore be static or interactive as a whole without affecting where the focus
goes.

**Tokens and rules.** The `--app-navigation-*` set: the sidebar's width, padding and gaps; the
background and the divider's colour and width; the product name's size, weight and colour; the
button's gap, padding, icon size, text colour, weight, border colours at rest and on hover, border
width and radius; the entry's minimum height, padding, gap, radius, icon size, weight and text
colours at rest and on hover; the current entry's weight and colour; and the bar's colour and
width. No new semantic role. Nine rules, each on the navigation's background in both themes: the
product name, the button text, the button border at rest and on hover, the entry text at rest, on
hover and current, the bar and the focus ring.

## Page header

`PageHeader` opens every page of the app: the page's title, optionally one introductory sentence
below it, and optionally the page's actions beside it. It lives in `Layouts/` next to the app
navigation, because it frames a page rather than sitting inside one of its sections.

**It carries the page's only `<h1>`.** Every page has exactly one first-level heading, and it is
this one. That is also where the focus lands after a page change inside the app (see
[App navigation](#app-navigation)), so the title is the first thing a screen reader announces. The
component does not add `tabindex`; the app's script does that when it moves the focus.

**It does not set the document title.** `<PageTitle>` stays on the page, because the document title
names the product as well and is not always the visible heading word for word. Keeping the two apart
leaves that choice with the page.

**It belongs inside `<main>`.** The component is a `<header>`; inside `<main>` that is not a banner
landmark, so it adds no landmark of its own and does not compete with the app navigation's.

**Actions follow the title.** In the markup the actions come after the text block, so reading order
and focus order both start with the title. On a wide screen they sit at the end of the same line;
when the space is narrower than the title needs, they wrap below it. That is plain flex wrapping
with a minimum width for the text block, not a media query, so it also holds at 400 % zoom and in a
narrow column. A long title breaks rather than overflowing.

**The description is short.** One sentence that says what the page is for, in the secondary text
colour, at most about 70 characters per line so it stays readable. Longer explanations belong in
the page's content.

**How it is used.** `PageHeader` takes the mandatory `Title`, an optional `Description` and an
optional `Actions` fragment, typically one or two buttons. Further attributes, including `class`,
are passed on to the `<header>`. A blank title throws, and so does a description that is set but
blank, because an empty paragraph would still be announced.

**Tokens and rules.** The `--page-header-*` set: the gap between text and actions, the space after
the header, the gap between title and description, the text block's minimum width, the title's
size, weight, line height and colour, the description's size, maximum width and colour, and the gap
between actions. No new semantic role. Two rules, each on the page background in both themes: the
title and the description.

## Dashboard grid

`DashboardGrid` lays out a page's overview: cards side by side, as many as fit. It lives in
`Layouts/`, because it arranges a page's content rather than presenting any of it. Each card sits in
a `DashboardGridItem`.

**It is a list.** The grid is a `<ul>` and every item an `<li>`, so a screen reader announces how
many cards there are before reading the first (1.3.1). The `role="list"` is set explicitly, because
some browsers drop the list semantics of a list without bullets, and it cannot be overridden by an
attribute.

**The order is the markup's order.** Cards flow row by row in the order they are written; the grid
never reorders them, so reading, Tab and visual order stay the same (1.3.2, 2.4.3).

**Columns follow the space, not the viewport.** Each column is at least `18rem` wide and as many
fit as the space allows; the rest is shared equally. A narrower space than one column gets a single
column at its full width. That needs no media query, so the grid works the same on a phone, at
400 % zoom (1.4.10) and in a narrow page area. Items in a row share its height, so their cards do
too.

**One item can span the row.** `Wide` on a `DashboardGridItem` stretches it across every column —
for a [metric chart](./data.md#metric-chart), or an [empty state](./feedback-and-overlays.md#empty-state) that stands in for one. It still takes its place in
the reading order.

**How it is used.** `DashboardGrid` and `DashboardGridItem` each require content; an empty one
throws. Further attributes, including `class`, are passed on to the list and the item. A heading
above the grid, if the page has one, is the page's own; the grid adds none.

**Tokens and rules.** The `--dashboard-grid-*` set: the gap and the columns' minimum width. No
colours, therefore no rules.

## How this is kept true

`tools/VitalSync.DesignTokens.Contrast` resolves every `var()` chain per theme and measures the
pairs named in the rule document — 750 contrast checks and 6 separation checks at the time of
writing. All pass except the two status-colour separation checks, which run under a waiver (see
[Data visualisation](./foundations.md#data-visualisation)).

Contrast rules name a foreground, a background and the criterion they answer to. Separation rules
name a set of colours that must stay tellable apart, and measure every pair under normal vision and
the three colour vision deficiencies. A pair whose token no longer exists fails as loudly as one
that is too faint, because a rule that silently stops applying is worse than no rule.

Where the system does not yet meet a rule, the shortfall is recorded as a waiver with the measured
value and a reason. A waived pair does not break the build, but it cannot drift: if the value gets
worse the check fails, and if the pair starts passing the waiver is reported as stale and has to be
removed.

Run it locally with `dotnet run --project tools/VitalSync.DesignTokens.Contrast`, adding `--strict`
to see the unvarnished state. The test project asserts the same result, so the ordinary test run is
the gate.

## Changing a colour

1. Change the value in the stylesheet.
2. Run the check. It names every edge that no longer holds, and every waiver that has become
   unnecessary — a waiver left behind after its problem is fixed is itself a defect.
3. Add rules for any pairing the change creates. A rule names **which edge** it means, because most
   of the violations this system has had lived in pairs that had no rule at all: text on a card
   rather than on the page, a placeholder on the error fill, a focus separator against the button
   it surrounds.
4. If a violation cannot be fixed straight away, record it as a waiver with the measured value and
   a reason. Never leave it silent.
5. Update the affected part of the [design system documentation](./README.md).

Two habits are worth keeping. Verify against the rounded hex value, not against an intermediate
result — the difference has already cost this palette half a step. And when a value looks like it
only needs to be a little darker, check what else moves: uniform darkening is what collapsed two
chart series into one colour for red-green blind readers.

## Adding a component

A component first needs its place in the library. Primitives and patterns are told apart by one
question: is it a single control or display, or is it put together from several parts, often with a
service or a JavaScript module of its own? Inside each level, components are grouped by family, so
a page imports a handful of namespaces rather than one per component. The folder is the namespace,
which `.editorconfig` enforces.

- **`Primitives/`** holds single controls and displays: `Actions/` (button), `Inputs/` (form and
  every field that takes input), `Display/` (badge, avatar, spinner, toggletip).
- **`Patterns/`** holds composed components: `Feedback/` (alert, alert banner, toast, empty state),
  `Overlays/` (dialogs), `Data/` (table, data table, vitals card, metric chart),
  `Navigation/` (tabs).
- **`Layouts/`** frames the whole page: the app navigation, the page header and the dashboard grid.
- **`Accessibility/`** holds infrastructure without a look of its own: the live announcer.
- **`Icons/`** holds the icon component and its paths.

A family folder is never named like a type inside it — `Tabs/Tabs` would make the namespace and the
component collide. A new family is added to both `_Imports.razor` files, the library's and the web
app's.

1. Add its component tokens first. They point **only** at semantic tokens, never straight at a
   global. Every dark-mode break this system has had came from that shortcut: a component token
   bound to a raw value keeps its light-mode colour when the theme switches, because only the
   semantic layer switches.
2. If the semantic role does not exist yet, create it — including its dark value — before the
   component token that needs it.
3. Add the contrast rules for the new pairings before building the component, not after. A token
   that cannot be measured is a token nobody will notice going wrong.
4. Build the component: ARIA handling, focus ring, touch target of at least 44×44 px.
5. Add bUnit tests.
6. Add its page to the component gallery, described below.

While building, no colour, duration or size is hard-coded in a `.razor.css`. Everything goes through
a token — that is what makes the check, the reduced-motion fallback and the theme switch reach the
component at all. A hard-coded value is invisible to all three.

## Component gallery

There is no external documentation tool for this — no Storybook equivalent, no separate build.
Every component's demo page lives as a `.razor` file under `Gallery/` in
`VitalSync.DesignSystem` itself, next to the tokens and the component it demonstrates: one project
carries the whole system, tokens through demo page, instead of splitting the living reference into
a second tool that drifts out of sync.

`VitalSync.Web` does not duplicate these pages. Its `Router` lists the design system's assembly in
`AdditionalAssemblies`, and the `@page` routes declared inside the library are picked up as-is.

Routes live under `/design-system/*`, guarded by a single check for
`IWebHostEnvironment.IsDevelopment()` at the top of the gallery's own layout rather than repeated
per page. In anything other than Development the layout calls `NavigationManager.NotFound()`, so
the route answers with a 404 and no content, the same as an unknown URL. The gallery is a
tool for building the system, not a part of the product; it therefore has no entry in the
[app navigation](#app-navigation) and is reached by URL only.

Each component's page shows every visual state (default, hover, focus, disabled, and both themes
side by side) together with the markup needed to use it. The markup is what makes the page a
reference and not just a visual check — a screenshot proves a component renders, a snippet proves
someone can use it correctly. Pages with something to operate — a button to click, a form to
submit, a password to reveal — are `InteractiveServer`; a statically rendered page would show the
control but silently do nothing. Every page sets its own `<PageTitle>`, and the gallery layout
reflows to a single column at narrow widths, so the reference itself meets the rules it documents.
The pages are German like the rest of the interface; component and parameter names in the prose and
snippets stay as they are in code.
