# Icon Strategy & Guidelines

## Overview

Icons are a critical part of the unified visual language across web (Blazor) and mobile (MAUI) clients. This document establishes naming conventions, organization patterns, and consumption strategies to ensure consistency and maintainability.

---

## Icon Naming Convention

All icons follow a **kebab-case, verb-noun pattern** for clarity and scalability.

### Naming Pattern

```text
{category}-{noun}[-{modifier}]
```

- **Category:** Semantic category (nav, action, status, social, common)
- **Noun:** The primary object or concept
- **Modifier (optional):** Variation, state, or direction

### Examples

```text
nav-home              # Navigation: home
nav-menu              # Navigation: menu/hamburger
nav-back              # Navigation: back/previous
action-add            # Action: create/add
action-edit           # Action: edit/modify
action-delete         # Action: delete/remove
action-search         # Action: search/magnify
action-filter         # Action: filter
status-success        # Status: success/checkmark
status-warning        # Status: warning/alert
status-error          # Status: error/x
status-info           # Status: info/information
status-pending        # Status: pending/hourglass
common-bell           # Common: notification/bell
common-user           # Common: user/person
common-settings       # Common: settings/gear
common-close          # Common: close/x
common-check          # Common: checkbox/tick
```

### Icon Categories

- **nav-\*:** Navigation-specific icons (back, home, menu, profile, settings)
- **action-\*:** Action-triggering icons (add, edit, delete, send, download)
- **status-\*:** State/status indicators (success, error, warning, info, pending)
- **social-\*:** Social/external integration (twitter, facebook, github, linkedin)
- **common-\*:** General-purpose utilities (user, bell, settings, clock, calendar)

---

## Web (Blazor) Icon Organization

### SVG Asset Structure

```text
web-portal/
├── wwwroot/
│   └── assets/
│       └── icons/
│           ├── nav-*.svg
│           ├── action-*.svg
│           ├── status-*.svg
│           ├── social-*.svg
│           └── common-*.svg
```

### Blazor Component Usage

**Icon Component:**

````razor
@* IconSprite.razor *@
@if (!string.IsNullOrEmpty(Name))
{
    <svg class="icon icon-@Size icon-@(ThemePreference == "dark" ? "dark" : "light")">
        <use xlink:href="/assets/icons/sprite.svg#@Name"></use>
    </svg>
}

@code {
    [Parameter]
    public string Name { get; set; } = "";

    [Parameter]
    public string Size { get; set; } = "md"; // xs, sm, md, lg, xl

    [Parameter]
    public string ThemePreference { get; set; } = "light";

    [Parameter]
    public string AriaLabel { get; set; } = "";
}
```text

**Usage in Pages:**

```razor
<IconSprite Name="nav-home" Size="lg" AriaLabel="Go to home" />
<IconSprite Name="action-add" Size="md" />
<IconSprite Name="status-success" Size="sm" />
````

**CSS Styling:**

````css
.icon {
    display: inline-block;
    width: 1em;
    height: 1em;
    fill: currentColor;
}

.icon-xs { width: 16px; height: 16px; }
.icon-sm { width: 20px; height: 20px; }
.icon-md { width: 24px; height: 24px; }
.icon-lg { width: 32px; height: 32px; }
.icon-xl { width: 48px; height: 48px; }
```text

---

## Mobile (.NET MAUI) Icon Organization

### Font Icon or Embedded Asset Strategy

**Option A: Icon Font (Recommended)**

````

mobile-app/
├── customer-worker/
│ ├── Resources/
│ │ └── Fonts/
│ │ └── icons.ttf # Icon font compiled from SVGs
│ └── App.xaml

```xml
<OnPlatform x:TypeArguments="x:String">
<On Platform="iOS">icons</On>
<On Platform="Android">Fonts/icons.ttf#icons</On>
</OnPlatform>
```

### Option B: Embedded SVG Assets

```text

mobile-app/
├── customer-worker/
│ └── Resources/
│ └── Images/
│ ├── nav-home.svg
│ ├── action-add.svg
│ └── ... (all icon SVGs)

````text

### MAUI Component Usage

**Icon Label (Font-based):**

```xaml
<Label
    Text="&#xE001;"
    FontFamily="icons"
    FontSize="24"
    TextColor="{AppThemeBinding Light=#{ColorTextPrimary},
                                Dark=#{ColorTextPrimaryDark}}" />
````

**ImageButton (SVG-based):**

````xaml
<ImageButton
    Source="Resources/Images/nav-home.svg"
    HeightRequest="48"
    WidthRequest="48"
    BackgroundColor="Transparent"
    Command="{Binding NavigateHomeCommand}" />
```text

