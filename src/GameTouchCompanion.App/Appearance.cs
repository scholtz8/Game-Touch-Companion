using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public sealed class Appearance : INotifyPropertyChanged
{
    public static Appearance Current { get; } = new();
    public static AppearanceSettingsStore CreateStore() => new(Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData), "GameTouchCompanion", "appearance.json"));
    public AppearanceSettings Settings { get; private set; } = new();
    public bool LoadFailed { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public Brush Background => Color(Settings.Palette switch { "ocean" => "#082F49", "forest" => "#052E25", "plum" => "#3B153F", _ => "#0F172A" });
    public Brush Surface => Color(Settings.Palette switch { "ocean" => "#0C4A6E", "forest" => "#064E3B", "plum" => "#581C63", _ => "#1E293B" });
    public Brush ButtonBackground => Color(Settings.Palette switch { "ocean" => "#BAE6FD", "forest" => "#A7F3D0", "plum" => "#F5D0FE", _ => "#E2E8F0" });
    public double ButtonSize => Settings.Density switch { "compact" => 44, "touch" => 64, _ => 52 };
    public double FontSize => Settings.Density switch { "compact" => 14, "touch" => 20, _ => 16 };
    public Brush ConfigurationBackground => Color(Settings.ConfigurationTheme == "dark" ? "#111827" : "#FFFFFF");
    public Brush ConfigurationSurface => Color(Settings.ConfigurationTheme == "dark" ? "#1F2937" : "#F1F5F9");
    public Brush ConfigurationControl => Color(Settings.ConfigurationTheme == "dark" ? "#374151" : "#FFFFFF");
    public Brush ConfigurationForeground => Color(Settings.ConfigurationTheme == "dark" ? "#F9FAFB" : "#0F172A");
    public Brush ConfigurationSecondaryForeground => Color(Settings.ConfigurationTheme == "dark" ? "#CBD5E1" : "#475569");
    public Brush ConfigurationBorder => Color(Settings.ConfigurationTheme == "dark" ? "#4B5563" : "#CBD5E1");
    public Brush ConfigurationNotice => Color(Settings.ConfigurationTheme == "dark" ? "#3F3212" : "#FFFBEB");
    public Brush ConfigurationNoticeForeground => Color(Settings.ConfigurationTheme == "dark" ? "#FDE68A" : "#92400E");
    public Brush ConfigurationError => Color(Settings.ConfigurationTheme == "dark" ? "#3F1D25" : "#FEE2E2");
    public Brush ConfigurationErrorForeground => Color(Settings.ConfigurationTheme == "dark" ? "#FCA5A5" : "#991B1B");
    public void Apply(AppearanceSettings settings)
    {
        Settings = settings.Validate();
        LoadFailed = false;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
    public async Task InitializeAsync()
    {
        try { Apply(await CreateStore().LoadAsync()); }
        catch (Exception) { LoadFailed = true; }
    }
    private static Brush Color(string hex)
    {
        var brush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze(); return brush;
    }
}
