# Forms

Part of the [Design System](./README.md).

## Button

Three variants: `Primary` for the main action, `Secondary` for a less prominent one, `Critical`
for a destructive one. The destructive variant carries the same name as the semantic role it is
filled with, `--color-surface-critical`; there is no separate `Danger`. `Primary` is the default.

**A label is mandatory once there is an icon.** `Icon` is an optional, decorative `RenderFragment`
rendered before the label and `aria-hidden`. An `Icon` without `ChildContent` throws: an icon-only
button has no visible name, and the same rule holds for `Input`'s label and `Badge`'s text. The
system has three icon-only buttons, the help button of [`Toggletip`](#toggletip) and the close
buttons of the [messages](./feedback-and-overlays.md#messages) and the [dialog](./feedback-and-overlays.md#dialog), for the reasons given there.

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

**Loading.** `Loading` marks a running action. The label is replaced by `LoadingLabel`, by default
"Bitte warten …", with a turning ring before it in place of the icon; a blank `LoadingLabel`
throws. The button keeps its variant colours, so the user still sees which action is running, but
it no longer darkens on hover and the cursor turns to `progress`. The width may change with the
label, which is accepted rather than reserved in advance.

**A loading button stays focusable.** It does not take the native `disabled`, which would drop focus
to the page when the user has just activated it. It is `aria-disabled="true"` instead and ignores
every click until it is done. `Loading` therefore wins over `Disabled`: a disabled button that
starts loading becomes focusable for that time. A submit button that is loading renders as
`type="button"`, so a click on it cannot submit the form again.

**The change is announced once.** The label itself turns into the loading label, so a screen reader
on the focused button reads it from there. When the button is not focused — the page started the
action, or focus has moved on — the start is announced once through the
[`LiveAnnouncer`](./feedback-and-overlays.md#live-announcer), only on the change from idle to loading and not for a button
that is already loading when it appears. There is no extra live region next to the button: JAWS
read the label and such a region one after the other, so "Bitte warten" came twice. The ring is
`aria-hidden`. How the action ended is not the button's to say; the page reports it through a
[toast or alert banner](./feedback-and-overlays.md#messages).

**The ring follows the Spinner, sized to the icon.** It takes `--icon-size-md`, the stroke width of
a thick border and `--motion-duration-spin`, so reduced motion slows it the same way. It is an arc
only, without a track, drawn in the variant's text colour and held to 3:1 against the variant's
fill — for `Secondary` against the page, a card, a dialog and every alert background it may sit on.
Under forced colours the arc is drawn in `ButtonText`.

**Inside a `Form` it loads on its own.** A submit button that triggered a valid submit is set to
loading by the [`Form`](#form) until `OnValidSubmit` has finished, without the page binding
`Loading`. Only the clicked button loads, not every submit button in the form.

## Form validation

**Visual coding.** An error is border plus icon plus text, optionally the subtle fill. Never colour
alone. The icon is the X in a circle, the same mark a critical `Badge` carries. There is no default
success tick — flagging every valid field is visual noise in a long form, so the error is the only
actively signalled state.

**Timing.** A field is checked once the user has typed in it and leaves it, not while typing and
not when focus merely passes over an untouched field — tabbing through an empty form raises no
errors. Typing counts even when the content ends where it started: typing a name and deleting it
again is checked on leaving, although the browser reports no change. After the first error, the
field is revalidated on every further keystroke, but what is shown follows two rules: an error that
is fixed disappears at once, so a correction is acknowledged
immediately; a new or different error appears only when the field is left. The message therefore
never changes under the user's fingers while they type. On submit, everything is validated again
and focus moves to the first field in error.

**Native validation is off.** `Form` sets `novalidate`. The browser's own bubbles appear before
the designed error state, in the browser's language and styling, and disappear again on their own;
the native `required` attribute stays for its semantics, only the browser's interception of the
submit goes.

**ARIA is contextual, not a blanket obligation.** `aria-describedby` only when helper or error text
actually exists — a plain search field needs none. `aria-required` only on custom controls with no
native equivalent; on a native `<input required>` it is redundant. `aria-invalid` on any control
that can enter an error state; a checkbox or radio button states `aria-invalid="false"` while it
has no error, because a required control without the attribute is reported as invalid by the
browser before the user has done anything.

**An error message is not a live region.** The message carries no `role="alert"`. A field that is
already in error when the page loads — a pre-filled value, a restored draft — would otherwise be
read out on load, and a message present in the first HTML was read before the page's heading. A
message is announced instead when it newly appears after a change, once, through the
[`LiveAnnouncer`](./feedback-and-overlays.md#live-announcer), as "label: message", politely. An error that is already shown
is not announced again. A change that clears a shown error is announced as "label: in Ordnung.":
choosing a radio option, a checkbox or a select option is read out by the screen reader at once,
before the server's answer removes `aria-invalid`, so the stale "invalid" would otherwise stand. A
text field stays silent here: its error disappears as soon as the typing fixes it, while the user
is still in the field. Once a summary is shown, the summary changes are announced instead, see
below, and the individual fields stay silent.

**Error summary.** Beyond three fields we add a summary at the top of the form in addition to the
inline errors, focusable with `tabindex="-1"` and focused programmatically on an invalid submit, so
the screen reader reads it there, with skip links to each field in error. It follows the same
timing as the fields: it changes when a field is left, not on every keystroke. Each change is
announced politely through the [`LiveAnnouncer`](./feedback-and-overlays.md#live-announcer), after the screen reader has
named the field the user moved to: what changed and what is left, as "E-Mail: in Ordnung. Noch 3
Angaben." or "label: message. Noch 4 Angaben.", and "Alle Angaben in Ordnung." once the last entry
is gone. Not `assertive`, which would cut off the name of that field. The summary itself is not a
live region: it would only say the new count, because removing a list entry is not read out, and
it would repeat what the announcement already says. The three-field threshold is our own
convention; WCAG techniques G83 and G85 do not name one.

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
required, and disappears with the last one. The summary's focus target exists from the first
render, empty, once more than three fields are registered.

**What happens on an invalid submit.** With up to three fields, focus moves to the first enabled
field in error. Beyond three, the summary fills, receives focus, and lists one link per field in
error, in rendering order, each reading "label: first message". A link moves focus to its field.
Its `href` also points at the field on the current page, so it stays correct with `<base href>`
and without a live connection. The summary follows every further validation change once the field
is left, announces it as described under the error summary, and empties on a valid submit.

**What happens on a valid submit.** While `OnValidSubmit` runs, a further submit is ignored without
a message, whether it comes from a click or from Enter in a field. The [`Button`](#button) that
started the submit shows its loading state for exactly that time and ends it when the handler
completes or throws; an exception is passed on, not swallowed. A submit started without a button,
such as Enter in a form that has none, is still guarded but shows no loading. The fields stay
editable. Whether a submit is running is not exposed to the page; a page that needs to know keeps
its own state, as for `Loading`.

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
rendered inside the field so they share its border. Both are part of the field's description
(`aria-describedby`, before the helper or error text), so a screen reader names the unit along with
the field rather than leaving it to be found by reading around it.

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
`<VitalSync.DesignSystem.Primitives.Inputs.Input>`. A self-closing `<Input … />` is unaffected.

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
error takes the subtle critical fill and border; the message below carries the icon, and the input
carries `aria-invalid` and `aria-describedby`. A checked box keeps
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
`aria-invalid` sits on every radio button, `false` while there is no error as on
[`Select`](#select), and never on the `<fieldset>`, whose role does not support it; the component
removes it from the attributes it passes through.

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
close buttons of the [messages](./feedback-and-overlays.md#messages) and the [dialog](./feedback-and-overlays.md#dialog): an info icon beside a label is a convention users already know,
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

**A status badge says its kind to screen readers.** A visually hidden "Erfolg:", "Warnung:" or
"Fehler:" comes before the label, so "Im Ziel" is read as "Erfolg: Im Ziel". The icon that shows the
kind on screen is `aria-hidden`, and a label alone rarely says whether it is good or bad news. The
prefix is not shown: on screen the icon and the colour already say it, and the word would only add
length. The hidden prefix keeps its trailing space, so it is not read run together with the label.
A neutral badge has no prefix.

**A neutral badge takes any icon.** By default it shows a tag; `Icon` replaces that with a
`RenderFragment` of the page's choosing, for a category that has its own symbol.

**A status badge has a fixed icon.** `Success` shows a check in a circle, `Warning` a triangle,
`Critical` an X in a circle — all from Phosphor's Fill set (see [Icons](./foundations.md#icons)). Passing an `Icon`
to a status variant throws. The shape, not just the colour, tells the three states apart, which is
what makes the status colours' measured separation shortfall acceptable (see
[Data visualisation](./foundations.md#data-visualisation)). The X in a circle is the same mark that flags an input
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

**It never wraps.** A badge is one short label on one line. Squeezed — a table column shrunk to its
narrowest content, a tight row — it keeps its width and the surroundings give way, instead of
breaking "Im Ziel" into two lines inside a pill. The label therefore has to stay short; a badge text
too long for a 320 px screen is the wrong text, not a case for wrapping.

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
reliably by every screen reader. JAWS announces it, as the manual screen-reader test with the
gallery demo that inserts the spinner on demand showed; other screen readers are untested.

**The end is the page's to say.** A spinner that disappears says nothing, so a screen reader hears
that loading started but not that it finished. The page announces the outcome through the
[`LiveAnnouncer`](./feedback-and-overlays.md#live-announcer) once the content is there, such as "Rezepte geladen.", or a
failure as a message.

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

**One size.** 24 px at the default text size, in rem so it grows with the user's text size. There
is no smaller size: the loading state of [`Button`](#button) draws its own ring, sized to the icon
it replaces, and has no track.

**Tokens and rules.** The `--spinner-*` set: size, stroke width, arc, track and text colours, the
gap and the duration, which points at the new global `--motion-duration-spin`. Five rules: the arc
against page, card and track, and the text against page and card.
