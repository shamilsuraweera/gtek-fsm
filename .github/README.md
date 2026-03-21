# GitHub Actions Workflows

This directory contains GitHub Actions CI/CD workflows for GTEK FSM.

## Workflows

### [ci.yml](workflows/ci.yml)

- **Name:** CI - Restore, Build, Test Discovery
- **Triggers:** Push to main/dev, Pull requests
- **Purpose:** NuGet restore, build (Debug + Release), test discovery
- **Duration:** ~2-3 minutes
- **Status:** ✅ Active

### [quality-checks.yml](workflows/quality-checks.yml)

- **Name:** Code Quality - Formatting & Analyzers
- **Triggers:** Push to main/dev, Pull requests
- **Purpose:** StyleCop, Roslyn Analyzer checks, EditorConfig validation
- **Duration:** ~2-3 minutes
- **Status:** ✅ Active

### [status.yml](workflows/status.yml)

- **Name:** CI Status Dashboard
- **Triggers:** After CI and quality-checks complete
- **Purpose:** Aggregate status across workflows
- **Duration:** <1 minute
- **Status:** ✅ Active

## Configuration

All workflows:

- Run on **Ubuntu** (cost-effective, consistent)
- Use **.NET 10 SDK** (matches project target)
- Triggered on **main**, **dev**, **develop** branches
- Also run on **pull requests** targeting these branches

## Documentation

For detailed information on workflow management, troubleshooting, and monitoring:

📄 **[Comprehensive CI Pipeline Guide](../config/CI_PIPELINE_GUIDE.md)**

## Quick Reference

| Workflow           | Restore | Build  | Test     | Quality | Status       |
| ------------------ | ------- | ------ | -------- | ------- | ------------ |
| ci.yml             | ✅ Yes  | ✅ Yes | 🧪 Ready | -       | ✅ Running   |
| quality-checks.yml | ✅ Yes  | -      | -        | ✅ Yes  | ✅ Running   |
| status.yml         | -       | -      | -        | -       | ✅ Dashboard |

## Running Workflows

### Automatic (via GitHub)

- Push to main/dev branch
- Create/update pull request targeting main/dev

### Manual (GitHub UI)

1. Go to **Actions** tab
2. Select workflow from sidebar
3. Click **Run workflow → Run workflow**

### Command Line (gh CLI)

````bash
# List workflows
gh workflow list

# Trigger workflow
gh workflow run ci.yml -r dev

# View last run
gh run view --workflow ci.yml
```text

## Next Steps

- Phase 0.8.4: Add environment file templates
- Phase 0.8.5: Add branch protection rules + pre-commit hooks
- Phase 1+: Add test execution, code coverage, deployment stages
````
