export interface Note {
  id: string;
  title: string;
  done: boolean;
}

export function summarise(notes: Note[]): string {
  const open = notes.filter((n) => !n.done).length;
  return `${notes.length} notes, ${open} open`;
}
