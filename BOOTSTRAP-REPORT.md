# Bootstrap report — contoso-notes

Repository: [`martincjarvis/guardrails-e2e-swa`](https://github.com/martincjarvis/guardrails-e2e-swa)
Branch: `guardrails-bootstrap` (one commit `3502df3`), opened against `main`.
Skill: `repository-bootstrap` v0.1.0 from the `forgeboard-guardrails` plugin,
applied unattended.

## 1. Survey (read-only)

| Item                  | Finding                                                                                                                                                                             | Proof                                                                                                                    |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Stacks                | Node/TypeScript, .NET, Bicep                                                                                                                                                        | `app/package.json`, `api/src/Api/Api.csproj`, `infra/main.bicep`                                                         |
| Existing tooling      | None wired. Build/test only (`tsc`, `node --test`, `dotnet test`).                                                                                                                  | `app/package.json:7` (`"build": "tsc"`), no eslint/prettier/husky/lint-staged present                                    |
| Existing hook manager | None                                                                                                                                                                                | `.husky/`, `.git/hooks/` both absent pre-bootstrap                                                                       |
| Host + CI             | GitHub; one deploy-only workflow                                                                                                                                                    | `git remote -v` → `martincjarvis/guardrails-e2e-swa`; `.github/workflows/azure-deploy.yml` (azd on push to main)         |
| Baseline CI state     | The repository had **no verify-quality CI** before bootstrap. The only workflow (`azure-deploy.yml`) deploys on push to main when `vars.AZURE_ENV_NAME` is set — it runs no checks. | Workflow file inspection                                                                                                 |
| Default branch        | `main`; protection status unknown (no GitHub credentials available in this environment)                                                                                             | `git config branch.main.remote` resolves; protection commands recorded in §6                                             |
| Baseline per-stack    | All green: Node build + 1 test pass; .NET build 0 warnings + 1 test pass; `bicep build infra/main.bicep` clean                                                                      | `npm --prefix app run build && npm --prefix app test`, `dotnet test api/tests/Api.Tests/Api.Tests.csproj`, `bicep build` |

## 2. Proposal

Wire all eleven capabilities from `docs/standards.md`. No capability is `off`.
Each stack's existing tool is reused; none is replaced. The Node toolchain is
the runner the repository "leans on" (the app already uses npm); the .NET and
Bicep stacks chain into `npm run verify` so there is **one** entry point.

Per the .NET reference's "newest LTS" guidance, the .NET stack is upgraded
from `net8.0` to `net10.0` (.NET 10 is the current LTS, SDK 10.0.302 is
installed). The xunit test framework is kept (not migrated to TUnit) per
"keep an existing suite's framework." Central package management
(`Directory.Packages.props`) is introduced; `.csproj` files now carry
`<PackageReference>` without `Version`.

Tool versions resolved on the day of bootstrap (today, not from memory):

| Tool                | Version  |
| ------------------- | -------- |
| prettier            | 3.9.6    |
| eslint              | 10.8.0   |
| typescript-eslint   | 8.66.0   |
| typescript          | 6.0.3    |
| cspell              | 10.0.1   |
| secretlint          | 13.0.4   |
| markdownlint-cli2   | 0.23.2   |
| c8                  | 12.0.0   |
| @commitlint/cli     | 21.2.1   |
| lint-staged         | 17.3.0   |
| husky               | 9.1.7    |
| @opencode-ai/plugin | 1.18.8   |
| Bicep CLI           | 0.46.1   |
| .NET SDK            | 10.0.302 |

## 3. Capabilities wired

Every capability maps to a tool in [`.guardrails.json`](.guardrails.json).
Stack-neutral capabilities are wired the same way across Node, .NET and Bicep.

| Capability        | Tool                                                                                                                                                                                                                   | Notes                                                                                                                         |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `format`          | `prettier` (TS/JS/JSON/YAML/MD) + `dotnet format` (C#) + `bicep format`                                                                                                                                                | Auto-fix in pre-commit (lint-staged runs `prettier --write`); `--check`/`--verify-no-changes` in verify                       |
| `lint`            | `eslint` + `typescript-eslint` (TS/JS), .NET SDK analysers (`TreatWarningsAsErrors` + `AnalysisLevel=latest` in `Directory.Build.props`), `bicep build`, `markdownlint-cli2` (with `markdownlint-rule-relative-links`) | Zero warnings enforced                                                                                                        |
| `typecheck`       | `tsc` via `app/` build (no `--noEmit` because the build is the typecheck for this stack), `dotnet build -warnaserror`                                                                                                  |                                                                                                                               |
| `tests`           | `node --test` (app), xunit + `Microsoft.NET.Test.Sdk` (api)                                                                                                                                                            | xunit kept per "existing framework wins"                                                                                      |
| `coverage`        | `c8 --lines=80` (app) + coverlet XPlat Code Coverage with `scripts/check-dotnet-coverage.mjs` floor at 80% (api)                                                                                                       | `coverage.runsettings` scopes coverlet to the product assembly; `Program.cs` and source-generated `*.g.cs` excluded           |
| `commit-messages` | `commitlint` + `@commitlint/config-conventional`                                                                                                                                                                       | Body/footer line-length rules relaxed so Dependabot's release-note bodies do not fail the gate                                |
| `secrets`         | `secretlint` (recommended preset + pattern rule blocking home-dir/UNC paths)                                                                                                                                           | Staged via lint-staged; full-tree sweep in `verify`                                                                           |
| `spelling`        | `cspell`                                                                                                                                                                                                               | Dictionary seeded with project terms (`contoso`, `slnx`, `warnaserror`, `opencode`, `nothrow`, etc.) so the gate starts green |
| `ci-verify`       | [`guardrails.yml`](.github/workflows/guardrails.yml) running `npm run verify` on every PR                                                                                                                              | Same command developers run; the existing `azure-deploy.yml` is untouched (deploy pipeline, not a guardrail)                  |
| `branch-review`   | GitHub branch protection on `main`                                                                                                                                                                                     | Exact `gh` commands in §6 (no creds in this environment)                                                                      |
| `supply-chain`    | Dependabot (npm + nuget + github-actions), `min-release-age=7` + `audit-level=high` in [`.npmrc`](.npmrc), CodeQL default setup                                                                                        | Backup tools noted in §6 if host toggles cannot be applied                                                                    |

## 4. Gates demonstrated failing then passing

Per the skill: every gate is shown to refuse a bad input before it is claimed
wired. Each demonstration ran against the post-bootstrap state.

### 4.1 Format gate (prettier + dotnet format + bicep format)

**Failing case** — hand-mis-formatted file:

````text
$ printf 'export const x=1;\nconst y={a:1,b:2,c:3};\nvoid y;\n' > app/src/scratch.ts
$ npx prettier --check app/src/scratch.ts
Checking formatting...
[warn] app/src/scratch.ts
[warn] Code style issues found in the above file. Run Prettier with --write to fix.
PRETTIER_EXIT=1
```text
The .NET formatter (`scripts/dotnet-format-check.mjs`) on a mis-formatted C#
file, **including** a regression in the .NET 10 SDK where the underlying
`dotnet format --verify-no-changes` exits 0 even when diagnostics are emitted
(the wrapper inspects the JSON report and exits 1):
```text
$ node scripts/dotnet-format-check.mjs
api/src/Api/Scratch.cs(5,23): error WHITESPACE: Fix whitespace formatting. Insert '\s'.
api/src/Api/Scratch.cs(5,19): error CS8618: Non-nullable property 'Name' must ...
dotnet format drift: 2 file(s) need formatting. Run: dotnet format api/ContosoNotes.slnx
EXIT=1
```text

**Passing case** — `npm run verify` exits 0 with no format drift.

### 4.2 Lint gate (eslint, with complexity + depth rules on)

**Failing case** — TypeScript file with deep nesting and unused vars:
```text
$ git commit -m "test: scratch for gate demonstration"
… Running tasks for staged files…
  *.{js,mjs,cjs,ts,tsx,jsx} — 1 file
  ✖ eslint --max-warnings 0 --no-warn-ignored
app/src/scratch.ts
  3:7   error  'z' is assigned a value but never used                  @typescript-eslint/no-unused-vars
  8:11  error  Blocks are nested too deeply (5). Maximum allowed is 4  max-depth
  9:23  error  Empty block statement                                   no-empty
✖ 9 problems (9 errors, 0 warnings)
husky - pre-commit script failed (code 1)
```text
**Passing case** — the file removed, the commit proceeds.

### 4.3 Commit-message gate

**Failing case** — non-conventional message:
```text
$ git commit --allow-empty -m "wip"
✖   subject may not be empty [subject-empty]
✖   type may not be empty [type-empty]
✖   found 2 problems, 0 warnings
husky - commit-msg script failed (code 1)
```text
**Passing case** — `git commit --allow-empty -m "chore: ..."` succeeds.

### 4.4 Secrets gate

**Failing case** — random AWS-shaped secret in JSON:
<!-- secretlint-disable -->
```text
$ printf '{ "aws_secret_access_key": "9m2vFqz8jQy5G7cR6pW1eTbYs4uHkD3iZxCvN8oL" }\n' > scratch-secret.json
$ git commit -m "test: fake aws secret for gate demo"
  ✖ secretlint --secretlintrc .secretlintrc.json --secretlintignore .secretlintignore
scratch-secret.json
  2:2  error  [AWSSecretAccessKey] found AWS Secret Access Key: ***  @secretlint/secretlint-rule-preset-recommend > @secretlint/secretlint-rule-aws
✖ 1 problem (1 error, 0 warnings, 0 infos)
husky - pre-commit script failed (code 1)
```
<!-- secretlint-enable -->text
> Note: secretlint's AWS rule recognises the well-known documentation sample
> key (`AKIAIOSFODNN7EXAMPLE`/`wJalrXUt…EXAMPLEKEY`) and treats it as a known
> fake. The demonstration uses a randomly-generated key shape, as a real
> attacker would. Cspell also blocks the AKIA prefix as a dictionary miss,
> defence-in-depth.

**Passing case** — file removed, the commit proceeds.

### 4.5 Tests + coverage gate

**Failing case** — assertion changed to expected value `"99 notes, 1 open"`
in `NoteStoreTests.cs`:
```text
$ npm run verify
…
[xUnit.net 00:00:00.16] Contoso.Notes.Api.Tests.NoteStoreTests.Summarise_counts_open_notes [FAIL]
  Expected: "99 notes, 1 open"
  Actual:   "2 notes, 1 open"
Failed! - Failed: 1, Passed: 2, Skipped: 0, Total: 3
EXIT=1
```text
**Coverage floor failure (separate earlier run)** — with only the original
single test, .NET coverage was 53.8%:
```text
$ node scripts/check-dotnet-coverage.mjs
Api coverage: 53.8% (floor 80%)
coverage below floor: 53.8% < 80%
```text
Bootstrap closed the gap with three mechanical unit tests
(`NoteStore.All()`, and `NotesFunction.Run()` driven by a minimal
in-repo `FakeHttpRequestData` stub — `api/tests/Api.Tests/TestSupport/`).
Coverage is now 100% on the product code, well above the 80% floor.

**Passing case** — assertion restored; `npm run verify` prints:
```text
==> .NET coverage floor check
Api coverage: 100.0% (floor 80%)
==> verify OK
FINAL_EXIT=0
```text

### 4.6 Pre-push / full verify

**Failing case** — same broken test, `bash .husky/pre-push` runs the chain and
exits non-zero, refusing the push.

**Passing case** — `bash scripts/verify.sh` from a clean tree prints
`==> verify OK` and exits 0.

### 4.7 Coverage summary published for CI

The CI workflow appends `coverage/coverage-summary.txt` (written by
`scripts/test-all.sh`) to `$GITHUB_STEP_SUMMARY`. The .NET coverage figure is
printed to stdout by `scripts/check-dotnet-coverage.mjs`.

## 5. Baseline CI state — before and after

| | Before | After |
| --- | --- | --- |
| Workflows | `azure-deploy.yml` (deploy only, gated on `vars.AZURE_ENV_NAME`) | `azure-deploy.yml` unchanged + `guardrails.yml` (verify on PR) + `dependabot.yml` + `dependabot-auto-merge.yml` |
| Required checks on `main` | none | `guardrails/verify` (commands in §6) |
| Local gates | none | pre-commit (format+lint+secrets+spelling on staged), commit-msg (conventional), pre-push (full verify) |

## 6. Remaining gaps — exact remote commands

The environment that ran this bootstrap has no GitHub credentials and no
admin permission on `martincjarvis/guardrails-e2e-swa`. These one-time
commands are the repository owner's to run; nothing in the tree depends on
them, but until they run, three capabilities (`branch-review`, the host side
of `supply-chain`, and CI parity) are wired only on the local machine.

### 6.1 Push the branch and open the PR

```bash
git push -u origin guardrails-bootstrap
gh pr create --base main --head guardrails-bootstrap \
  --title "chore: bootstrap repository guardrails" \
  --body-file BOOTSTRAP-REPORT.md
```text

### 6.2 Branch protection on `main` (capability: `branch-review`)

`enforce_admins: true` is mandatory — without it the owner bypasses
everything and so does any agent running with the owner's token.

```bash
gh api -X PUT repos/martincjarvis/guardrails-e2e-swa/branches/main/protection \
  -F required_status_checks[strict]=true \
  -F required_status_checks[contexts][]="guardrails/verify" \
  -F enforce_admins=true \
  -F required_pull_request_reviews=null \
  -F restrictions=null \
  -F allow_force_pushes=false
```text

> Solo-maintainer note: `required_pull_request_reviews=null` is deliberate —
> GitHub refuses self-approval, so a required review deadlocks a one-account
> repository. PR-only + required `guardrails/verify` + `enforce_admins` +
> linear history keeps ratification intact; only the second pair of eyes is
> lost, and that is a fact about the team, not the tooling.

### 6.3 Supply-chain host toggles

```bash
# Advisories + automated security fixes
gh api -X PUT repos/martincjarvis/guardrails-e2e-swa/vulnerability-alerts
gh api -X PUT repos/martincjarvis/guardrails-e2e-swa/automated-security-fixes

# CodeQL default setup (free on public repositories)
gh api -X PATCH repos/martincjarvis/guardrails-e2e-swa/code-scanning/default-setup \
  -f state=configured

# Allow auto-merge (used by .github/workflows/dependabot-auto-merge.yml)
gh api -X PATCH repos/martincjarvis/guardrails-e2e-swa \
  -F allow_auto_merge=true

# Secret scanning + push protection
gh api -X PATCH repos/martincjarvis/guardrails-e2e-swa \
  -f security_and_analysis[secret_scanning][status]=enabled \
  -f security_and_analysis[secret_scanning_push_protection][status]=enabled
```text

If any of these cannot be applied (private repo without Advanced Security),
the backup tools named in `references/shared.md` are the fallback:
`osv-scanner` for advisories, `semgrep` for SAST, `lizard` for complexity.
None is wired here because GitHub-native tooling is strictly better when
available.

### 6.4 Verify the CI check goes green

After the PR is opened:

```bash
gh pr checks --watch         # wait for guardrails/verify to report
gh pr merge --squash --delete-branch   # once green and reviewed
```text

The `azure-deploy.yml` workflow is unchanged; it stays deploy-only and is not
required by branch protection (deploy pipelines are `deployment-review`'s
checklist, not a guardrail).

## 7. Notes for the reviewer

- **`.NET` upgrade `net8.0` → `net10.0`.** Driven by the reference's
  "newest LTS" guidance. .NET 10 SDK is installed (`global.json` pins
  `10.0.302`). The Functions isolated worker builds clean against it.
- **`api/ContosoNotes.slnx`** is the .NET 10 default solution format
  (`dotnet new sln` now emits `.slnx`, not `.sln`). All `dotnet` commands
  accept it.
- **`dotnet format` exit-code regression.** On SDK 10.0.302,
  `dotnet format --verify-no-changes` exits 0 even when formatting drift is
  present. `scripts/dotnet-format-check.mjs` reads the JSON report and
  enforces the gate; revisit when the SDK fixes the exit code.
- **`FakeHttpRequestData` test stub.** The Azure Functions worker has no
  public test harness for `HttpRequestData`, so the bootstrap ships a
  60-line in-repo stub under `api/tests/Api.Tests/TestSupport/`. It builds a
  real worker `HostBuilder` so `WriteAsJsonAsync` resolves the
  `ObjectSerializer` the runtime expects. Replace with `Aspire.Hosting.Testing`
  when the integration tier lands.
- **opencode agent plugin.** Format-on-edit is wired as a project-scoped
  plugin at `.opencode/plugin/format-on-edit.ts` (post-tool hook on
  `edit`/`write` running prettier). **verify-at-stop is not wired** because
  opencode's plugin hook surface does not expose a session-stop event; the
  pre-push git hook (`.husky/pre-push` running `npm run verify`) is the
  enforcement point. Claude Code's `.claude/settings.json` equivalent is
  not added — this repository's contributors use opencode.
- **`docs/guardrails.md`** is the project-local copy of the plugin's
  `standards.md`, rewritten to cite this repository's actual tools. No link
  in any file under `/workspaces/e2e-swa` requires the plugin to be installed
  to read.

## 8. Files added/changed by this bootstrap

```text
added:     .editorconfig .gitattributes .guardrails.json .lintstagedrc.json
added:     .markdownlint-cli2.jsonc .npmrc .opencode/plugin/format-on-edit.ts
added:     .prettierrc.json .secretlintignore .secretlintrc.json
added:     AGENTS.md CODEOWNERS Directory.Build.props Directory.Packages.props
added:     api/ContosoNotes.slnx api/tests/Api.Tests/TestSupport/FakeHttpRequestData.cs
added:     commitlint.config.cjs coverage.runsettings cspell.json docs/guardrails.md
added:     eslint.config.mjs global.json opencode.json package.json package-lock.json
added:     scripts/bicep-format-check.sh scripts/check-dotnet-coverage.mjs
added:     scripts/dotnet-format-check.mjs scripts/test-all.sh scripts/verify.sh
added:     .github/workflows/dependabot.yml .github/workflows/dependabot-auto-merge.yml
added:     .github/workflows/guardrails.yml
added:     .husky/pre-commit .husky/commit-msg .husky/pre-push
modified:  .gitignore
modified:  api/src/Api/Api.csproj                (CPM, TargetFramework centralised)
modified:  api/tests/Api.Tests/Api.Tests.csproj  (CPM, coverlet added)
modified:  api/tests/Api.Tests/NoteStoreTests.cs (closed coverage gap)
```text

`npm run verify` is the source of truth. Run it before claiming work done.
````
