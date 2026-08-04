using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace Contoso.Notes.Api.E2E;

// E2E user journey: a client lists their notes.
// Black-box: starts the real Azure Functions host (func) over the real Api
// project and drives it through HTTP only — no internal hooks. Requires
// Azure Functions Core Tools on PATH (CI installs them).
public class ListNotesJourneyTests
{
    private const int Port = 7348;

    [Fact]
    public async Task Listing_notes_returns_the_seed_notes()
    {
        var projectDir = FindApiProjectDir();
        EnsureLocalSettings(projectDir);

        using var host = StartFunctionsHost(projectDir);
        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{Port}") };

        try
        {
            var response = await WaitForHost(client, host);
            Assert.Equal(200, (int)response.StatusCode);

            var notes = JsonSerializer.Deserialize<List<Note>>(
                await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(notes);
            Assert.Equal(2, notes!.Count);
            Assert.Equal("Buy milk", notes[0].Title);
            Assert.True(notes[1].Done);
        }
        finally
        {
            if (!host.HasExited)
            {
                host.Kill(entireProcessTree: true);
            }
        }
    }

    private static async Task<HttpResponseMessage> WaitForHost(HttpClient client, Process host)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (host.HasExited)
            {
                throw new InvalidOperationException(
                    $"func host exited early with code {host.ExitCode} — is Azure Functions Core Tools installed?");
            }
            try
            {
                return await client.GetAsync("/api/notes");
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
        throw new TimeoutException("func host did not serve /api/notes within the wait window");
    }

    private static Process StartFunctionsHost(string projectDir)
    {
        var start = new ProcessStartInfo
        {
            FileName = "func",
            Arguments = $"start --port {Port}",
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var process = Process.Start(start)
            ?? throw new InvalidOperationException("failed to start func");
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static void EnsureLocalSettings(string projectDir)
    {
        var path = Path.Combine(projectDir, "local.settings.json");
        if (!File.Exists(path))
        {
            File.WriteAllText(
                path,
                """{ "IsEncrypted": false, "Values": { "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated" } }""");
        }
    }

    private static string FindApiProjectDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ContosoNotes.slnx")))
        {
            dir = dir.Parent;
        }
        if (dir is null)
        {
            throw new InvalidOperationException("could not locate the api solution root");
        }
        return Path.Combine(dir.FullName, "src", "Api");
    }

    private sealed record Note(string Id, string Title, bool Done);
}
