#!/usr/bin/env bash
# Multi-stack test runner. Each stack's tests run in its own directory so
# runners and coverage output land in the right place. A failure in any stack
# fails the gate.
set -euo pipefail

echo "==> Node (app) tests with c8 coverage"
# app/ builds TS to dist/ before tests run; c8 scopes to the compiled product
# (dist/src/**), excluding the test build under dist/test. Reports land in
# coverage/ at the repository root so the CI step can publish a summary.
mkdir -p coverage
( cd app && npm run build && ../node_modules/.bin/c8 --check-coverage --lines=80 --per-file \
  --reporter=text --reporter=text-summary \
  --reports-directory=../coverage \
  --include 'dist/src/**/*.js' --exclude 'dist/test/**' \
  node --test "dist/test/notes.test.js" ) | tee coverage/coverage-summary.txt

echo "==> .NET (api) tests with coverlet coverage"
# Floor measured from the in-process test project only — a second test
# project writing cobertura makes the newest-file pick nondeterministic.
dotnet test api/tests/Api.Tests/Api.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings coverage.runsettings
echo "==> .NET coverage floor check"
node scripts/check-dotnet-coverage.mjs

echo "==> E2E journey (real Functions host)"
dotnet test api/tests/Api.E2E/Api.E2E.csproj
