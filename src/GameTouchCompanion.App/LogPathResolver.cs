using System.IO;

namespace GameTouchCompanion.App;

internal static class LogPathResolver
{
    public static string Resolve()
    {
        var portable = Path.Combine(AppContext.BaseDirectory, "Logs");
        if (CanWrite(portable)) return portable;
        var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameTouchCompanion", "Logs");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static bool CanWrite(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probe = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}.tmp");
            using (File.Create(probe)) { }
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
