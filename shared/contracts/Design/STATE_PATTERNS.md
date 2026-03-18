# State Patterns & Visual Feedback Guidelines

## Overview

This document defines baseline patterns for common UI states (empty, loading, error) to ensure consistent user feedback and visual consistency across web (Blazor) and mobile (MAUI) clients. These patterns establish predictable, accessible user experiences when data is unavailable, requests are pending, or operations fail.

---

## Empty State Pattern

Empty states occur when a list, view, or section contains no data. They should communicate clearly why the area is empty and provide actionable next steps.

### Scenarios

1. **No Results (Filtered)** — User applied filters or search, resulting in zero matches
2. **First-Time UX (Onboarding)** — User hasn't yet created content
3. **Cleared/Deleted** — All items were deleted or data was cleared
4. **No Permissions** — User lacks permission to view data in this section
5. **Not Applicable** — Feature unavailable for current context (e.g., worker with no assigned jobs)

### Visual Treatment

Empty states consist of four elements:

```
┌─────────────────────────────────┐
│                                 │
│          [Icon]                 │
│      ↑ 32px-48px icon           │
│                                 │
│        Heading                  │
│    Clear, short title           │
│        (H3 or H4)               │
│                                 │
│      Description Text           │
│  Explains the situation,        │
│  max 2 lines, secondary color   │
│                                 │
│         [CTA Button]            │
│      (Optional, contextual)     │
│                                 │
└─────────────────────────────────┘
```

#### Element Specifications

##### Icon
- **Size:** 48px (xl) on web, 40dp on mobile
- **Color:** Use `ColorTextMuted` or `ColorAccent` depending on context
- **Semantics:** Choose icons matching the scenario:
  - No results: `action-search` or `status-info`
  - First-time: `common-star` or `action-add`
  - Deleted: `status-info` with neutral tone
  - No permissions: `status-warning` or similar

##### Heading
- **Typography:** H3 or H4
- **Color:** `ColorTextPrimary`
- **Length:** Short, one line maximum
- **Examples:**
  - "No requests yet"
  - "No results found"
  - "You don't have access"

##### Description
- **Typography:** Body or Body Small
- **Color:** `ColorTextMuted`
- **Length:** 1–2 lines, explain context and why empty
- **Tone:** Helpful, not apologetic
- **Examples:**
  - "Try adjusting your filters or search terms"
  - "Create your first request to get started"
  - "You'll see jobs here once assigned"

##### CTA Button (Optional)
- **When to include:** Only if there's a clear, actionable next step
- **Variant:** Primary or secondary depending on priority
- **Examples:**
  - "Create Request" (when empty due to onboarding)
  - "Clear Filters" or "Modify Search" (when empty due to filters)
  - "Browse Help" (when no permissions)
  - **Don't include:** When nothing can be done (e.g., awaiting assignment)

#### Spacing

```
Container Padding:    SpacingMd (16px) or SpacingLg (24px)
Icon to Heading:      SpacingMd (16px)
Heading to Description: SpacingSm (8px)
Description to Button: SpacingLg (24px)
Button Margin:        SpacingMd (16px) on both sides
```

### Implementation Examples

#### Blazor Component

```razor
@* EmptyState.razor *@
<div class="empty-state">
    <div class="empty-state__icon">
        <IconSprite Name="@Icon" Size="xl" />
    </div>
    
    <h3 class="empty-state__heading">@Heading</h3>
    
    <p class="empty-state__description">@Description</p>
    
    @if (!string.IsNullOrEmpty(ButtonLabel) && ButtonAction.HasDelegate)
    {
        <Button Variant="primary" @onclick="ButtonAction">
            @ButtonLabel
        </Button>
    }
</div>

@code {
    [Parameter]
    public string Icon { get; set; } = "status-info";
    
    [Parameter]
    public string Heading { get; set; } = "No results";
    
    [Parameter]
    public string Description { get; set; } = "";
    
    [Parameter]
    public string? ButtonLabel { get; set; }
    
    [Parameter]
    public EventCallback ButtonAction { get; set; }
}
```

