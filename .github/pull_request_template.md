## Description
<!--
Briefly describe what this PR does and why it was necessary.
What problem does it solve? What feature does it add?
-->

## Type of Change
<!--
Delete options that don't apply.
Add an 'x' inside the brackets: [x]
-->

- [ ] New feature (non-breaking change which adds functionality)
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] Breaking change (fix or feature that would cause existing functionality to change)
- [ ] Documentation update (no code changes)
- [ ] Dependency update (package versions, analyzers, etc)
- [ ] Code refactoring (no functional change)
- [ ] Configuration/infrastructure change

## Related Issues
<!--
Link related issue(s) using # notation.
Examples: Fixes #123, Related to #456, Closes #789
-->

Fixes #
Related to #

## Testing Performed
<!--
Describe how you tested this change.
Check tests that apply.
-->

- [ ] Manual testing on local environment
- [ ] Tested with Docker Compose setup
- [ ] Tested in multiple browsers (if UI change)
- [ ] Unit tests added/updated (Phase 1+)
- [ ] Integration tests added/updated (Phase 1+)
- [ ] Smoke tested staging environment (Phase 2+)

### Testing Details
<!--
Provide specific testing scenarios:
- What did you test?
- What inputs did you try?
- What was the expected and actual output?
-->

## Screenshots/Demo
<!--
If this PR includes UI changes, include before/after screenshots.
For API changes, include example request/response.
Remove this section if not applicable.
-->

## Checklist
<!--
Verify all items before requesting review.
Add an 'x' inside the brackets: [x]
-->

### Code Quality
- [ ] Code follows the style guidelines (EditorConfig, analyzer rules)
- [ ] No StyleCop violations introduced
- [ ] No new warnings in build output
- [ ] Naming conventions followed (PascalCase, camelCase, _prefix rules)
- [ ] No hardcoded secrets committed (no .env, no API keys)
- [ ] Commented complex logic (explaining WHY, not WHAT)

### Conventions
- [ ] Branch name follows convention: `type/scope/description`
- [ ] Commit messages follow format: `type(scope): subject`
- [ ] PR title follows format: `type(scope): description`

### Architecture & Design
- [ ] Code follows architectural rules (see ARCHITECTURE_RULES.md)
- [ ] Respects project boundaries (see PROJECT_BOUNDARIES.md)
- [ ] Maintains tenant isolation patterns (where applicable)
- [ ] Dependency direction correct (no downward dependencies)

### Documentation
- [ ] README updated (if needed)
- [ ] Code comments added for complex logic
- [ ] CONFIGURATION_GUIDE.md updated (if adding config)
- [ ] Inline documentation complete

### Dependencies
- [ ] No unnecessary package dependencies added
- [ ] Dependency versions pinned appropriately
- [ ] LICENSE file updated (if adding external dependency)

## Deployment Notes
<!--
Include any special deployment considerations, database migration needs, environment variable changes, feature flags to enable, etc.
-->

## Rollback Plan
<!--
If needed in production, how would this be rolled back?
Are there any data considerations?
-->

## Additional Notes
<!--
Any additional context, gotchas, or things reviewers should know.
-->

---

**Related Documentation:**
- [Git Workflow Conventions](../config/GIT_WORKFLOW_CONVENTIONS.md)
- [Code Quality Baseline](../config/CODE_QUALITY_BASELINE.md)
- [Architecture Rules](../config/ARCHITECTURE_RULES.md)
- [Contributing Guide](.github/CONTRIBUTING.md)

