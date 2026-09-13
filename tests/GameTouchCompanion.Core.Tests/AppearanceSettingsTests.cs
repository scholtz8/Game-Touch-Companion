using System.Text.Json;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class AppearanceSettingsTests
{
    [Fact]
    public async Task MissingDefaultsRoundtripAndMalformedFileAreSafe()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GtcAppearance-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "appearance.json");
        try
        {
            var store = new AppearanceSettingsStore(path);
            Assert.Equal(new(), await store.LoadAsync());
            var settings = new AppearanceSettings { Palette = "ocean", Density = "touch", Order = "home", Caption = "My Companion", ShowLabels = false };
            await store.SaveAsync(settings);
            Assert.Equal(settings, await store.LoadAsync());
            await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync(settings with { Caption = "invalid\ncaption" }));
            Assert.Equal(settings, await store.LoadAsync());
            await File.WriteAllTextAsync(path, "broken");
            await Assert.ThrowsAsync<JsonException>(() => store.LoadAsync());
            Assert.Equal("broken", await File.ReadAllTextAsync(path));
            Assert.Single(Directory.GetFiles(folder));
        }
        finally { if (File.Exists(path)) File.Delete(path); if (Directory.Exists(folder)) Directory.Delete(folder); }
    }
    [Theory]
    [InlineData("wrong", "compact", "home", "")]
    [InlineData("slate", "tiny", "home", "")]
    [InlineData("slate", "touch", "missing", "")]
    [InlineData("slate", "touch", "home", "a\tb")]
    public void RejectsInvalidPresets(string palette, string density, string order, string caption) =>
        Assert.Throws<InvalidDataException>(() => new AppearanceSettings { Palette = palette, Density = density, Order = order, Caption = caption }.Validate());
    [Fact]
    public void RejectsLongOrNullCaption()
    {
        Assert.Throws<InvalidDataException>(() => new AppearanceSettings { Caption = new string('x', 41) }.Validate());
        Assert.Throws<InvalidDataException>(() => new AppearanceSettings { Caption = null! }.Validate());
    }
}
