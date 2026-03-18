# CI/CD Pipeline & GitHub Actions Guide

This document describes the continuous integration (CI) pipeline configured for GTEK FSM on GitHub Actions.

## Overview

The GTEK FSM platform uses **GitHub Actions** for automated quality checks on every push and pull request. The pipeline performs:

1. **NuGet Restore** — Download and cache all package dependencies
2. **Build (Debug & Release)** — Compile solution in both configurations  
3. **Code Quality Analysis** — StyleCop + Roslyn Analyzer checks
4. **Test Discovery** — Identify and prepare test projects for Phase 1+
5. **EditorConfig Validation** — Confirm formatting standards

All checks must **pass before merge** to main/dev branches via branch protection rules (Phase 0.8.5).

---

## Workflow Triggers

### Automatic Triggers

All workflows execute automatically on:

- **Push events:**
  - Branch: `main`, `dev`, `develop`
  - Any commit pushed triggers the pipeline
  
- **Pull requests:**
  - Targeting: `main`, `dev`, `develop`
  - PR opened/updated triggers checks
  - Results block merge until passing

### Manual Triggers (Future)

Can be triggered manually via:
```
GitHub UI → Actions → Select Workflow → "Run workflow"
```

---

## Workflows

### 1. CI - Restore, Build, Test Discovery

**File:** `.github/workflows/ci.yml`

**Schedule:** On push/PR to main, dev, develop  
**Duration:** ~2-3 minutes

**Steps:**

| # | Step | Purpose | Status in Phase 0 |
|---|------|---------|-------------------|
| 1 | Checkout code | Clone repository with full history | ✅ Running |
| 2 | Setup .NET SDK | Install .NET 10 runtime & tooling | ✅ Running |
| 3 | Restore packages | Download NuGet dependencies | ✅ Running |
| 4 | Build Debug | Compile with Debug symbols | ✅ Running |
| 5 | Build Release | Compile optimized Release build | ✅ Running |
| 6 | Test Discovery | Scan for test projects (*.Tests.csproj) | ✅ Ready |
| 7 | Test Execution | Run tests (when added in Phase 1+) | ⏳ Pending |

**Success Criteria:**
- ✅ All NuGet packages restore successfully
- ✅ Debug build shows 0 errors
- ✅ Release build shows 0 errors
- ✅ Test discovery completes (reports "no tests" in Phase 0)

**Example Output (Phase 0):**
```
Test Project Discovery
✅ Status: No active test projects found (expected in Phase 0)
   Note: Test projects will be discovered automatically during Phase 1 and beyond.

CI Pipeline Ready for Phase 1 Tests
✅ Restore and Build: PASSING
✅ Test Discovery: READY
⏳ Test Execution: Will activate when tests are added
```

---

### 2. Code Quality - Formatting & Analyzers

**File:** `.github/workflows/quality-checks.yml`

**Schedule:** On push/PR to main, dev, develop  
**Duration:** ~2-3 minutes

**Steps:**

| # | Step | Purpose | Analyzer |
|---|------|---------|----------|
| 1 | Checkout & Setup .NET | Environment prep | - |
| 2 | Restore packages | Download dependencies | - |
| 3 | Build with Analyzers | Compile + run static analysis | StyleCop + Roslyn |
| 4 | Generate Report | Create summary for PR/commit | - |
| 5 | EditorConfig Check | Validate formatting standards | EditorConfig |

**Analyzers Included:**

- **StyleCop.Analyzers (v1.2.0-beta.556)**
  - Spacing rules (SA1000-SA1009)
  - Ordering rules (SA1100-SA1202)
  - Documentation (SA1600 - disabled)
  - Naming conventions

- **Microsoft.CodeAnalysis.NetAnalyzers (v8.0.0)**
  - Code quality (CA1000+)
  - Security patterns (CA2000+)
  - Performance hints (CA1820+)

**Success Criteria:**
- ✅ Build completes with **0 errors**
- ✅ Warnings limited to non-critical issues (pre-existing Java SDK, .NET version notices)
- ✅ No code quality violations in user code

**Example Output:**
```
Code Quality Report

Analyzers Active:
- ✅ StyleCop.Analyzers
- ✅ Microsoft.CodeAnalysis.NetAnalyzers

Status: ✅ All code quality checks passed
```

---

### 3. CI Status Dashboard

**File:** `.github/workflows/status.yml`

**Schedule:** Runs after CI and Quality Checks workflows  
**Duration:** <1 minute

**Purpose:**
- Aggregate results from all workflows
- Post summary to workflow run details
- Provides single status view

**Output:**
```
GTEK FSM CI Status

| Check | Status |
|-------|--------|
| 🔧 Restore | ✅ Enabled |
| 🔨 Build (Debug) | ✅ Enabled |
| 🔨 Build (Release) | ✅ Enabled |
| 🧪 Test Discovery | ✅ Enabled |
| 🔍 StyleCop Analysis | ✅ Enabled |
| 🔍 Roslyn Analyzers | ✅ Enabled |
| 📋 EditorConfig | ✅ Validated |
```

---

## Viewing Pipeline Results

### In GitHub Web UI

1. **Push or PR → Checks Tab:**
   - Shows all workflow runs
   - Click workflow name for details
   - Expand each step to see logs

