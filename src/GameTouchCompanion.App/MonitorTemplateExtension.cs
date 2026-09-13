using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public sealed class MonitorTemplateExtension : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new MultiBinding { Converter = new LabelConverter() };
        binding.Bindings.Add(new Binding());
        binding.Bindings.Add(new Binding(nameof(Localization.LanguageState.Current)) { Source = Localization.State });
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, binding);
        return new DataTemplate { VisualTree = text };
    }
    private sealed class LabelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => Localization.MonitorLabel(values[0] as MonitorProfile);
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
