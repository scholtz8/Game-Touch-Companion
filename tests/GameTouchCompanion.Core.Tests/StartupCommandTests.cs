using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class StartupCommandTests
{
    [Fact]
    public void QuotesAbsoluteExecutableAndAddsOnlyStartupArgument()
    {
        Assert.Equal("\"C:\\Apps with spaces\\GameTouchCompanion.App.exe\" --startup",
            StartupCommand.Create(@"C:\Apps with spaces\GameTouchCompanion.App.exe"));
    }
    [Theory]
    [InlineData("")]
    [InlineData("GameTouchCompanion.App.exe")]
    [InlineData(@"C:\App\dotnet.exe")]
    [InlineData(@"C:\App\GameTouchCompanion.App.dll")]
    [InlineData("C:\\Bad\"\\GameTouchCompanion.App.exe")]
    public void RejectsInvalidTargets(string path) => Assert.Throws<ArgumentException>(() => StartupCommand.Create(path));
    [Fact]
    public void RejectsCommandOverRunKeyLimit() => Assert.Throws<ArgumentException>(() =>
        StartupCommand.Create("C:\\" + new string('a', 250) + "\\GameTouchCompanion.App.exe"));
}
