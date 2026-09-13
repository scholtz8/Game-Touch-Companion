using GameTouchCompanion.App;

namespace GameTouchCompanion.IntegrationTests;

public sealed class SetupGuideTests
{
    [Fact]
    public void GuideRequiresMonitorReadinessAndHasBoundedNavigation()
    {
        var guide = new SetupGuide();
        Assert.False(guide.Next(false));
        Assert.Equal(0, guide.Step);
        guide.Back(); Assert.Equal(0, guide.Step);
        Assert.True(guide.Next(true)); Assert.Equal(1, guide.Step);
        Assert.True(guide.Next(true)); Assert.Equal(2, guide.Step);
        guide.Next(true); Assert.Equal(2, guide.Step);
        Assert.Contains("no abre", guide.Description);
        guide.Back(); Assert.Equal(1, guide.Step);
        guide.Restart(); Assert.Equal(0, guide.Step);
    }
}
