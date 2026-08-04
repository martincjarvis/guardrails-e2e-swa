#!/usr/bin/env bash
# Bicep format + lint gate. `bicep lint` writes findings to stderr; `bicep
# build` is the validator (a parse failure = a build failure). This script
# reformats a temp copy and diffs against the checked-in file so a formatting
# drift fails the gate without mutating the working tree.
set -euo pipefail

shopt -s nullglob
exit_code=0

for f in infra/*.bicep; do
  tmp="$(mktemp)"
  bicep format "$f" --stdout > "$tmp"
  if ! diff -u "$f" "$tmp"; then
    echo "bicep format drift in $f — run: bicep format $f --outfile $f" >&2
    exit_code=1
  fi
  rm -f "$tmp"

  # `bicep build` is the canonical validate; emits warnings/errors to stderr.
  if ! bicep build "$f" --outfile "$(mktemp)" >/dev/null 2>/tmp/bicep-lint.err; then
    echo "bicep build failed for $f:" >&2
    cat /tmp/bicep-lint.err >&2
    exit_code=1
  fi
  # Surface warnings too — `bicep build` succeeds with warnings, the gate must not.
  if [ -s /tmp/bicep-lint.err ]; then
    echo "bicep warnings for $f:" >&2
    cat /tmp/bicep-lint.err >&2
    exit_code=1
  fi
done

exit "$exit_code"
