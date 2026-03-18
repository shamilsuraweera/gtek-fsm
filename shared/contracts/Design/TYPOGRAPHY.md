# Typography System & Guidelines

## Overview

Typography establishes hierarchy, readability, and visual consistency across web (Blazor) and mobile (MAUI) clients. This document defines the complete type scale, font families, sizing rules, and usage patterns.

---

## Font Family Stack

### Primary Font (Body & UI)
- **Web:** `"Segoe UI", -apple-system, BlinkMacSystemFont, "Helvetica Neue", sans-serif`
- **Mobile:** Native system font (Segoe UI on Android, SF Pro Display on iOS via MAUI defaults)
- **Purpose:** Default font for all body text, labels, and UI elements
- **Characteristics:** Clean, modern, highly legible at small sizes

### Secondary Font (Headings - Optional)
- **Web:** `"Segoe UI", -apple-system, BlinkMacSystemFont, sans-serif`
- **Mobile:** Same as primary (consider custom font in future phases)
- **Purpose:** Emphasis and hierarchy; currently matches primary for consistency
- **Future:** Consider a distinct heading font (e.g., Inter, Poppins) in Phase 4+

### Monospace Font (Code & Technical)
- **Web:** `"Courier New", "Consolas", "Monaco", monospace`
- **Mobile:** `"Courier New"` via XAML FontFamily
- **Purpose:** Code blocks, error messages, technical data
- **Usage:** Limited in MVP; primarily for error reporting and logs

---

## Type Scale

### Heading Levels

| Token | Size (web) | Size (mobile) | Weight | Line Height | Usage |
|-------|-----------|--------------|--------|-------------|-------|
| Display | 48px | 40px | 700 | 1.2 | Hero/page title (rare) |
| H1 | 32px | 28px | 700 | 1.3 | Page title, major section |
| H2 | 24px | 22px | 600 | 1.35 | Section heading |
| H3 | 20px | 18px | 600 | 1.4 | Subsection/card title |
| H4 | 18px | 16px | 600 | 1.4 | Minor heading/label |
| H5 | 16px | 14px | 600 | 1.45 | Tertiary heading |
| H6 | 14px | 12px | 500 | 1.5 | Metadata/timestamp |

### Body Text

| Token | Size (web) | Size (mobile) | Weight | Line Height | Usage |
|-------|-----------|--------------|--------|-------------|-------|
| BodyLarge | 18px | 16px | 400 | 1.6 | Lead text, introduction |
| Body | 16px | 14px | 400 | 1.6 | Default paragraph text |
| BodySmall | 14px | 12px | 400 | 1.5 | Secondary text, descriptions |

### UI Text

| Token | Size (web) | Size (mobile) | Weight | Line Height | Usage |
|-------|-----------|--------------|--------|-------------|-------|
| Label | 14px | 12px | 500 | 1.4 | Form labels, captions |
| Caption | 12px | 11px | 400 | 1.4 | Timestamps, hints, metadata |
| ButtonText | 16px | 14px | 600 | 1.2 | Button labels (all caps optional) |
| BadgeText | 12px | 11px | 600 | 1.2 | Badge labels (all caps) |

### Emphasis Variants

- **Bold:** Weight 700 (for emphasis within text)
- **Semibold:** Weight 600 (for subheadings and secondary emphasis)
- **Regular:** Weight 400 (for body and UI defaults)
- **Light:** Weight 300 (for subtle text, rarely used)

---

## CSS/SCSS Implementation (Web)

### CSS Custom Properties

```css
:root {
  /* Display & Headings */
  --font-display-size: 48px;
  --font-display-weight: 700;
  --font-display-line-height: 1.2;

  --font-h1-size: 32px;
  --font-h1-weight: 700;
  --font-h1-line-height: 1.3;

  --font-h2-size: 24px;
  --font-h2-weight: 600;
  --font-h2-line-height: 1.35;

  --font-h3-size: 20px;
  --font-h3-weight: 600;
  --font-h3-line-height: 1.4;

  --font-h4-size: 18px;
  --font-h4-weight: 600;
  --font-h4-line-height: 1.4;

  --font-h5-size: 16px;
  --font-h5-weight: 600;
  --font-h5-line-height: 1.45;

  --font-h6-size: 14px;
  --font-h6-weight: 500;
  --font-h6-line-height: 1.5;

  /* Body */
  --font-body-large-size: 18px;
  --font-body-large-weight: 400;
  --font-body-large-line-height: 1.6;

  --font-body-size: 16px;
  --font-body-weight: 400;
  --font-body-line-height: 1.6;

  --font-body-small-size: 14px;
  --font-body-small-weight: 400;
  --font-body-small-line-height: 1.5;

  /* UI */
  --font-label-size: 14px;
  --font-label-weight: 500;
  --font-label-line-height: 1.4;

  --font-caption-size: 12px;
  --font-caption-weight: 400;
  --font-caption-line-height: 1.4;

  /* Font Families */
  --font-family-primary: "Segoe UI", -apple-system, BlinkMacSystemFont, "Helvetica Neue", sans-serif;
  --font-family-mono: "Courier New", "Consolas", "Monaco", monospace;
}
```

### SCSS Mixins

