using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class MonitorPreviewTests
{
    private static MonitorProfile Monitor(int x, int y, int w = 1920, int h = 1080) =>
        new($"{x},{y}", new(x, y, w, h), new(x, y, w, h), false);

    [Fact]
    public void NegativeCoordinatesPreserveRelativePlacementAndUniformScale()
    {
        var input = new[] { Monitor(-1920, -1080), Monitor(0, 0), Monitor(1920, 0, 1080, 1920) };
        var result = MonitorPreviewLayout.Create(input, 600, 240);
        Assert.Equal(3, result.Count);
        Assert.True(result[0].X < result[1].X && result[0].Y < result[1].Y);
        Assert.True(result[1].X < result[2].X);
        foreach (var item in result)
        {
            Assert.InRange(item.X, 0, 600); Assert.InRange(item.Y, 0, 240);
            Assert.True(item.X + item.Width <= 600.00001 && item.Y + item.Height <= 240.00001);
            Assert.Equal(item.Width / item.Monitor.Bounds.Width, item.Height / item.Monitor.Bounds.Height, 10);
        }
        Assert.Equal(-1920, input[0].Bounds.X);
    }

    [Theory]
    [InlineData(0, 200)]
    [InlineData(200, -1)]
    [InlineData(double.NaN, 200)]
    [InlineData(200, double.PositiveInfinity)]
    public void InvalidViewportReturnsEmpty(double width, double height) =>
        Assert.Empty(MonitorPreviewLayout.Create([Monitor(0, 0)], width, height));

    [Fact]
    public void EmptyInvalidAndExtremeBoundsAreSafe()
    {
        Assert.Empty(MonitorPreviewLayout.Create([], 600, 200));
        Assert.Empty(MonitorPreviewLayout.Create([Monitor(0, 0, 0, 100)], 600, 200));
        var result = MonitorPreviewLayout.Create([Monitor(int.MinValue, 0), Monitor(int.MaxValue, 0)], 600, 200);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(double.IsFinite(r.X) && r.Width > 0));
    }
}
