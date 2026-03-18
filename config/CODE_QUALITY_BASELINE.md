# Code Quality and Formatting Baseline

This document describes the code formatting, linting, and quality standards established for the GTEK FSM platform.

## Overview

The project enforces consistent code style, formatting, and quality rules across all .NET projects and frontend code. All rules are automatically applied via:

1. **EditorConfig** (`.editorconfig`) — IDE formatting, spacing, indentation
2. **StyleCop Analyzers** — C# naming conventions, documentation, spacing rules
3. **Microsoft.CodeAnalysis.NetAnalyzers** — General code quality and security
4. **Configuration** (`.stylecop.json`) — Customized rule settings

All developers must ensure code passes these checks before committing. Violations are treated as warnings by default, escalated to errors for critical issues.

---

## Edition Tools Integration

### Visual Studio Code (Recommended)

Install extensions:
- **C# Dev Kit** (ms-dotnettools.csharp)
- **EditorConfig for Visual Studio Code** (editorconfig.editorconfig)

VS Code will automatically apply formatting on save per `.editorconfig` settings.

### Visual Studio 2022

EditorConfig is built-in; enable **Format document on save** in Tools > Options > Text Editor > C# > Code Style > Formatting.

### Rider (JetBrains)

EditorConfig is natively supported; configure in Settings > Code Style > Enable EditorConfig support.

---

## C# Formatting Rules

### Indentation & Spacing

