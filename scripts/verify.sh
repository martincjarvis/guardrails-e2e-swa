#!/usr/bin/env bash
# The repository's one verify entry point. CI and the pre-push hook run the
# same command; CI exists to catch what `--no-verify` skipped, not to be the
# first place checks run. Order: format-check, then lint (so lint judges
# formatted bytes), then typecheck, then tests with coverage.
set -euo pipefail

echo "==> restore (.NET packages)"
dotnet restore api/ContosoNotes.slnx

echo "==> format:check (prettier, dotnet format, bicep format)"
npm run format:check
node scripts/dotnet-format-check.mjs
bash scripts/bicep-format-check.sh

echo "==> lint (eslint, markdownlint, cspell, secretlint)"
npm run lint

echo "==> typecheck (tsc + dotnet build)"
npm run typecheck
dotnet build api/ContosoNotes.slnx -warnaserror --no-incremental

echo "==> test (Node + .NET, with coverage)"
bash scripts/test-all.sh

echo "==> verify OK"