2. **Commit Status Badge:**
   - Green ✅ = All checks passing
   - Red ❌ = At least one check failing
   - Yellow ⏳ = Check in progress

3. **Pull Request Checks:**
   - "All checks must pass" blocks merge
   - Click "Details" to jump to failing step

### In VS Code / Local IDE

No direct integration yet (Phase 0.8.5 with git hooks).

### Command Line

```bash
# View all workflow runs
gh run list --repo shamil-suraweera/gtek-fsm

# View specific workflow
gh run view <run-id> --log

# Monitor live
gh run watch <run-id>
```

---

## Common Issues & Solutions

### Issue: "Build succeeded but Warnings found"

**Cause:** Analyzer violations in code  
**Solution:** 
1. Refer to [CODE_QUALITY_BASELINE.md](../config/CODE_QUALITY_BASELINE.md)
2. Fix violations in code
3. Or add suppression with explanation (see [ANALYZER_SUPPRESSIONS_GUIDE.md](../config/ANALYZER_SUPPRESSIONS_GUIDE.md))
4. Push fix — pipeline re-runs automatically

**Example Warning:**
```
warning CA1822: Member xxx does not use instance data, mark as static
```

### Issue: "Timeout waiting for NuGet restore"

**Cause:** Network issue or very large package**Solution:**
1. GitHub Actions has 10-minute timeout per step
2. This is rare and usually transient
3. Retry: Push an empty commit (`git commit --allow-empty`) to re-trigger

### Issue: "Test Discovery found tests but none executed"

**Cause:** Tests exist but no test runners installed (Phase 0)  
**Solution:**
- Phase 0: Only discovery enabled
- Phase 1: Add xUnit / NUnit and activate test execution
- Pipeline auto-detects when tests are added

---

## Local Validation (Before Push)

Run locally to catch issues before pushing:

```bash
# Full build with analyzers
dotnet build GTEK.FSM.slnx -c Debug

# Verbose output for issues
dotnet build GTEK.FSM.slnx -c Debug /v:detailed

# Check just for analyzer violations
dotnet build GTEK.FSM.slnx --no-restore /p:EnforceCodeStyleInBuild=true
```

---

## Future Enhancements

### Phase 0.8.4+

- [ ] Add SonarCloud integration for code coverage tracking
- [ ] Add dependabot alerts for security updates
- [ ] Add automated dependency updates (Dependabot)

### Phase 0.8.5

- [ ] Add pre-commit hooks to block non-quality code locally
- [ ] Add branch protection rules to enforce passing checks
- [ ] Add "Deploy to Staging" on successful main branch builds

### Phase 1+

- [ ] Activate unit test execution (xUnit)
- [ ] Add code coverage reporting
- [ ] Add integration test stage
- [ ] Add deployment stages (Dev → Staging → Prod)

---

## Workflow File Documentation

### ci.yml Structure

```yaml
name: CI - Restore, Build, Test Discovery

on:  # Triggers
  push:
    branches: [main, dev, develop]
  pull_request:
    branches: [main, dev, develop]

jobs:
  build:  # Job 1: Build & Restore
    runs-on: ubuntu-latest  # Run on Linux (cost-effective)
    steps:
      - uses: actions/checkout@v4  # GitHub-provided action
      - uses: actions/setup-dotnet@v4  # Install .NET SDK
      - run: dotnet restore  # Custom shell command
      - run: dotnet build ...
  
  test-discovery:  # Job 2: Test Discovery
    needs: build  # Depends on build job completing
    runs-on: ubuntu-latest
    steps: ...
```

### Secrets & Environment Variables

Currently **no secrets used** (auth-free NuGet, public repo).

Future (Phase 1):
- `SONARCLOUD_TOKEN` — Code quality scanning
- `DEPLOY_KEY` — Deployment credentials

---

## Performance & Costs

**GitHub Actions Free Tier (applies to public repos):**
- ✅ Unlimited job runs
- ✅ 20 concurrent jobs
- ✅ Linux/macOS/Windows runners
- ❌ Time limits: 30 days for free tier users (not applicable to public repos)

**GTEK FSM Current Usage:**
- **CI workflow:** ~120 seconds per run
- **Quality checks workflow:** ~150 seconds per run
- **Estimated monthly:** ~20-40 runs (commits + PRs) = ~1.5 hours = **Well within free tier**

---

## Monitoring & Alerts

### Email Notifications

GitHub sends emails when:
- Workflow fails
- Workflow completes (configurable in settings)

Configure per-repo:  
GitHub → Settings → Notifications

### Slack Integration (Future)

Can add Slack notifications via GitHub App (Phase 0.8.5).

---

## References

- [GitHub Actions Documentation](https://docs.github.com/actions)
- [GitHub Actions for .NET](https://github.com/actions/setup-dotnet)
- [Workflow Syntax](https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions)
- [StyleCop Analyzers on GitHub](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
- [Microsoft.CodeAnalysis.NetAnalyzers](https://github.com/microsoft/NetAnalyzers)

---

## Support & Questions

For questions about the CI pipeline:

1. **Check logs:** GitHub UI → Actions → Workflow Run → Failing Step
2. **Local reproduction:** Run `dotnet build` locally with same parameters
3. **Reference config:** [CODE_QUALITY_BASELINE.md](../config/CODE_QUALITY_BASELINE.md)

