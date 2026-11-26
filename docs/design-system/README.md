# Design System

The design system is developed in code — there is no Figma and no design tool export. It lives in
the `VitalSync.DesignSystem` Razor Class Library: `wwwroot/vitalsync-tokens.css` holds the tokens,
`vitalsync-contrast-rules.json` next to it holds the accessibility rules those tokens are held to.

The documents below carry the reasoning. The stylesheet carries none, because it holds no comments,
and a rationale that lives next to a value is a rationale nobody updates when the value changes.

- **[Foundations](./foundations.md)** — what the system is for, the token architecture, the themes,
  the colour rules, data visualisation, icons, the conventions no token can enforce, the responsive
  strategy and the tone of voice.
- **[Forms](./forms.md)** — the button, form validation, the form and its fields, and the small
  displays used next to them: toggletip, badge, avatar and spinner.
- **[Feedback and overlays](./feedback-and-overlays.md)** — the live announcer, alert, alert banner,
  toast, empty state, dialog and tabs.
- **[Data](./data.md)** — the table, the data table, the vitals card and the metric chart.
- **[Layout and maintenance](./layout-and-maintenance.md)** — the app navigation, the page header
  and the dashboard grid, how the rules are kept true, how to change a colour, how to add a
  component, and the component gallery.
