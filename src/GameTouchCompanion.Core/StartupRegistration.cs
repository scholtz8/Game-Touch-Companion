namespace GameTouchCompanion.Core;

public sealed record StartupRegistration(bool Exists, bool MatchesCurrentExecutable);

public interface IStartupRegistrationService
{
    StartupRegistration Read();
    void SetEnabled(bool enabled, bool replaceExisting = false);
}

public static class StartupCommand
{
    public static string Create(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || executablePath.Contains('"') ||
            !Path.IsPathFullyQualified(executablePath) ||
            !string.Equals(Path.GetFileName(executablePath), "GameTouchCompanion.App.exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Se requiere la ruta absoluta del ejecutable GameTouchCompanion.App.exe.", nameof(executablePath));
        var command = $"\"{executablePath}\" --startup";
        if (command.Length > 260) throw new ArgumentException("La ruta supera el límite de inicio de Windows.", nameof(executablePath));
        return command;
    }
}
