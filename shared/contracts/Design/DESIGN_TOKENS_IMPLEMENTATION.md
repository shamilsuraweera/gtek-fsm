# Design Tokens Implementation Guide

## Overview

This document explains how to consume and apply design tokens in the platform. Design tokens are the single source of truth for colors, spacing, typography, and other visual properties. All UI components should reference tokens rather than hardcoding values.

---

## Web Portal (Blazor) Implementation

### Including Design Tokens in Your Project

**In `App.razor` or main layout:**
```razor
@* Link the design tokens CSS first, before any component styles *@
<link rel="stylesheet" href="css/_design-tokens.css" />
<link rel="stylesheet" href="app.css" />
```

### Consuming Tokens in Component Styles

#### CSS Classes

Use token values via CSS custom properties:

```scss
// SCSS Component Styles
.card {
  background-color: var(--color-bg-surface);
  padding: var(--spacing-lg);
  border: 1px solid var(--color-border-default);
  border-radius: var(--radius-lg);
  box-shadow: var(--elevation-md);
}

.card-title {
  font-size: var(--font-h3-size);
  font-weight: var(--font-h3-weight);
  line-height: var(--font-h3-line-height);
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-md);
}

.card-content {
  font-size: var(--font-body-size);
  color: var(--color-text-muted);
  line-height: var(--font-body-line-height);
}
```

#### Utility Classes

Use pre-built utility classes for quick styling:

```razor
<div class="p-lg mb-md gap-md shadow-md rounded-lg">
    <h2 class="text-primary">Title</h2>
    <p class="text-muted">Content</p>
</div>
```

**Available utilities:**
- Margin: `.m-*`, `.mt-*`, `.mb-*`, `.ml-*`, `.mr-*` (xs, sm, md, lg, xl)
- Padding: `.p-*`, `.pt-*`, `.pb-*`, `.pl-*`, `.pr-*` (xs, sm, md, lg, xl)
- Gap: `.gap-*` (xs, sm, md, lg, xl)
- Text color: `.text-primary`, `.text-secondary`, `.text-muted`, `.text-accent`
- Background: `.bg-page`, `.bg-surface`, `.bg-accent`
- Status: `.text-success`, `.text-error`, `.text-warning`, `.text-info`
- Shadow: `.shadow-none`, `.shadow-sm`, `.shadow-md`, `.shadow-lg`, `.shadow-xl`
- Radius: `.rounded-sm`, `.rounded-md`, `.rounded-lg`, `.rounded-full`

#### SCSS Mixins

Use mixins for consistent typography and patterns:

```scss
@mixin typography-heading {
  font-family: var(--font-family-primary);
  font-weight: 600;
  line-height: 1.3;
}

@mixin typography-body {
  font-family: var(--font-family-primary);
  font-size: var(--font-body-size);
  font-weight: var(--font-body-weight);
  line-height: var(--font-body-line-height);
}

@mixin card-style {
  background-color: var(--color-bg-surface);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  box-shadow: var(--elevation-md);
}

@mixin button-base {
  padding: 12px var(--spacing-md);
  border-radius: var(--radius-md);
  font-size: var(--font-button-size);
  font-weight: var(--font-button-weight);
  border: none;
  cursor: pointer;
  transition: background-color var(--duration-normal) ease-in-out;
}
```

### Creating Components with Tokens

**Example: Button Component**

```razor
@* Button.razor *@
<button class="button button--@Variant button--@Size">
    @ChildContent
</button>

@code {
    [Parameter]
    public string Variant { get; set; } = "primary"; // primary, secondary, ghost

    [Parameter]
    public string Size { get; set; } = "md"; // sm, md, lg

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
```

**Styling:**

