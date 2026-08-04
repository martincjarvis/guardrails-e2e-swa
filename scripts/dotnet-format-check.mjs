// dotnet format --verify-no-changes exits 0 even when diagnostics are
// emitted (observed on .NET 10 SDK 10.0.302). This wrapper inspects the
// JSON report to fail the gate when formatting drift is present.
// Run as: node scripts/dotnet-format-check.mjs
import { existsSync, mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { execSync } from "node:child_process";

const sln = "api/ContosoNotes.slnx";
const dir = mkdtempSync(join(tmpdir(), "dotnet-format-"));
const report = join(dir, "report.json");

try {
  execSync(
    `dotnet format "${sln}" --verify-no-changes --no-restore --report "${report}"`,
    { stdio: "inherit" },
  );
} catch {
  // exit-code path is unreliable; the report is the source of truth below.
}

if (!existsSync(report)) {
  console.error(
    "dotnet format: no report produced; run `dotnet format` locally to investigate.",
  );
  rmSync(dir, { recursive: true, force: true });
  process.exit(2);
}

const entries = JSON.parse(readFileSync(report, "utf8"));
const count = Array.isArray(entries) ? entries.length : 0;
if (count > 0) {
  console.error(
    `dotnet format drift: ${count} file(s) need formatting. Run: dotnet format ${sln}`,
  );
  rmSync(dir, { recursive: true, force: true });
  process.exit(1);
}

console.log("dotnet format: clean.");
rmSync(dir, { recursive: true, force: true });
