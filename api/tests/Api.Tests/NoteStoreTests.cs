using Contoso.Notes.Api;
using Contoso.Notes.Api.Tests.TestSupport;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Contoso.Notes.Api.Tests;

public class NoteStoreTests
{
    [Fact]
    public void Summarise_counts_open_notes()
    {
        Assert.Equal("2 notes, 1 open", NoteStore.Summarise());
    }

    [Fact]
    public void All_returns_seed_notes()
    {
        var notes = NoteStore.All();
        Assert.Equal(2, notes.Count);
        Assert.Contains(notes, n => n.Title == "Buy milk");
    }
}

public class NotesFunctionTests
{
    [Fact]
    public async Task Run_returns_all_notes_as_json()
    {
        var context = new FakeFunctionContext();
        var request = new FakeHttpRequestData(context);
        var function = new NotesFunction();

        var response = await function.Run(request);

        response.Body.Position = 0;
        var document = await JsonDocument.ParseAsync(response.Body);
        Assert.Equal(2, document.RootElement.GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
