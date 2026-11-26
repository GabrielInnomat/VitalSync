# Foundations

Part of the [Design System](./README.md).

## What the system is for

VitalSync shows vital data to a user base that explicitly includes older people and blind or
partially sighted people. WCAG 2.1 AA is the floor, not the goal, and it is not negotiable per
screen. Four principles decide the close calls:

- **Trustworthy and calm.** No aggressive color or motion around vital data.
- **Accessible by default.** Accessibility is a property of the tokens, not something added later.
- **Data legibility before aesthetics.** A chart has to be readable at a glance.
- **Platform-honest but consistent.** One codebase, one visual language.

## Token architecture

Three layers, each referencing only the one below it:

- **Global tokens** are raw values: `--neutral-500`, `--blue-600`, `--data-1-light`. They carry no
  meaning beyond the value itself. The neutrals follow the Slate scale with one step of their own:
  `--neutral-550` sits between 500 and 600 because the placeholder needs it to reach 4.5:1.
- **Semantic tokens** name a role: `--color-text-critical`, `--color-border-interactive`. This is
  the only layer that switches per theme.
- **Component tokens** name a use: `--input-border-color`, `--button-primary-text`. They reference
  semantic tokens, never globals.

That last rule is the one worth enforcing. Every colour defect this system has had came from a
component token pointing straight at a global: it then kept its light-mode value in dark mode,
because only the semantic layer switches. 

## Themes

`[data-theme]` on `<html>` is the single source of truth. There is no parallel
`@media (prefers-color-scheme)` block. A small inline script in the document head reads the system
preference and sets `data-theme="light"` or `data-theme="dark"` before the first paint, which
avoids a flash of the wrong theme.

The semantic layer is declared for `:root` and `[data-theme="light"]`, and overridden for
`[data-theme="dark"]`. The component layer is declared for `:root` and every `[data-theme]`
element, so a component token resolves against the theme of its nearest themed ancestor. That is
what lets a subtree carry a different theme than the page — the gallery shows both side by side —
without a component keeping the colours of the page it happens to sit on.

This is the automatic default, not the only path. A manual override belongs in the settings area,
not in the main navigation — a theme choice is an infrequent, personal preference, not something
that competes for space with everyday navigation. The choice is stored in the device's
`localStorage`, not on the server, so it applies to that browser on that device only. The same
inline script reads it before the first paint; a stored choice replaces the system-preference
read, and `prefers-color-scheme` remains the fallback for a user who has not chosen.

