namespace Contoso.Notes.Api;

public record Note(string Id, string Title, bool Done);

public static class NoteStore
{
    private static readonly List<Note> Notes =
    [
        new("1", "Buy milk", false),
        new("2", "Ship guardrails", true),
    ];

    public static IReadOnlyList<Note> All() => Notes;

    public static string Summarise() =>
        $"{Notes.Count} notes, {Notes.Count(n => !n.Done)} open";
}
