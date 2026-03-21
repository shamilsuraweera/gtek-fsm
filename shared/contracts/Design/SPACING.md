# Spacing System & Guidelines

## Overview

Consistent spacing creates visual rhythm, improves readability, and establishes relationships between elements. This document defines the spacing scale, application patterns, and implementation across web (Blazor) and mobile (MAUI) clients.

---

## Spacing Scale

A linear spacing scale based on multiples of 4px provides flexibility and consistency.

| Token | Value | Usage                                    |
| ----- | ----- | ---------------------------------------- |
| xs    | 4px   | Tight spacing between related elements   |
| sm    | 8px   | Small gaps, icon spacing, tight grouping |
| md    | 16px  | Default spacing between components       |
| lg    | 24px  | Section separation, medium grouping      |
| xl    | 32px  | Major section breaks, prominent spacing  |
| 2xl   | 48px  | Full section separation, hero spacing    |
| 3xl   | 64px  | Very large sections, rare usage          |

---

## CSS/SCSS Implementation (Web)

### CSS Custom Properties

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

### SCSS Mixins & Helper Classes

```scss
/* Margin utilities */
.m-xs {
  margin: var(--spacing-xs);
}
.m-sm {
  margin: var(--spacing-sm);
}
.m-md {
  margin: var(--spacing-md);
}
.m-lg {
  margin: var(--spacing-lg);
}
.m-xl {
  margin: var(--spacing-xl);
}
.m-2xl {
  margin: var(--spacing-2xl);
}

/* Margin sizing */
.mt-xs {
  margin-top: var(--spacing-xs);
}
.mt-sm {
  margin-top: var(--spacing-sm);
}
.mt-md {
  margin-top: var(--spacing-md);
}
/* ... and so on for mb, ml, mr, mx, my */

/* Padding utilities */
.p-xs {
  padding: var(--spacing-xs);
}
.p-sm {
  padding: var(--spacing-sm);
}
.p-md {
  padding: var(--spacing-md);
}
.p-lg {
  padding: var(--spacing-lg);
}
.p-xl {
  padding: var(--spacing-xl);
}
.p-2xl {
  padding: var(--spacing-2xl);
}

/* Padding sizing */
.pt-xs {
  padding-top: var(--spacing-xs);
}
.pt-sm {
  padding-top: var(--spacing-sm);
}
/* ... and so on for pb, pl, pr, px, py */

/* Gap utilities (flexbox) */
.gap-xs {
  gap: var(--spacing-xs);
}
.gap-sm {
  gap: var(--spacing-sm);
}
.gap-md {
  gap: var(--spacing-md);
}
.gap-lg {
  gap: var(--spacing-lg);
}
.gap-xl {
  gap: var(--spacing-xl);
}

/* Mixin for common spacing scenarios */
@mixin container-padding {
  padding: var(--spacing-md);
  @media (min-width: 768px) {
    padding: var(--spacing-lg);
  }
}

@mixin card-spacing {
  padding: var(--spacing-lg);
  margin-bottom: var(--spacing-md);
}

@mixin section-spacing {
  margin-top: var(--spacing-2xl);
  margin-bottom: var(--spacing-2xl);
}
````

### Blazor Component Spacing

````razor
@* Container component with automatic spacing *@
<div class="container p-lg">
    <div class="section gap-md">
        @ChildContent
    </div>
</div>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
```text

**Usage:**

```razor
<Container>
    <h1 class="mb-md">Page Title</h1>
    <p class="mb-sm">Subtitle text</p>
    <div class="gap-lg">
        <!-- Items -->
    </div>
</Container>
````

---

## MAUI Implementation (Mobile)

### XAML Spacing in App.xaml

````xaml
<ResourceDictionary>
    <!-- Spacing values -->
    <x:Double x:Key="SpacingXs">4</x:Double>
    <x:Double x:Key="SpacingSm">8</x:Double>
    <x:Double x:Key="SpacingMd">16</x:Double>
    <x:Double x:Key="SpacingLg">24</x:Double>
    <x:Double x:Key="SpacingXl">32</x:Double>
    <x:Double x:Key="Spacing2xl">48</x:Double>
    <x:Double x:Key="Spacing3xl">64</x:Double>

    <!-- VerticalStackLayout spacing presets -->
    <Style x:Key="StackLayoutTight" TargetType="VerticalStackLayout">
        <Setter Property="Spacing" Value="{StaticResource SpacingSm}" />
        <Setter Property="Padding" Value="{StaticResource SpacingMd}" />
    </Style>

    <Style x:Key="StackLayoutDefault" TargetType="VerticalStackLayout">
        <Setter Property="Spacing" Value="{StaticResource SpacingMd}" />
        <Setter Property="Padding" Value="{StaticResource SpacingMd}" />
    </Style>

    <Style x:Key="StackLayoutRelaxed" TargetType="VerticalStackLayout">
        <Setter Property="Spacing" Value="{StaticResource SpacingLg}" />
        <Setter Property="Padding" Value="{StaticResource SpacingLg}" />
    </Style>

    <!-- Section spacing -->
    <Style x:Key="SectionSeparator" TargetType="VerticalStackLayout">
        <Setter Property="Spacing" Value="{StaticResource Spacing2xl}" />
    </Style>
</ResourceDictionary>
```text

### XAML Usage

