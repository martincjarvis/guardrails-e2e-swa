// .NET coverage floor check. coverlet writes a cobertura file per test run
// under each test project's TestResults/<guid>/coverage.cobertura.xml; this
// script picks the newest, reads the aggregate line-rate, asserts >= 0.80,
// and prints the figure. Run after `dotnet test --collect:"XPlat Code Coverage"`.
import { readFileSync, readdirSync, existsSync, statSync } from "node:fs";
import { join } from "node:path";

const ROOT = "api/tests";
const FLOOR = 0.8;

function latestCobertura(dir) {
  if (!existsSync(dir)) return null;
  const runs = [];
  for (const sub of readdirSync(dir)) {
    if (!/^[0-9a-f-]{36}$/i.test(sub)) continue;
    const p = join(dir, sub, "coverage.cobertura.xml");
    if (existsSync(p)) runs.push({ path: p, mtime: statSync(p).mtimeMs });
  }
  if (runs.length === 0) return null;
  runs.sort((a, b) => b.mtime - a.mtime);
  return readFileSync(runs[0].path, "utf8");
}

function aggregateLineRate(xml) {
  // cobertura <coverage line-rate="0.NNN"> is the project-wide ratio.
  const m = xml.match(/<coverage[^>]*line-rate="([0-9.]+)"/);
  return m ? parseFloat(m[1]) : null;
}

let best = null;
for (const proj of readdirSync(ROOT)) {
  const dir = join(ROOT, proj, "TestResults");
  const xml = latestCobertura(dir);
  if (!xml) continue;
  const rate = aggregateLineRate(xml);
  if (rate !== null && (best === null || rate > best)) best = rate;
}

if (best === null) {
  console.error(
    "coverage: no cobertura report found under api/tests/*/TestResults/",
  );
  process.exit(2);
}
const pct = (best * 100).toFixed(1);
console.log(`Api coverage: ${pct}% (floor ${FLOOR * 100}%)`);
if (best < FLOOR) {
  console.error(`coverage below floor: ${pct}% < ${FLOOR * 100}%`);
  process.exit(1);
}
