using System.ComponentModel;
using System.Runtime.CompilerServices;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public sealed class ProfileTabDraft : INotifyPropertyChanged
{
    private string id = Guid.NewGuid().ToString("N");
    private string name = string.Empty;
    private string url = BrowserUrlPolicy.LocalHomeUrl;
    private bool isPrimary;

    public string Id { get => id; set => Set(ref id, value); }
    public string Name { get => name; set => Set(ref name, value); }
    public string Url { get => url; set => Set(ref url, value); }
    public bool IsPrimary { get => isPrimary; set => Set(ref isPrimary, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
