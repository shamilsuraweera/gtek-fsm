# Design Tokens & Unified Reference

## Overview

Design tokens are the atomic decision units of a design system. They encapsulate visual properties (colors, typography, spacing, shadows, etc.) and provide a single source of truth for both web (Blazor) and mobile (MAUI) clients. This document serves as the unified reference point for all design decisions across the platform.

---

## Token Categories

### 1. **Color Tokens**

Color tokens define the semantic palette for light and dark themes.

#### Theme-Aware Color System

```text
ColorBgPage          # Page background (primary background)
ColorBgPageDark      # Page background (dark mode)
ColorBgSurface       # Surface/card background (raised elements)
ColorBgSurfaceDark   # Surface background (dark mode)
ColorTextPrimary     # Primary text (high contrast)
ColorTextPrimaryDark # Primary text (dark mode)
ColorTextMuted       # Secondary text (reduced contrast)
ColorTextMutedDark   # Secondary text (dark mode)
ColorBorderDefault   # Default border color
ColorBorderDefaultDark # Default border (dark mode)
ColorAccent          # Primary accent/CTA color
ColorAccentDark      # Primary accent (dark mode)
```

#### Status Colors

```text
ColorStatusSuccess   # Success/positive state (#10B981 or similar)
ColorStatusError     # Error/destructive state (#EF4444 or similar)
ColorStatusWarning   # Warning/caution state (#F59E0B or similar)
ColorStatusInfo      # Informational state (#3B82F6 or similar)
```

#### Usage Examples

**Web (CSS Custom Properties):**

````css
:root {
  /* Light Mode */
  --color-bg-page: #FFFFFF;
  --color-bg-surface: #F5F5F5;
  --color-text-primary: #000000;
  --color-text-muted: #666666;
  --color-border-default: #E0E0E0;
  --color-accent: #0F6ABD;

  /* Status Colors */
  --color-status-success: #10B981;
  --color-status-error: #EF4444;
  --color-status-warning: #F59E0B;
  --color-status-info: #3B82F6;
}

@media (prefers-color-scheme: dark) {
  :root {
    /* Dark Mode */
    --color-bg-page: #1A1A1A;
    --color-bg-surface: #2C2C2C;
    --color-text-primary: #FFFFFF;
    --color-text-muted: #999999;
    --color-border-default: #444444;
    --color-accent: #2563EB;
  }
}
```text

**Mobile (XAML Resources):**

```xaml
<Color x:Key="ColorBgPage">#FFFFFF</Color>
<Color x:Key="ColorBgPageDark">#1A1A1A</Color>
<Color x:Key="ColorBgSurface">#F5F5F5</Color>
<Color x:Key="ColorBgSurfaceDark">#2C2C2C</Color>
<Color x:Key="ColorTextPrimary">#000000</Color>
<Color x:Key="ColorTextPrimaryDark">#FFFFFF</Color>
<Color x:Key="ColorAccent">#0F6ABD</Color>
<Color x:Key="ColorStatusSuccess">#10B981</Color>
<Color x:Key="ColorStatusError">#EF4444</Color>
<Color x:Key="ColorStatusWarning">#F59E0B</Color>
<Color x:Key="ColorStatusInfo">#3B82F6</Color>
````

---

### 2. **Typography Tokens**

Typography tokens define font families, sizes, weights, and line heights.

| Token               | Size | Weight | Line Height | Category |
| ------------------- | ---- | ------ | ----------- | -------- |
| TypographyDisplay   | 48px | 700    | 1.2         | Heading  |
| TypographyH1        | 32px | 700    | 1.3         | Heading  |
| TypographyH2        | 24px | 600    | 1.35        | Heading  |
| TypographyH3        | 20px | 600    | 1.4         | Heading  |
| TypographyH4        | 18px | 600    | 1.4         | Heading  |
| TypographyBody      | 16px | 400    | 1.6         | Body     |
| TypographyBodySmall | 14px | 400    | 1.5         | Body     |
| TypographyCaption   | 12px | 400    | 1.4         | UI       |
| TypographyLabel     | 14px | 500    | 1.4         | UI       |

**Font Families:**

- Primary: `"Segoe UI", -apple-system, BlinkMacSystemFont, sans-serif`
- Mono: `"Courier New", "Consolas", monospace`

**Reference:** See [TYPOGRAPHY.md](TYPOGRAPHY.md) for full documentation.

---

### 3. **Spacing Tokens**

Spacing tokens define layout intervals based on a 4px grid.

```text
SpacingXs    = 4px
SpacingSm    = 8px
SpacingMd    = 16px
SpacingLg    = 24px
SpacingXl    = 32px
Spacing2xl   = 48px
Spacing3xl   = 64px
```

