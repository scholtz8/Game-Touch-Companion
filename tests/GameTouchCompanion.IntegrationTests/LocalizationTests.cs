using System.Collections;
using System.Globalization;
using System.Resources;
using GameTouchCompanion.App;

namespace GameTouchCompanion.IntegrationTests;

public sealed class LocalizationTests
{
    [Fact]
    public void SpanishAndEnglishHaveIdenticalNonemptyKeys()
    {
        var resources = new ResourceManager("GameTouchCompanion.App.Resources.Strings", typeof(Localization).Assembly);
        var spanish = resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var english = resources.GetResourceSet(CultureInfo.GetCultureInfo("en"), true, false)!;
        var es = spanish.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
        var en = english.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
        Assert.Equal(es.Keys.Order(), en.Keys.Order());
        Assert.True(es.Count >= 120);
        Assert.All(es.Values.Concat(en.Values), value => Assert.False(string.IsNullOrWhiteSpace(value)));
    }

    [Fact]
    public void LookupUsesSelectedLanguageAndRejectsMissingKeys()
    {
        Assert.Equal("Ajustes", Localization.Get("Ui033", "es"));
        Assert.Equal("Settings", Localization.Get("Ui033", "en"));
        Assert.Equal("Close Companion", Localization.Get("Ui063", "en"));
        Assert.Throws<MissingManifestResourceException>(() => Localization.Get("UnknownKey"));
    }

    [Fact]
    public void TranslatedFormatsPreserveArgumentCounts()
    {
        var resources = new ResourceManager("GameTouchCompanion.App.Resources.Strings", typeof(Localization).Assembly);
        var spanish = resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        foreach (DictionaryEntry entry in spanish)
        {
            var source = (string)entry.Value!;
            var translated = Localization.Get((string)entry.Key, "en");
            Assert.Equal(System.Text.CompositeFormat.Parse(source).MinimumArgumentCount,
                System.Text.CompositeFormat.Parse(translated).MinimumArgumentCount);
        }
    }
}
