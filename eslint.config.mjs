// Flat config, @eslint/js recommended at zero warnings. typescript-eslint for
// the TypeScript front-end under app/; plain JS recommended elsewhere.
import js from "@eslint/js";
import tseslint from "typescript-eslint";
import globals from "globals";

export default tseslint.config(
  {
    ignores: [
      "**/node_modules/**",
      "**/dist/**",
      "**/coverage/**",
      "**/bin/**",
      "**/obj/**",
      "**/.azure/**",
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ["**/*.{js,mjs,cjs}"],
    languageOptions: {
      ecmaVersion: "latest",
      sourceType: "module",
      globals: { ...globals.node },
    },
  },
  {
    files: ["**/*.ts"],
    languageOptions: {
      ecmaVersion: "latest",
      sourceType: "module",
      globals: { ...globals.node, ...globals.browser },
    },
    rules: {
      // Sensible defaults on; a repository loosens them with a reason, not by
      // never enabling them.
      complexity: ["error", 15],
      "max-depth": ["error", 4],
      "no-unused-expressions": "error",
    },
  },
);