**Web Implementation:**

````css
:root {
  --spacing-xs: 4px;
  --spacing-sm: 8px;
  --spacing-md: 16px;
  --spacing-lg: 24px;
  --spacing-xl: 32px;
  --spacing-2xl: 48px;
  --spacing-3xl: 64px;
}
```text

**Mobile Implementation:**

```xaml
<x:Double x:Key="SpacingXs">4</x:Double>
<x:Double x:Key="SpacingSm">8</x:Double>
<x:Double x:Key="SpacingMd">16</x:Double>
<x:Double x:Key="SpacingLg">24</x:Double>
<x:Double x:Key="SpacingXl">32</x:Double>
<x:Double x:Key="Spacing2xl">48</x:Double>
<x:Double x:Key="Spacing3xl">64</x:Double>
````

**Reference:** See [SPACING.md](SPACING.md) for full documentation.

---

### 4. **Elevation/Shadow Tokens**

Elevation tokens create depth through shadows.

```text
ElevationNone       # No shadow
ElevationSm         # Subtle shadow (small, raised elements)
ElevationMd         # Medium shadow (cards, modals)
ElevationLg         # Pronounced shadow (floating elements, dropdowns)
ElevationXl         # Strong shadow (top-level overlays)
```

**Web CSS:**

````css
:root {
  --elevation-none: 0;
  --elevation-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
  --elevation-md: 0 4px 6px rgba(0, 0, 0, 0.1);
  --elevation-lg: 0 10px 15px rgba(0, 0, 0, 0.15);
  --elevation-xl: 0 20px 25px rgba(0, 0, 0, 0.2);
}

.card {
  box-shadow: var(--elevation-md);
}

.modal {
  box-shadow: var(--elevation-xl);
}
```text

**Mobile XAML:**

```xaml
<!-- MAUI doesn't have native shadow tokens; use Frame border/padding instead -->
<Frame HasShadow="True" CornerRadius="8" Padding="16">
    <!-- Content -->
</Frame>
````

---

### 5. **Border Radius Tokens**

Border radius tokens define corner rounding for consistency.

```text
RadiusNone    = 0px   # Sharp corners
RadiusSm      = 2px   # Minimal rounding
RadiusMd      = 4px   # Default rounding
RadiusLg      = 8px   # Prominent rounding
RadiusXl      = 12px  # Strong rounding
RadiusFull    = 50%   # Fully rounded (circles, pills)
```

**Web CSS:**

````css
:root {
  --radius-none: 0;
  --radius-sm: 2px;
  --radius-md: 4px;
  --radius-lg: 8px;
  --radius-xl: 12px;
  --radius-full: 50%;
}

.button {
  border-radius: var(--radius-md);
}

.card {
  border-radius: var(--radius-lg);
}

.avatar {
  border-radius: var(--radius-full);
}
```text

**Mobile XAML:**

```xaml
<Button CornerRadius="4" />
<Frame CornerRadius="8" />
<Ellipse /><!-- For fully rounded elements -->
````

---

### 6. **Duration/Animation Tokens**

Duration tokens define animation timing for consistency.

```text
DurationFast     = 100ms   # Quick feedback
DurationNormal   = 200ms   # Standard transitions
DurationSlow     = 300ms   # Emphasis transitions
```

**Web CSS:**

````css
:root {
  --duration-fast: 100ms;
  --duration-normal: 200ms;
  --duration-slow: 300ms;
}

.button {
  transition: background-color var(--duration-normal) ease-in-out;
}
```text

**Mobile Animation (C#):**

```csharp
// Standard animation duration in code-behind
const int AnimationDurationMs = 200;

element.FadeTo(1, (uint)AnimationDurationMs);
````

---

## Unified Token Reference Table

| Category       | Token              | Value                        | Usage                               |
| -------------- | ------------------ | ---------------------------- | ----------------------------------- |
| **Color**      | ColorBgPage        | #FFFFFF (#1A1A1A dark)       | Page background                     |
|                | ColorAccent        | #0F6ABD (#2563EB dark)       | Primary CTA, emphasis               |
|                | ColorStatusSuccess | #10B981                      | Success messages, confirmations     |
|                | ColorStatusError   | #EF4444                      | Error messages, destructive actions |
|                | ColorStatusWarning | #F59E0B                      | Warning alerts, caution states      |
|                | ColorStatusInfo    | #3B82F6                      | Informational messages              |
| **Typography** | TypographyH1       | 32px, 700 weight             | Page titles                         |
|                | TypographyBody     | 16px, 400 weight             | Paragraph text                      |
|                | TypographyCaption  | 12px, 400 weight             | Metadata, hints                     |
| **Spacing**    | SpacingMd          | 16px                         | Default padding/margin              |
|                | SpacingLg          | 24px                         | Section separation                  |
| **Elevation**  | ElevationMd        | 0 4px 6px rgba(0,0,0,0.1)    | Cards                               |
|                | ElevationLg        | 0 10px 15px rgba(0,0,0,0.15) | Floating elements                   |
| **Radius**     | RadiusMd           | 4px                          | Buttons, inputs                     |
|                | RadiusLg           | 8px                          | Cards, modals                       |
| **Duration**   | DurationNormal     | 200ms                        | Standard transitions                |

