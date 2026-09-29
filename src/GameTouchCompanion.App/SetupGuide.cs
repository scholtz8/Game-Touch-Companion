namespace GameTouchCompanion.App;

/// <summary>Repeatable navigation guide; does not write settings or open windows.</summary>
public sealed class SetupGuide
{
    public int Step { get; private set; }
    public string Title => Localization.T(Step switch { 0 => "1 de 3 · Elige tus pantallas", 1 => "2 de 3 · Prepara el contenido", _ => "3 de 3 · Prueba y juega" });
    public string Description => Localization.T(Step switch
    {
        0 => "En Pantallas revisa el esquema y selecciona juego y Companion. Confirma cualquier aviso de desconexión. Esta guía solo avanza con dos pantallas distintas y una selección válida.",
        1 => "En Navegador prepara una URL o en Perfiles guarda el ejecutable, las pestañas y el monitor. La detección automática permanece activa mientras Game Touch Companion esté ejecutándose. Puedes usar Companion manualmente sin crear perfiles.",
        _ => "Configuración preparada: abre Companion manualmente o vuelve al juego para usar la detección automática. Verifica el foco con FocusProbe. Terminar esta guía no abre ventanas."
    });
    public bool Next(bool monitorsReady)
    {
        if (Step == 0 && !monitorsReady) return false;
        if (Step < 2) Step++;
        return true;
    }
    public void Back() { if (Step > 0) Step--; }
    public void Restart() => Step = 0;
}
