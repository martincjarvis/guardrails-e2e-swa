import { test } from "node:test";
import assert from "node:assert/strict";
import { summarise } from "../src/notes.js";

test("summarises open notes", () => {
  const notes = [
    { id: "1", title: "a", done: true },
    { id: "2", title: "b", done: false },
  ];
  assert.equal(summarise(notes), "2 notes, 1 open");
});
