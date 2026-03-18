# Contributing to GTEK FSM

Thank you for contributing to GTEK FSM! This guide ensures all contributions maintain code quality, consistency, and our architectural vision.

## Quick Start for Contributors

### 1. Fork/Clone and Branch

```bash
git clone https://github.com/shamil-suraweera/gtek-fsm.git
cd gtek-fsm

# Create feature branch (follows convention: feature/*, bugfix/*, etc.)
git checkout -b feature/your-feature-name
```

### 2. Set Up Local Development

```bash
# Follow setup guide (15-25 minutes)
cp .env.example .env
dotnet restore GTEK.FSM.slnx
dotnet build GTEK.FSM.slnx

# Start services
./deploy/scripts/start-all.sh
```

Detailed setup: [LOCAL_SETUP_GUIDE.md](../LOCAL_SETUP_GUIDE.md)

### 3. Make Changes

- Follow [Git Workflow Conventions](../config/GIT_WORKFLOW_CONVENTIONS.md)
- Follow [Code Quality Baseline](../config/CODE_QUALITY_BASELINE.md)
- Follow [Architecture Rules](../config/ARCHITECTURE_RULES.md)

### 4. Commit with Proper Format

```bash
# Format: type(scope): subject
git commit -m "feat(auth): add login endpoint"
```

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`

See [GIT_WORKFLOW_CONVENTIONS.md](../config/GIT_WORKFLOW_CONVENTIONS.md#commit-message-conventions) for details.

### 5. Push and Create PR

```bash
git push origin feature/your-feature-name
```

Then create PR on GitHub. Template auto-populates.

---

## Code Quality Requirements

### Before Submitting PR

**All of these must pass locally:**

```bash
# 1. Clean build (no errors)
dotnet build GTEK.FSM.slnx

# Expected: 0 errors
# Warnings are OK if justified (see ANALYZER_SUPPRESSIONS_GUIDE.md)

# 2. No StyleCop violations
# (Build output shows analyzer results)

# 3. EditorConfig compliance
# (VS Code auto-fixes on save if EditorConfig extension installed)

# 4. Follow code quality baseline
# See config/CODE_QUALITY_BASELINE.md for naming, formatting, patterns
```

**If violations appear:**
1. Fix the violation (preferred)
2. Or add justified suppression with explanation (see ANALYZER_SUPPRESSIONS_GUIDE.md)

### GitHub Automated Checks

Your PR automatically runs 3 workflows:

1. **CI Pipeline** - NuGet restore, Build, Test discovery
2. **Quality Checks** - StyleCop, Roslyn, EditorConfig
3. **Status** - Overall pass/fail

**All checks must pass before merge** ✅

---

## Commit Message Guidelines

### Format

```
type(scope): subject line, max 50 chars

Optional body explaining WHY not WHAT.
Keep lines under 72 characters.
Reference related issues.

Fixes #123
```

### Examples

✅ **Good:**
```
feat(auth): add jwt token validation

Implement JWT token parsing and validation in auth middleware.
Validates token signature and expiration before allowing request.

Fixes #42
```

```
fix(api): handle null reference in product mapper

