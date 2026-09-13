namespace GameTouchCompanion.Core;

public sealed record MonitorPreviewItem(MonitorProfile Monitor, double X, double Y, double Width, double Height);

/// <summary>Uniform schematic transform only; never use these coordinates for window placement.</summary>
public static class MonitorPreviewLayout
{
    public static IReadOnlyList<MonitorPreviewItem> Create(IEnumerable<MonitorProfile> monitors, double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0) return [];
        var valid = monitors.Where(m => m.Bounds.Width > 0 && m.Bounds.Height > 0).ToArray();
        if (valid.Length == 0) return [];
        double left = valid.Min(m => (double)m.Bounds.X), top = valid.Min(m => (double)m.Bounds.Y);
        double right = valid.Max(m => (double)m.Bounds.X + m.Bounds.Width);
        double bottom = valid.Max(m => (double)m.Bounds.Y + m.Bounds.Height);
        var scale = Math.Min(width / (right - left), height / (bottom - top));
        var offsetX = (width - (right - left) * scale) / 2;
        var offsetY = (height - (bottom - top) * scale) / 2;
        return valid.Select(m => new MonitorPreviewItem(m,
            offsetX + (m.Bounds.X - left) * scale, offsetY + (m.Bounds.Y - top) * scale,
            m.Bounds.Width * scale, m.Bounds.Height * scale)).ToArray();
    }
}