```xaml
<VerticalStackLayout Style="{StaticResource StackLayoutDefault}">
    <Label Text="Section Title" FontSize="20" FontAttributes="Bold" />

    <VerticalStackLayout Spacing="{StaticResource SpacingMd}">
        <Label Text="Item 1" />
        <Label Text="Item 2" />
        <Label Text="Item 3" />
    </VerticalStackLayout>

    <VerticalStackLayout
        Margin="{StaticResource SpacingLg}"
        Padding="{StaticResource SpacingMd}"
        BackgroundColor="{AppThemeBinding Light=#F5F5F5, Dark=#2C2C2C}">
        <Label Text="Card content" />
    </VerticalStackLayout>
</VerticalStackLayout>
````

---

## Common Spacing Patterns

### Page/Container Padding

```text
Desktop: 32px (lg)
Tablet: 24px (lg)
Mobile: 16px (md)
```

**Web CSS:**

````css
.page {
  padding: var(--spacing-md);
}

@media (min-width: 768px) {
  .page {
    padding: var(--spacing-lg);
  }
}
```text

**Mobile XAML:**

```xaml
<VerticalStackLayout Padding="16,0">
    <!-- Page content -->
</VerticalStackLayout>
````

### List Item Spacing

```text
Item padding: 16px (md)
Item margin-bottom: 8px (sm)
```

**Web CSS:**

````css
.list-item {
  padding: var(--spacing-md);
  margin-bottom: var(--spacing-sm);
}
```text

**Mobile XAML:**

```xaml
<VerticalStackLayout Spacing="{StaticResource SpacingSm}">
    <Frame Padding="{StaticResource SpacingMd}">
        <Label Text="Item 1" />
    </Frame>
    <Frame Padding="{StaticResource SpacingMd}">
        <Label Text="Item 2" />
    </Frame>
</VerticalStackLayout>
````

### Card Spacing

```text
Card padding: 24px (lg) outside, 16px (md) inside
Gap between cards: 16px (md)
```

**Web CSS:**

````scss
.card {
  @include card-spacing;

  &__content {
    display: flex;
    flex-direction: column;
    gap: var(--spacing-md);
  }
}

.card-container {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}
```text

**Mobile XAML:**

```xaml
<VerticalStackLayout Spacing="{StaticResource SpacingMd}">
    <Frame Padding="{StaticResource SpacingLg}" CornerRadius="8">
        <VerticalStackLayout Spacing="{StaticResource SpacingMd}">
            <Label Text="Card Title" FontAttributes="Bold" />
            <Label Text="Card content" />
        </VerticalStackLayout>
    </Frame>
</VerticalStackLayout>
````

### Section Separation

```text
Between major sections: 48px (2xl)
Between subsections: 32px (xl)
Between content groups: 24px (lg)
```

**Web HTML:**

````html
<section>
  <h1>Section 1</h1>
  <!-- content -->
</section>

<section class="mt-2xl">
  <h1>Section 2</h1>
  <!-- content -->
</section>
```text **Mobile XAML:** ```xaml
<VerticalStackLayout Spacing="{StaticResource Spacing2xl}">
  <VerticalStackLayout>
    <label Text="Section 1" FontAttributes="Bold" />
    <!-- content -->
  </VerticalStackLayout>

  <VerticalStackLayout>
    <label Text="Section 2" FontAttributes="Bold" />
    <!-- content -->
  </VerticalStackLayout>
</VerticalStackLayout>
````

### Form Field Spacing

```text
Label to input: 4px (xs)
Input to input: 16px (md)
Input to button: 24px (lg)
```

**Web CSS:**

````css
.form-group {
  margin-bottom: var(--spacing-md);
}

.form-group label {
  display: block;
  margin-bottom: var(--spacing-xs);
}

.form-group input {
  width: 100%;
}

.form-actions {
  margin-top: var(--spacing-lg);
}
```text

**Mobile XAML:**

```xaml
<VerticalStackLayout Spacing="{StaticResource SpacingMd}">
    <Label Text="Email" FontAttributes="Bold" />
    <Entry Placeholder="Enter email" Margin="0,0,0,8" />

    <Label Text="Password" FontAttributes="Bold" Margin="0,8,0,0" />
    <Entry Placeholder="Enter password" IsPassword="True" />

    <Button Text="Sign In" Margin="0,24,0,0" />
</VerticalStackLayout>
````

---

## Button & Icon Spacing

### Internal Button Spacing

```text
Icon + text gap: 8px (sm)
Button padding: 12px (vertical), 16px (horizontal)
```

**Web CSS:**

````css
.button {
  padding: 12px var(--spacing-md);
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}
```text

### Icon Spacing Guidelines

````

Icon + label gap: 8px (sm)
Standalone icon + text gap: 16px (md)

```text

---

## Best Practices

1. **Use the Scale:** Always select spacing values from the defined scale. Never use arbitrary values.

2. **Consistency:** Apply spacing through utility classes and pre-built styles, not inline. This ensures consistency and enables global updates.

3. **Hierarchy:** Use larger spacing to separate major sections, smaller spacing to group related items.

4. **Responsive:** Adjust spacing for different screen sizes (typically reduce on mobile).

5. **White Space:** Don't fear white space. It improves readability and visual clarity.

6. **Grouping:** Use spacing to establish visual relationships:
   - Small spacing (xs, sm) = tightly related items
   - Medium spacing (md) = related but distinct items
   - Large spacing (lg, xl, 2xl) = independent sections

7. **Alignment:** All spacing increments should align to the 4px grid for perfect pixel alignment on modern displays.

---

## Future Expansion

1. **Responsive Spacing Tokens:** Automatically scale spacing based on viewport width.
2. **Layout Components:** Pre-built container/section components with automatic spacing.
3. **Spacing Inspector Tool:** Visual tool to inspect and validate spacing in components.
4. **Animation Spacing:** Define spacing transitions for state changes and animations.

---

## Related Resources

- Design Tokens: [DESIGN_TOKENS.md](DESIGN_TOKENS.md)
- Typography System: [TYPOGRAPHY.md](TYPOGRAPHY.md)
- Component Naming: [COMPONENT_NAMING.md](COMPONENT_NAMING.md)
```