```scss
.button {
  @include button-base;
  
  &--primary {
    background-color: var(--color-accent);
    color: white;
    
    &:hover {
      background-color: var(--color-accent-hover);
    }
    
    &:active {
      background-color: var(--color-accent-active);
    }
  }
  
  &--secondary {
    background-color: var(--color-bg-surface);
    color: var(--color-text-primary);
    border: 1px solid var(--color-border-default);
    
    &:hover {
      background-color: var(--color-border-default);
    }
  }
  
  &--ghost {
    background-color: transparent;
    color: var(--color-text-primary);
    
    &:hover {
      background-color: var(--color-bg-surface);
    }
  }
  
  &--sm {
    padding: 8px var(--spacing-sm);
    font-size: 14px;
  }
  
  &--md {
    padding: 12px var(--spacing-md);
    font-size: 16px;
  }
  
  &--lg {
    padding: 16px var(--spacing-lg);
    font-size: 18px;
  }
}
```

### Theme Switching at Runtime

The design tokens automatically adapt to light/dark theme via `prefers-color-scheme` media query. For manual theme switching:

```razor
@* ThemeToggle component *@
<button @onclick="ToggleTheme">
    @if (isDarkMode)
    {
        <span>☀️ Light Mode</span>
    }
    else
    {
        <span>🌙 Dark Mode</span>
    }
</button>

@code {
    private bool isDarkMode;
    
    private void ToggleTheme()
    {
        isDarkMode = !isDarkMode;
        
        if (isDarkMode)
        {
            Document.DocumentElement.Style.ColorScheme = "dark";
        }
        else
        {
            Document.DocumentElement.Style.ColorScheme = "light";
        }
    }
}
```

---

## Mobile App (.NET MAUI) Implementation

### Including Design Tokens in Your Project

**In `App.xaml`:**
```xaml
<?xml version="1.0" encoding="utf-8" ?>
<Application
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    x:Class="GTEK.FSM.MobileApp.App">
    <Application.Resources>
        <ResourceDictionary>
            <!-- Include design tokens -->
            <ResourceDictionary Source="Resources/DesignTokens.xaml" />
            <!-- Include other app resources -->
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### Consuming Tokens in XAML

#### Direct References

Use `StaticResource` to reference token values:

```xaml
<Label
    Text="Card Title"
    FontSize="{StaticResource FontSizeH2}"
    FontAttributes="Bold"
    TextColor="{AppThemeBinding
        Light={StaticResource ColorTextPrimaryLight},
        Dark={StaticResource ColorTextPrimaryDark}}" />
```

#### AppThemeBinding for Theme Support

Automatically switch between light and dark variants:

```xaml
<Frame
    Padding="{StaticResource SpacingMd}"
    CornerRadius="{StaticResource RadiusLg}"
    BackgroundColor="{AppThemeBinding
        Light={StaticResource ColorBgSurfaceLight},
        Dark={StaticResource ColorBgSurfaceDark}}"
    BorderColor="{AppThemeBinding
        Light={StaticResource ColorBorderDefaultLight},
        Dark={StaticResource ColorBorderDefaultDark}}">
    
    <VerticalStackLayout Spacing="{StaticResource SpacingMd}">
        <Label
            Text="Frame Title"
            Style="{StaticResource StyleHeadingH3}" />
        <Label
            Text="Frame content"
            Style="{StaticResource StyleBodyDefault}" />
    </VerticalStackLayout>
</Frame>
```

### Using Pre-Built Styles

Apply pre-built style presets from `DesignTokens.xaml`:

```xaml
<!-- Heading Styles -->
<Label Text="Page Title" Style="{StaticResource StyleHeadingH1}" />
<Label Text="Section" Style="{StaticResource StyleHeadingH2}" />
<Label Text="Subsection" Style="{StaticResource StyleHeadingH3}" />

<!-- Body Text Styles -->
<Label Text="Body text" Style="{StaticResource StyleBodyDefault}" />
<Label Text="Small text" Style="{StaticResource StyleBodySmall}" />

