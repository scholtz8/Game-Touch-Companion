using System.Text.Json;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class LanguagePreferenceStoreTests
{
    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GtcLanguageTest-" + Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public void Dispose()
        {
            // Only this test's files and one empty child directory are created here.
            foreach (var file in Directory.GetFiles(Path)) File.Delete(file);
            foreach (var child in Directory.GetDirectories(Path)) Directory.Delete(child);
            Directory.Delete(Path);
        }
    }
    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    public async Task MissingPreferenceThenRoundtrip(string language)
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "language.json");
        var store = new LanguagePreferenceStore(path);
        Assert.Null(await store.LoadAsync());
        await store.SaveAsync(language);
        Assert.Equal(language, await new LanguagePreferenceStore(path).LoadAsync());
        Assert.Single(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task InvalidChoiceAndCancellationPreservePreviousFile()
    {
        using var directory = new TemporaryDirectory();
        var store = new LanguagePreferenceStore(Path.Combine(directory.Path, "language.json"));
        await store.SaveAsync("en");
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync("fr"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync("es", new CancellationToken(true)));
        Assert.Equal("en", await store.LoadAsync());
    }

    [Theory]
    [InlineData("{\"Language\":\"fr\"}")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task UnknownOrAbsentLanguageRequiresChoice(string json)
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "language.json");
        await File.WriteAllTextAsync(path, json);
        Assert.Null(await new LanguagePreferenceStore(path).LoadAsync());
        Assert.Equal(json, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task MalformedFileIsNotOverwrittenAndFailedWriteDoesNotLeaveTemporaryFiles()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "language.json");
        await File.WriteAllTextAsync(path, "broken");
        await Assert.ThrowsAsync<JsonException>(() => new LanguagePreferenceStore(path).LoadAsync());
        Assert.Equal("broken", await File.ReadAllTextAsync(path));
        var occupiedPath = Path.Combine(directory.Path, "occupied");
        Directory.CreateDirectory(occupiedPath);
        var error = await Record.ExceptionAsync(() => new LanguagePreferenceStore(occupiedPath).SaveAsync("en"));
        Assert.True(error is IOException or UnauthorizedAccessException);
        Assert.Empty(Directory.GetFiles(directory.Path, ".language.*.tmp"));
    }
}
