using AFMediaBar.Classes.Settings;

namespace AFMediaBar.Classes.Services;

/// <summary>在物理像素工作区内计算曲目通知位置。 / Calculates track-notification placement in a physical-pixel work area.</summary>
public static class NotificationPlacementCalculator
{
    /// <summary>把 WPF DIP 尺寸转换成目标显示器的物理像素。 / Converts a WPF DIP size to physical pixels for the target display.</summary>
    public static Size ToPhysicalSize(Size dipSize, uint dpiX, uint dpiY)
    {
        var scaleX = (dpiX == 0 ? 96u : dpiX) / 96d;
        var scaleY = (dpiY == 0 ? 96u : dpiY) / 96d;
        var width = double.IsFinite(dipSize.Width) ? Math.Max(0, dipSize.Width) : 0;
        var height = double.IsFinite(dipSize.Height) ? Math.Max(0, dipSize.Height) : 0;
        return new Size(
            Math.Ceiling(width * scaleX),
            Math.Ceiling(height * scaleY));
    }

    /// <summary>根据六位置设置计算并夹取窗口左上角。 / Calculates and clamps the window's top-left point for one of six positions.</summary>
    public static Point Calculate(
        Rect workArea,
        Size windowSize,
        TrackChangeNotificationPosition position,
        double margin)
    {
        if (workArea.IsEmpty || workArea.Width <= 0 || workArea.Height <= 0)
            return new Point(0, 0);

        var width = double.IsFinite(windowSize.Width) ? Math.Max(0, windowSize.Width) : 0;
        var height = double.IsFinite(windowSize.Height) ? Math.Max(0, windowSize.Height) : 0;
        var safeMargin = double.IsFinite(margin) ? Math.Max(0, margin) : 0;
        var minX = workArea.Left + safeMargin;
        var maxX = Math.Max(minX, workArea.Right - safeMargin - width);
        var minY = workArea.Top + safeMargin;
        var maxY = Math.Max(minY, workArea.Bottom - safeMargin - height);

        var x = position switch
        {
            TrackChangeNotificationPosition.TopCenter or TrackChangeNotificationPosition.BottomCenter =>
                workArea.Left + (workArea.Width - width) / 2,
            TrackChangeNotificationPosition.TopRight or TrackChangeNotificationPosition.BottomRight => maxX,
            _ => minX
        };
        var y = position is TrackChangeNotificationPosition.TopLeft or
            TrackChangeNotificationPosition.TopCenter or TrackChangeNotificationPosition.TopRight
            ? minY
            : maxY;

        return new Point(Math.Clamp(x, minX, maxX), Math.Clamp(y, minY, maxY));
    }

    /// <summary>在预设锚点上应用物理像素偏移，或按工作区原点放置自定义位置，并夹入可见区域。</summary>
    public static Point Calculate(
        Rect workArea,
        Size windowSize,
        TrackChangeNotificationPosition position,
        double margin,
        double offsetX,
        double offsetY)
    {
        if (workArea.IsEmpty || workArea.Width <= 0 || workArea.Height <= 0)
            return new Point(0, 0);

        var width = double.IsFinite(windowSize.Width) ? Math.Max(0, windowSize.Width) : 0;
        var height = double.IsFinite(windowSize.Height) ? Math.Max(0, windowSize.Height) : 0;
        var basePoint = Calculate(workArea, windowSize, position, margin);
        var safeX = double.IsFinite(offsetX) ? offsetX : 0;
        var safeY = double.IsFinite(offsetY) ? offsetY : 0;
        var rightAnchored = position is TrackChangeNotificationPosition.TopRight or TrackChangeNotificationPosition.BottomRight;
        var bottomAnchored = position is TrackChangeNotificationPosition.BottomLeft or
            TrackChangeNotificationPosition.BottomCenter or TrackChangeNotificationPosition.BottomRight;
        var x = position == TrackChangeNotificationPosition.Custom
            ? workArea.Left + safeX
            : basePoint.X + (rightAnchored ? -safeX : safeX);
        var y = position == TrackChangeNotificationPosition.Custom
            ? workArea.Top + safeY
            : basePoint.Y + (bottomAnchored ? -safeY : safeY);
        return new Point(
            Math.Clamp(x, workArea.Left, Math.Max(workArea.Left, workArea.Right - width)),
            Math.Clamp(y, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - height)));
    }
}