- **Indentation**: 4 spaces (no tabs in C# files)
- **Braces**: Always on new line (Allman style)
- **Line length**: Soft limit 120 characters (not enforced, guideline for readability)

**Valid:**
```csharp
if (condition)
{
    DoSomething();
}
```

**Invalid:**
```csharp
if (condition) {
    DoSomething();
}
```

### Naming Conventions

| Identifier Type | Convention | Example |
|---|---|---|
| Public classes | PascalCase | `UserService`, `OrderRequest` |
| Public interfaces | PascalCase (prefix `I`) | `IUserService`, `IOrderValidator` |
| Public properties | PascalCase | `FirstName`, `OrderId` |
| Public methods | PascalCase | `GetUser()`, `ValidateOrder()` |
| Public events | PascalCase | `OnUserCreated` |
| Private instance fields | camelCase with `_` prefix | `_userId`, `_userName` |
| Private static fields | UPPER_SNAKE_CASE | `CACHE_TIMEOUT`, `DEFAULT_RETRY_COUNT` |
| Local variables | camelCase | `userName`, `isValid` |
| Local constants | PascalCase | `MaxRetries`, `DefaultTimeout` |
| Parameters | camelCase | `userId`, `orderRequest` |
| Async methods | PascalCase with `Async` suffix | `GetUserAsync()`, `ValidateOrderAsync()` |

**Valid:**
```csharp
private readonly string _userId;
private const int MaxRetryCount = 3;

public string FirstName { get; set; } = string.Empty;
public async Task ProcessOrderAsync(int orderId) { }

public event EventHandler? OnComplete;
```

**Invalid:**
```csharp
private readonly string userId;  // Missing underscore
private const int max_retry_count = 3;  // Wrong case
public string firstname { get; set; }  // Wrong case
```

### Using Statements

- `using` directives placed **outside** namespace (top of file)
- Ordered: System → System.* → Third-party → Project-specific
- Unused usings removed on build

**Valid:**
```csharp
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using GTEK.FSM.Backend.Domain;

namespace GTEK.FSM.Backend.Application;
```

### Properties & Auto-Properties

Prefer auto-properties with expression bodies when possible:

**Valid:**
```csharp
public string Name { get; set; } = string.Empty;
public string DisplayName => $"{FirstName} {LastName}";
public int Count => Items.Count;
```

**Avoid:**
```csharp
private string _name;
public string Name
{
    get { return _name; }
    set { _name = value; }
}
```

### Null Handling

Use null-coalescing and null-propagation operators:

**Valid:**
```csharp
var name = user?.FirstName ?? "Unknown";
if (order?.Items?.Count > 0) { }
```

**Avoid:**
```csharp
var name = user != null ? user.FirstName : "Unknown";
if (order != null && order.Items != null && order.Items.Count > 0) { }
```

### Pattern Matching

Use modern pattern matching where applicable:

**Valid:**
```csharp
if (obj is User { IsActive: true } user)
{
    ProcessUser(user);
}

return result switch
{
    Success => "OK",
    Failure => "Error",
    _ => "Unknown"
};
```

### Type Usage

Use built-in type keywords instead of BCL types:

**Valid:**
```csharp
public string Name { get; set; }
public int Count { get; set; }
public bool IsActive { get; set; }
```

**Avoid:**
```csharp
public String Name { get; set; }
public Int32 Count { get; set; }
public Boolean IsActive { get; set; }
```

### Accessibility Modifiers

Always include explicit accessibility modifiers:

**Valid:**
```csharp
public class UserService
{
    private readonly IUserRepository _repository;
    
    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<User> GetUserAsync(int userId) { }
    private async Task LogAsync(string message) { }
}
```

**Invalid:**
```csharp
public class UserService
{
    readonly IUserRepository _repository;  // Missing internal/private
}
```

---

## StyleCop Analyzer Rules

### Key Rules (Enabled)

| Rule ID | Description | Severity |
|---------|---|---|
| SA1000 | Keywords must be spaced correctly | Warning |
| SA1001 | Commas must be spaced correctly | Warning |
| SA1008 | Opening parenthesis must be spaced correctly | Warning |
| SA1009 | Closing parenthesis must not be preceded by space | Warning |
| SA1028 | Code line should not end with whitespace | Warning |
| SA1100 | Do not prefix calls to base class members with `base.` | Suggestion |
| SA1101 | Prefix local calls with `this.` (disabled in favor of modern style) | Disabled |
| SA1200 | Using directives must be placed correctly | Warning |
| SA1201 | Elements must appear in the correct order | Warning |
| SA1202 | Elements must be ordered by access level | Warning |
| SA1309 | Field names must not begin with underscore (disabled - we use `_` prefix) | Disabled |
| SA1310 | Field names must not contain underscore (disabled - checked differently) | Disabled |
| SA1402 | File may only contain a single namespace | Warning |
| SA1600 | Elements must be documented (disabled - internal elements not required) | Disabled |
| SA1609 | Property documentation text should begin with standard text (disabled) | Disabled |
| SA1633 | File header copyright text missing (disabled - organization decision) | Disabled |

### Common Suppressions

Use `#pragma` to suppress specific rules when justified. Always include explanatory comments:

**Valid:**
```csharp
#pragma warning disable SA1600 // Elements must be documented
/// <summary>
/// This method is intentionally undocumented per design.
/// </summary>
private void InternalHelper() { }
#pragma warning restore SA1600
```

---

## Microsoft.CodeAnalysis.NetAnalyzers Rules

Core security and code quality analyzers from Microsoft:

| Rule ID | Description | Severity |
|---------|---|---|
| CA1001 | Types that own disposable fields should be disposable | Warning |
| CA1018 | Mark attributes with AttributeUsageAttribute | Warning |
| CA1050 | Declare types in namespaces | Warning |
| CA1502 | Avoid excessive complexity | Warning |
| CA1505 | Avoid unmaintainable code | Warning |
| CA1507 | Use nameof to express symbol names | Warning |
| CA1707 | Identifiers should not contain underscores (exceptions allowed for private fields) | Suggestion |
| CA1711 | Identifiers should not have incorrect suffix | Warning |
| CA1716 | Identifiers should not match keywords | Warning |
| CA1720 | Identifier contains type name | Suggestion |
| CA1815 | Override equals and operator equals on value types | Warning |
| CA1819 | Properties should not return arrays | Warning |
| CA1822 | Mark members as static | Suggestion |
| CA1827 | Do not use Count()/LongCount() when Any() can be used | Warning |
| CA1829 | Use Enumerable.OfType instead of casts | Warning |
| CA1836 | Prefer IsEmpty over Count when available | Suggestion |
| CA1845 | Use span-based string.Concat | Suggestion |
| CA1861 | Avoid constants as inline arguments | Suggestion |
| CA2000 | Dispose objects before losing scope | Warning |
| CA2213 | Disposable fields should be disposed | Warning |

---

## ASP.NET and Blazor Specific Rules

### Razor Component Formatting

- **File name**: PascalCase + `.razor` suffix (e.g., `UserProfile.razor`)
- **Component parameters**: PascalCase
- **Event handlers**: `On[EventName]` (e.g., `OnClick`, `OnSubmit`)
- **Cascading parameters**: Explicit `[CascadingParameter]` attributes

**Valid:**
```razor
@page "/users/{UserId:int}"

<div @onclick="HandleClick">
    @if (User?.IsActive)
    {
        <p>@User.DisplayName</p>
    }
</div>

@code {
    [Parameter]
    public int UserId { get; set; }

    [CascadingParameter]
    public required User? User { get; set; }

    private async Task HandleClick() { }
}
```

### Configuration Convention

- Startup configuration classes: `Program.cs`
- Extension methods for DI: `DependencyInjectionExtensions.cs`
- Middleware configuration: Separate `MiddlewareExtensions.cs` if complex

---

## Configuration Files Formatting

### JSON Files
- Indentation: 2 spaces
- Trailing commas: Not allowed

### XML Project Files (.csproj, .props)
- Indentation: 2 spaces
- Attributes before elements

### Shell Scripts (.sh)
- Indentation: 2 spaces
- Line endings: LF (Unix)

---

## Enforcement & Integration

### Pre-Commit Checks (Future Phase 0.8.5)

Code is validated via:
1. **IDE warnings** — Visual Studio / VS Code displays violations
2. **Build warnings** — `dotnet build` shows all analyzer violations
3. **Azure Pipelines** (Phase 0.8.3) — CI/CD enforces clean builds

### Build Verification

```bash
# Run build with analyzer checks
dotnet build -c Debug

# Run specific analyzer checks
dotnet format verify

# Auto-fix formatting violations (Phase 0.8.5)
dotnet format --verify-no-changes
```

### Treat Warnings as Errors (Optional Enhancement)

Add to `.csproj` to escalate warnings to errors:

```xml
<PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

---

## Frontend Formatting (JavaScript/TypeScript in Blazor)

If future phases introduce JavaScript/TypeScript code:

- **Prettier** for auto-formatting
- **ESLint** for code quality
- Configuration: `.prettierrc` and `.eslintrc` to be added

Current focus: Blazor components (C#) only.

---

## Common Issues and Fixes

### Issue: "SA1309: Field names must not begin with underscore"

**Solution:** This rule is disabled in `.stylecop.json`. Our convention requires underscore prefix on private fields.

### Issue: "CA1822: Member does not use instance data"

**Solution:** Mark method as `static` if it doesn't use instance members:

```csharp
// Before
public string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");

// After
public static string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");
```

### Issue: "SA1600: Elements must be documented"

**Solution:** Rule is disabled by default. Add documentation if code clarity requires it:

```csharp
/// <summary>
/// Processes the user order and generates an invoice.
/// </summary>
/// <param name="orderId">The order ID to process.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public async Task ProcessOrderAsync(int orderId) { }
```

### Issue: Unused using statements

**Solution:** Remove or use `dotnet format` (add in Phase 0.8.5).

---

## Future Enhancements (Phase 0.8+)

- [ ] Add `dotnet format` integration to build pipeline
- [ ] Add pre-commit hooks to validate formatting locally
- [ ] Add ESLint/Prettier for JavaScript/TypeScript (if frontend expands)
- [ ] Configure rule suppressions per project (if needed)
- [ ] Add team-specific code review guidelines based on analyzer feedback

---

## References

- [EditorConfig Documentation](https://editorconfig.org/)
- [StyleCop Analyzers on GitHub](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [Microsoft.CodeAnalysis.NetAnalyzers](https://github.com/microsoft/NetAnalyzers)
- [C# Coding Guidelines](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)