Added null check before accessing product.Category.
Prevents NullReferenceException when category is null.
```

```
docs(setup): add Docker troubleshooting section
```

❌ **Bad:**
```
fixed stuff
WIP
auth
Updated
```

See full guide: [GIT_WORKFLOW_CONVENTIONS.md#commit-message-conventions](../config/GIT_WORKFLOW_CONVENTIONS.md#commit-message-conventions)

---

## Branch Naming

### Format

```
{type}/{scope}/{description-in-kebab-case}
```

### Examples

✅ **Good:**
- `feature/user-authentication`
- `bugfix/email-validation`
- `hotfix/payment-processor`
- `chore/upgrade-dependencies`
- `docs/api-documentation`

❌ **Bad:**
- `feature` (too vague)
- `my-work` (no type)
- `Feature/User Authentication` (mixed case, spaces)

See full guide: [GIT_WORKFLOW_CONVENTIONS.md#branch-naming-conventions](../config/GIT_WORKFLOW_CONVENTIONS.md#branch-naming-conventions)

---

## Architecture & Design

### Know the Rules

**Before coding, understand:**
1. [Architectural Rules](../config/ARCHITECTURE_RULES.md) - Dependency direction, layering
2. [Project Boundaries](../config/PROJECT_BOUNDARIES.md) - What belongs where
3. [Tenancy Approach](../config/TENANCY_APPROACH.md) - Data isolation patterns

### Follow These Patterns

**Backend Architecture:**
- Domain layer: Business logic (no frameworks)
- Application layer: Use cases and workflows
- Infrastructure layer: Database, external services
- API layer: Route handlers and DTOs
- Shared contracts: Cross-project models

**Code Organization:**
- Namespace follows folder structure
- One public type per file (or closely related types)
- Naming: PascalCase (public), camelCase (local), _camelCase (private fields)

**Configuration:**
- Environment-aware via `.env` and `appsettings.{Env}.json`
- No hardcoded secrets
- Feature flags for gradual rollout

---

## Testing (Phase 1+)

When tests are added:

```bash
# Run all tests locally before pushing
dotnet test GTEK.FSM.slnx

# Expected: All green ✅
```

**Test requirements:**
- Unit tests: 80%+ coverage target
- Integration tests: Critical paths only
- Naming: `{ClassUnderTest}Tests.cs`
- Pattern: Arrange-Act-Assert (AAA)

---

## PR Review Process

### What to Expect

1. **Automatic Checks** (5-10 min)
   - CI/CD pipeline runs
   - Code quality checks run
   - Status badge shows pass/fail

2. **Manual Review** (1-2 hours for main, same-day for dev)
   - 2 reviewers required for main
   - 1 reviewer for dev
   - Reviewers check correctness, style, architecture

3. **Requested Changes?**
   - Address feedback
   - Push additional commits (or amend)
   - Re-request review
   - Loop until approved

4. **Approved!**
   - Reviewer clicks "Approve"
   - All checks green ✅
   - Ready to merge

### Reviewer Expectations

Reviewers look for:
- ✅ Code correctness (works, no bugs)
- ✅ Architecture compliance (follows rules)
- ✅ Style consistency (naming, formatting)
- ✅ Code quality (no unjustified violations)
- ✅ Security (no secrets committed, no vulnerabilities)
- ✅ Documentation (clear, updated)
- ✅ Testing (adequate coverage, Phase 1+)

---

## Merge Process

1. **All checks pass** ✅
2. **All reviews approved** ✅
3. **Ready to merge** (maintainer merges)
4. **Source branch deleted** (auto)
5. **Local cleanup:**
   ```bash
   git checkout dev
   git pull origin dev
   git branch -D feature/your-feature
   ```

---

## Style & Naming Conventions

### Quick Reference

| Element | Convention | Example |
|---------|-----------|---------|
| Class/Type | PascalCase | `CustomerService`, `ValidationError` |
| Method | PascalCase | `GetCustomer()`, `ValidateEmail()` |
| Public Property | PascalCase | `CustomerId`, `IsActive` |
| Local Variable | camelCase | `totalAmount`, `isValid` |
| Private Field | _camelCase | `_logger`, `_repository` |
| Constant | UPPER_SNAKE_CASE | `MAX_RETRIES`, `DEFAULT_TIMEOUT` |
| Interface | IPascalCase | `ICustomerService`, `IValidator` |

### Code Examples

✅ **Good:**
```csharp
public class CustomerService
{
    private readonly ICustomerRepository _repository;

    public async Task<Customer> GetCustomer(int customerId)
    {
        var customer = await _repository.GetById(customerId);
        return customer;
    }
}
```

❌ **Bad:**
```csharp
public class customer_service  // Wrong: lowercase
{
    public ICustomerRepository Repository;  // Wrong: public field