```scss
@mixin typography-display {
  font-size: var(--font-display-size);
  font-weight: var(--font-display-weight);
  line-height: var(--font-display-line-height);
  letter-spacing: -0.5px;
}

@mixin typography-h1 {
  font-size: var(--font-h1-size);
  font-weight: var(--font-h1-weight);
  line-height: var(--font-h1-line-height);
}

@mixin typography-h2 {
  font-size: var(--font-h2-size);
  font-weight: var(--font-h2-weight);
  line-height: var(--font-h2-line-height);
}

@mixin typography-body {
  font-size: var(--font-body-size);
  font-weight: var(--font-body-weight);
  line-height: var(--font-body-line-height);
}

@mixin typography-caption {
  font-size: var(--font-caption-size);
  font-weight: var(--font-caption-weight);
  line-height: var(--font-caption-line-height);
}
```

### Blazor Component Usage

```razor
@* Heading component *@
<@(@Tag) class="typography-@Type">
    @ChildContent
</@(@Tag)>

@code {
    [Parameter]
    public string Type { get; set; } = "h1"; // h1, h2, h3, body, caption

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string Tag => Type switch
    {
        "h1" => "h1",
        "h2" => "h2",
        "h3" => "h3",
        "body" => "p",
        "caption" => "span",
        _ => "p"
    };
}
```

**Usage:**
```razor
<Typography Type="h1">Page Title</Typography>
<Typography Type="body">This is body text with normal weight.</Typography>
<Typography Type="caption">Metadata timestamp</Typography>
```

---

## MAUI Implementation (Mobile)

### Named Styles (App.xaml)

```xaml
<!-- Headings -->
<Style x:Key="HeadingH1" TargetType="Label">
    <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
    <Setter Property="FontSize" Value="28" />
    <Setter Property="FontAttributes" Value="Bold" />
    <Setter Property="LineBreakMode" Value="WordWrap" />
</Style>

<Style x:Key="HeadingH2" TargetType="Label">
    <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
    <Setter Property="FontSize" Value="22" />
    <Setter Property="FontAttributes" Value="Bold" />
</Style>

<Style x:Key="HeadingH3" TargetType="Label">
    <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
    <Setter Property="FontSize" Value="18" />
    <Setter Property="FontAttributes" Value="Bold" />
</Style>

<!-- Body Text -->
<Style x:Key="BodyDefault" TargetType="Label">
    <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="LineBreakMode" Value="WordWrap" />
</Style>

<Style x:Key="BodySmall" TargetType="Label">
    <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
    <Setter Property="FontSize" Value="12" />
    <Setter Property="LineBreakMode" Value="WordWrap" />
</Style>

<!-- UI Text -->
<Style x:Key="LabelDefault" TargetType="Label">
    <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
    <Setter Property="FontSize" Value="12" />
    <Setter Property="FontAttributes" Value="Bold" />
</Style>

<Style x:Key="Caption" TargetType="Label">
    <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
    <Setter Property="FontSize" Value="11" />
    <Setter Property="Opacity" Value="0.7" />
</Style>
```

### XAML Usage

```xaml
<VerticalStackLayout Padding="16" Spacing="12">
    <Label Text="Page Title" Style="{StaticResource HeadingH1}" />
    <Label Text="Section heading" Style="{StaticResource HeadingH2}" />
    <Label Text="Body text content" Style="{StaticResource BodyDefault}" />
    <Label Text="Secondary text" Style="{StaticResource BodySmall}" />
    <Label Text="Meta information" Style="{StaticResource Caption}" />
</VerticalStackLayout>
```

---

## Responsive Typography (Web)

### Mobile Breakpoint Overrides

```scss
@media (max-width: 768px) {
  :root {
    --font-h1-size: 28px;
    --font-h2-size: 22px;
    --font-h3-size: 18px;
    --font-body-size: 14px;
    --font-body-small-size: 12px;
  }
}

@media (max-width: 480px) {
  :root {
    --font-h1-size: 24px;
    --font-h2-size: 18px;
    --font-body-size: 13px;
  }
}
```

---

## Best Practices

1. **Use the Scale:** Always select from the defined type scale. Do not use arbitrary font sizes.

2. **Hierarchy:** Use size and weight to create visual hierarchy, not by changing color alone.

3. **Line Length:** On web, keep line length between 50–75 characters for body text. Use multi-column layouts on large screens.

4. **Contrast:** Ensure sufficient contrast between text and background for accessibility (WCAG AA minimum 4.5:1).

5. **Readability:**
   - Line height increases with smaller text (1.5–1.6 for body)
   - Line height decreases with larger text (1.2–1.3 for headings)
   - Letter spacing can help with all-caps labels

6. **Font Loading (Web):** Use `font-display: swap` to ensure text renders immediately with fallback fonts.

7. **Consistency:** Apply typography through predefined components/styles, not inline. This ensures consistency and enables global updates.

---

## Future Expansion

1. **Variable Font:** Adopt a variable font for finer weight gradations and smaller file sizes.
2. **Localization:** Adjust sizing for languages with larger character sets (e.g., Chinese, Arabic).
3. **Dynamic Scaling:** Implement responsive typography that scales based on viewport width more granularly.
4. **Web Fonts:** Integrate with design CDN (Google Fonts, Typekit) if custom fonts are added.
5. **Animation:** Define transitions for typography changes during state updates (e.g., focus states).

---

## Related Resources

- Design Tokens: [DESIGN_TOKENS.md](DESIGN_TOKENS.md)
- Spacing System: [SPACING.md](SPACING.md)
- Component Naming: [COMPONENT_NAMING.md](COMPONENT_NAMING.md)
- Accessibility Guidelines: (Link to WCAG resources)
