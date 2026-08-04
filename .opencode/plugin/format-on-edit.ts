// opencode plugin: format-on-edit. Runs prettier on every file the agent
// edits, so commit-time format refusals become rare. The Claude Code
// equivalent lives in templates/claude-hooks.json; this is opencode's native
// shape for the same behaviour.
//
// verify-at-stop is not wired here: opencode's plugin hook surface does not
// currently expose a session-stop event. The pre-push git hook
// (`.husky/pre-push` running `npm run verify`) is the enforcement point —
// any failure is caught before code reaches the remote.
import type { Plugin } from "@opencode-ai/plugin";

const PRETTIER_EXTENSIONS = [
  ".js",
  ".mjs",
  ".cjs",
  ".ts",
  ".tsx",
  ".jsx",
  ".json",
  ".jsonc",
  ".yml",
  ".yaml",
  ".md",
  ".mdx",
  ".css",
  ".scss",
  ".html",
];

function isPrettierTarget(path: string): boolean {
  return PRETTIER_EXTENSIONS.some((ext) => path.endsWith(ext));
}

function extractPaths(args: unknown): string[] {
  if (!args || typeof args !== "object") return [];
  const a = args as Record<string, unknown>;
  const candidates: unknown[] = [
    a.filePath,
    a.path,
    a.file_path,
    a.paths,
    a.file_paths,
  ];
  const out: string[] = [];
  for (const c of candidates) {
    if (typeof c === "string") out.push(c);
    else if (Array.isArray(c))
      out.push(...c.filter((x): x is string => typeof x === "string"));
  }
  return out;
}

export default (async ({ $ }) => {
  return {
    "tool.execute.after": async (input) => {
      const tool = input?.tool;
      if (tool !== "edit" && tool !== "write" && tool !== "multi_edit") return;
      const targets = extractPaths(input?.args).filter((p) =>
        isPrettierTarget(p),
      );
      if (targets.length === 0) return;
      const result = await $`npx --no-install prettier --write ${targets}`
        .quiet()
        .nothrow();
      if (result.exitCode !== 0) {
        // Format failure must not block the agent's edit; the gate does that.
        console.error(
          "prettier format-on-edit failed:",
          result.stderr.toString(),
        );
      }
    },
  };
}) satisfies Plugin;