<!-- UI Text Styles -->
<Label Text="Label" Style="{StaticResource StyleLabel}" />
<Label Text="Caption" Style="{StaticResource StyleCaption}" />

<!-- Stack Layout Presets -->
<VerticalStackLayout Style="{StaticResource StyleStackDefault}">
    <Label Text="Default spacing layout" />
</VerticalStackLayout>

<VerticalStackLayout Style="{StaticResource StyleStackRelaxed}">
    <Label Text="Relaxed spacing layout" />
</VerticalStackLayout>

<!-- Card Style -->
<Frame Style="{StaticResource StyleCardDefault}">
    <Label Text="Card content" />
</Frame>
```

### Creating Custom Styles with Tokens

**Example: Custom Button Style**

```xaml
<!-- In DesignTokens.xaml or App.xaml -->
<Style x:Key="StyleButtonPrimary" TargetType="Button">
    <Setter Property="Padding" Value="16,12" />
    <Setter Property="CornerRadius" Value="{StaticResource RadiusMd}" />
    <Setter Property="FontSize" Value="{StaticResource FontSizeButton}" />
    <Setter Property="FontAttributes" Value="Bold" />
    <Setter Property="BackgroundColor" Value="{AppThemeBinding Light={StaticResource ColorAccentLight}, Dark={StaticResource ColorAccentDark}}" />
    <Setter Property="TextColor" Value="#FFFFFF" />
</Style>

<Style x:Key="StyleButtonSecondary" TargetType="Button">
    <Setter Property="Padding" Value="16,12" />
    <Setter Property="CornerRadius" Value="{StaticResource RadiusMd}" />
    <Setter Property="FontSize" Value="{StaticResource FontSizeButton}" />
    <Setter Property="FontAttributes" Value="Bold" />
    <Setter Property="BackgroundColor" Value="{AppThemeBinding
        Light={StaticResource ColorBgSurfaceLight},
        Dark={StaticResource ColorBgSurfaceDark}}" />
    <Setter Property="TextColor" Value="{AppThemeBinding Light={StaticResource ColorTextPrimaryLight}, Dark={StaticResource ColorTextPrimaryDark}}" />
    <Setter Property="BorderColor" Value="{AppThemeBinding Light={StaticResource ColorBorderDefaultLight}, Dark={StaticResource ColorBorderDefaultDark}}" />
    <Setter Property="BorderWidth" Value="1" />
</Style>
```

**Usage:**
```xaml
<Button Text="Save" Style="{StaticResource StyleButtonPrimary}" />
<Button Text="Cancel" Style="{StaticResource StyleButtonSecondary}" />
```

### Accessing Tokens in Code-Behind

For dynamic styling or animations:

```csharp
// C# Code-Behind
var primaryColor = Application.Current.Resources["ColorAccentLight"] as Color;
var spacing = (double)Application.Current.Resources["SpacingMd"];

myLabel.TextColor = (Color)Application.Current.Resources["ColorTextPrimary"];
myFrame.Padding = spacing;
```

---

## Color Palette Reference

### Status Colors

| Status | Light | Dark |
|--------|-------|------|
| Success | #10B981 (green) | #10B981 (green) |
| Error | #EF4444 (red) | #F87171 (lighter red) |
| Warning | #F59E0B (amber) | #FBBF24 (lighter amber) |
| Info | #3B82F6 (blue) | #60A5FA (lighter blue) |

### Neutral Palette

| Level | Light | Dark |
|-------|-------|------|
| Page Background | #FFFFFF | #0A0A0A |
| Surface | #F5F5F5 | #171717 |
| Border | #E8E8E8 | #404040 |
| Text Primary | #000000 | #FFFFFF |
| Text Muted | #737373 | #9CA3AF |

### Brand Color (Accent)

| Context | Light | Dark |
|---------|-------|------|
| Default | #0F6ABD | #2563EB |
| Hover | #0B4A85 | #1D4ED8 |
| Active | #073157 | #1E40AF |
| Background | #E0F2FE | #1E3A8A |

---

## Spacing Token Usage

### Common Patterns

**Page/Container Padding:**
```scss
.page { padding: var(--spacing-md); } /* 16px */
@media (min-width: 768px) {
    .page { padding: var(--spacing-lg); } /* 24px */
}
```

**List Item Spacing:**
```scss
.list-item {
    padding: var(--spacing-md);      /* 16px */
    margin-bottom: var(--spacing-sm); /* 8px gap between items */
}
```

**Form Field Spacing:**
```scss
.form-group {
    margin-bottom: var(--spacing-md); /* 16px between fields */
}