    public async Task<Customer> get_customer(int customer_id)  // Wrong: snake_case
    {
        var Customer = await Repository.GetById(customer_id);
        return Customer;
    }
}
```

See full style guide: [CODE_QUALITY_BASELINE.md](../config/CODE_QUALITY_BASELINE.md)

---

## Common Issues & Solutions

### Issue: Build fails locally but CI passes

```bash
# Clear cache and rebuild
rm -rf ~/.nuget/packages/*gtek* 2>/dev/null
dotnet clean GTEK.FSM.slnx
dotnet restore GTEK.FSM.slnx
dotnet build GTEK.FSM.slnx
```

### Issue: StyleCop violations won't go away

1. Verify EditorConfig is installed (VS Code extension)
2. Reload VS Code window
3. If justified, add suppression per ANALYZER_SUPPRESSIONS_GUIDE.md

### Issue: Merge conflicts

```bash
# Pull latest dev
git fetch origin
git merge origin/dev

# VS Code: Use "Resolve in Merge Editor"
# Or manually edit files marked with <<<<<<< >>>>>>>>

# After resolving
git add .
git commit -m "merge: resolve conflicts from dev"
git push origin feature/your-feature
```

### Issue: Accidentally committed secrets

**Do NOT push this branch!**

```bash
# Remove file from history
git rm --cached .env
git commit -m "remove: .env file with secrets"

# Rotate compromised secrets immediately
# Contact security team

# Then push
git push origin your-branch
```

---

## Asking Questions

### Getting Help

1. **Setup issues?** → See [LOCAL_SETUP_GUIDE.md](../LOCAL_SETUP_GUIDE.md)
2. **Architecture questions?** → See [ARCHITECTURE_RULES.md](../config/ARCHITECTURE_RULES.md)
3. **Code quality?** → See [CODE_QUALITY_BASELINE.md](../config/CODE_QUALITY_BASELINE.md)
4. **Git workflow?** → See [GIT_WORKFLOW_CONVENTIONS.md](../config/GIT_WORKFLOW_CONVENTIONS.md)
5. **Still stuck?** → Open a discussion or reach out to maintainers

### Reporting Issues

When reporting bugs or suggesting features:

1. **Use GitHub Issues**
2. **Provide context:**
   - OS and .NET version (`dotnet --version`)
   - Steps to reproduce
   - Expected vs actual behavior
   - Error messages/screenshots
3. **Reference related issues** if applicable

---

## Code Review Checklist

**Before requesting review, verify:**

- [ ] Feature branch follows naming: `type/scope/description`
- [ ] All commits follow format: `type(scope): subject`
- [ ] Local build passes: `dotnet build GTEK.FSM.slnx`
- [ ] No StyleCop violations (or justified suppressions)
- [ ] EdgeConfig compliance verified
- [ ] Architecture rules followed
- [ ] Naming conventions applied
- [ ] No secrets committed (.env, keys, etc)
- [ ] Optional: Updated documentation if needed
- [ ] PR template filled out completely
- [ ] Self-reviewed own code

---

## Phase-Specific Guidelines

### Phase 0 (Current - Foundation)
- Follow conventions to establish patterns
- 1 reviewer minimum
- Focus on architecture compliance

### Phase 1 (Domain & Database)
- Tests required for new domain logic
- 2 reviewers for main
- Entity mapping validation

### Phase 2+ (Features)
- Unit test coverage: 80%+
- Security review for auth/identity
- Performance testing for queries

### Phase 11 (Production)
- GPG-signed commits
- Deploy checklist verification
- Automated test suite 100% passing

---

## Recognition

Contributors are recognized in:
- Commit history (forever in git log)
- Release notes (Phase 1+)
- Contributors file (if added)

Thank you for making GTEK FSM better! 🎉

---

## Need Help?

- 📖 **Documentation:** Check config/ directory
- 🐛 **Bug Reports:** GitHub Issues
- 💬 **Questions:** GitHub Discussions
- 🚀 **Ready to contribute?** Start with "good first issue" label

Happy coding!

