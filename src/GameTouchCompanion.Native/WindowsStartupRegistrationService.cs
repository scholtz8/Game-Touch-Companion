using GameTouchCompanion.Core;
using Microsoft.Win32;

namespace GameTouchCompanion.Native;

// Backend injection keeps tests away from the user's actual login settings.
public interface IStartupValueStore
{
    string? Read();
    void Write(string command);
    void Delete();
}

public sealed class WindowsStartupRegistrationService(string executablePath, IStartupValueStore? store = null) : IStartupRegistrationService
{
    private readonly IStartupValueStore values = store ?? new RegistryStartupValueStore();
    public StartupRegistration Read()
    {
        var existing = values.Read();
        string? current;
        try { current = StartupCommand.Create(executablePath); }
        catch (ArgumentException) { current = null; }
        return new(existing is not null, current is not null && string.Equals(existing, current, StringComparison.OrdinalIgnoreCase));
    }
    public void SetEnabled(bool enabled, bool replaceExisting = false)
    {
        if (!enabled) { values.Delete(); return; }
        var command = StartupCommand.Create(executablePath);
        if (!File.Exists(executablePath)) throw new FileNotFoundException("No se encontró el ejecutable actual.");
        var existing = values.Read();
        if (existing is not null && !string.Equals(existing, command, StringComparison.OrdinalIgnoreCase) && !replaceExisting)
            throw new InvalidOperationException("Existe un registro de otra ruta. Usa Registrar esta copia para reemplazarlo explícitamente.");
        values.Write(command);
    }
}

internal sealed class RegistryStartupValueStore : IStartupValueStore
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Name = "GameTouchCompanion";
    public string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(Key, writable: false);
        var value = key?.GetValue(Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return value is null ? null : value as string ?? "[registro de tipo no admitido]";
    }
    public void Write(string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key, writable: true);
        key.SetValue(Name, command, RegistryValueKind.String);
    }
    public void Delete()
    {
        using var key = Registry.CurrentUser.OpenSubKey(Key, writable: true);
        key?.DeleteValue(Name, throwOnMissingValue: false);
    }
}
