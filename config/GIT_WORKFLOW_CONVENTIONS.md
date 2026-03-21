# GTEK FSM - Git Workflow Conventions

## Overview

This document defines branch naming, commit message format, pull request procedures, code review standards, and merge strategies for the GTEK FSM project.

All contributors must follow these conventions to maintain a clean, readable, and auditable git history.

---

## Branch Naming Conventions

### Structure

```text
{type}/{scope}/{description}
```

### Types

| Type          | Purpose                        | Duration  | Example                         |
| ------------- | ------------------------------ | --------- | ------------------------------- |
| `feature/`    | New functionality              | 1-2 weeks | `feature/tenant-isolation`      |
| `bugfix/`     | Fix non-critical bug           | 1-3 days  | `bugfix/email-validation`       |
| `hotfix/`     | Critical production fix        | Same day  | `hotfix/security-patch`         |
| `chore/`      | Maintenance (deps, config)     | 1-2 days  | `chore/update-nuget-packages`   |
| `refactor/`   | Code restructuring             | 1 week    | `refactor/api-request-handling` |
| `docs/`       | Documentation only             | 1-2 days  | `docs/setup-guide`              |
| `experiment/` | Exploration (may be discarded) | 1-2 weeks | `experiment/ai-suggestions`     |

### Scope

Narrow scope to **single feature/area**:

✅ **Good:**

- `feature/admin-dashboard`
- `bugfix/cors-headers`
- `chore/docker-compose`

❌ **Bad:**

- `feature/everything` (too broad)
- `work` (too vague)
- `feature/a` (meaningless)

### Description

Use **lowercase hyphenated** format, max 50 characters:

✅ **Good:**

- `feature/email-notifications`
- `bugfix/null-reference-error`
- `hotfix/payment-processing`

❌ **Bad:**

- `feature/Email_Notifications` (mixed case)
- `feature/email-notifications-with-templates-and-attachments` (too long)
- `feature/email notifications` (spaces)

### Examples

````bash
# Feature development (1-2 weeks)
git checkout -b feature/user-authentication

# Quick bug fix (1-3 days)
git checkout -b bugfix/form-validation-error

# Critical production fix (immediate)
git checkout -b hotfix/missing-service-endpoint

# Dependency updates (1 day)
git checkout -b chore/upgrade-aspectcore-sdk

# Learning/exploration (may be discarded)
git checkout -b experiment/graphql-migration
```text

---

## Commit Message Conventions

### Structure

````

{type}({scope}): {subject}

{body}

{footer}

```text

### Format Rules

1. **Subject (required):** Max 50 characters, lowercase, no period
2. **Type (required):** One of: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`
3. **Scope (required):** Area affected (e.g., `auth`, `api`, `database`)
4. **Body (optional):** Detailed explanation (max 72 chars/line)
5. **Footer (optional):** References, breaking changes

### Commit Types

| Type | Purpose | Example |
| ------ | --------- | --------- |
| `feat` | New feature | `feat(auth): add jwt token validation` |
| `fix` | Bug fix | `fix(api): handle null reference in mapper` |
| `docs` | Documentation | `docs(setup): add local development guide` |
| `style` | Formatting, EditorConfig | `style(csharp): apply naming conventions` |
| `refactor` | Code restructuring | `refactor(domain): extract service layer` |
| `perf` | Performance improvement | `perf(query): add database index on user_id` |
| `test` | Test additions/fixes | `test(auth): add token expiration scenarios` |
| `chore` | Deps, CI, tooling | `chore(deps): update StyleCop.Analyzers` |

### Examples

#### Example 1: Simple Feature

```

feat(auth): add login endpoint

- Accept email and password
- Validate credentials against database
- Return JWT token on success
- Return 401 on invalid credentials

```text

#### Example 2: Bug Fix

```

fix(api): handle empty request body

Previously, POST requests with empty bodies threw NullReferenceException.
Added validation in request deserializer to return 400 Bad Request instead.

Fixes #1234

