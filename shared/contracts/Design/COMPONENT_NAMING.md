# Component Naming & Organization Strategy

## Overview

Consistent component naming and organization enables teams to discover, understand, and build on existing components. This document defines naming patterns, folder structures, and variant strategies across Blazor (web) and MAUI (mobile) clients.

---

## Component Naming Convention

All components follow **PascalCase** naming with clear, descriptive names.

### Pattern

```
{Noun}{Descriptor}
```

- **Noun:** The primary component (Button, Input, Card, Modal, Alert)
- **Descriptor (optional):** Refinement or specialized variant (Primary, Small, Outlined)

### Examples

```
Button              # Base button
ButtonPrimary       # Primary action button
ButtonSecondary     # Secondary action button
ButtonSmall         # Compact button variant
InputText           # Text input field
InputSelect         # Dropdown select
InputCheckbox       # Checkbox input
InputToggle         # Toggle switch
Card                # Base card container
CardHeader          # Card header section
CardBody            # Card content area
CardFooter          # Card footer section
Modal               # Modal dialog
ModalHeader         # Modal title area
Alert               # Alert/notification banner
AlertSuccess        # Success state alert
Badge               # Small label badge
Spinner             # Loading indicator
Avatar              # User profile image
Tabs                # Tab navigation
TabPanel            # Individual tab pane
```

### Naming Rules

1. **Clarity First:** Component names should be immediately understood without explanation.
2. **Consistency:** Similar components use the same naming pattern.
3. **No Abbreviations:** Avoid abbreviations; use full words (Button not Btn, Checkbox not Chk).
4. **Specificity:** More specific variants inherit the base name as prefix (`Button` → `ButtonPrimary`).
5. **State in Code:** Use properties/attributes for state, not the component name (use `Button Disabled="true"`, not `ButtonDisabled`).

---

## Web (Blazor) Component Organization

### Folder Structure

```
web-portal/
├── Components/
│   ├── Common/
│   │   ├── Button.razor
│   │   ├── Input.razor
│   │   ├── Card.razor
│   │   ├── Modal.razor
│   │   ├── Alert.razor
│   │   ├── Badge.razor
│   │   ├── Avatar.razor
│   │   └── Spinner.razor
│   ├── Navigation/
│   │   ├── Navbar.razor
│   │   ├── Sidebar.razor
│   │   ├── Tabs.razor
│   │   ├── Breadcrumb.razor
│   │   └── Pagination.razor
│   ├── Forms/
│   │   ├── FormGroup.razor
│   │   ├── InputText.razor
│   │   ├── InputSelect.razor
│   │   ├── InputCheckbox.razor
│   │   ├── InputToggle.razor
│   │   └── Textarea.razor
│   ├── Layouts/
│   │   ├── Container.razor
│   │   ├── Grid.razor
│   │   ├── Section.razor
│   │   └── Flex.razor
│   ├── Data/
│   │   ├── Table.razor
│   │   ├── DataGrid.razor
│   │   └── List.razor
│   └── Feedback/
│       ├── Toast.razor
│       ├── Tooltip.razor
│       └── Popover.razor
├── Styles/
│   ├── _variables.scss
│   ├── _components.scss
│   └── app.scss
└── wwwroot/
    ├── css/
    └── assets/
```

### Component File Structure

**Single-File Component:**
```razor
@* Button.razor *@
<button class="btn btn-@Variant btn-@Size" @onclick="OnClick">
    @ChildContent
</button>

@code {
    [Parameter]
    public string Variant { get; set; } = "primary"; // primary, secondary, outlined

    [Parameter]
    public string Size { get; set; } = "md"; // sm, md, lg

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
```

**Multi-Section Component (Card example):**
```razor
@* Card.razor *@
<div class="card">
    @ChildContent
</div>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}

@* CardHeader.razor *@
<div class="card-header">
    @ChildContent
</div>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}

@* CardBody.razor *@
<div class="card-body">
    @ChildContent
</div>
```

**Usage:**
```razor
<Card>
    <CardHeader>
        <h2>Card Title</h2>
    </CardHeader>
    <CardBody>
        Content goes here
    </CardBody>
</Card>
```