**Usage:**
```razor
<EmptyState
    Icon="status-info"
    Heading="No requests yet"
    Description="Create your first request to get started"
    ButtonLabel="Create Request"
    ButtonAction="@OnCreateRequest" />
```

**Styling:**
```scss
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-lg);
  text-align: center;
  min-height: 300px;
  
  &__icon {
    margin-bottom: var(--spacing-md);
    color: var(--color-text-muted);
  }
  
  &__heading {
    @include typography-h3;
    margin-bottom: var(--spacing-sm);
    color: var(--color-text-primary);
  }
  
  &__description {
    @include typography-body;
    margin-bottom: var(--spacing-lg);
    color: var(--color-text-muted);
    max-width: 360px;
  }
}
```

#### MAUI Component

```xaml
@* EmptyStateView.xaml *@
<VerticalStackLayout
    Padding="{StaticResource SpacingLg}"
    Spacing="{StaticResource SpacingMd}"
    VerticalOptions="Center"
    HorizontalOptions="Center">
    
    <!-- Icon -->
    <Label
        x:Name="IconLabel"
        FontSize="48"
        HorizontalOptions="Center"
        TextColor="{AppThemeBinding Light=#999999, Dark=#666666}" />
    
    <!-- Heading -->
    <Label
        x:Name="HeadingLabel"
        FontSize="20"
        FontAttributes="Bold"
        HorizontalOptions="Center"
        TextColor="{AppThemeBinding Light=#000000, Dark=#FFFFFF}"
        Margin="0,8,0,0" />
    
    <!-- Description -->
    <Label
        x:Name="DescriptionLabel"
        FontSize="14"
        LineBreakMode="WordWrap"
        HorizontalOptions="Center"
        TextColor="{AppThemeBinding Light=#666666, Dark=#999999}"
        MaximumWidthRequest="300"
        Margin="0,0,0,24" />
    
    <!-- Button -->
    <Button
        x:Name="ActionButton"
        Padding="16,12"
        CornerRadius="4"
        IsVisible="False" />
</VerticalStackLayout>
```

**Code-Behind:**
```csharp
public partial class EmptyStateView : ContentView
{
    public EmptyStateView(string icon, string heading, string description, 
                         string? buttonLabel = null)
    {
        InitializeComponent();
        
        IconLabel.Text = icon;
        HeadingLabel.Text = heading;
        DescriptionLabel.Text = description;
        
        if (!string.IsNullOrEmpty(buttonLabel))
        {
            ActionButton.Text = buttonLabel;
            ActionButton.IsVisible = true;
        }
    }
}
```

**Usage:**
```xaml
<local:EmptyStateView
    Icon="📋"
    Heading="No requests yet"
    Description="Create your first request to get started"
    ButtonLabel="Create Request" />
```

---

## Loading State Pattern

Loading states indicate that a request is in progress. They reassure users that the app hasn't frozen and provide visual feedback about progress.

### Duration Classification

Loading indicators should vary based on expected wait time:

| Duration | Strategy | Indicator |
|----------|----------|-----------|
| **Quick** (<1s) | Suppress indicator (show immediately if >500ms) | None or subtle spinner |
| **Normal** (1–3s) | Show standard spinner | Animated spinner + optional text |
| **Long** (>3s) | Show progress bar or detailed status | Spinner + "Taking longer than expected..." message |

### Indicators

#### 1. **Spinner (Most Common)**

Best for: General loading without progress tracking

```
Visual: Rotating circle or dots
Duration: 200ms per rotation (smooth, not jarring)
Color: ColorAccent
Size: 24px–40px depending on context
```

**Blazor:**
```razor
<div class="spinner">
    <div class="spinner__ring"></div>
</div>

<style>
.spinner {
  display: inline-block;
  width: 40px;
  height: 40px;
}

.spinner__ring {
  display: inline-block;
  width: 40px;
  height: 40px;
  border: 4px solid var(--color-border-default);
  border-top-color: var(--color-accent);
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>
```