**The switcher is a [radio group](./forms.md#radio-group), not a select and not a toggle.** Hell, Dunkel and
System are three mutually exclusive choices, and all three stay visible without opening anything; a
toggle has only two states. Choosing applies at once, without a save button — the result is the
feedback. That is a change of content, not of context, so it stays within WCAG 3.2.2. "System"
removes the stored choice rather than storing a third value, and its label names what the device
currently delivers — "System (Hell)" — so the option never hides which theme it means, and a screen
reader hears it as part of the option's name.

**The settings page is interactive; the theme itself stays in the browser.** The page renders over
the server and binds the radio group like any other form field, so the choice passes through the
server and can later be stored with the user instead of the device. Writing the choice and setting
`data-theme` is one call into a small JavaScript module. Until the circuit connects, the page cannot
know the stored choice, so it renders with nothing selected and the plain label "System", and then
reads both the choice and the current system theme. While the page is open, it follows a change of
the system preference and a choice made in another tab. On every other page, the module started by
the web app's JavaScript initializer does the same, and it restores `data-theme` after an enhanced
navigation, which replaces the attributes of `<html>` with the server's.

## The rule that replaced the two-shade system

The original convention was "600 shades for fills, 700 shades for text". It was verified against
white, and white is not a background anywhere in this product: the page is `neutral-50`, a card is
`neutral-100`. Measured against the surfaces that actually render, `amber-600` reaches 2.91:1 and
fails 1.4.11, and `green-600` reaches 3.01:1 with no margin at all.

The rule in force now is simpler and holds: **every role is verified against the surface it appears
on, and that surface is the card** — the darker of the two light surfaces, and the lighter of the
two dark ones. Never the page background, which is the more forgiving of the pair, and never white.

In practice text and status fills therefore share a value: the 700 shades in light, the 400 shades
in dark. That is safe, because 4.5:1 is the stricter of the two thresholds. `green-600` and
`amber-600` are unused as a result. A future badge that needs more vibrancy is a new value with its
own verified entry, not a reach back to the old ones.

## Roles that look like one role but are four

The error state of a form field needs its own token per part, because the parts have different
obligations and move in different directions between themes:

- `--color-text-critical` — the message below the field. 4.5:1. `red-700` becomes `red-400`.
- `--color-border-critical` — the field border. 3:1 under 1.4.11, so the more vibrant `red-600`
  is enough in light; it becomes `red-400`.
- `--color-icon-critical` — the error mark, an X in a circle, next to the message below the field.
  Same obligation as the border, measured against the page and the card it sits on.
- `--color-surface-critical-subtle` — the fill. No threshold at all. `red-50` becomes `red-950`.

The fill is deliberately almost indistinguishable from the card, 1.00:1 in light and 1.10:1 in
dark. It is never the sole error signal, which 1.4.1 forbids anyway; border, mark and text carry
the state.

## Borders are not all the same

`--color-border-subtle` and `--color-border-default` are decorative: dividers, card outlines, table
rules. No contrast threshold applies to them.

`--color-border-interactive` and its hover variant are the boundary of something operable — input,
select, checkbox, toggle. WCAG 1.4.11 applies with 3:1, and the decorative token does not meet it
(1.36:1 on the card). Every new operable component takes the interactive token.

The direction reverses per theme: in light the border is darker than the surface (`neutral-500`),
in dark it is lighter (`neutral-400`). A hard-wired neutral in a component token is therefore
always wrong — it does not travel.

## The focus indicator is neutral, not brand blue

It started as `blue-600`, the same colour as the primary button it surrounds — 1.00:1 against the
component. It was visible only because the 2px offset let the background show through, and it read
as an extension of the button rather than a ring.

It is now two-tone: an outer ring in `--color-focus-ring` (`neutral-900` in light, `neutral-50` in
dark) and an inner separator in `--color-focus-ring-offset`. Both edges are checked — ring against
surface, ring against separator, separator against the component it surrounds. The ring is the
`outline`, drawn at `--focus-ring-offset` from the component; the separator is a `box-shadow` of
the same width that fills that gap, so the gap shows the separator colour rather than whatever
surface lies behind it. Only `:focus-visible` draws it — a mouse click does not.

Neutral rather than brand is the point. A neutral ring has enough contrast against every surface
and every component fill in the system, including future green, amber or red ones. A brand colour
can be devalued by any new component that happens to share it, which is exactly what had happened.
This is the direction WCAG 2.2 takes with 2.4.11.

`--input-border-color-focus` is decoupled from the ring and stays blue. The ring says "the keyboard
focus is here", the border says "this field is active". Two statements, two tokens. On `Input` the
ring sits around the whole field, prefix, suffix and reveal toggle included, not around the bare
`<input>` inside it; the thicker focus border is drawn inset, so focusing a field never shifts the
layout.

## Data visualisation

Okabe-Ito is colour-blind safe but not contrast-strong. Measured against the card, four of its six
colours fell below 3:1 in light mode and the blue fell below in dark. There are therefore two
derived sets, `--data-N-light` and `--data-N-dark`, and the semantic `--color-data-*` tokens switch
between them.

**The trap is uniform darkening.** Okabe-Ito separates some of its pairs by lightness rather than
by hue. Flattening the lightness to hit a contrast floor deletes that separation: measured, series
1 and series 5 collapse to ΔE 1.9 under deuteranopia, which makes nutrition and heart rate the same
colour for a red-green blind reader. That passes 1.4.11 and is worse than the starting point.

The values were therefore searched under three simultaneous constraints — at least 3.3:1 against
the card, at most 16° of hue drift, and a smallest pairwise ΔE of at least 16.0 under normal
vision, protanopia, deuteranopia and tritanopia. Within those constraints the deviation from
Okabe-Ito was minimised rather than the separation maximised; maximising separation pulls the
palette apart until it no longer looks like a health application.

Both sets are verified against the rounded hex values, not against intermediate floating point.
That distinction is not academic: the first version of this palette was verified before rounding,
reported 16.0, and actually delivered 15.52. The check in the tool found it.

**Status colours get the same separation check, and do not pass it.** `--color-text-success`,
`-warning`, `-critical` and `-info` are individually contrast-checked already, but not against each
other.
They rarely sit side by side — except in a legend or a table column, where they do. The rule
`status-text` measures them with the same method as the data-visualisation palette: paired ΔE under
normal vision and the three colour vision deficiencies, against a minimum of 16. It applies to the
text tokens only, not to the subtle fills, which are deliberately near-indistinguishable from the
card and were never meant to carry the distinction.

The measured values fall short: ΔE 9.91 in light (warning against critical) and 9.17 in dark
(success against critical), both under deuteranopia. The shortfall is accepted and recorded as a
waiver in both themes, because a status in this system never appears by colour alone — every status
badge and every message carries its own fixed icon and a text label, which is what 1.4.1 asks for.
The information colour, added with [`Alert`](./feedback-and-overlays.md#alert), did not lower either value. The waiver keeps
the gap from widening unnoticed; a colour change that makes it worse fails the check.

## Icons

The design system is VitalSync-specific, not a generic library shared across products — the
principles above (calm, accessible by default, this user base) are the product's business case, not
a neutral baseline. Icons follow the same logic: there is no reason to limit the set to what a
generic library ships.

**Phosphor is the source of every icon.** Generic UI icons (warning, info, close, chevron) come
from **Phosphor** (MIT-licensed, free), and so do the icons of the app's areas in the
[navigation](./layout-and-maintenance.md#app-navigation): a house, a fork and knife, a barbell, a line chart, a gear. Phosphor
ships every icon in several weights on one consistent 256-unit grid. VitalSync uses the **Fill**
weight throughout. That matters directly for WCAG 1.4.1: a critical and a warning icon differ in
shape, not in colour alone.

**Custom icons are only a supplement.** A custom SVG is drawn only where Phosphor has no fitting
shape for a VitalSync-specific subject. It must resemble Phosphor's style closely enough not to
stand out next to it. That means the same 256-unit grid, the Fill weight, and comparable mass and
corner rounding. A custom icon never replaces a Phosphor icon that fits. None exist yet; one is
added when a screen needs a shape Phosphor does not have.

All icons live in `VitalSync.DesignSystem`; a component never imports an icon library directly.

**How the icons are delivered.** There is no package and no icon font. The few Phosphor
icons the system uses are copied into the library as SVG path data, in the **Fill** weight, and
rendered by one component, `SystemIcon`, selected by the `SystemIconName` enum. Phosphor's MIT
licence travels with them in `Icons/PHOSPHOR-LICENSE.txt`. An icon is added by adding its path
from Phosphor's Fill set and a name to the enum — never by drawing a lookalike by hand.

**Two exceptions: the bare check mark and the bare X.** Phosphor's Fill weight has no free-standing
check — its filled "check" is a square with the tick knocked out of it, which inside a checkbox would
draw a box within a box. The check mark in `Checkbox`, and on a switched-on `Toggle`, therefore
comes from Phosphor's **Bold** weight, the closest in stroke mass to Fill. The X that closes a
message or a dialog has the same problem: Fill's X sits in a square, and the X in a circle is already taken by
the error mark, so a close button drawn with it would read as an error. It too comes from Bold.
These are the only icons outside the Fill set; any further exception needs the same reason: Fill has
no shape that works in that place. The dot of a selected radio button needs no exception: it is
Phosphor's Fill circle.

`SystemIcon` renders an `<svg>` with `fill="currentColor"`, `aria-hidden="true"` and
`focusable="false"`, sized to fill its container. The container decides the size through its own
component token and the colour through `color`, so the icon inherits whatever the surrounding text
has already been verified against. An icon is always decorative: the meaning is in the text next to
it. The icons with no visible text beside them, the info icon of [`Toggletip`](./forms.md#toggletip) and
the X that closes a [message](./feedback-and-overlays.md#messages) or a [dialog](./feedback-and-overlays.md#dialog), are no exception to this: their buttons are named by
visually hidden text, and the icon stays `aria-hidden`.

## Conventions no token can enforce

- **Disabled is colour or opacity, never both.** Text in a disabled control takes
  `--color-text-disabled` and no opacity; non-text parts take `--opacity-disabled`. Stacked they
  multiply, landing at 1.48:1 in light and 1.75:1 in dark. Disabled controls are exempt from 1.4.3,
  but for this user base that is effectively invisible. Dimming a whole control with `opacity` is
  the convenient move and therefore the likely mistake.
- **Disabled text stays readable, just not prominent.** On `--color-surface-disabled`,
  `--color-text-disabled` sits at about 2:1 in both themes. Lower and a disabled field's value can
  no longer be read; higher and it starts to look editable. In dark the placeholder already takes
  `--neutral-400`, so disabled text sits one step below it.
- **Large text is not the same as a large heading.** The 3:1 exception in 1.4.3 starts at
  `--font-size-large-text-min` (24px), or `--font-size-large-text-bold-min` (19px) when bold.
  `--font-size-300` is 20px and qualifies only in bold; `--font-size-200` never qualifies.
- **Light mode has two elevation surfaces, not three.** Between the card and white there is 1.09:1;
  there is no room for a third perceptible step. `--color-background-elevated-2` and `-3` are
  deliberately equal, and the dropdown/modal distinction is carried by the shadow alone.
- **Every transition and animation duration references a motion token.** The reduced-motion
  fallback only reaches durations that go through a motion token; a hard-coded `300ms` in a
  component stylesheet is invisible to it. For transitions, `--motion-duration-fast` and `-base`
  drop to `0.01ms` rather than `0ms`, because `transitionend` does not fire at exactly zero in
  several browsers, which would strand logic that waits for it — for the very users who asked for
  reduced motion. A looping animation goes the other way: `--motion-duration-spin` grows from one
  to three seconds, because a loop shortened to almost nothing does not stop, it races.
- **A filled surface needs its own colour for the label on it.** `--color-text-on-action` does this
  for the button. The value of a fill says nothing about what is readable on top of it.

## What the form fill does not do

`--color-background-input` exists because form fields need their own variants for disabled and
error. It does **not** separate the field from the card — the neutral range is too narrow for that,
1.10:1 in light and 1.38:1 in dark. That separation is carried entirely by
`--color-border-interactive`, which is why the border is held to 3:1. There is deliberately no
contrast rule for the fill itself, the same way there is none for `--color-background-elevated-2/3`
being equal or for the critical fill being barely different from the card: 1.4.11 covers component
boundaries, and the boundary here is the border, not the fill behind it.

The token sits at one extreme of the neutral scale in each theme, and in both themes for the same
reason: `--color-text-primary` reaches its highest contrast there. That symmetry broke in dark mode
until `neutral-950` replaced `neutral-900` — before that, the field's own fill was the exact same
colour as the page, not merely close to it like the light-mode white is close to the card. Same
value, two unrelated reasons to have chosen it, coincidence rather than design. `neutral-950` keeps
the same maximised text contrast while giving the field a fill of its own again.

## Responsive strategy

Media queries at the page and layout level — navigation and content width. Container queries at
the component level — card, chart, button groups.

A component in `Primitives/` or `Patterns/` that is reused in more than one layout context gets a
wrapping container with `container-type` and no media query of its own. A media query inside a
component's stylesheet ties it to the viewport rather than to the space it was given, which breaks
reuse in a narrower column.

Components in `Layouts/` are that page level: they frame the whole page rather than sit in a
column, so they respond to the viewport with media queries. The [app navigation](./layout-and-maintenance.md#app-navigation)
changes its form at `--breakpoint-lg`. The [page header](./layout-and-maintenance.md#page-header) needs none: its actions wrap
below the title when the space runs out. Neither does the [dashboard grid](./layout-and-maintenance.md#dashboard-grid): its
columns follow the space it is given, not the viewport.

`var()` is not allowed inside a media query condition, so the `--breakpoint-*` tokens are the
documented source of truth and the pixel values are repeated literally in the `@media` rules.

## Tone of voice

Calm, never urgent — including at critical severity. This is a precision of the "trustworthy and
calm" principle above, not a new one: the user base includes older and visually impaired people
reading vital data, and alarmist phrasing (exclamation marks, imperative "Achtung!") raises stress
in exactly that group without communicating urgency any better than a plain statement does. A
message names the fact and the action: "The value could not be saved. Please try again," not a
warning-toned rewrite of the same sentence. Urgency is carried by the visual coding described
below — border, icon, colour — not by the words.

**The interface is German for Switzerland.** Every text a user sees or hears — labels, messages,
button text, accessible names, page titles — is German, and the document declares `lang="de-CH"`
so screen readers pick the right pronunciation. The app runs in the `de-CH` culture, so numbers
take the Swiss format: a right single quotation mark (’) groups the thousands and a point separates
the decimals — "10’000", "7.5". Component, parameter and variant names stay English, as in the rest
of the code, and so do exception messages, which are written for developers.
