using GameTouchCompanion.App;

namespace GameTouchCompanion.IntegrationTests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public async Task SecondaryInstanceSignalsPrimaryWithoutBecomingPrimary()
    {
        var instanceName = $"GameTouchCompanion.Tests.{Guid.NewGuid():N}";
        using var primary = new SingleInstanceCoordinator(instanceName);
        using var secondary = new SingleInstanceCoordinator(instanceName);
        Assert.True(primary.IsPrimary);
        Assert.False(secondary.IsPrimary);

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        primary.StartListening(command =>
        {
            received.TrySetResult(command);
            return Task.CompletedTask;
        });

        Assert.True(await secondary.SignalPrimaryAsync("show").WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("show", await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task PrimaryNeverSignalsItself()
    {
        using var primary = new SingleInstanceCoordinator($"GameTouchCompanion.Tests.{Guid.NewGuid():N}");
        Assert.True(primary.IsPrimary);
        Assert.False(await primary.SignalPrimaryAsync("show"));
    }
}
