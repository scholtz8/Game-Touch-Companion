namespace GameTouchCompanion.Core;

public static class GameProfileValidation
{
    public static GameProfile Normalize(GameProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.Id) || profile.Id.Length > 80 ||
            profile.Id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
            throw new InvalidDataException("El identificador del perfil no es válido.");
        var name = profile.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120 || name.Any(char.IsControl))
            throw new InvalidDataException("El nombre debe contener entre 1 y 120 caracteres sin controles.");
        var process = profile.ProcessName?.Trim() ?? string.Empty;
        if (process.Length > 200 || (process.Length > 0 &&
            (!process.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || process.Length <= 4 ||
             process.Any(c => char.IsControl(c) || "\\/:*?\"<>|".Contains(c)))))
            throw new InvalidDataException("Usa solo el nombre del ejecutable terminado en .exe, sin ruta ni argumentos.");
        if (profile.AutoLaunch && process.Length == 0)
            throw new InvalidDataException("La opción de autoarranque requiere el nombre del ejecutable.");
        if (!BrowserUrlPolicy.TryNormalize(profile.Url, out var url))
            throw new InvalidDataException("La URL del mapa debe ser HTTP/HTTPS válida y sin credenciales.");
        var monitor = string.IsNullOrWhiteSpace(profile.CompanionMonitor) ? null : profile.CompanionMonitor.Trim();
        if (monitor is not null && (monitor.Length > 128 || monitor.Any(char.IsControl)))
            throw new InvalidDataException("La preferencia de monitor no es válida.");
        if (!profile.NoActivate || profile.RestoreGameFocusFallback)
            throw new InvalidDataException("Los perfiles deben conservar NoActivate y no pueden restaurar el foco mediante fallback.");
        return profile with { DisplayName = name, ProcessName = process, Url = url, CompanionMonitor = monitor };
    }

    public static GameProfileDocument Normalize(GameProfileDocument? document)
    {
        if (document is null || document.SchemaVersion != 1 || document.Profiles is null)
            throw new InvalidDataException("profiles.json debe contener una colección válida con schemaVersion 1.");
        var profiles = new List<GameProfile>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in document.Profiles)
        {
            if (profile is null) throw new InvalidDataException("Un perfil no puede ser null.");
            var normalized = Normalize(profile);
            if (!ids.Add(normalized.Id)) throw new InvalidDataException("Hay identificadores de perfil duplicados.");
            profiles.Add(normalized);
        }
        return document with { Profiles = profiles };
    }
}
