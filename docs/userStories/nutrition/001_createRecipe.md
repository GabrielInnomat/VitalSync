# 001 - Create Recipe

## Description

As a user, I want to be able to create a recipe so that all my recipes are stored digitally in one place and can be accessed at any time.

## Acceptance Criteria

- Page title: **Create Recipe**
- **Save** button (primary)
- **Cancel** button (secondary)
- Required fields are marked with an asterisk (\*)
- public, default: false, toggler

### Recipe Name

- Required
- Single-line text input
- Maximum length: 200 characters

### Description

- Optional
- Multi-line text input

### Image

- Optional
- Maximum of one image
- Maximum file size: 1 MB
- Supported formats: `.jpg`, `.jpeg`, `.png`, `.gif`
- If no image is uploaded, a placeholder image is displayed.

### Servings

- Required
- Integer value
- Must be greater than 0

### Ingredient Overview (Calculated)

- Quantity
- Unit
- Ingredient name

### Preparation Step

- Heading with the step number: **Step x**
- Required
- Multi-line description field

#### Ingredients

- **Quantity**
  - Required
  - Decimal number
  - Must be greater than 0

- **Unit**
  - Required
  - Selection list
  - See the [Units User Story](./002_units.md)

- **Ingredient**
  - Required
  - Selection list

- **Add Ingredient** button (secondary) to add another ingredient
- **Remove Ingredient** button (danger) to remove an ingredient without confirmation

### Step Management

- Two preparation steps are available by default.
- Additional steps can be added using the **Add Step** button (secondary).
- A step can be removed using the **Remove Step** button (danger).

### Validation

- Required fields that are left empty are highlighted with a red border and a red error message:
  - **"This field is required."**

- Invalid input (e.g., `0` or a negative number) is also highlighted with a red border and an appropriate validation error message.