---

## Token Consumption Examples

### Web (Blazor)

**SCSS:**

````scss
.card {
  background-color: var(--color-bg-surface);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  box-shadow: var(--elevation-md);
  transition: box-shadow var(--duration-normal) ease-in-out;
}

.card:hover {
  box-shadow: var(--elevation-lg);
}

.card-title {
  font-size: var(--font-h2-size);
  font-weight: var(--font-h2-weight);
  color: var(--color-text-primary);
  line-height: var(--font-h2-line-height);
}
```text

**Razor Component:**

```razor
<div class="card">
    <h2 class="card-title">@Title</h2>
    <p class="card-content">@Content</p>
</div>
````

### Mobile (.NET MAUI)

**XAML:**

````xaml
<Frame
    Padding="{StaticResource SpacingLg}"
    CornerRadius="8"
    HasShadow="True"
    BackgroundColor="{AppThemeBinding
        Light={StaticResource ColorBgSurface},
        Dark={StaticResource ColorBgSurfaceDark}}">
    <VerticalStackLayout Spacing="{StaticResource SpacingMd}">
        <Label
            Text="Card Title"
            FontSize="24"
            FontAttributes="Bold"
            TextColor="{AppThemeBinding
                Light={StaticResource ColorTextPrimary},
                Dark={StaticResource ColorTextPrimaryDark}}" />
        <Label Text="Card content" />
    </VerticalStackLayout>
</Frame>
```text

---

## Token Versioning & Updates

### Version Format
`MAJOR.MINOR.PATCH`

- **MAJOR:** Breaking token changes (renamed, removed, or significantly changed semantics)
- **MINOR:** New tokens added without modifying existing ones
- **PATCH:** Token value updates not affecting structure or naming

### Example Changelog

````

v1.0.0 — Initial token system (colors, typography, spacing, elevation, radius, duration)
v1.1.0 — Add animation tokens for transitions
v1.2.0 — Add soft/muted color variants for secondary UI
v2.0.0 — Rename ColorBgPageDark to ColorBgPageDarkMode (breaking change)

```text

### Update Process

1. Define changes in design system
2. Update tokens in this document
3. Update CSS custom properties (web)
4. Update XAML resources (mobile)
5. Update consuming components
6. Test in both clients
7. Tag version and release notes

---

## Best Practices

1. **Use Tokens:** Always use defined tokens. Never use hardcoded values for design properties.

2. **Semantic Naming:** Use semantic names (`ColorAccent`, not `ColorBlue`) so tokens can be updated globally.

3. **Theme Support:** Provide light and dark variants for all relevant tokens.

4. **Consistency:** Apply the same token across both web and mobile clients whenever possible.

5. **Documentation:** Document token usage and rationale for future reference.

6. **Scalability:** Design tokens to accommodate future needs without breaking existing implementations.

7. **Review:** Review token usage regularly to identify unused or redundant tokens.

---

## Related Documentation

- [ICON_STRATEGY.md](ICON_STRATEGY.md) — Icon naming and organization
- [TYPOGRAPHY.md](TYPOGRAPHY.md) — Font stack, type scale, and sizing
- [SPACING.md](SPACING.md) — Spacing scale and application patterns
- [COMPONENT_NAMING.md](COMPONENT_NAMING.md) — Component naming and organization

---

## Future Enhancements

1. **Centralized Token Repository:** Move tokens to a dedicated design token tool (Tokens Studio, Amazon Style Dictionary, etc.)
2. **Automated Generation:** Generate CSS, XAML, and TypeScript from a single source
3. **Token Validation:** Automated checks to prevent invalid token combinations
4. **Design Tool Sync:** Sync tokens between design tool (Figma) and code
5. **Performance Analytics:** Track token usage and identify optimization opportunities
6. **Accessibility Compliance:** Automated contrast and accessibility checks for color tokens
```
