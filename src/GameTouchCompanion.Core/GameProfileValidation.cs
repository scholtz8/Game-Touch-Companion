namespace GameTouchCompanion.Core;

public static class GameProfileValidation
{
    public const int CurrentSchemaVersion = 2;

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

        var tabs = NormalizeTabs(profile);
        var primaryId = profile.PrimaryTabId;
        if (string.IsNullOrWhiteSpace(primaryId) || !tabs.Any(tab => string.Equals(tab.Id, primaryId, StringComparison.OrdinalIgnoreCase)))
            primaryId = tabs[0].Id;
        var primary = tabs.First(tab => string.Equals(tab.Id, primaryId, StringComparison.OrdinalIgnoreCase));

        var monitor = string.IsNullOrWhiteSpace(profile.CompanionMonitor) ? null : profile.CompanionMonitor.Trim();
        if (monitor is not null && (monitor.Length > 128 || monitor.Any(char.IsControl)))
            throw new InvalidDataException("La preferencia de monitor no es válida.");
        if (!profile.NoActivate || profile.RestoreGameFocusFallback)
            throw new InvalidDataException("Los perfiles deben conservar NoActivate y no pueden restaurar el foco mediante fallback.");
        return profile with
        {
            DisplayName = name,
            ProcessName = process,
            Url = primary.Url,
            Tabs = tabs,
            PrimaryTabId = primary.Id,
            CompanionMonitor = monitor
        };
    }

    private static List<GameProfileTab> NormalizeTabs(GameProfile profile)
    {
        var source = profile.Tabs ?? [];
        if (source.Count == 0)
        {
            if (!BrowserUrlPolicy.TryNormalize(profile.Url, out var legacyUrl))
                throw new InvalidDataException("El perfil debe contener al menos una URL HTTP/HTTPS válida y sin credenciales.");
            return [new GameProfileTab { Id = "main", Name = "Principal", Url = legacyUrl, Order = 0 }];
        }
        if (source.Count > 20)
            throw new InvalidDataException("Un perfil puede contener como máximo 20 pestañas.");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<GameProfileTab>(source.Count);
        foreach (var (tab, index) in source.OrderBy(t => t.Order).Select((value, index) => (value, index)))
        {
            if (tab is null) throw new InvalidDataException("Una pestaña del perfil no puede ser null.");
            var id = tab.Id?.Trim();
            if (string.IsNullOrWhiteSpace(id) || id.Length > 80 ||
                id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_') || !ids.Add(id))
                throw new InvalidDataException("Las pestañas deben tener identificadores válidos y únicos.");
            var tabName = tab.Name?.Trim();
            if (string.IsNullOrWhiteSpace(tabName) || tabName.Length > 80 || tabName.Any(char.IsControl))
                throw new InvalidDataException("Cada pestaña debe tener un nombre de entre 1 y 80 caracteres.");
            if (!BrowserUrlPolicy.TryNormalize(tab.Url, out var tabUrl))
                throw new InvalidDataException("La URL de cada pestaña debe ser HTTP/HTTPS válida y sin credenciales.");
            normalized.Add(tab with { Id = id, Name = tabName, Url = tabUrl, Order = index });
        }
        return normalized;
    }

    public static GameProfileDocument Normalize(GameProfileDocument? document)
    {
        if (document is null || document.Profiles is null || document.SchemaVersion is < 1 or > CurrentSchemaVersion)
            throw new InvalidDataException("profiles.json contiene una versión de esquema no compatible.");
        var profiles = new List<GameProfile>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in document.Profiles)
        {
            if (profile is null) throw new InvalidDataException("Un perfil no puede ser null.");
            var normalized = Normalize(profile);
            if (!ids.Add(normalized.Id)) throw new InvalidDataException("Hay identificadores de perfil duplicados.");
            profiles.Add(normalized);
        }
        return new GameProfileDocument { SchemaVersion = CurrentSchemaVersion, Profiles = profiles };
    }
}
