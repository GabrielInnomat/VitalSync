# Design System

The design system is developed in code — there is no Figma and no design tool export. It lives in
the `VitalSync.DesignSystem` Razor Class Library: `wwwroot/vitalsync-tokens.css` holds the tokens,
`vitalsync-contrast-rules.json` next to it holds the accessibility rules those tokens are held to.

This document carries the reasoning. The stylesheet deliberately carries almost none, because a
rationale that lives next to a value is a rationale nobody updates when the value changes.

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
  meaning beyond the value itself.
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
not in the main navigation — a theme choice is an infrequent, per-user preference, not something
that competes for space with everyday navigation. It is persisted as a user setting on the backend,
not in `localStorage`: this product is used across devices, and a choice tied to one browser's
storage would silently fail to follow the user anywhere else. The override, once set, replaces the
system-preference read; `prefers-color-scheme` remains the fallback for a user who has not chosen.

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
The information colour, added with [`Alert`](#alert), did not lower either value. The waiver keeps
the gap from widening unnoticed; a colour change that makes it worse fails the check.

## Icons

The design system is VitalSync-specific, not a generic library shared across products — the
principles above (calm, accessible by default, this user base) are the product's business case, not
a neutral baseline. Icons follow the same logic: there is no reason to limit the set to what a
generic library ships.

Two layers:

- **Generic UI icons** — warning, info, close, chevron, and similar — come from **Phosphor**
  (MIT-licensed, free). It ships every icon in several weights on one consistent 256-unit grid;
  VitalSync uses the **Fill** weight throughout. That matters directly for WCAG 1.4.1: a critical
  and a warning icon differ in shape, not in colour alone.
- **Domain icons** — running, cycling, a dumbbell, a heart-rate trace, a meal, a scale — are
  VitalSync-specific and Phosphor does not carry them at the needed specificity. These are drawn as
  a small custom SVG set, matched to Phosphor's grid and Fill weight so the two do not visually
  diverge. None exist yet; they are added when the first domain screen needs one.

Both layers live in `VitalSync.DesignSystem`; a component never imports an icon library directly.

**How the generic layer is delivered.** There is no package and no icon font. The few Phosphor
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
it. The icons with no visible text beside them, the info icon of [`Toggletip`](#toggletip) and
the X that closes a [message](#messages) or a [dialog](#dialog), are no exception to this: their buttons are named by
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

Media queries at the page and layout level — navigation, grid columns, content width. Container
queries at the component level — card, chart, button groups.

A component in `Primitives/` or `Patterns/` that is reused in more than one layout context gets a
wrapping container with `container-type` and no media query of its own. A media query inside a
component's stylesheet ties it to the viewport rather than to the space it was given, which breaks
reuse in a narrower column.

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

**The interface is German.** Every text a user sees or hears — labels, messages, button text,
accessible names, page titles — is German, and the document declares `lang="de"` so screen readers
pick the right pronunciation. Component, parameter and variant names stay English, as in the rest of
the code, and so do exception messages, which are written for developers.

## Button

Three variants: `Primary` for the main action, `Secondary` for a less prominent one, `Critical`
for a destructive one. The destructive variant carries the same name as the semantic role it is
filled with, `--color-surface-critical`; there is no separate `Danger`. `Primary` is the default.

**A label is mandatory once there is an icon.** `Icon` is an optional, decorative `RenderFragment`
rendered before the label and `aria-hidden`. An `Icon` without `ChildContent` throws: an icon-only
button has no visible name, and the same rule holds for `Input`'s label and `Badge`'s text. The
system has three icon-only buttons, the help button of [`Toggletip`](#toggletip) and the close
buttons of the [messages](#messages) and the [dialog](#dialog), for the reasons given there.

**No size variants.** Minimum height and width are `--touch-target-min`, 44×44 px. A smaller
button would miss the target, so there is one size everywhere.

**States.** The fill darkens on hover, because the whole surface is the affordance. `Disabled`
sets the native `disabled` attribute and follows the disabled convention above: text takes
`--color-text-disabled`, the fill `--color-surface-disabled`, no opacity on top. A disabled
`Secondary` keeps no fill and shows its outline in `--color-border-disabled` instead. The focus ring
is the global one.

**It is a plain `<button>`.** `Type` defaults to `button`, so a `Button` inside a form does not
submit it by accident; a submit button says `Type="submit"`. `OnClick` is an ordinary
`EventCallback`. Any other attribute is passed through to the element, and a passed `class` is
appended to the component's own classes rather than replacing them.

## Form validation

**Visual coding.** An error is border plus icon plus text, optionally the subtle fill. Never colour
alone. The icon is the X in a circle, the same mark a critical `Badge` carries. There is no default
success tick — flagging every valid field is visual noise in a long form, so the error is the only
actively signalled state.

**Timing.** First check on blur, not while typing. After the first error, revalidate live on every
further keystroke in that field, so a correction is acknowledged immediately. On submit, everything
is validated again and focus moves to the first field in error.

**Native validation is off.** `Form` sets `novalidate`. The browser's own bubbles appear before
the designed error state, in the browser's language and styling, and disappear again on their own;
the native `required` attribute stays for its semantics, only the browser's interception of the
submit goes.

**ARIA is contextual, not a blanket obligation.** `aria-describedby` only when helper or error text
actually exists — a plain search field needs none. `aria-required` only on custom controls with no
native equivalent; on a native `<input required>` it is redundant. `aria-invalid` on any control
that can enter an error state. `role="alert"` goes on the individual message, never on a container
around several of them, or the screen reader announces twice.

**Error summary.** Beyond three fields we add a summary at the top of the form in addition to the
inline errors, in an `aria-live="polite"` region — not `assertive`, which would interrupt someone
mid-entry — focusable with `tabindex="-1"` and focused programmatically, with skip links to each
field in error. The three-field threshold is our own convention; WCAG techniques G83 and G85 do not
name one.

**Required fields.** Not the asterisk alone: the native `required` attribute as well, plus one
"* = Pflichtfeld" note per form, because some screen readers do not announce a bare asterisk.

All of this is implemented once, in [`Form`](#form), rather than repeated in every form.

## Form

`Form` wraps `EditForm` and turns the rules above into behaviour, so a page cannot forget them. It
takes either a `Model` or an `EditContext` — exactly one, otherwise it throws — and passes every
other attribute through to the `<form>`.

**Fields register themselves.** Every `Input`, `Checkbox`, `RadioGroup` and `Select` inside a `Form` finds it through a cascading value and
registers on first render. The form therefore knows its fields in rendering order, without the page
listing them. A field rendered later, behind an `@if`, joins at the end of that order.

**What it renders.** The "* = Pflichtfeld" note appears as soon as one registered field is
required, and disappears with the last one. The `aria-live` region for the summary exists from the
first render, empty, once more than three fields are registered — a live region added together with
its content is not reliably announced.

**What happens on an invalid submit.** With up to three fields, focus moves to the first enabled
field in error. Beyond three, the summary fills, receives focus, and lists one link per field in
error, in rendering order, each reading "label: first message". A link moves focus to its field.
Its `href` also points at the field on the current page, so it stays correct with `<base href>`
and without a live connection. The summary follows every further validation change and empties on
a valid submit.

**What it leaves to the page.** The validator — `DataAnnotationsValidator` or another — is chosen by
the page, as with `EditForm`. Field layout belongs to the page as well, because the `<form>` element
is rendered by `EditForm` and is outside `Form`'s CSS scope. A model-level error that belongs to no
registered field is not listed in the summary.

## Input

One generic `Input<TValue>` component, not a family of `InputText`/`InputNumber`/`InputPassword`.
`Type` selects the native `<input type>` (`text`, `email`, `number`, `password`, …); parsing goes
through `BindConverter.TryConvertTo<TValue>`, the same mechanism `@bind` uses internally, so the
component works for `string`, numeric and other convertible types without per-type components.

It is bound to an `EditContext` — it inherits `InputBase<TValue>` and is used with `@bind-Value`
inside a [`Form`](#form), where it registers itself, or inside a plain `EditForm`, the same as
Blazor's own input components. It is not meant to work standalone outside a form. A value the type
cannot parse is reported in German, naming the field's label.

**Label is mandatory.** There is no label-less `Input`, the same rule as `Button`'s no-icon-only
rule: a missing `Label` throws rather than silently rendering an inaccessible field.

**No size variants.** Standard size only, consistent with `Button`.

**Optional decorative `LabelIcon`.** A `RenderFragment` rendered before the label text, `aria-hidden`,
purely a visual scanning aid — not a help mechanism. Help is `Info`, below.

**Optional `Info`.** A short explanation behind a [`Toggletip`](#toggletip) beside the label, with
the label as its subject. It sits next to the `<label>`, never inside it, so the label alone stays
the field's accessible name. With or without it, the label only spans its own text: the empty space
to its right is not a click target for the field.

**Optional `Prefix` / `Suffix`.** `RenderFragment`s for units (`kg`, `kcal`) or fixed adornments,
rendered inside the field so they share its border.

**Password reveal toggle.** Only rendered when `Type="password"`. A visible text button
("Anzeigen" / "Verbergen"), never icon-only, following the same rule as `Button`. Its accessible
name adds the field's label ("Passwort anzeigen"), because a page with two password fields would
otherwise have two identical "Anzeigen" buttons; `aria-controls` points at the field. It is
disabled together with the field, and its hit area is the full 44×44 px target. Reaching that
target means it stretches over the field's own border, so the global ring would land on that border;
the toggle draws its ring inside the field instead, rounded and clear of the border, checked against
both the normal and the error fill. It toggles the
native `type` between `password` and `text`; the toggle itself is a plain `<button>`, not a `Button`
component, because `Button` has no compact size and would look oversized inline in a field.

**Attributes pass through, identity does not.** Anything not declared as a parameter —
`autocomplete`, `inputmode`, `data-*` — lands on the `<input>`. `id`, `aria-describedby` and
`aria-invalid` are set by the component after them, because the label, the helper text and the
error message are wired to those exact values.

**Razor caveat.** The component is named `Input`, and inside a C# block (`@if`, `@foreach`) Razor
reads `<Input>…</Input>` with child content as the void HTML `<input>`. Inside such a block, an
`Input` with `Prefix` or `Suffix` is written with its full name,
`<VitalSync.DesignSystem.Primitives.Input>`. A self-closing `<Input … />` is unaffected.

**`Required`.** Sets the native `required` attribute and, next to the label, a decorative `*`
(`aria-hidden`, inherits the label's already-verified text colour via `currentColor`). The
form-wide "* = Pflichtfeld" note from [Form validation](#form-validation) is rendered by `Form`, not
by `Input` itself.

## Checkbox

A native `<input type="checkbox">`, restyled with `appearance: none` and nothing else replaced: the
keyboard, the checked state and what a screen reader announces all stay the browser's own. Like
`Input`, it inherits `InputBase<bool>`, is used with `@bind-Value` inside a [`Form`](#form), where it
registers itself, and is not meant to work standalone.

**Label is mandatory, and it is the hit area.** A missing `Label` throws. The label wraps both the
box and its text, so the whole row is clickable; the row is at least 44 px high, while the box
itself is 24 px. A long label wraps beside the box, which stays aligned with the first line. `Info`
adds a [`Toggletip`](#toggletip) after the label on the same row, outside the `<label>`; it follows
the box in tab order, as it does on screen, and stays usable when the checkbox is disabled.

**The checked state is a shape, not a colour.** Checked means a filled box *and* a check mark, so
the state survives greyscale and forced colours. The check mark is decorative (`aria-hidden`); the
state itself is the native `checked`.

**Boundaries are held to 3:1.** The border uses `--color-border-interactive` and the checked fill
`--color-action-primary`, each measured against page, card and the box's own fill, and the check
mark against the fill it sits on.

**Errors follow [Form validation](#form-validation)**, with one difference in timing: a checkbox
has no typing phase, so it is checked when it changes rather than on blur. An unchecked box in
error takes the subtle critical fill and border; the message below carries the icon and
`role="alert"`, and the input carries `aria-invalid` and `aria-describedby`. A checked box keeps
its checked look even while in error, because the fill already marks the state the user chose.

**No indeterminate state.** It only makes sense for a parent of a nested selection, and there is
none yet.

**Shared tokens with the radio group.** The `--choice-*` tokens describe what a checkbox and a
radio button have in common — size, border, fill, mark, label, helper and error colours — so the two
cannot drift apart. Only the corner radius is the checkbox's own, `--checkbox-radius`, which takes the smallest step of the radius scale, `--radius-xs`.

## Radio group

`RadioGroup<TValue>` is a `<fieldset>` of native `<input type="radio">`, restyled the same way as
[`Checkbox`](#checkbox). The group holds the value: it inherits `InputBase<TValue>`, is used with
`@bind-Value` inside a [`Form`](#form), where it registers itself as one field, and is not meant to
work standalone. Each `RadioOption<TValue>` inside it contributes one radio button with its own
`Value` and `Label`; the type is inferred from the binding, so an enum works as well as a string.
An option outside a group throws.

**The group is named by its legend.** `Label` is mandatory and becomes the `<legend>`, which a
screen reader announces when focus enters the group. Every option needs its own `Label` as well; a
missing one throws on either level. As with `Checkbox`, the label wraps the circle and its text, so
the whole row is the hit area and at least 44 px high.

**`Info` sits in the legend.** A disabled `<fieldset>` disables everything inside it except the
content of its first `<legend>`, so the legend is the only place where the
[`Toggletip`](#toggletip) stays usable while the group is disabled. Because its button would
otherwise become part of the group's name, the `<fieldset>` is named by `aria-labelledby`, pointing
at the label text alone.

**The keyboard is the browser's.** All options share one `name`, so the group is a single tab stop:
Tab lands on the selected option, or on the first one while nothing is selected, and the arrow
keys move and select in one step. Nothing of this is scripted.

**The selected state is a shape, not a colour.** Selected means a filled circle *and* a dot. The dot
is an icon drawn in `currentColor`, not a coloured background, so it stays visible in forced colours,
where background colours are dropped. Options stack vertically, and only the whole group can be
disabled, through the native `disabled` of the `<fieldset>`.

**Errors follow [Checkbox](#checkbox)**: checked on change, the subtle critical fill and border on
every unselected option, and a selected option in error keeps its selected look. The message — or
the helper text — sits below the options, aligned with the legend rather than indented, because it
belongs to the group and not to one option; the `<fieldset>` points at it with `aria-describedby`.
`aria-invalid` sits on every radio button and never on the `<fieldset>`, whose role does not support
it; the component removes it from the attributes it passes through.

**In a `Form`.** A summary link points at the first option. Focus — after an invalid submit or from
a summary link — goes to the selected option, or to the first one while nothing is selected, which
is where Tab would land as well.

**Tokens.** Everything shared with `Checkbox` comes from `--choice-*`. The radio button's own are
`--radio-radius`, which takes `--radius-full`, and `--radio-mark-size` for the dot. No colour pairing
is new, so the group adds no contrast rules.

## Toggle

A native `<input type="checkbox">` with `role="switch"`, restyled like [`Checkbox`](#checkbox): the
keyboard (Space switches), the checked state and the announcement stay the browser's own; a screen
reader reads the label, "switch" and on or off. A toggle takes effect the moment it changes, so it
has no validation, no error state and no place in a [`Form`](#form): it is a plain component with
`Value` and `ValueChanged`, used with `@bind-Value`, and does not inherit `InputBase`. A choice that
only applies on submit, or that has more than two states, is a `Checkbox` or a
[`RadioGroup`](#radio-group) — the theme switcher, with its three states, is a radio group.

**The label sits above the switch.** `Label` is mandatory; a missing one throws. Below it, the
switch and its state text share one row of at least 44 px. The label text and that row are two
`<label>`s for the same input, so the whole block is the hit area while the
[`Toggletip`](#toggletip) of `Info` sits beside the label text, outside both. The state text is
`aria-hidden`, so the switch is still named by its label alone. Placing the label above rather than
beside keeps the state text from reading as part of the label, keeps label and switch together at
any width, and lets a long label wrap without moving the switch. Optional helper text sits below
and is linked with `aria-describedby`.

**The state is a position, a mark and a word, never a colour.** On moves the thumb to the end of
the track and shows a check mark in the space it leaves; off leaves the track empty. Next to the
switch, "Ein" or "Aus" says the same in words. The word, the thumb and the mark are `aria-hidden`,
because the native checked state already carries the meaning. The thumb is drawn as a border, not
a background, so it survives forced colours, where background colours are dropped; the mark is a
`currentColor` icon for the same reason. The thumb's movement uses `--motion-duration-base`, which
the reduced-motion fallback shortens.

**Disabled uses colours, never opacity**, from the same disabled roles as `Checkbox`; a disabled
toggle is skipped by Tab.

**Tokens and rules.** The toggle has its own `--toggle-*` set rather than borrowing `--choice-*`:
the track is twice as wide as it is high, the thumb is `--icon-size-sm`, and the colours point at
the same semantic roles as a checkbox. Its own rules measure the track border and the filled track
against page and card, the thumb against the track it sits in, thumb and mark against the filled
track, the label, state and helper text, and the focus separator against border and fill.

## Select

`Select<TValue>` is a native `<select>` for one choice out of a list. Like
[`Input`](#input), it inherits `InputBase<TValue>`, is used with `@bind-Value` inside a
[`Form`](#form), where it registers itself, and is not meant to work standalone. Each
`SelectOption<TValue>` inside it contributes one `<option>` with its own `Value` and `Label`; the
type is inferred from the binding, so an enum works as well as a string. An option outside a
`Select` throws. A choice between a few options that should all stay visible is a
[`RadioGroup`](#radio-group); a `Select` is for lists too long for that.

**Only the closed field is styled.** It looks like an `Input` — the same border, height, fill,
error state and focus treatment, from the same `--input-*` tokens — with a caret on the right in
place of the browser's own arrow. The caret is Phosphor's Fill caret, decorative (`aria-hidden`)
and ignored by the pointer, so a click on it still opens the list. The open list is the operating
system's own and is left alone: its keyboard handling, its touch behaviour and what a screen reader
announces stay native. Its options only take the field's background and text colour, so the list
follows the theme where the browser allows it.

**An empty first choice.** The first `<option>` has an empty value and shows `Placeholder`, by
default "Bitte wählen", in the placeholder colour. It says that nothing has been chosen yet, instead
of quietly preselecting the first real option. `Placeholder` must not be blank, because it is the
visible text of that choice; an option may not use the empty value, which is reserved for it. For a
nullable type the empty choice is a valid value and binds `null`. For a non-nullable type it cannot
be stored, and choosing it is reported as an error naming the label.

**Label is mandatory**, on the `Select` and on every option; a missing one throws. `Info` adds a
[`Toggletip`](#toggletip) beside the label, as on `Input`.

**Errors follow [Checkbox](#checkbox)**: a select has no typing phase, so it is checked when it
changes. The error look and the message are the same as `Input`'s. `aria-invalid` is always set,
`false` while there is no error: an empty `required` select is invalid to the browser from the
start, and without an explicit value a screen reader would announce it as invalid before the user
has done anything.

**Attributes pass through, identity does not**, as with `Input`.

**Tokens and rules.** The field reuses `Input`'s tokens. The caret has its own `--select-icon-*`
tokens, and its rules measure it against the normal and the error fill.

## Toggletip

A small info button that opens a short explanation on click, Enter or Space — a toggletip, not a
tooltip. A hover tooltip does not work on touch, is hard to keep open and dismissable as 1.4.13
requires, and hides its text from anyone who does not hover. A toggletip is a button, so pointer,
touch and keyboard all reach it the same way.

**Standalone and on every field.** `Toggletip` takes `Subject` and `Text`, both mandatory; a blank
one throws. It is phrasing content, so it sits inside a paragraph or beside a heading. `Input`,
`Select`, `Checkbox`, `RadioGroup` and `Toggle` take the same explanation through `Info` and render
the toggletip beside their label, with the label as its subject. It never sits inside a `<label>`:
a button there would become part of the field's name and take the label's clicks. It explains the
field rather than operating it, so it stays usable when the field is disabled.

**An icon-only button.** It shows only Phosphor's Fill info icon, which is decorative. Its name
is visually hidden text, "Hilfe zu …" followed by `Subject`, so two help buttons on one page never
share a name. It is one of three exceptions to `Button`'s no-icon-only rule, the others being the
close buttons of the [messages](#messages) and the [dialog](#dialog): an info icon beside a label is a convention users already know,
and a visible word there would compete with the label it explains.

**No script.** The button opens the popup through the native Popover API (`popovertarget`,
`popover="auto"`). The browser handles Enter and Space, closes the popup on Esc or a click outside,
closes any other open toggletip, keeps focus on the button and exposes the button as expanded or
collapsed. The popup directly follows the button in the DOM, so a screen reader reading on reaches
the text right after the button. It holds plain text only — nothing focusable, no links — because
anything interactive inside would need focus management that a toggletip should not have.

**Placement.** CSS Anchor Positioning places the popup below the button, starting at its edge, and
flips it to the other side or above when it does not fit. Its width is capped at
`--toggletip-max-width` and at the viewport less a margin, so at 320 px it wraps instead of
scrolling sideways.

**Target size.** The visible icon is 20 px, with an invisible 44×44 px hit area centred on it. That
deliberately differs from [`Badge`](#badge), which refuses an invisible enlarged hit area: a badge
at 44 px still looks right, an info icon at 44 px would dwarf the label it explains.
`--toggletip-spacing` keeps the hit area clear of the label text. Above a `Select` it reaches a few
pixels into the field's top edge; the help button lies on top there, and the rest of the field
stays a click target.

**Tokens and rules.** The `--toggletip-*` set: the icon in the secondary text colour, the primary
one on hover and while open; the popup on the elevated surface, with the default border and shadow.
Rules measure both icon colours against page and card, and the popup text against its background.

## Badge

One component, `Badge`, with a `Variant`: `Neutral` for a tag, `Success`, `Warning` and `Critical`
for a status. Every badge has a text label and an icon; the variant decides which icon.

**The text is mandatory.** A badge without `ChildContent` throws. The label carries the meaning,
the icon only supports it.

**A neutral badge takes any icon.** By default it shows a tag; `Icon` replaces that with a
`RenderFragment` of the page's choosing, for a category that has its own symbol.

**A status badge has a fixed icon.** `Success` shows a check in a circle, `Warning` a triangle,
`Critical` an X in a circle — all from Phosphor's Fill set (see [Icons](#icons)). Passing an `Icon`
to a status variant throws. The shape, not just the colour, tells the three states apart, which is
what makes the status colours' measured separation shortfall acceptable (see
[Data visualisation](#data-visualisation)). The X in a circle is the same mark that flags an input
error, so critical looks the same wherever it appears.

**It can be a link.** It renders `<span>` by default, or `<a href="…">` when `Href` is given, for a
badge that is also a navigation link (e.g. a tag that filters a list). Same 44×44 px touch target
as `Button` and `Input`, met by giving the badge that literal height rather than an invisible
enlarged hit area around a smaller visual — a badge that size does not look out of place next to
those two.

**Bold fill, not the subtle one.** `Input`'s error state uses `--color-surface-critical-subtle`,
barely different from the card, because border, icon and text already carry that state and the
fill only needs to avoid competing with them. A badge has no border and no adjacent field to
lean on, so it takes the vibrant `--color-surface-success/warning/critical` used by `Button`'s
critical variant, not the subtle one. The neutral fill needed a semantic role that did not exist
yet, `--color-surface-neutral` (`neutral-700` in light, `neutral-400` in dark, the same shade
pattern as the three status fills), verified against card and page background like they are.

**No hover fill change.** A `Button`'s fill darkens on hover because the whole surface is the
affordance. A linked badge already gets the global focus ring and, since 1.4.1 forbids colour
alone here too, an underline on hover — that is enough; a new fill tone for `Success`/`Warning`
would have meant new palette stops (`green-800`/`amber-800` in light, `green-500`/`amber-500` in
dark) with their own contrast and separation checks, for a state that already has a working,
cheaper signal.

**No compact size.** A table full of badges was a candidate for a smaller variant, but a smaller
badge that is also a link cannot keep the 44×44 px target without an invisible enlarged hit area,
which is a worse trade than one consistent size everywhere.

## Avatar

A person's photo, or their initials when there is no photo or it fails to load. Not operable:
it never takes focus, and a page that wants a clickable avatar wraps it in its own link or button.

**The name is mandatory.** `Avatar` takes `Name`, and a blank one throws, even when `Src` is given:
a photo can always fail to load, and the initials are what replaces it. They are the first letter
of the first and of the last word, or a single letter for a one-word name, counted in text
elements so that an accented letter stays whole.

**One accessible name, wherever it is needed.** Standing alone, the avatar is one image
(`role="img"`) named after the person; photo and initials inside are hidden, so a screen reader
announces the name once, whether or not the photo loaded. Beside a visible name, `Decorative`
hides the whole avatar instead, so the name is not read twice.

**The photo needs no script to fail.** It is a CSS background on a layer above the initials, not
an `<img>`. A broken `<img>` draws the browser's broken-image mark even with an empty `alt`; a
broken background draws nothing, and the initials beneath show through. This also works during
prerendering, before any interactive handler could react to an error event. `Src` is escaped into
a CSS string, so it cannot break out of the `url()`.

**Three sizes.** `Small`, `Medium` (the default) and `Large` — 32, 40 and 64 px at the default text
size, set in rem so they grow with the user's text size. None is held to the 44×44 px target,
because none is operable. The smallest still fits the widest pair of initials under 1.4.12's text
spacing.

**One neutral fill.** Every avatar without a photo gets the same fill, not a colour derived from
the name: per-person colours would need a set of fills with their own contrast rules in both
themes, and would invite reading meaning into a colour that has none. The fill is a new semantic
role, `--color-surface-neutral-subtle` (`neutral-200` in light, `neutral-700` in dark), quieter than
`Badge`'s bold `--color-surface-neutral` so that a list of avatars does not dominate the names
beside it. The fill itself carries nothing, so it has no threshold against the page; the initials
on it are measured as normal text. A transparent border makes the circle visible in forced colours.

**No status indicator.** An online or availability dot would be colour carrying meaning on a
non-operable element; if one is ever needed, it is a separate decision with a text alternative.

**Tokens and rules.** The `--avatar-*` set: three sizes with their font sizes, the radius, the
fill and the initials colour (the primary text colour). One rule measures the initials against
the fill.

## Spinner

A turning ring with a line of text beside it, shown while something loads. Not operable.

**The text is always visible.** `Label` defaults to "Wird geladen …" and may be replaced with
something more specific, such as "Rezepte werden geladen …"; a blank one throws. The ring alone
would leave out anyone who cannot see it, does not read the motion as loading, or has stopped it;
the text says the same thing to all of them. A non-breaking space keeps the ellipsis on the line
of the word before it.

**The text is announced, the ring is not.** The component is a `role="status"` live region, so a
screen reader reads its text politely, without interrupting. The ring is `aria-hidden`. A live
region inserted together with its text, which is how a spinner normally appears, is not announced
reliably by every screen reader; whether it is in practice is left to the manual screen-reader
test, and the component gallery has a demo that inserts the spinner on demand for exactly that.

**Reduced motion slows it, never stops it.** The rotation takes `--motion-duration-spin`, one
second, and three seconds under `prefers-reduced-motion`. A ring that stood still would look like
a decoration, and the transition tokens' `0.01ms` would make it race instead. The motion itself is
allowed under 2.2.2: it only runs while the content it stands for is loading, and indicating that
is its whole purpose.

**The arc is held to 3:1.** The text already carries the meaning, but the motion is information
too: the arc is measured against the page, the card and the track it travels along. The track is
quieter on purpose and carries nothing on its own. Arc and text share the secondary text colour, so
the spinner reads as a calm status rather than an action.

**Forced colours draw the arc in `CanvasText` on a `Canvas` track.** Left alone, forced colours
would paint the whole border in one system colour, and a ring of one colour turns invisibly. The
ring opts out with `forced-color-adjust: none` and takes the two system colours directly, so it
stays in the user's palette.

**One size.** 24 px at the default text size, in rem so it grows with the user's text size. A
smaller size waits for the loading state of `Button`, which is a separate step.

**Tokens and rules.** The `--spinner-*` set: size, stroke width, arc, track and text colours, the
gap and the duration, which points at the new global `--motion-duration-spin`. Five rules: the arc
against page, card and track, and the text against page and card.

## Messages

Three components tell the user something without being asked, and each has its own rules, because
each answers a different question:

- **`Alert`** belongs to the content. It sits where its subject is — in a card, above a form, in a
  section — and stays as long as the state it describes.
- **`AlertBanner`** belongs to the whole application. It runs across the top of the page for a
  state that affects everything, such as maintenance or being offline.
- **`Toast`** answers an action the user has just taken. It floats above the page, outside the
  content, and reports how the action went.

`Alert` is built; `AlertBanner` and `Toast` are not yet, and their rules below are decided.

**What they share.** Every message has a mandatory title and a mandatory text. The kind is written
out at the start of the title, followed by a dash — "Fehler – Speichern fehlgeschlagen" — so it
reaches everyone in words and never depends on colour or icon (1.4.1). The words are "Information",
"Erfolg", "Warnung" and "Fehler". Each kind has a fixed icon from the [Icons](#icons) set: a check in
a circle for success, a triangle for a warning and an X in a circle for an error, as on `Badge`, and
an i in a circle for information. The icon cannot be replaced and stays `aria-hidden`,
because the title already says what it shows. Tone follows [Tone of voice](#tone-of-voice): calm
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
[Data visualisation](#data-visualisation). Success and warning gain the subtle fill, border and
icon roles that so far only critical had.

**Only a message that appears later is announced.** A message that is part of the page when it
loads is not announced; it is found by reading and by heading navigation, like the rest of the
content, and announcing it on top of the page would interrupt the page's own announcement. A message
that appears later — after saving, after a check — is announced, and the page states that it does:
`Warning` and `Critical` interrupt (`role="alert"`), `Info` and `Success` wait until the screen
reader is idle (`role="status"`). Whether a message inserted together with its text is announced
reliably is left to the manual screen-reader test, as with the [Spinner](#spinner).

**It can be closable, and is not by default.** Closing suits a message that is no longer needed
once read, such as a success. A message about a problem that still exists is never closable; it
disappears when the problem is solved. The close button is named "Schliessen" together with the
title, has the 44×44 px target of every other control, and on closing moves focus to an element the
page must name. Without that, focus would fall back to the start of the document (2.4.3), and only
the page knows what comes next.

**How it is used.** `Variant` defaults to `Info`. `Title`, `HeadingLevel` (2 to 6) and the text as
`ChildContent` are mandatory, and a blank title, a missing text or a level outside that range
throws. The single action is passed as `Action`. `Announce` marks a message that appears later and
gives it its live-region role. `OnClose` adds the close button; it requires `CloseFocusTarget`, a
function that returns the element to focus, and throws without it. Focus moves first and `OnClose`
runs after, so the page can remove the message without focus being lost on the way. A message about
a problem that still exists must not be given `OnClose`; the component cannot tell, so that stays
the page's responsibility.

**The close button.** It shows the bare X described under [Icons](#icons), in the secondary text
colour and the primary one on hover, with an invisible 44×44 px hit area centred on it, like
[`Toggletip`](#toggletip)'s help button. Its visually hidden name is "Schliessen:" followed by the
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

**Built on `Alert`.** Same fill, border, icon, title format, text and optional single action, such
as "Mehr erfahren", across the full width.

**Three kinds:** `Info`, `Warning`, `Critical`. A success is never news for the whole application.

**Announced like `Alert`.** A banner present when the page loads is not announced; one that appears
later, such as "Du bist offline", is: `Warning` and `Critical` interrupt, `Info` waits.

**At the very top, in its own landmark.** Banners sit above the header in a region named
"Systemmeldungen", which screen readers list among the landmarks and can jump to. The region exists
only while there is a banner. The title is a heading, like `Alert`'s.

**It scrolls away.** A bar fixed to the top would cover too much of the page at high zoom and on
small screens (1.4.10).

**Closable like `Alert`.** Optional, never for a problem that still exists. Focus moves to the main
content. A closed banner stays closed until the page is reloaded; nothing is stored.

**Several at once stack.** One under the other, the most severe first: `Critical`, `Warning`,
`Info`.

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

**Announced through live regions that are always there.** The layout holds one polite and one
assertive live region from the start, and the toast's text is written into them; a live region
created together with its text is not announced reliably. `Success` and `Info` wait until the
screen reader is idle, so a confirmation does not cut into what the user is doing; `Critical`
interrupts.

**It never takes focus.** Toasts sit in a region named "Benachrichtigungen" at the end of the page,
reached with Tab and by landmark navigation. Closing a toast moves focus to the next toast's close
button, or to the main content when none is left.

**Bottom right; on narrow screens full width, and only the newest.** Three full-width toasts would
cover too much of the page, and high zoom makes every screen narrow (1.4.10). The others wait in the
stack and show again once the newest is closed.

**At most three.** A fourth makes the oldest give way, never the one that holds focus. A toast only
reports on an action and carries nothing the user needs later, so giving way loses nothing.

**Raised through a service.** Pages call a service; the region is placed once in the layout. No
page renders a toast itself.

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
[Icons](#icons), and may also have an "Abbrechen" button among its buttons. The confirmation has no
X; its "Abbrechen" button does that job. Esc always closes and counts as cancelling. A click on the
dimmed background does not close: a click beside the dialog is often a slip, and it would discard
what was typed. With the X, "Abbrechen" and Esc there are enough ways out, and all of them are
reachable by keyboard.

**Unsaved input.** Every way of closing an ordinary dialog first asks the page. The page may refuse
and open a confirmation on top instead, such as "Änderungen verwerfen?". That is the only dialog
allowed over another one; nothing else nests. Esc then closes only the confirmation. A
[`Toggletip`](#toggletip) open inside a dialog takes Esc first; the next Esc closes the dialog.

**Focus on opening is fixed.** In the ordinary dialog it moves to the first operable element below
the title, in a form the first field, so typing can start at once; when nothing below the title is
operable, it moves to the X. In the confirmation it moves to "Abbrechen", so an Enter pressed too
early never confirms. The page cannot change either.

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

**The close button.** The bare X described under [Icons](#icons), in the secondary text colour and
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

## How this is kept true

`tools/VitalSync.DesignTokens.Contrast` resolves every `var()` chain per theme and measures the
pairs named in the rule document — 464 contrast checks and 6 separation checks at the time of
writing. All pass except the two status-colour separation checks, which run under a waiver (see
[Data visualisation](#data-visualisation)).

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
5. Update the affected part of this document.

Two habits are worth keeping. Verify against the rounded hex value, not against an intermediate
result — the difference has already cost this palette half a step. And when a value looks like it
only needs to be a little darker, check what else moves: uniform darkening is what collapsed two
chart series into one colour for red-green blind readers.

## Adding a component

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
tool for building the system, not a part of the product; it therefore has no entry in `NavMenu` and
is reached by URL only.

Each component's page shows every visual state (default, hover, focus, disabled, and both themes
side by side) together with the markup needed to use it. The markup is what makes the page a
reference and not just a visual check — a screenshot proves a component renders, a snippet proves
someone can use it correctly. Pages with something to operate — a button to click, a form to
submit, a password to reveal — are `InteractiveServer`; a statically rendered page would show the
control but silently do nothing. Every page sets its own `<PageTitle>`, and the gallery layout
reflows to a single column at narrow widths, so the reference itself meets the rules it documents.
The pages are German like the rest of the interface; component and parameter names in the prose and
snippets stay as they are in code.
