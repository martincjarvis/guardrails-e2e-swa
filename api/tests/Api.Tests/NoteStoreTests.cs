using Contoso.Notes.Api;
using Xunit;

namespace Contoso.Notes.Api.Tests;

public class NoteStoreTests
{
    [Fact]
    public void Summarise_counts_open_notes()
    {
        Assert.Equal("2 notes, 1 open", NoteStore.Summarise());
    }
}
