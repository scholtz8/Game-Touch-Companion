using System.Text.Json;

namespace GameTouchCompanion.Core;

public sealed record AppearanceSettings
{
    public string Palette { get; init; } = "slate";
    public string Density { get; init; } = "comfortable";
    public string Order { get; init; } = "navigation";
    public string ConfigurationTheme { get; init; } = "light";
    public bool ShowLabels { get; init; } = true;
    public string Caption { get; init; } = string.Empty;
    public AppearanceSettings Validate()
    {
        if (Palette is not ("slate" or "ocean" or "forest" or "plum") ||
            Density is not ("compact" or "comfortable" or "touch") || Order is not ("navigation" or "home") ||
            ConfigurationTheme is not ("light" or "dark"))
            throw new InvalidDataException("Invalid appearance preset.");
        if (Caption is null || Caption.Length > 40 || Caption.Any(char.IsControl))
            throw new InvalidDataException("Invalid appearance caption.");
        return this;
    }
}

public sealed class AppearanceSettingsStore(string path)
{
    public async Task<AppearanceSettings> LoadAsync()
    {
        try
        {
            await using var file = File.OpenRead(path);
            return (await JsonSerializer.DeserializeAsync<AppearanceSettings>(file) ?? new()).Validate();
        }
        catch (FileNotFoundException) { return new(); }
        catch (DirectoryNotFoundException) { return new(); }
    }
    public async Task SaveAsync(AppearanceSettings settings)
    {
        settings.Validate();
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".appearance." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(file, settings);
                await file.FlushAsync();
            }
            File.Move(temporary, destination, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