**MAUI:**
```xaml
<ActivityIndicator
    IsRunning="True"
    IsVisible="True"
    Color="{StaticResource ColorAccent}"
    Scale="1.5" />
```

#### 2. **Skeleton Screens**

Best for: List/table layouts where layout shift should be minimized

```
Visual: Placeholder shapes matching content layout
Duration: Subtle pulse or shimmer animation (300–500ms cycle)
```

**Blazor:**
```razor
<div class="skeleton-item">
    <div class="skeleton-avatar"></div>
    <div class="skeleton-text skeleton-text--title"></div>
    <div class="skeleton-text skeleton-text--body"></div>
</div>

<style>
.skeleton-item {
  display: flex;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
}

.skeleton-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  animation: shimmer 2s infinite;
}

.skeleton-text {
  height: 16px;
  border-radius: 4px;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  animation: shimmer 2s infinite;
  flex: 1;
}

.skeleton-text--title {
  margin-bottom: var(--spacing-sm);
  width: 60%;
}

.skeleton-text--body {
  width: 90%;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
</style>
```

#### 3. **Progress Bar**

Best for: Long-running operations with measurable progress (e.g., large file uploads)

```
Visual: Horizontal bar with percentage fill
Duration: Smooth animation (200ms per % increment)
```

---

### Loading State Variants

#### Full-Screen Loading
Display when loading initial page/section data.

```
┌─────────────────────────────┐
│                             │
│                             │
│         [Spinner]           │
│        "Loading..."         │
│                             │
│                             │
└─────────────────────────────┘
```

#### Overlay Loading
Display when performing action within existing content.

```
┌─────────────────────────────┐
│  Existing Content           │
│  [Semi-transparent overlay] │
│      [Spinner]              │
│     "Please wait..."        │
└─────────────────────────────┘
```

#### Inline Loading
Display within components during refresh.

```
List Item 1
List Item 2
[Item 3 with spinner] ← Loading update
List Item 4
```

### Implementation Examples

#### Blazor Loading Overlay

```razor
@* LoadingOverlay.razor *@
@if (IsVisible)
{
    <div class="loading-overlay" aria-busy="true" aria-label="Loading">
        <div class="loading-overlay__content">
            <div class="spinner">
                <div class="spinner__ring"></div>
            </div>
            <p class="loading-overlay__text">@Message</p>
        </div>
    </div>
}

@code {
    [Parameter]
    public bool IsVisible { get; set; }
    
    [Parameter]
    public string Message { get; set; } = "Loading...";
}
```

**Styling:**
```scss
.loading-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  
  &__content {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--spacing-md);
    background: var(--color-bg-surface);
    padding: var(--spacing-lg);
    border-radius: var(--radius-lg);
  }
  
  &__text {
    @include typography-body;
    color: var(--color-text-primary);
  }
}
```

#### MAUI Loading Indicator

```xaml
<Grid IsVisible="{Binding IsLoading}" Opacity="0.7">
    <Grid.GestureRecognizers>
        <TapGestureRecognizer NumberOfTapsRequired="1" />
    </Grid.GestureRecognizers>
    
    <VerticalStackLayout VerticalOptions="Center" HorizontalOptions="Center">
        <ActivityIndicator IsRunning="{Binding IsLoading}" />
        <Label Text="Loading..." HorizontalOptions="Center" />
    </VerticalStackLayout>
</Grid>
```

---

## Error State Pattern

Error states communicate that something went wrong and provide a path to recovery.

### Error Hierarchy

Errors should be categorized and handled consistently:

#### 1. **Validation Error** (User Fault)
- **Message:** Points to specific input or field
- **Action:** User corrects and retries
- **Example:** "Email address is invalid"

#### 2. **Network Error** (Connectivity Issue)
- **Message:** Clear statement of connectivity problem
- **Action:** "Retry" button, suggests checking connection
- **Example:** "No internet connection. Check your network and try again."

#### 3. **Server Error** (Backend Issue)
- **Message:** Generic message + error code (for support)
- **Action:** "Retry" or "Contact Support"
- **Example:** "Something went wrong. Error #500. Please try again or contact support."

