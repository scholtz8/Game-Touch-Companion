using GameTouchCompanion.Core;
using GameTouchCompanion.Native;

namespace GameTouchCompanion.Native.Tests;

public sealed class WindowsStartupRegistrationTests
{
    [Fact]
    public void ExplicitEnableReplaceDisableUsesOnlyInjectedStore()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GtcStartupTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var exe = Path.Combine(folder, "GameTouchCompanion.App.exe");
        File.WriteAllBytes(exe, []); // Only existence is needed; never execute this file.
        try
        {
            var store = new MemoryValue();
            var service = new WindowsStartupRegistrationService(exe, store);
            Assert.False(service.Read().Exists);
            Assert.Equal(0, store.Writes);
            service.SetEnabled(true);
            Assert.Equal(StartupCommand.Create(exe), store.Value);
            Assert.True(service.Read().MatchesCurrentExecutable);
            store.Value = "\"C:\\old\\GameTouchCompanion.App.exe\" --startup";
            Assert.True(service.Read().Exists);
            Assert.False(service.Read().MatchesCurrentExecutable);
            Assert.Throws<InvalidOperationException>(() => service.SetEnabled(true));
            service.SetEnabled(true, replaceExisting: true);
            Assert.True(service.Read().MatchesCurrentExecutable);
            service.SetEnabled(false);
            Assert.Null(store.Value);
            Assert.Equal(1, store.Deletes);
        }
        finally { File.Delete(exe); Directory.Delete(folder); }
    }

    [Fact]
    public void MissingExecutableAndDeleteFailurePreserveRegistration()
    {
        var store = new MemoryValue { Value = "old" };
        var service = new WindowsStartupRegistrationService(@"C:\missing-gtc-test\GameTouchCompanion.App.exe", store);
        Assert.Throws<FileNotFoundException>(() => service.SetEnabled(true, true));
        Assert.Equal("old", store.Value);
        store.FailDelete = true;
        Assert.Throws<UnauthorizedAccessException>(() => service.SetEnabled(false));
        Assert.Equal("old", store.Value);
        Assert.Equal(0, store.Writes);
    }

    private sealed class MemoryValue : IStartupValueStore
    {
        public string? Value { get; set; }
        public int Writes { get; private set; }
        public int Deletes { get; private set; }
        public bool FailDelete { get; set; }
        public string? Read() => Value;
        public void Write(string command) { Value = command; Writes++; }
        public void Delete() { if (FailDelete) throw new UnauthorizedAccessException(); Value = null; Deletes++; }
    }
}
