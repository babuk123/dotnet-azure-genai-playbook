# GitHub Copilot in day-to-day .NET work

| Task | How I use it | What I always check |
|------|--------------|---------------------|
| Boilerplate (DTOs, mappings, EF configurations) | Inline completions from a clear method signature + comment | Naming, nullability, money as `decimal` |
| Unit tests | Copilot Chat `/tests` on a selected method, then add edge cases | Tests actually assert behaviour, not implementation |
| Explaining legacy code (WCF, .NET Framework) | `/explain` on a class before refactoring | Cross-check with logs and real behaviour |
| Upgrades (.NET 6/8 → 10) | Copilot **app modernization** for .NET: upgrade plan, package updates, build fixes | Breaking changes, full test run, perf test |
| Code review | Copilot PR review comments as a first pass | Security, business rules, performance remain human-reviewed |
| Agent mode / coding agent | Small, well-specified issues (add endpoint + tests) assigned to the agent; review the PR | Scope, secrets, licence of suggested code |

Good practice:
- Give context: open the related files, use `#file` / `#codebase` references, keep a `.github/copilot-instructions.md` with team conventions.
- Never paste secrets or customer data into prompts; enterprise settings block suggestions matching public code.
- Treat output as a draft from a fast junior developer: compile, test, review.
