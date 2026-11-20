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
`-warning` and `-critical` are individually contrast-checked already, but not against each other.
They rarely sit side by side — except in a legend or a table column, where they do. The rule
`status-text` measures them with the same method as the data-visualisation palette: paired ΔE under
normal vision and the three colour vision deficiencies, against a minimum of 16. It applies to the
text tokens only, not to the subtle fills, which are deliberately near-indistinguishable from the
card and were never meant to carry the distinction.

The measured values fall short: ΔE 9.91 in light (warning against critical) and 9.17 in dark
(success against critical), both under deuteranopia. The shortfall is accepted and recorded as a
waiver in both themes, because a status in this system never appears by colour alone — every status
badge carries its own fixed icon and a text label, which is what 1.4.1 asks for. The waiver keeps
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

`SystemIcon` renders an `<svg>` with `fill="currentColor"`, `aria-hidden="true"` and
`focusable="false"`, sized to fill its container. The container decides the size through its own
component token and the colour through `color`, so the icon inherits whatever the surrounding text
has already been verified against. An icon is always decorative: the meaning is in the text next to
it.

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
  fallback only reaches durations that go through `--motion-duration-fast` or `-base`; a hard-coded
  `300ms` in a component stylesheet is invisible to it. The fallback uses `0.01ms` rather than `0ms`
  because `transitionend` does not fire at exactly zero in several browsers, which would strand
  logic that waits for it — for the very users who asked for reduced motion.
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
button has no visible name, and the same rule holds for `Input`'s label and `Badge`'s text.

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

**Fields register themselves.** Every `Input` inside a `Form` finds it through a cascading value and
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
purely a visual scanning aid — not a tooltip or help mechanism. That would need a `Tooltip`
component, which does not exist yet.

**Optional `Prefix` / `Suffix`.** `RenderFragment`s for units (`kg`, `kcal`) or fixed adornments,
rendered inside the field so they share its border.

**Password reveal toggle.** Only rendered when `Type="password"`. A visible text button
("Anzeigen" / "Verbergen"), never icon-only, following the same rule as `Button`. Its accessible
name adds the field's label ("Passwort anzeigen"), because a page with two password fields would
otherwise have two identical "Anzeigen" buttons; `aria-controls` points at the field. It is
disabled together with the field, and its hit area is the full 44×44 px target. It toggles the
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

## How this is kept true

`tools/VitalSync.DesignTokens.Contrast` resolves every `var()` chain per theme and measures the
pairs named in the rule document — 204 contrast checks and 6 separation checks at the time of
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
