# StyleCop Analyzer Suppressions Guide

This document provides common suppressions and patterns used in the GTEK FSM codebase.

## Why Suppress Rules?

StyleCop rules are **guidelines, not absolutes**. Suppressions are appropriate when:

- A rule conflicts with organizational or project conventions
- Exception cases justify a deviation from the standard
- Code clarity or performance requires breaking a rule
- A rule is deliberately disabled (noted in `.stylecop.json`)

All suppressions should include explanatory comments.

---

## File-Level Suppressions

Suppress a rule for an entire file using `#pragma warning disable` at the top:

````csharp
// Suppress SA1600 (documentation) for this file - generated code
#pragma warning disable SA1600

namespace GTEK.FSM.Backend.Domain.Generated;

// Content here...
```text

---

## Method-Level Suppressions

Suppress a rule for a specific method or class:

```csharp
#pragma warning disable SA1600 // Elements must be documented
/// <summary>
/// Internal helper used only by the parent class.
/// </summary>
private void ProcessInternal()
{
}
#pragma warning restore SA1600
````

---

## Common Suppressions

### SA1600: Elements must be documented

**When to use:**

- Internal or private helper methods
- Auto-generated code
- Properties with obvious intent

````csharp
#pragma warning disable SA1600
private List<Order> _orders = [];
#pragma warning restore SA1600
```text

### SA1651: Do not use @

**When to use:**

- Not applicable to modern C# namespace syntax (`namespace X.Y.Z;`)

### CA1707: Identifiers should not contain underscores

**Handled by configuration** — private fields intentionally use underscore prefix.

### CA1822: Member does not use instance data

**Use when:**

- Helper method is intentionally shared but not static (rare)
- Violating this would require refactoring that reduces clarity

```csharp
#pragma warning disable CA1822
public string FormatInternal(string value)
{
    // This uses instance state in subclasses; suppress warning
    return value.ToUpper();
}
#pragma warning restore CA1822
````

### CA1001: Types that own disposable fields should be disposable

**Use when:**

- Object manages disposable fields but lifetime is externally managed
- Disposal is handled by a parent container

````csharp
#pragma warning disable CA1001
public class ServiceContainer
{
    private HttpClient _client; // Disposed by DI container, not this class
}
#pragma warning restore CA1001
```text

---

## Rule Exceptions (Disabled in Configuration)

The following rules are **disabled globally** in `.stylecop.json`:

- **SA1600** — Internal elements documentation (not required)
- **SA1609** — Property documentation text (not required)
- **SA1633** — File header copyright (organizational decision)
- **SA1309** — Field names with underscore (our convention)
- **SA1310** — Field names with underscores (related to SA1309)
- **SA1101** — Base class call prefix (modern style preferred)

These require **no suppression** since they're globally disabled.

---

## When NOT to Suppress

Avoid suppressing when:

❌ **Better option exists:**

```csharp
// Don't do this:
#pragma warning disable CA1822
public string Name => _name.ToUpper();  // Use static instead
#pragma warning restore CA1822

// Do this:
public static string NameFormat(string name) => name.ToUpper();
````

❌ **Indicates design issue:**

````csharp
// Don't suppress CA1505 (complexity) — refactor instead
#pragma warning disable CA1505
public void ProcessOrder(var x, var y) { /* 200 lines */ }
#pragma warning restore CA1505

// Do this — refactor into smaller methods
public void ProcessOrder(...)
{
    ValidateOrder();
    CalculateTotal();
    ApplyDiscounts();
}
```text

❌ **Masking real issues:**

```csharp
// Don't suppress CA2000 (dispose objects) — use 'using' instead
#pragma warning disable CA2000
var stream = new FileStream(...);
#pragma warning restore CA2000

// Do this:
using var stream = new FileStream(...);
````

---

## Adding Suppressions to Version Control

All suppressions are **committed to Git** and reviewed in pull requests. Treat suppressions like code — document and justify them.

**Good commit message:**

```text
fix: suppress SA1600 for auto-generated DTO methods

Generated mappers do not require documentation per project convention.
Suppressions located in GeneratedCode/Mappers.cs.
```

**Bad commit message:**

```text
suppress warnings
```

---

## Audit Suppressions

Periodically review suppressions to ensure they remain justified:

````bash
# Find all suppressions in the codebase
grep -r "#pragma warning disable" src/

# Remove unnecessary suppressions during Phase 1 refactoring
# Suppressions should trend downward as code matures
```text

---

## References

- StyleCop Analyzer Rules: https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/RULES.md
- Microsoft NetAnalyzers: https://github.com/microsoft/NetAnalyzers
````