**Icon Helper Class (C# abstraction):**

```csharp
public static class IconHelper
{
    // Icon font codepoints mapping
    public static readonly Dictionary<string, string> Icons = new()
    {
        { "nav-home", "\uE001" },
        { "nav-menu", "\uE002" },
        { "action-add", "\uE101" },
        { "action-edit", "\uE102" },
        { "status-success", "\uE201" },
        { "status-error", "\uE202" },
    };

    public static string GetIconCodepoint(string iconName) =>
        Icons.ContainsKey(iconName) ? Icons[iconName] : "";
}
````

---

## Icon Sizing Scale

Consistent sizing across platforms:

| Token | Size px | Usage                            |
| ----- | ------- | -------------------------------- |
| xs    | 16      | Small indicators, badges         |
| sm    | 20      | Secondary actions, captions      |
| md    | 24      | Standard buttons, list items     |
| lg    | 32      | Primary actions, section headers |
| xl    | 48      | Hero elements, empty states      |

---

## Dark Theme Support

Icons should automatically adapt to light/dark theme via:

- **Web:** CSS `currentColor` + theme CSS variable updates
- **Mobile:** `AppThemeBinding` with light/dark variants

**SVG Coloring (Web):**

````xml
<!-- Use currentColor to inherit parent color -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
    <path fill="currentColor" d="..."/>
</svg>
```text

Alternatively, use theme-aware colors in CSS:

```css
.icon {
    fill: var(--color-text-primary);
}

@media (prefers-color-scheme: dark) {
    .icon {
        fill: var(--color-text-primary-dark);
    }
}
````

---

## Icon Categories & Placeholder Inventory

### Navigation Icons (nav-\*)

- nav-home (house icon)
- nav-menu (hamburger/three-line icon)
- nav-back (left arrow/chevron)
- nav-forward (right arrow/chevron)
- nav-profile (person silhouette)
- nav-settings (gear icon)
- nav-requests (briefcase/document icon)
- nav-jobs (checkmark/task icon)

### Action Icons (action-\*)

- action-add (plus icon)
- action-edit (pencil icon)
- action-delete (trash/bin icon)
- action-search (magnifying glass icon)
- action-filter (funnel icon)
- action-download (downward arrow icon)
- action-upload (upward arrow icon)
- action-send (paper plane icon)
- action-more (three dots icon)
- action-share (share/export icon)

### Status Icons (status-\*)

- status-success (checkmark icon)
- status-error (x/cross icon)
- status-warning (exclamation/alert icon)
- status-info (i/information icon)
- status-pending (hourglass/clock icon)
- status-loading (spinner icon)

### Social Icons (social-\*)

- social-twitter
- social-facebook
- social-github
- social-linkedin

### Common Icons (common-\*)

- common-user (person icon)
- common-bell (notification icon)
- common-clock (time icon)
- common-calendar (date icon)
- common-check (checkbox icon)
- common-close (close/x icon)
- common-phone (telephone icon)
- common-email (envelope icon)

---

## Best Practices

1. **Naming Consistency:** Always use the established kebab-case convention. Never use abbreviations that break the pattern.

2. **Accessibility:**
   - Include `AriaLabel` or `ContentDescription` for meaningful icons
   - Do not use icons as the only means of communication
   - Pair icons with text labels when clarity is needed

3. **Single Source of Truth:**
   - Store SVG sources in a centralized location (design tool or GitHub)
   - Generate web sprite and mobile font from the same source
   - Version control all icon assets

4. **Performance:**
   - Use SVG sprite sheets on web to reduce HTTP requests
   - Consider icon font for mobile to reduce app bundle size vs. individual images
   - Compress SVG assets

5. **Theming Support:**
   - Icons should respect light/dark theme preferences automatically
   - Test icons in both themes early and often

6. **Usage Context:**
   - Navigation icons appear primarily in nav bars and sidebars
   - Action icons appear on buttons, inputs, and interactive elements
   - Status icons appear in alerts, badges, and feedback messages
   - Do not repurpose icons for mismatched contexts

---

## Future Expansion

As the design system matures:

1. **Icon Library Tool:** Integrate with design tools (Figma) for automated sprite/font generation
2. **Icon Variants:** Add filled vs. outlined variants (e.g., `nav-home`, `nav-home-filled`)
3. **Animation Support:** Define hover/active animation states for icons
4. **Internationalization:** Consider RTL-aware icon orientations for future language support
5. **Performance Analytics:** Track most-used icons to optimize asset delivery

---

## Related Resources

- Design Tokens: [DESIGN_TOKENS.md](DESIGN_TOKENS.md)
- Complete Icon Inventory: (Link to design tool or inventory spreadsheet)
- Accessibility Guidelines: (Link to a11y documentation)