.form-label {
    margin-bottom: var(--spacing-xs); /* 4px between label and input */
}
```

---

## Typography Token Usage

### Blazor Examples

```razor
@* Apply typography styles via CSS classes *@
<h1 class="typography-h1">Page Title</h1>
<p class="typography-body">Body paragraph text</p>
<span class="typography-caption">Caption text</span>
```

**SCSS:**
```scss
.typography-h1 {
  font-size: var(--font-h1-size);
  font-weight: var(--font-h1-weight);
  line-height: var(--font-h1-line-height);
  font-family: var(--font-family-primary);
}

.typography-body {
  font-size: var(--font-body-size);
  font-weight: var(--font-body-weight);
  line-height: var(--font-body-line-height);
  font-family: var(--font-family-primary);
}
```

### Mobile Examples

```xaml
@* Apply typography styles via Style resources *@
<Label Text="Page Title" Style="{StaticResource StyleHeadingH1}" />
<Label Text="Body text" Style="{StaticResource StyleBodyDefault}" />
<Label Text="Caption" Style="{StaticResource StyleCaption}" />
```

---

## Animation/Duration Token Usage

### Web (CSS Transitions)

```scss
.button {
  transition: background-color var(--duration-normal) ease-in-out;
  
  &:hover {
    background-color: var(--color-accent-hover);
  }
}

.spinner {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
```

### Mobile (MAUI Animations)

```csharp
// Fade animation
await label.FadeTo(1, (uint)Application.Current.Resources["DurationNormal"]);

// Scale animation
await button.ScaleTo(1.05, (uint)Application.Current.Resources["DurationFast"]);

// Translate animation
await frame.TranslateTo(0, 10, (uint)Application.Current.Resources["DurationSlow"]);
```

---

## Best Practices

1. **Always use tokens:** Never hardcode design values. Use tokens for all colors, spacing, sizing, and timing.

2. **Semantic naming:** Use semantic token names (e.g., `ColorAccent`, not `ColorBlue`) so changes propagate globally.

3. **Theme support:** Ensure all components support both light and dark themes via `AppThemeBinding` (mobile) or `prefers-color-scheme` (web).

4. **Consistency:** Apply tokens through shared styles/components, not inline. This enables global updates.

5. **Accessibility:** Ensure sufficient contrast for status colors and text combinations (WCAG AA minimum 4.5:1).

6. **Documentation:** Document when new tokens are added and what they're used for.

---

## Future Token Expansion

As the platform grows, add tokens for:

- Additional color variants (subtle backgrounds, hover states)
- Responsive spacing overrides (different spacing for mobile vs. tablet)
- Custom durations for specific animations
- Additional typography scales or weights
- Accessible color combinations for data visualization

---

## Related Resources

- [DESIGN_TOKENS.md](DESIGN_TOKENS.md) — Token definitions and reference
- [COMPONENT_NAMING.md](COMPONENT_NAMING.md) — Component structure and patterns
- [TYPOGRAPHY.md](TYPOGRAPHY.md) — Typography system details
- [SPACING.md](SPACING.md) — Spacing patterns and usage
- [ICON_STRATEGY.md](ICON_STRATEGY.md) — Icon implementation
