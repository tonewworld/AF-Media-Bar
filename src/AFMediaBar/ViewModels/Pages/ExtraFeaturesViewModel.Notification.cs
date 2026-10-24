// 曲目通知外观绑定与输入校验；只经现有设置管理器写入，不持有窗口。
using System.Windows.Media;
using AFMediaBar.Classes.Settings;
using AFMediaBar.Classes.Utils;
using AFMediaBar.Resources;

namespace AFMediaBar.ViewModels.Pages;

public partial class ExtraFeaturesViewModel
{
    private string? _titleColorHex;
    private string? _artistColorHex;

    public int NotificationTitleFontSize
    {
        get => Notification.TitleFontSize;
        set => UpdateNotification(Notification with { TitleFontSize = value });
    }

    public int NotificationArtistFontSize
    {
        get => Notification.ArtistFontSize;
        set => UpdateNotification(Notification with { ArtistFontSize = value });
    }

    public int NotificationCoverSize
    {
        get => Notification.CoverSize;
        set => UpdateNotification(Notification with { CoverSize = value });
    }

    public double NotificationOffsetX
    {
        get => Notification.OffsetX;
        set => UpdateNotification(Notification with { OffsetX = value });
    }

    public double NotificationOffsetY
    {
        get => Notification.OffsetY;
        set => UpdateNotification(Notification with { OffsetY = value });
    }

    public bool NotificationAnimationEnabled
    {
        get => Notification.AnimationEnabled == true;
        set => UpdateNotification(Notification with { AnimationEnabled = value });
    }

    public string TitleColorHex
    {
        get => _titleColorHex ?? Notification.TitleColor ?? "#202020";
        set
        {
            if (!SetProperty(ref _titleColorHex, value ?? string.Empty))
                return;
            OnPropertyChanged(nameof(HasTitleColorError));
            if (!_isRefreshing && ColorHex.TryParse(value, out var color))
                UpdateNotification(Notification with { TitleColor = ColorHex.Format(color) });
        }
    }

    public string ArtistColorHex
    {
        get => _artistColorHex ?? Notification.ArtistColor ?? "#666666";
        set
        {
            if (!SetProperty(ref _artistColorHex, value ?? string.Empty))
                return;
            OnPropertyChanged(nameof(HasArtistColorError));
            if (!_isRefreshing && ColorHex.TryParse(value, out var color))
                UpdateNotification(Notification with { ArtistColor = ColorHex.Format(color) });
        }
    }

    public bool HasTitleColorError => !ColorHex.TryParse(TitleColorHex, out _);
    public bool HasArtistColorError => !ColorHex.TryParse(ArtistColorHex, out _);

    public Brush TitlePreviewBrush => MakeBrush(Notification.TitleColor, Colors.Black);
    public Brush ArtistPreviewBrush => MakeBrush(Notification.ArtistColor, Colors.DimGray);
    public string NotificationPreviewTitle => Translations.Get("Media.Notification.Preview.SampleTitle");
    public string NotificationPreviewArtist => Translations.Get("Media.Notification.Preview.SampleArtist");

    [RelayCommand]
    private void SetTitleColor(string? hex)
    {
        if (ColorHex.TryParse(hex, out var color))
            TitleColorHex = ColorHex.Format(color);
    }

    [RelayCommand]
    private void SetArtistColor(string? hex)
    {
        if (ColorHex.TryParse(hex, out var color))
            ArtistColorHex = ColorHex.Format(color);
    }

    [RelayCommand]
    private void ShowTestNotification() => _notificationCoordinator.ShowPreview();

    [RelayCommand]
    private void ResetNotificationDefaults() => UpdateNotification(TrackChangeNotificationSettings.Default);

    private void NotifyNotificationVisualProperties()
    {
        _titleColorHex = Notification.TitleColor;
        _artistColorHex = Notification.ArtistColor;
        OnPropertyChanged(nameof(NotificationTitleFontSize));
        OnPropertyChanged(nameof(NotificationArtistFontSize));
        OnPropertyChanged(nameof(NotificationCoverSize));
        OnPropertyChanged(nameof(NotificationOffsetX));
        OnPropertyChanged(nameof(NotificationOffsetY));
        OnPropertyChanged(nameof(NotificationAnimationEnabled));
        OnPropertyChanged(nameof(TitleColorHex));
        OnPropertyChanged(nameof(ArtistColorHex));
        OnPropertyChanged(nameof(HasTitleColorError));
        OnPropertyChanged(nameof(HasArtistColorError));
        OnPropertyChanged(nameof(TitlePreviewBrush));
        OnPropertyChanged(nameof(ArtistPreviewBrush));
    }

    private static Brush MakeBrush(string? hex, Color fallback) =>
        new SolidColorBrush(ColorHex.TryParse(hex, out var color) ? color : fallback);
}
