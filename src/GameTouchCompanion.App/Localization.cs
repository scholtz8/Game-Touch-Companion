using System.Globalization;
using System.IO;
using System.Resources;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public static partial class Localization
{
    public sealed class LanguageState : INotifyPropertyChanged
    {
        public string Current => Language;
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
    }
    public static LanguageState State { get; } = new();
    private static readonly ResourceManager Resources = new("GameTouchCompanion.App.Resources.Strings", typeof(Localization).Assembly);
    public static string Language { get; private set; } = "es";
    public static LanguagePreferenceStore CreateStore() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameTouchCompanion", "language.json"));
    public static void Initialize(string language)
    {
        if (!LanguagePreferenceStore.IsSupported(language)) throw new ArgumentException("Unsupported language.", nameof(language));
        Language = language;
        State.Notify();
    }
    public static string Get(string key, string? language = null) =>
        Resources.GetString(key, CultureInfo.GetCultureInfo(language ?? Language))
        ?? throw new MissingManifestResourceException($"Missing UI resource: {key}");

    public static string T(string source) => DynamicKeys.TryGetValue(source, out var key) ? Get(key) : source;
    public static string F(FormattableString message) => string.Format(CultureInfo.GetCultureInfo(Language), T(message.Format), message.GetArguments());
    public static string MonitorIssue(MonitorSelectionIssue issue)
    {
        if (issue.FormattedMessage is not { } message) return T(issue.Message);
        if (message.Format == "The configured {0} monitor {1} is unavailable; using {2}.")
        {
            var args = message.GetArguments().ToArray();
            args[0] = T((string)args[0]!);
            return string.Format(CultureInfo.GetCultureInfo(Language), T(message.Format), args);
        }
        return F(message);
    }
    public static string MonitorLabel(MonitorProfile? monitor) => monitor is null ? T("no seleccionado") :
        $"{monitor.DeviceName} — {monitor.Bounds.Width} × {monitor.Bounds.Height} @ ({monitor.Bounds.X}, {monitor.Bounds.Y})" +
        (monitor.IsPrimary ? Get("PrimaryDisplaySuffix") : string.Empty);
    public static Binding Binding(Func<string> render) => new(nameof(LanguageState.Current))
    {
        Source = State, Mode = BindingMode.OneWay, Converter = new RenderConverter(render)
    };
    public static Binding ObjectBinding(Func<object> render) => new(nameof(LanguageState.Current))
    {
        Source = State, Mode = BindingMode.OneWay, Converter = new ObjectRenderConverter(render)
    };
    public static void Text(TextBlock target, Func<string> render) => BindingOperations.SetBinding(target, TextBlock.TextProperty, Binding(render));
    private sealed class RenderConverter(Func<string> render) : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => render();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
    private sealed class ObjectRenderConverter(Func<object> render) : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => render();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}

[MarkupExtensionReturnType(typeof(object))]
public sealed class TrExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => Localization.Binding(() => Localization.Get(key)).ProvideValue(serviceProvider);
}

/// <summary>Retains message identity/arguments, never translates user data or reconstructs it from rendered text.</summary>
public sealed record LocalizedMessage(Func<string> Render)
{
    public static implicit operator LocalizedMessage(string source) => new(() => Localization.T(source));
    public static LocalizedMessage Format(FormattableString source) => new(() => Localization.F(source));
}
