# Phase 5 Web Portal Test Baseline

This project establishes the Phase 5 test baseline for the web operations shell:

- Component tests: focused behavior checks for isolated UI components.
- Integration tests: page-level security-aware behavior and guardrails.
- End-to-end workflow tests: key operator workflow path within rendered pages.
- Performance budget tests: baseline render/load timing and markup-size guardrails.

## Run

```bash
dotnet test web-portal.tests/GTEK.FSM.WebPortal.Tests.csproj
```