```text

#### Example 3: Refactoring

```

refactor(domain): extract validation logic to separate service

Moved customer validation from Application layer to dedicated
CustomerValidator service for better reusability and testability.

BREAKING CHANGE: CustomerService.Validate() method removed
(use CustomerValidator.Validate() instead)

```text

#### Example 4: Chore

```

chore(deps): upgrade nuget packages to latest stable

- StyleCop.Analyzers 1.2.0 → 1.2.1
- Microsoft.CodeAnalysis.NetAnalyzers 7.0.2 → 8.0.0
- No functional changes

````text

### Commit Best Practices

✅ **DO:**

- Commit logically related changes together
- Write in imperative mood ("add", "fix", "update", NOT "added", "fixed")
- Reference issue numbers: `Fixes #123` or `Related to #456`
- Keep commits small and focused (ideal: 1-5 files changed)
- Sign commits with GPG key (Phase 11 security hardening)

❌ **DON'T:**

- Mix unrelated features in one commit
- Use vague messages: `fix stuff`, `update code`
- Commit large generated files (use `.gitignore`)
- Commit commented-out code (delete or document why needed)
- Rewrite public branch history (use `--force-with-lease` only if necessary)

### Viewing Commit History

```bash
# Standard log
git log

# One-line compact format
git log --oneline

# With graph visualization
git log --graph --oneline --all

# Search by message
git log --grep="auth" --oneline

# Search by author
git log --author="shamil" --oneline

# View changes in a commit
git show <commit-hash>
````

---

## Pull Request Process

### Before Creating PR

1. **Ensure branch is up to date:**

   ```bash
   git checkout feature/my-feature
   git pull origin dev
   # Resolve any conflicts
   ```

2. **Run local verification:**

   ```bash
   dotnet restore GTEK.FSM.slnx
   dotnet build GTEK.FSM.slnx
   dotnet test GTEK.FSM.slnx  # When Phase 1+ adds tests
   ```

3. **Code quality check:**

   ```bash
   # Verify no StyleCop violations
   # Review EditorConfig compliance
   dotnet build --verbosity detailed
   ```

### Creating PR

1. **Push feature branch:**

   ```bash
   git push origin feature/my-feature
   ```

2. **Go to GitHub → "New Pull Request"**

3. **Fill out PR template** (auto-populated from `.github/pull_request_template.md`):
   - Descriptive title
   - Detailed description
   - Related issue numbers
   - Type of change
   - Testing performed
   - Screenshots (if UI changes)
   - Checklist verification

4. **Request reviewers** (at least 2 for main/dev):
   - Architecture team for core changes
   - Feature owner for domain logic
   - Infrastructure team for deployment changes

### PR Title Format

```text
{type}({scope}): {description}
```

Same convention as commit messages:

✅ **Good:**

- `feat(auth): add jwt token validation`
- `fix(api): handle null reference in mapper`
- `docs(setup): add local development guide`

❌ **Bad:**

- `WIP: auth stuff` (vague)
- `Fix bugs and add features` (no scope)
- `updated code` (no type)

### PR Description Template

````markdown
## Description

[What does this PR do? Why?)

## Type of Change

- [ ] New feature
- [ ] Bug fix
- [ ] Breaking change
- [ ] Documentation update

## Related Issues

Fixes #123
Related to #456

## Testing Performed

- [x] Manual testing on local environment
- [ ] Added unit tests (Phase 1+)
- [ ] Integration tests pass (Phase 1+)
- [ ] API contract tests pass (Phase 3+)

## Screenshots

[If UI changes: add before/after screenshots]

## Checklist

- [x] Code follows style guidelines
- [x] No StyleCop violations
- [x] EditorConfig compliance verified
- [x] Self-reviewed my own code
- [x] Comments added for complex logic
- [x] Documentation updated
- [x] No new warnings generated

```text

---

## Code Review Standards

### Reviewer Responsibilities

**Review for:**

