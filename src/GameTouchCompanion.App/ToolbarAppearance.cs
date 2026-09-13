using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameTouchCompanion.App;

/// <summary>Creates the compact, DPI-independent artwork used by the companion toolbar.</summary>
internal static class ToolbarAppearance
{
    internal static readonly string[] NavigationActions = ["back", "forward", "reload"];
    internal static readonly string[] SavedActions = ["save", "favorites"];

    internal static string Label(string action) => Localization.Get(action switch
    {
        "back" => "Ui066", "forward" => "Ui068", "reload" => "Ui069",
        "save" => "Ui071", "favorites" => "Ui072", "close" => "Ui063",
        "show" => "ShowToolbar", _ => "HideToolbar"
    });

    internal static object Content(string action, bool showLabel, double iconSize)
    {
        var icon = new Image { Source = Icon(action), Width = iconSize, Height = iconSize, Stretch = Stretch.Uniform,
            IsHitTestVisible = false, SnapsToDevicePixels = true };
        if (!showLabel) return icon;
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(icon);
        panel.Children.Add(new TextBlock { Text = Label(action), Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White });
        return panel;
    }

    internal static double IconSize(double buttonSize) => buttonSize switch { >= 64 => 30, <= 44 => 22, _ => 26 };

    private static ImageSource Icon(string action)
    {
        var drawing = action switch
        {
            "reload" => Reload(),
            "back" => CircleArrow(false), "forward" => CircleArrow(true), "save" => Plus(),
            "favorites" => Bookmark(), "close" => Close(), "show" => Eye(true), _ => Eye(false)
        };
        drawing.Freeze(); return drawing;
    }
    private static DrawingImage CircleArrow(bool forward) => Drawing("M12,1 A11,11 0 1 1 12,23 A11,11 0 1 1 12,1", "#3348D8",
        forward ? "M13.5,6 L20,12 L13.5,18 L13.5,14 L5,14 L5,10 L13.5,10 Z" : "M10.5,6 L4,12 L10.5,18 L10.5,14 L19,14 L19,10 L10.5,10 Z", "White");
    private static DrawingImage Reload()
    {
        var green = new SolidColorBrush(Color.FromRgb(105, 199, 85));
        green.Freeze();
        var group = new DrawingGroup();
        var pen = new Pen(green, 3.4)
        {
            StartLineCap = PenLineCap.Flat,
            EndLineCap = PenLineCap.Flat,
            LineJoin = PenLineJoin.Miter
        };
        pen.Freeze();
        var upperArc = Geometry.Parse(
            "M 6.3,5.3 " +
            "A 8.4,8.4 0 0 1 20.1,12.2"
        );
        group.Children.Add(
            new GeometryDrawing(null, pen, upperArc)
        );
        var rightArrow = Geometry.Parse(
            "M 17.0,11.0 " +
            "L 24.0,11.0 " +
            "L 20.4,15.2 " +
            "Z"
        );
        group.Children.Add(
            new GeometryDrawing(green, null, rightArrow)
        );

        var lowerArc = Geometry.Parse(
            "M 17.8,18.6 " +
            "A 8.4,8.4 0 0 1 3.9,11.5"
        );
        group.Children.Add(
            new GeometryDrawing(null, pen, lowerArc)
        );
        var leftArrow = Geometry.Parse(
            "M 7.0,12.8 " +
            "L 0.0,12.8 " +
            "L 3.7,8.6 " +
            "Z"
        );
        group.Children.Add(
            new GeometryDrawing(green, null, leftArrow)
        );
        group.Freeze();
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
    private static DrawingImage Plus() => Drawing("M12,1 A11,11 0 1 1 12,23 A11,11 0 1 1 12,1", "#087AB7",
        "M10,5 L14,5 L14,10 L19,10 L19,14 L14,14 L14,19 L10,19 L10,14 L5,14 L5,10 L10,10 Z", "White");
    private static DrawingImage Bookmark() => Drawing("M5,2 L19,2 L19,21 L12,17 L5,21 Z", "#F8B633",
        "M12,5 L13.8,9 L18,9.4 L14.8,12.1 L15.8,16.2 L12,13.9 L8.2,16.2 L9.2,12.1 L6,9.4 L10.2,9 Z", "White");
    private static DrawingImage Close() => Drawing("M3,3 L21,3 L21,21 L3,21 Z", "#DC2626",
        "M7,5.5 L12,10.5 L17,5.5 L18.5,7 L13.5,12 L18.5,17 L17,18.5 L12,13.5 L7,18.5 L5.5,17 L10.5,12 L5.5,7 Z", "White");
    private static DrawingImage Eye(bool crossed)
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brush("#65B95B"), null, Geometry.Parse("M1,12 C5,5 8,3 12,3 C16,3 19,5 23,12 C19,19 16,21 12,21 C8,21 5,19 1,12 Z")));
        group.Children.Add(new GeometryDrawing(Brush("White"), null, Geometry.Parse("M7,12 A5,5 0 1 0 17,12 A5,5 0 1 0 7,12")));
        group.Children.Add(new GeometryDrawing(Brush("#65B95B"), null, Geometry.Parse("M9,12 A3,3 0 1 0 15,12 A3,3 0 1 0 9,12")));
        if (crossed) group.Children.Add(new GeometryDrawing(null, new Pen(Brush("#E94D4D"), 3.5), Geometry.Parse("M3,3 L21,21")));
        return new DrawingImage(group);
    }
    private static DrawingImage Drawing(string background, string backgroundColor, string foreground, string foregroundColor)
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brush(backgroundColor), null, Geometry.Parse(background)));
        group.Children.Add(new GeometryDrawing(Brush(foregroundColor), null, Geometry.Parse(foreground)));
        return new DrawingImage(group);
    }
    private static Brush Brush(string color) { var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze(); return brush; }
}
