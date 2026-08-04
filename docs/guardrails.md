# The standards

One page. What each capability means, where it is enforced, and who owns
exceptions. Tool choices for this repository are recorded in
[`.guardrails.json`](../.guardrails.json); the canonical entry point is
`npm run verify`.

## Principles

- **Clean builds.** Zero warnings, zero errors, enforced by tools — a warning
  is either compensated at runtime (fine) or a detected defect (not fine).
- **Enforce at the point of modification.** Auto-fix on edit, check on commit,
  sweep on push/PR. The later a defect is caught, the more it costs.
- **Scripts and hard stops over prose.** A standard that only lives in a
  document is a suggestion.
- **The stack's own tools.** Well-understood, well-supported tools over
  bespoke code, always. Platform-native tooling (Dependabot, CodeQL, push
  protection) beats a CI-installed tool, which beats anything bespoke.
- **Sensible defaults, on.** Templates ship with the strict options enabled —
  analyser packs at their latest level, complexity limits, personal-detail
  scanning — and a repository loosens them deliberately, with a reason, not
  by never turning them on.
- **Exceptions are owned.** Anything switched off or suppressed carries a
  reason and a human owner, and is visible in review.
- **CI is parity, not authority.** CI re-runs the same `verify` the developer
  ran; it exists to catch what `--no-verify` skipped, not to be the first
  place checks run.
- **A task is done when verifiably tested.** TDD for changes; a feature's
  end-to-end journey test exists before the feature is called complete.

## Capabilities

| Capability        | Tool on this repo                                                                                                           | Enforced at                       |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------- | --------------------------------- |
| `format`          | `prettier` (TS/JS/JSON/YAML/MD), `dotnet format` (C#), `bicep format` (Bicep)                                               | pre-commit (staged), `verify`, CI |
| `lint`            | `eslint` (TS/JS), .NET SDK analysers (`TreatWarningsAsErrors` + `AnalysisLevel=latest`), `bicep build`, `markdownlint-cli2` | pre-commit (staged), `verify`, CI |
| `typecheck`       | `tsc --noEmit` (TS via `app/` build), `dotnet build -warnaserror` (C#)                                                      | `verify`, CI                      |
| `tests`           | `node --test` (app), xunit + Microsoft.NET.Test.Sdk (api)                                                                   | `verify`, CI                      |
| `coverage`        | `c8 --lines=80` (app), coverlet + `scripts/check-dotnet-coverage.mjs` floor 80%                                             | `verify`, CI                      |
| `commit-messages` | `commitlint` + `@commitlint/config-conventional`                                                                            | commit-msg hook, CI PR range      |
| `secrets`         | `secretlint` (recommended preset + pattern rule blocking home-dir/UNC paths)                                                | pre-commit (staged), `verify`, CI |
| `spelling`        | `cspell` (dictionary seeded for this repository)                                                                            | pre-commit (staged), `verify`, CI |
| `ci-verify`       | [`guardrails.yml`](../.github/workflows/guardrails.yml) runs `npm run verify`                                               | CI, required check                |
| `branch-review`   | GitHub branch protection on `main`                                                                                          | host                              |
| `supply-chain`    | Dependabot (npm + nuget + github-actions), `min-release-age=7` in `.npmrc`, CodeQL                                          | host tooling                      |

## The record: `.guardrails.json`

One file at the repository root. Each capability maps to the tool implementing
it, or to `off` with `why` and `who`:

```json
{
  "format": { "tool": "prettier + dotnet format + bicep format" },
  "coverage": { "off": true, "why": "spike repo, throwaway", "who": "mjarvis" }
}
```

Turning a capability off is a human decision. The enforcement is the host's:
bootstrap adds a `CODEOWNERS` line for `.guardrails.json`, so no change to it
merges without a human review. There are no registers, approval scripts, or
provenance checks — the PR is the audit trail.

**An `off` entry is a proposal until it merges.** The decision exists only on
the default branch, which can only be reached through a reviewed PR; an entry
that differs from the default branch's copy is an unratified proposal. An
agent can propose loosening a standard; only the merge ratifies it.

## Exceptions in code

Use the tool's own suppression syntax (`eslint-disable`, `#pragma`,
`noqa`) **with a reason on the same line**, and keep the tool's setting that
requires reasons switched on where it exists.

## Stacks in this repository

- **Node/TypeScript** — `app/` (front-end). Workspaces-managed from the root
  `package.json`. `npm run verify` is the canonical entry point; the
  `app/package.json` `build`/`test` scripts are wired into it.
- **.NET** — `api/` (Functions BFF). Central package management
  (`Directory.Packages.props`), the .NET 10 SDK pinned in `global.json`,
  solution `api/ContosoNotes.slnx`. `dotnet format`, `dotnet build
-warnaserror` and `dotnet test` chain into `verify`.
- **Bicep** — `infra/`. Format drift and `bicep build` warnings are caught by
  `scripts/bicep-format-check.sh` in `verify`.

## Hooks

- `pre-commit` (`npx lint-staged`) — format + lint + secrets + spelling over
  staged files only.
- `commit-msg` (`npx --no-install commitlint --edit "$1"`) — conventional
  commit message.
- `pre-push` (`npm run verify`) — the full chain.

Do not pass `--no-verify` to land work that fails a gate — fix the gate or
fix the work.