1. ✅ Correctness (does it work? are there bugs?)
2. ✅ Architecture (follows defined patterns?)
3. ✅ Style (follows code quality baseline?)
4. ✅ Performance (any obvious inefficiencies?)
5. ✅ Security (any vulnerabilities?)
6. ✅ Documentation (clear and complete?)
7. ✅ Tests (adequate coverage? Phase 1+)

### Review Comments Format

**Good comments are:**

- Specific and actionable
- Polite and collaborative
- Reference code sections
- Suggest improvements (not just criticism)

### Comment Types

**Blocker 🚫** (must fix before merge):
```
````

🚫 This violates our tenant isolation rules.
Customer data would be visible across tenants.
See ARCHITECTURE_RULES.md rule 3.2.
Please add tenant filter before product query.

```text

**Important ⚠️** (should fix):

```

⚠️ This introduces StyleCop violation SA1309 (\_prefix).
See CODE_QUALITY_BASELINE.md.
Consider renaming \_customer to customer or remove leading underscore.

```text

**Suggestion 💡** (nice to have):

```

💡 This could be more efficient using System.Linq.Skip/.Take
instead of manual loop. Not critical for Phase 0.

```text

**Approved ✅** (good to merge):

```

✅ Looks good! Follows conventions, no violations detected.
Ready to merge when all reviews pass.

````text

### Review Checklist

Reviewers should verify:

- [ ] **Branch name** follows convention (`feature/*, bugfix/*, etc.`)
- [ ] **Commit messages** follow format (type(scope): subject)
- [ ] **Code style** passes EditorConfig ruleset
- [ ] **No StyleCop violations** introduced
- [ ] **No analyzer warnings** ignored unjustifiably
- [ ] **Naming conventions** followed (PascalCase, camelCase, _prefix rules)
- [ ] **No secrets** committed (.env, credentials, API keys)
- [ ] **Documentation** updated if needed
- [ ] **Architecture** respected (dependency direction, layering)
- [ ] **Comments** explain "why", not "what"

---

## Merge Strategy & Main Branch Protection

### Branch Protection Rules

**On `main` branch:**

- ✅ Require pull request reviews (2 approvals)
- ✅ Require status checks to pass (CI pipeline, code quality)
- ✅ Dismiss stale PR approvals when new commits pushed
- ✅ Require branches up to date before merge
- ✅ Restrict who can push: only admins

**On `dev` branch:**

- ✅ Require pull request reviews (1 approval)
- ✅ Require status checks to pass (CI pipeline, code quality)
- ✅ Allow force push for hotfixes (with `--force-with-lease` only)

### Merge Strategies

#### Feature Branches → Dev (Standard)

```bash
# Squash commits for cleaner history
git merge --squash feature/user-auth

# Creates single commit with all feature changes
# good for keeping main history readable
````

**When to squash:**

- Multiple small commits (~3-10)
- Work-in-progress commits
- Intermediate checkpoint commits

#### Dev → Main (Release)

````bash
# Create release commit without squashing
git merge --no-ff dev

# Preserves full history of all commits
# Important for traceability and rollback
```text

**Never squash release merges** - maintain full history.

#### Hotfix → Main (Emergency)

```bash
# Fast-forward merge for critical fixes
git merge --ff-only hotfix/security-patch

# Immediately merge same fix to dev
git checkout dev
git merge --ff-only main
````

### Merge Procedure

1. **All review approvals received**

   ```text
   ✅ Review 1: Approved
   ✅ Review 2: Approved
   ✅ CI Pipeline: Passing
   ```

2. **Resolve any outstanding conversations**
   - All code review comments addressed
   - All suggestions acknowledged

3. **Delete source branch after merge** (GitHub option: auto-delete)
   - Keeps repository clean
   - Prevents accidental commits to old branch

4. **Document in issue tracker** (Phase 2+):
   - Link PR to issue
   - Note deployment date (if applicable)

### Example Complete Workflow

````bash
# 1. Create and push feature branch
git checkout dev
git pull origin dev
git checkout -b feature/email-service

# 2. Make changes and commit with proper format
echo "Adding email service" > src/Services/EmailService.cs
git add src/Services/EmailService.cs
git commit -m "feat(email): add email service with template support"

# 3. Push to GitHub
git push origin feature/email-service

# 4. Create PR on GitHub (fills template from .github/pull_request_template.md)

# 5. Reviewers approve (2 for main, 1 for dev)

# 6. Merge via GitHub UI with option:
#    - Squash if many small commits
#    - Create merge commit for releases
#    - Auto-delete branch

# 7. Delete local branch
git checkout dev
git pull origin dev
git branch -D feature/email-service
```text

---

## Rollback & Recovery

### Undo Recent Commits (Not Yet Pushed)

```bash
# Undo last commit, keep changes
git reset --soft HEAD~1

# Undo last commit, discard changes
git reset --hard HEAD~1

# Ammend last commit (before pushing)
git add .
git commit --amend --no-edit
````

### Undo After Push (Published)

````bash
# Create new commit that reverts previous commit
git revert <commit-hash>
git push origin dev

# This is safer than force-push for published history
```text

### Emergency Rollback

```bash
# Only for production hotfixes or major issues
# Use --force-with-lease (safer than --force)
git push origin main --force-with-lease

# Then notify team immediately
````

---

## CI/CD Integration

### GitHub Actions Validation

Every PR automatically runs:

1. **Build Check** (`.github/workflows/ci.yml`)
   - NuGet restore
   - Build Debug & Release
   - Test discovery

2. **Code Quality** (`.github/workflows/quality-checks.yml`)
   - StyleCop analysis
   - Roslyn analyzers
   - EditorConfig validation

3. **Status** (`.github/workflows/status.yml`)
   - Aggregated pass/fail

**PR cannot merge until all checks pass** ✅

### Viewing CI Results

````bash
# View locally before pushing
dotnet build GTEK.FSM.slnx
dotnet test GTEK.FSM.slnx  # Phase 1+

# View on GitHub > PR > Checks tab
# Click details to see full build log
```text

---

## Common Scenarios

### Scenario 1: Last Commit Message Typo

```bash
git commit --amend -m "fix(auth): add token validation"
# If not pushed yet
````

### Scenario 2: Forgot File in Commit

````bash
git add forgotten-file.cs
git commit --amend --no-edit
# Still unpushed
```text

### Scenario 3: Squash Multiple Commits Before Push

```bash
# Last 3 commits
git rebase -i HEAD~3

# Mark first as 'pick', rest as 'squash'
# Save and enter combined message

git push origin feature/my-feature
````

### Scenario 4: Pull Dev Into Feature (Stay Updated)

````bash
git checkout feature/my-feature
git fetch origin
git merge origin/dev
# Resolve conflicts if any
git push origin feature/my-feature
```text

### Scenario 5: Accidentally Committed to Main

```bash
# Create new branch from current commit
git branch feature/oops-main

# Reset main to previous
git reset --hard origin/main

# Switch to new branch and push
git checkout feature/oops-main
git push origin feature/oops-main

# Create PR as normal
````

---

## Phase-Specific Guidelines

### Phase 0-1 (Foundation)

- Feature branches for all work
- Commit messages optional but encouraged
- 1 reviewer minimum

### Phase 2 (Identity)

- Strict code review required (security focus)
- All secrets reviewed before merge
- Commit messages mandatory

### Phase 3+ (Scaling)

- Squash commits on merge
- Release branches for staging
- Automated deployment on merge to main

### Phase 11 (Production)

- GPG-signed commits required
- Two-phase approval for main
- Immutable release tags

---

## References

- [GitHub Flow Guide](https://guides.github.com/introduction/flow/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Git Documentation](https://git-scm.com/doc)
- [ARCHITECTURE_RULES.md](architecture-rules.json)
- [CODE_QUALITY_BASELINE.md](CODE_QUALITY_BASELINE.md)
