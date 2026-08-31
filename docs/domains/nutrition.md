# Nutrition

The business domain concerned with food and diet. It is the most developed domain in the project
and the one the [user stories](../userStories/nutrition/001_createRecipe.md) currently describe.

**Status:** the bounded context is cut but not implemented. The service exists as a skeleton without
domain code, so everything below describes intent rather than shipped behavior.

## Purpose

A user manages the food side of their life here: what ingredients exist and what they contain, how
those ingredients combine into recipes, how recipes are planned over time, what has to be bought
for that plan, and what was actually consumed.

## Use cases

- Manage ingredients and their nutritional values.
- Create and edit recipes.
- Compose meal plans from recipes.
- Generate shopping lists from a meal plan.
- Calculate nutrient intake from consumed meals.

## Core concepts

### Ingredient

A single food item together with its nutritional values. The ingredient catalog is a reference data
set: many reads, comparatively few writes, and no history worth keeping.

### Recipe

The central aggregate of this context. A recipe carries a name, an optional description, an optional
image, a number of servings and a visibility flag, and it is composed of **preparation steps**. Each
step has a description and the ingredients it consumes, each with a quantity and a unit.

The ingredient overview of a recipe is derived from its steps rather than entered separately, which
makes it a calculated view rather than stored data.

A recipe is the canonical example used throughout the architecture documentation — `Recipe`,
`RecipeId`, `RecipeCreated` — to illustrate aggregates, strongly typed identifiers and domain
events.

### Unit

The measure a quantity is expressed in. Units are shared vocabulary across recipes and ingredients;
see the [units user story](../userStories/nutrition/002_units.md).

### Meal plan

Composes recipes over time. It is the basis for both the shopping list and the nutrient-intake
calculation.

### Shopping list

Derived from a meal plan: what has to be bought, aggregated across the recipes it contains.

## Persistence strategy

**Not yet decided.** The context is a candidate for state-stored persistence: the ingredient catalog
and recipe management are largely CRUD-shaped, and the history of an edited recipe has no obvious
business value.

Nutrient intake over time is the one part that argues the other way, being append-only by nature. If
that turns out to need a real event history, it is a signal that intake belongs in its own bounded
context rather than that this one needs two strategies — see the decision rule in
[Patterns](../patterns.md).

## Published integration events

None yet. Candidates are events about recipes and consumed meals, which
[Health Analytics](./health-analytics.md) would consume.

## Open questions

- Does nutrient intake stay in this context or become its own?
- Is a recipe's edit history of business value?
- How are units modeled — a fixed set, or user-extensible?
