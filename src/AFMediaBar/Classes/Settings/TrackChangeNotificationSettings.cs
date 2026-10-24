// 按功能归组的设置契约；保留既有枚举值并兼容旧配置字段。
using AFMediaBar.Classes.Models;
using AFMediaBar.Classes.Utils;

namespace AFMediaBar.Classes.Settings;


/// <summary>曲目切换通知在目标工作区中的位置。 / Position of the track-change notification in the target work area.</summary>
public enum TrackChangeNotificationPosition
{
    BottomLeft = 0,
    TopLeft = 1,
    TopCenter = 2,
    TopRight = 3,
    BottomCenter = 4,
    BottomRight = 5,
    Custom = 6
}

/// <summary>曲目切换通知选择显示器的方式。 / Method used to select the display for track-change notifications.</summary>
public enum NotificationTargetMode
{
    Fixed = 0,
    ForegroundWindow = 1
}

/// <summary>曲目切换通知的用户设置。 / User settings for track-change notifications.</summary>
public readonly record struct TrackChangeNotificationSettings(
    bool Enabled,
    bool ShowWhenFullscreen,
    int DurationMilliseconds,
    TrackChangeNotificationPosition Position,
    NotificationTargetMode TargetMode,
    string? FixedMonitorDeviceId)
{
    public const int MinimumTitleFontSize = 12;
    public const int MaximumTitleFontSize = 48;
    public const int MinimumArtistFontSize = 10;
    public const int MaximumArtistFontSize = 36;
    public const int MinimumCoverSize = 40;
    public const int MaximumCoverSize = 160;
    public const int MaximumOffsetDip = 10000;

    // 新字段不加入位置构造函数，旧 JSON 可按原字段读取，再由 Normalize 补齐缺失值。
    public int TitleFontSize { get; init; }
    public string? TitleColor { get; init; }
    public int ArtistFontSize { get; init; }
    public string? ArtistColor { get; init; }
    public int CoverSize { get; init; }
    public double OffsetX { get; init; }
    public double OffsetY { get; init; }
    public bool? AnimationEnabled { get; init; }

    /// <summary>Minimum track notification duration stored in milliseconds.</summary>
    public const int MinimumDurationMilliseconds = 1000;
    /// <summary>Maximum track notification duration stored in milliseconds.</summary>
    public const int MaximumDurationMilliseconds = 10000;
    /// <summary>通知的默认设置。 / Default notification settings.</summary>
    public static TrackChangeNotificationSettings Default { get; } = new(
        true,
        true,
        3500,
        TrackChangeNotificationPosition.BottomRight,
        NotificationTargetMode.Fixed,
        null)
    {
        TitleFontSize = 22,
        TitleColor = "#202020",
        ArtistFontSize = 15,
        ArtistColor = "#666666",
        CoverSize = 72,
        AnimationEnabled = true
    };

    /// <summary>归一化枚举、时长和设备标识。 / Normalizes enums, duration, and the device identifier.</summary>
    public TrackChangeNotificationSettings Normalize()
    {
        var defaults = Default;
        return this with
        {
            DurationMilliseconds = Math.Clamp(DurationMilliseconds, MinimumDurationMilliseconds, MaximumDurationMilliseconds),
            Position = Enum.IsDefined(Position) ? Position : defaults.Position,
            TargetMode = Enum.IsDefined(TargetMode) ? TargetMode : defaults.TargetMode,
            FixedMonitorDeviceId = string.IsNullOrWhiteSpace(FixedMonitorDeviceId)
                ? null
                : FixedMonitorDeviceId.Trim(),
            TitleFontSize = TitleFontSize == 0 ? defaults.TitleFontSize : Math.Clamp(TitleFontSize, MinimumTitleFontSize, MaximumTitleFontSize),
            ArtistFontSize = ArtistFontSize == 0 ? defaults.ArtistFontSize : Math.Clamp(ArtistFontSize, MinimumArtistFontSize, MaximumArtistFontSize),
            CoverSize = CoverSize == 0 ? defaults.CoverSize : Math.Clamp(CoverSize, MinimumCoverSize, MaximumCoverSize),
            TitleColor = ColorHex.TryParse(TitleColor, out var titleColor) ? ColorHex.Format(titleColor) : defaults.TitleColor,
            ArtistColor = ColorHex.TryParse(ArtistColor, out var artistColor) ? ColorHex.Format(artistColor) : defaults.ArtistColor,
            OffsetX = double.IsFinite(OffsetX) ? Math.Clamp(OffsetX, -MaximumOffsetDip, MaximumOffsetDip) : 0,
            OffsetY = double.IsFinite(OffsetY) ? Math.Clamp(OffsetY, -MaximumOffsetDip, MaximumOffsetDip) : 0,
            AnimationEnabled = AnimationEnabled ?? true
        };
    }
}
