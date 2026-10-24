// 验证通知在物理工作区内按锚点、偏移和自定义坐标定位，不需要真实显示器。
using AFMediaBar.Classes.Services;
using AFMediaBar.Classes.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;

namespace AFMediaBar.Layout.Tests;

/// <summary>通知预设与自定义位置的几何回归。</summary>
[TestClass]
public sealed class MinimalNotificationPlacementTests
{
    [TestMethod]
    public void BottomRightOffsetMovesInwardAndStaysInsideWorkArea()
    {
        var area = new Rect(0, 0, 1920, 1040);
        var point = NotificationPlacementCalculator.Calculate(area, new Size(350, 96),
            TrackChangeNotificationPosition.BottomRight, 24, 40, 20);
        Assert.AreEqual(1506, point.X);
        Assert.AreEqual(900, point.Y);

        var clamped = NotificationPlacementCalculator.Calculate(area, new Size(350, 96),
            TrackChangeNotificationPosition.BottomRight, 24, -5000, -5000);
        Assert.AreEqual(1570, clamped.X);
        Assert.AreEqual(944, clamped.Y);
    }

    [TestMethod]
    public void CustomPositionUsesWorkAreaOriginAndClampsOversizedOffsets()
    {
        var area = new Rect(1920, 100, 1280, 900);
        var point = NotificationPlacementCalculator.Calculate(area, new Size(400, 120),
            TrackChangeNotificationPosition.Custom, 24, 250, 300);
        Assert.AreEqual(2170, point.X);
        Assert.AreEqual(400, point.Y);

        var clamped = NotificationPlacementCalculator.Calculate(area, new Size(400, 120),
            TrackChangeNotificationPosition.Custom, 24, 9000, -9000);
        Assert.AreEqual(2800, clamped.X);
        Assert.AreEqual(100, clamped.Y);
    }
}