#### 4. **Permission Error** (Authorization Issue)
- **Message:** Clear but don't expose implementation details
- **Action:** Suggest alternative or contacting admin
- **Example:** "You don't have permission to view this. Contact your administrator."

#### 5. **Not Found Error** (Resource Missing)
- **Message:** Clarify what's missing
- **Action:** Navigate back or search
- **Example:** "This request no longer exists or has been deleted."

### Visual Treatment

Error states consist of four elements:

```
┌─────────────────────────────────┐
│    [Error Icon]                 │
│   (Red/status-error color)      │
│                                 │
│   Error Heading                 │
│   "Something went wrong"         │
│                                 │
│   Error Message                 │
│   Clear, actionable explanation │
│   (max 2 lines)                 │
│                                 │
│   [Retry] [Help] [Back]         │
│   CTAs based on error type      │
│                                 │
└─────────────────────────────────┘
```

#### Element Specifications

##### Icon
- **Size:** 48px (xl) on web, 40dp on mobile
- **Color:** `ColorStatusError` (red, #EF4444)
- **Icon:** `status-error` or similar X/alert icon

##### Heading
- **Typography:** H3 or H4
- **Color:** `ColorTextPrimary`
- **Examples:**
  - "Something went wrong"
  - "Unable to load"
  - "Connection failed"

##### Message
- **Typography:** Body or Body Small
- **Color:** `ColorTextMuted`
- **Length:** 1–2 lines, actionable and specific
- **Tone:** Professional, helpful, no blame
- **Guidelines:**
  - ❌ "The database exploded"
  - ✅ "Unable to load requests. Please try again."
  - ❌ "You messed up"
  - ✅ "Please check the email field and try again."

##### Actions
- **Retry:** Always available for transient errors
- **Details:** Link to error code or logs (for technical users)
- **Back/Home:** Navigation escape routes
- **Contact Support:** For unrecoverable errors

#### Spacing
```
Container Padding:    SpacingMd (16px) or SpacingLg (24px)
Icon to Heading:      SpacingMd (16px)
Heading to Message:   SpacingSm (8px)
Message to Actions:   SpacingLg (24px)
Action Button Gap:    SpacingSm (8px)
```

### Implementation Examples

#### Blazor Component

```razor
@* ErrorState.razor *@
<div class="error-state">
    <div class="error-state__icon">
        <IconSprite Name="status-error" Size="xl" />
    </div>
    
    <h3 class="error-state__heading">@Heading</h3>
    
    <p class="error-state__message">@Message</p>
    
    <div class="error-state__actions">
        @if (OnRetry.HasDelegate)
        {
            <Button Variant="primary" @onclick="OnRetry">
                Retry
            </Button>
        }
        
        @if (!string.IsNullOrEmpty(ErrorCode))
        {
            <Button Variant="ghost" @onclick="ShowDetails">
                Details (Error: @ErrorCode)
            </Button>
        }
        
        @if (OnBack.HasDelegate)
        {
            <Button Variant="secondary" @onclick="OnBack">
                Go Back
            </Button>
        }
    </div>
</div>

@code {
    [Parameter]
    public string Heading { get; set; } = "Something went wrong";
    
    [Parameter]
    public string Message { get; set; } = "";
    
    [Parameter]
    public string? ErrorCode { get; set; }
    
    [Parameter]
    public EventCallback OnRetry { get; set; }
    
    [Parameter]
    public EventCallback OnBack { get; set; }
    
    private async Task ShowDetails()
    {
        // Open error details modal or log viewer
    }
}
```

**Styling:**
```scss
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-lg);
  text-align: center;
  min-height: 300px;
  
  &__icon {
    margin-bottom: var(--spacing-md);
    color: var(--color-status-error);
  }
  
  &__heading {
    @include typography-h3;
    margin-bottom: var(--spacing-sm);
    color: var(--color-text-primary);
  }
  
  &__message {
    @include typography-body;
    margin-bottom: var(--spacing-lg);
    color: var(--color-text-muted);
    max-width: 360px;
  }
  
  &__actions {
    display: flex;
    gap: var(--spacing-sm);
    flex-wrap: wrap;
    justify-content: center;
  }
}
```

#### MAUI Component

```xaml
<VerticalStackLayout
    Padding="{StaticResource SpacingLg}"
    Spacing="{StaticResource SpacingMd}"
    VerticalOptions="Center"
    HorizontalOptions="Center">
    
    <Label
        FontSize="48"
        HorizontalOptions="Center"
        TextColor="{StaticResource ColorStatusError}">❌</Label>
    
    <Label
        FontSize="20"
        FontAttributes="Bold"
        HorizontalOptions="Center"
        TextColor="{AppThemeBinding Light=#000000, Dark=#FFFFFF}">
        @((string)BindingContext)
    </Label>
    
    <Label
        FontSize="14"
        LineBreakMode="WordWrap"
        HorizontalOptions="Center"
        TextColor="{AppThemeBinding Light=#666666, Dark=#999999}"
        MaximumWidthRequest="300" />
    
    <HorizontalStackLayout Spacing="8" HorizontalOptions="Center">
        <Button Text="Retry" />
        <Button Text="Go Back" />
    </HorizontalStackLayout>
</VerticalStackLayout>
```

---

## State Transitions & Animations

### State Flow Diagrams

#### Typical Data Load Flow

```
Initial State
    ↓
[Trigger] User navigates to list
    ↓
Loading State (show spinner)
    ├─→ Success State (display data)
    │      ↓
    │   [User Action: Refresh]
    │      ↓
    │   Loading State (inline spinner)
    │      ↓
    │   Success State (updated data)
    │
    └─→ Error State (show error message)
           ↓
        [User Action: Retry]
           ↓
        Loading State
           ↓
        Success/Error State
```

#### Form Submission Flow

```
User Fills Form
    ↓
[Click Submit]
    ↓
Loading State (button disabled, spinner visible)
    ├─→ Success State (confirmation message)
    │      ↓
    │   [Auto-navigate or close]
    │
    └─→ Validation Error (highlight fields)
    └─→ Network Error (retry option)
    └─→ Server Error (retry or contact support)
```

### Transition Timing

Use duration tokens for consistency:

| Transition | Duration | Easing |
|-----------|----------|--------|
| Fade in spinner | `DurationFast` (100ms) | ease-out |
| Fade out loading | `DurationNormal` (200ms) | ease-in-out |
| Slide in error banner | `DurationNormal` (200ms) | ease-out |
| Pulse empty state icon | `DurationNormal` (200ms) per cycle | ease-in-out |

### Animation Principles

1. **Smooth Transitions:** Never jarring state changes; use fade/slide animations
2. **Meaningful Motion:** Motion reinforces meaning (e.g., error slides in, success fades in)
3. **Reduced Motion:** Respect `prefers-reduced-motion` preference
4. **Accessibility:** Don't rely solely on animation to communicate state; include text labels

**CSS Example:**
```scss
@media (prefers-reduced-motion: reduce) {
  * {
    animation: none !important;
    transition: none !important;
  }
}

.fade-in {
  animation: fadeIn var(--duration-normal) ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}
```

---

## Case Studies & Examples

### Case 1: List Fetch Flow

**Scenario:** User opens Requests page for first time

```
1. Initial Render
   - Show loading spinner centered in list area
   - "Fetching requests..."

2. API Call In Progress (1–2 seconds)
   - Spinner continues
   - No skeleton loading (prevents layout shift for fresh list)

3. Success
   - Spinner fades out
   - List items fade in with staggered animation
   - Table/list shows data

4. Empty Result
   - Spinner fades out
   - Empty state appears with icon, heading, description
   - Optional CTA: "Create Request"

5. Network Error
   - Spinner fades out
   - Error state appears with heading "Connection failed"
   - Message: "Unable to fetch requests. Check your connection and try again."
   - CTA: "Retry"
```

### Case 2: Form Submission

**Scenario:** User submits a new request form

```
1. User Clicks Submit
   - Button text: "Submitting..."
   - Button disabled (pointer-events: none)
   - Spinner inside button animates

2. Validation Fails (Immediate)
   - Button re-enables
   - Error banner slides in above form
   - Focus moves to invalid field
   - No loading state (validation is instant)

3. API Call Succeeds (2–3 seconds)
   - Loading spinner overlay appears (non-blocking submit)
   - Success toastappears: "Request created successfully"
   - Auto-navigate to detail page or close modal

4. API Call Fails (Network)
   - Loading overlay fades out
   - Error state in modal
   - Message: "Unable to create request. Check your connection."
   - CTA: "Retry" — re-submits with same data
   - CTA: "Cancel" — closes modal, preserves form data
```

### Case 3: Network Timeout Recovery

**Scenario:** User's network times out during data load

```
1. Initial Load
   - Spinner shows, message: "Loading..."

2. 3 Seconds Pass Without Response
   - Message updates: "Taking longer than expected..."
   - Spinner continues
   - Retry button appears (non-blocking)

3. User Clicks Retry
   - Spinner resets
   - Message: "Retrying..."
   - Counter hidden

4. Request Succeeds
   - Spinner fades out
   - Data loads
   - Success message (optional toast): "Reconnected"

5. Repeated Failures
   - After 3 retries, show error state
   - Message: "Unable to connect. Please check your network."
   - Larger retry button
   - "Contact Support" link
```

---

## Accessibility Considerations

### ARIA Attributes

- **Loading State:** `aria-busy="true"`, `aria-label="Loading requests..."`
- **Error State:** `role="alert"`, `aria-label="Error: Unable to connect"`
- **Empty State:** No special attributes (informational only)

### Screen Reader Announcements

Use live regions to announce state changes:

```html
<!-- Blazor -->
<div aria-live="polite" aria-atomic="true" role="status">
    @if (IsLoading)
    {
        <span>Loading requests, please wait...</span>
    }
    else if (IsError)
    {
        <span>Error: Unable to load requests. Please try again.</span>
    }
</div>
```

### Keyboard Navigation

- **Loading overlay:** Does not block keyboard escape (can close if appropriate)
- **Error actions:** Retry button is focusable and activated with Enter/Space
- **Empty state CTA:** Button receives focus naturally when page loads

### Color Not Alone

- **Don't:** Use only red to indicate an error
- **Do:** Use red + icon (X or alert) + text label

---

## Best Practices

1. **Minimize Perceived Wait Time:**
   - Show loading indicator after 500ms (don't flash for quick loads)
   - Display skeleton screens for predictable layouts
   - Show progress for long operations

2. **Clarity Over Cleverness:**
   - Use straightforward language in error messages
   - Avoid technical jargon
   - Provide actionable next steps

3. **Consistency:**
   - Empty, loading, and error states follow the same layout structure
   - Colors, icons, and spacing are consistent across all states
   - messaging tone is consistent (professional, helpful, never blaming)

4. **Context Matters:**
   - Network error in list: Show retry
   - Validation error in form: Highlight field + message
   - Permission error: Suggest reaching out to admin
   - Not found: Provide navigation options

5. **Test with Real Networks:**
   - Simulate slow connections (3G, 4G)
   - Test timeout scenarios
   - Verify error recovery workflows

---

## Future Enhancements

1. **Optimistic Updates:** Update UI immediately, rollback on failure
2. **Progressive Loading:** Show partial data while fetching remainder
3. **Offline Support:** Distinguish network errors from server errors
4. **Retry Logic:** Exponential backoff with jitter for failed retries
5. **Error Analytics:** Track error frequency and types for improvements
6. **Contextual Help:** Link errors to knowledge base or support

---

## Related Resources

- Design Tokens: [DESIGN_TOKENS.md](DESIGN_TOKENS.md)
- Component Naming: [COMPONENT_NAMING.md](COMPONENT_NAMING.md)
- Typography System: [TYPOGRAPHY.md](TYPOGRAPHY.md)
- Spacing System: [SPACING.md](SPACING.md)
- Icon Strategy: [ICON_STRATEGY.md](ICON_STRATEGY.md)
