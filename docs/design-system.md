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

`[data-theme="dark"]` on `<html>` is the single source of truth. There is no parallel
`@media (prefers-color-scheme)` block. The Blazor app reads the system preference once at startup
through JS interop and sets the attribute before the first render, which avoids a flash of the
wrong theme. 

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
- `--color-icon-critical` — the warning mark inside the field. Same obligation as the border.
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
surface, ring against separator, separator against the component it surrounds.

Neutral rather than brand is the point. A neutral ring has enough contrast against every surface
and every component fill in the system, including future green, amber or red ones. A brand colour
can be devalued by any new component that happens to share it, which is exactly what had happened.
This is the direction WCAG 2.2 takes with 2.4.11.

`--input-border-color-focus` is decoupled from the ring and stays blue. The ring says "the keyboard
focus is here", the border says "this field is active". Two statements, two tokens.

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

## Conventions no token can enforce

- **Disabled is colour or opacity, never both.** Text in a disabled control takes
  `--color-text-disabled` and no opacity; non-text parts take `--opacity-disabled`. Stacked they
  multiply, landing at 1.48:1 in light and 1.37:1 in dark. Disabled controls are exempt from 1.4.3,
  but for this user base that is effectively invisible. Dimming a whole control with `opacity` is
  the convenient move and therefore the likely mistake.
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

## Form validation

**Visual coding.** An error is border plus icon plus text, optionally the subtle fill. Never colour
alone. There is no default success tick — flagging every valid field is visual noise in a long
form, so the error is the only actively signalled state.

**Timing.** First check on blur, not while typing. After the first error, revalidate live on every
further keystroke in that field, so a correction is acknowledged immediately. On submit, everything
is validated again and focus moves to the first field in error.

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
"* = required" note per form, because some screen readers do not announce a bare asterisk.

## How this is kept true

`tools/VitalSync.DesignTokens.Contrast` resolves every `var()` chain per theme and measures the
pairs named in the rule document — 116 contrast checks and 4 separation checks at the time of
writing, all passing, with no open waiver.

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

While building, no colour, duration or size is hard-coded in a `.razor.css`. Everything goes through
a token — that is what makes the check, the reduced-motion fallback and the theme switch reach the
component at all. A hard-coded value is invisible to all three.
