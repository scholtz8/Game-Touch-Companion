using System.Net;

namespace GameTouchCompanion.Core;

public static class BrowserZoomPolicy
{
    public const int MinimumPercent = 50;
    public const int MaximumPercent = 200;
    public const int StepPercent = 10;
    public const int DefaultPercent = 100;

    public static bool IsValidPercent(int percent) =>
        percent is >= MinimumPercent and <= MaximumPercent && percent % StepPercent == 0;

    public static int NormalizePercent(int percent)
    {
        var clamped = Math.Clamp(percent, MinimumPercent, MaximumPercent);
        return (int)Math.Round(clamped / (double)StepPercent, MidpointRounding.AwayFromZero) * StepPercent;
    }

    public static bool TryGetHost(string? url, out string host)
    {
        host = string.Empty;
        if (!BrowserUrlPolicy.TryNormalize(url, out var normalized) ||
            !Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return false;

        return TryNormalizeHost(uri.Host, out host);
    }

    public static bool TryNormalizeHost(string? value, out string host)
    {
        host = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var candidate = value.Trim().TrimEnd('.');
        if (candidate.Length == 0 || candidate.Any(char.IsControl)) return false;

        if (IPAddress.TryParse(candidate, out var address))
        {
            host = address.ToString().ToLowerInvariant();
            return true;
        }

        if (string.Equals(candidate, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            host = "localhost";
            return true;
        }

        if (Uri.CheckHostName(candidate) != UriHostNameType.Dns) return false;
        host = candidate.ToLowerInvariant();
        return true;
    }
}
