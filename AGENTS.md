# Agent instructions — contoso-notes

This is the contoso-notes Azure Static Web App (TypeScript front-end, .NET
Functions BFF, Bicep infra, azd deploy). Quality gates are wired through the
stacks' own tools; their canonical entry point is `npm run verify`.

Working rules:

- **`npm run verify` is the source of truth.** Format-check, lint, typecheck
  and tests for every stack chain through it; CI runs the same command. Do
  not invent bespoke checks.
- **Multi-stack:** Node/TypeScript (`app/`), .NET (`api/`), Bicep (`infra/`).
  Each stack's tools are listed in `.guardrails.json` and `docs/guardrails.md`.
- **`Directory.Build.props` and `Directory.Packages.props` are central** for
  .NET — change a target framework or package version there, not in a
  `.csproj`. The solution file is `api/ContosoNotes.slnx`.
- **Hooks are on at commit and push.** `pre-commit` runs lint-staged (format,
  lint, secrets, spelling over staged files); `commit-msg` lints the message;
  `pre-push` runs the full `verify`. Do not pass `--no-verify` to land work
  that fails a gate — fix the gate or fix the work.
- **Coverage floor is 80%** on both stacks (`c8` for Node, coverlet for
  .NET). The .NET floor is enforced by `scripts/check-dotnet-coverage.mjs`.
- **Secrets:** never commit credentials. The secretlint pattern rule blocks
  home-dir and UNC paths too — keep infra and config out of those shapes.

Before claiming work complete: `npm run verify` must pass.