---

## Mobile (.NET MAUI) Component Organization

### Folder Structure

```
mobile-app/
└── customer-worker/
    ├── Components/
    │   ├── Common/
    │   │   ├── GtkButton.xaml / GtkButton.xaml.cs
    │   │   ├── GtkCard.xaml / GtkCard.xaml.cs
    │   │   ├── GtkAlert.xaml / GtkAlert.xaml.cs
    │   │   ├── GtkBadge.xaml / GtkBadge.xaml.cs
    │   │   └── GtkSpinner.xaml / GtkSpinner.xaml.cs
    │   ├── Forms/
    │   │   ├── GtkInput.xaml / GtkInput.xaml.cs
    │   │   ├── GtkSelect.xaml / GtkSelect.xaml.cs
    │   │   ├── GtkCheckbox.xaml / GtkCheckbox.xaml.cs
    │   │   └── GtkToggle.xaml / GtkToggle.xaml.cs
    │   └── Navigation/
    │       ├── GtkTabs.xaml / GtkTabs.xaml.cs
    │       └── GtkBottomSheet.xaml / GtkBottomSheet.xaml.cs
    └── Resources/
        └── Styles/
            └── ComponentStyles.xaml
```

### MAUI Component Pattern

**Namespace Convention:** `GTEK.FSM.MobileApp.CustomerWorker.Components.Common` (or Forms, Navigation, etc.)

**XAML Component File:**
```xaml
@* GtkButton.xaml *@
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="GTEK.FSM.MobileApp.CustomerWorker.Components.Common.GtkButton">
    <Button
        x:Name="button"
        Padding="16,12"
        CornerRadius="4"
        FontSize="16"
        FontAttributes="Bold"
        Text="{Binding Text, Source={x:Reference this}}" />
</ContentView>
```

**Code-Behind:**
```csharp
namespace GTEK.FSM.MobileApp.CustomerWorker.Components.Common;

public partial class GtkButton : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(GtkButton), "");

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(nameof(Variant), typeof(string), typeof(GtkButton), "primary");

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Variant
    {
        get => (string)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public GtkButton()
    {
        InitializeComponent();
    }
}
```

**Usage in XAML:**
```xaml
<components:GtkButton
    Text="Click Me"
    Variant="primary"
    Margin="16" />
```

---

## Component Variants System

### Variant Categories

All major components support these variant properties:

#### 1. **Variant** (Style)
- `primary` — Primary action (filled, prominent)
- `secondary` — Secondary action (outlined, less prominent)
- `ghost` — Minimal style (text-only or very subtle)
- `danger` — Destructive action (delete, cancel)
- `success` — Confirmation or positive state
- `warning` — Caution or alert state
- `info` — Informational state

#### 2. **Size**
- `sm` / `small` — Compact, 20–24px height
- `md` / `medium` — Default, 32–40px height
- `lg` / `large` — Prominent, 48px height

#### 3. **State**
Represented as boolean properties, not variant names:
- `Disabled` — Disabled/unavailable
- `Loading` — Processing/waiting
- `Active` — Currently selected or focused
- `Error` — Error state

#### 4. **Width**
- `Full` — 100% width
- `Fit` — Fit to content

### Component Examples with Variants

**Button Component:**
```razor
<Button Variant="primary" Size="md" Disabled="false">Click</Button>
<Button Variant="secondary" Size="lg">Cancel</Button>
<Button Variant="danger" Size="sm" Loading="true">Delete...</Button>
```

**Input Component:**
```razor
<Input Type="text" Placeholder="Enter text" />
<Input Type="email" Error="true" ErrorMessage="Invalid email" />
<Input Type="password" Disabled="true" />
```

**Card Component:**
```razor
<Card Variant="default" Clickable="true">
    <CardHeader>Title</CardHeader>
    <CardBody>Content</CardBody>
</Card>
```

---

## Cross-Platform Component Mapping

Map component names consistently between web and mobile clients:

| Purpose | Blazor | MAUI |
|---------|--------|------|
| Primary Button | Button (primary) | GtkButton (primary) |
| Form Input | InputText | GtkInput |
| Checkbox | InputCheckbox | GtkCheckbox |
| Card Container | Card | GtkCard |
| Alert/Banner | Alert | GtkAlert |
| Loading Indicator | Spinner | GtkSpinner |
| Tab Navigation | Tabs | GtkTabs |
| Modal Dialog | Modal | GtkBottomSheet / Modal |
| Badge Label | Badge | GtkBadge |

---

## Placeholder Component Inventory

### Common Components (Phase 0)

- **Button** — CTA, navigation, form submission
- **Input** — Text, email, password, search
- **Card** — Container for grouped content
- **Alert** — Error, warning, info, success messages
- **Badge** — Status labels, tags
- **Spinner** — Loading indicators
- **Modal** — Dialogs and overlays

### Navigation Components (Phase 0)

- **Navbar** — Top navigation bar (web)
- **Sidebar** — Side navigation (web)
- **Tabs** — Tab-based navigation
- **BottomSheet** — Bottom drawer (mobile)
- **Breadcrumb** — Navigation trail (web)

### Form Components (Phase 1)

- **InputSelect** — Dropdown/picker
- **InputCheckbox** — Multi-select checkbox
- **InputToggle** — On/off toggle
- **InputRadio** — Single-select radio
- **FormGroup** — Label + input wrapper
- **Textarea** — Multi-line text input

### Data Components (Phase 1)

- **Table** — Data table presentation
- **DataGrid** — Interactive data grid with sorting/filtering
- **List** — Simple item list

### Layout Components (Utility)

- **Container** — Content max-width wrapper
- **Grid** — CSS Grid layout helper
- **Flex** — Flexbox layout helper
- **Section** — Semantic section with spacing

---

## Component Documentation Pattern

Each component should include:

1. **Purpose:** What is this component used for?
2. **Variants:** Available variant/size/state options
3. **Props/Parameters:** Accepted inputs and callbacks
4. **Usage Examples:** Code samples for common use cases
5. **Accessibility:** ARIA attributes, keyboard behavior
6. **Mobile Considerations:** Touch targets, spacing for mobile

**Example Documentation Comment:**

```csharp
/// <summary>
/// GtkButton is a reusable button component supporting multiple variants and sizes.
/// 
/// Variants: primary (default), secondary, ghost, danger, success, warning, info
/// Sizes: sm (20px), md (32px), lg (48px)
/// 
/// Usage:
/// <code>
/// <GtkButton Text="Click Me" Variant="primary" Size="md" />
/// </code>
/// 
/// Accessibility:
/// - Button text is announced to screen readers
/// - Keyboard accessible (Tab, Enter/Space to activate)
/// - Disabled state disables interaction
/// 
/// Mobile: Touch target minimum 48x48 px
/// </summary>
```

---

## Best Practices

1. **Consistency:** Use the same naming pattern across all components.
2. **Single Responsibility:** Each component does one thing well.
3. **Composition:** Build complex UIs by composing simpler components.
4. **Props Over Variants:** Use boolean/enum properties for behavior, not multiple component names.
5. **Accessibility:** Every component includes semantic HTML and ARIA attributes where appropriate.
6. **Documentation:** Document purpose, variants, and common usage patterns clearly.
7. **Testing:** Each component should have unit tests covering main variants and edge cases.

---

## Future Expansion

1. **Component Library Package:** Extract and publish shared components as NuGet packages.
2. **Storybook/UIKit:** Create a visual component catalog for design and development alignment.
3. **Auto-Generated Docs:** Generate component documentation from code comments and props.
4. **Component Versioning:** Manage component versions and breaking changes across clients.
5. **Figma Integration:** Link Figma components to code components for design-dev alignment.

---

## Related Resources

- Design Tokens: [DESIGN_TOKENS.md](DESIGN_TOKENS.md)
- Icon Strategy: [ICON_STRATEGY.md](ICON_STRATEGY.md)
- Typography System: [TYPOGRAPHY.md](TYPOGRAPHY.md)
- Spacing System: [SPACING.md](SPACING.md)
