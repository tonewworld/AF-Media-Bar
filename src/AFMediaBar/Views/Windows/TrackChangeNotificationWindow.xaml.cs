// 曲目通知窗口只拥有透明 HWND、动画与隐藏计时器；媒体选择和显示器枚举沿用已有服务。
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AFMediaBar.Classes.Interop;
using AFMediaBar.Classes.Models;
using AFMediaBar.Classes.Services;
using AFMediaBar.Classes.Services.Layout;
using AFMediaBar.Classes.Settings;
using AFMediaBar.Classes.Utils;

namespace AFMediaBar.Views.Windows;

/// <summary>复用同一个非激活透明窗口，显示当前曲目的封面、标题和歌手。</summary>
public partial class TrackChangeNotificationWindow : Window
{
    private enum PresentationState { Hidden, Entering, Visible, Exiting }

    private readonly IDisplayMonitorService _monitorService;
    private readonly DispatcherTimer _hideTimer;
    private DisplayMonitorInfo? _currentMonitor;
    private TrackChangeNotificationSettings _currentSettings;
    private string? _currentIdentity;
    private PresentationState _state;
    private int _presentationVersion;

    /// <summary>创建通知窗口；关闭时释放计时器和设置、显示器订阅。</summary>
    public TrackChangeNotificationWindow(IDisplayMonitorService monitorService)
    {
        _monitorService = monitorService;
        WindowHelper.SetNoActivate(this);
        InitializeComponent();
        Left = -10000;
        Top = -10000;
        _hideTimer = new DispatcherTimer();
        _hideTimer.Tick += HideTimer_Tick;
        SettingsManager.TrackChangeNotificationSettingsChanged += OnNotificationSettingsChanged;
        _monitorService.MonitorsChanged += OnMonitorsChanged;
    }

    /// <summary>替换内容并重新计时；旧入场、退出和延迟布局回调由代际拦截。</summary>
    public void ShowNotification(TrackChangeNotificationRequest request)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ShowNotification(request));
            return;
        }

        var version = ++_presentationVersion;
        var resumeMotion = _state is PresentationState.Entering or PresentationState.Exiting;
        var startX = resumeMotion ? EntryTransform.X : 16d;
        var startOpacity = resumeMotion ? AnimatedRoot.Opacity : 0d;
        _hideTimer.Stop();
        StopAnimations();
        _currentSettings = request.Settings.Normalize();
        _currentMonitor = request.Monitor;
        _currentIdentity = TrackChangeNotificationPolicy.CreateIdentity(request.Snapshot);
        ApplySettings(_currentSettings);
        ApplySnapshot(request.Snapshot);

        if (!IsVisible)
        {
            AnimatedRoot.Opacity = 0;
            Show();
            Dispatcher.BeginInvoke(() => CompletePresentation(version, startX, startOpacity), DispatcherPriority.Loaded);
            return;
        }

        CompletePresentation(version, startX, startOpacity);
    }

    private void CompletePresentation(int version, double startX, double startOpacity)
    {
        if (version != _presentationVersion || !IsVisible || _currentMonitor is null)
            return;

        UpdateLayout();
        PositionOnMonitor(_currentMonitor, _currentSettings);
        StartEntry(version, startX, startOpacity);
    }

    /// <summary>只补全当前曲目的封面或文本；旧曲目的迟到结果不能覆盖新曲目。</summary>
    public void UpdateSnapshot(MediaSnapshot snapshot)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => UpdateSnapshot(snapshot));
            return;
        }

        if (_state == PresentationState.Hidden ||
            !string.Equals(_currentIdentity, TrackChangeNotificationPolicy.CreateIdentity(snapshot), StringComparison.Ordinal))
            return;

        ApplySnapshot(snapshot);
        UpdateLayout();
        RefreshPlacement();
    }

    private void ApplySnapshot(MediaSnapshot snapshot)
    {
        TitleText.Text = snapshot.Title;
        TitleText.ToolTip = snapshot.Title;
        var artist = string.IsNullOrWhiteSpace(snapshot.Artist) ? snapshot.SourceName : snapshot.Artist;
        ArtistText.Text = artist;
        ArtistText.ToolTip = artist;
        if (snapshot.Artwork is null)
            ArtworkBorder.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
        else
            ArtworkBorder.Background = new ImageBrush(snapshot.Artwork) { Stretch = Stretch.Uniform };
        ArtworkPlaceholder.Visibility = snapshot.Artwork is null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplySettings(TrackChangeNotificationSettings settings)
    {
        ArtworkBorder.Width = settings.CoverSize;
        ArtworkBorder.Height = settings.CoverSize;
        ArtworkColumn.Width = new GridLength(settings.CoverSize);
        TitleText.FontSize = settings.TitleFontSize;
        ArtistText.FontSize = settings.ArtistFontSize;
        if (ColorHex.TryParse(settings.TitleColor, out var titleColor))
            TitleText.Foreground = new SolidColorBrush(titleColor);
        if (ColorHex.TryParse(settings.ArtistColor, out var artistColor))
            ArtistText.Foreground = new SolidColorBrush(artistColor);
    }

    private void PositionOnMonitor(DisplayMonitorInfo monitor, TrackChangeNotificationSettings settings)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
            return;

        var scaleX = (monitor.DpiX == 0 ? 96u : monitor.DpiX) / 96d;
        var scaleY = (monitor.DpiY == 0 ? 96u : monitor.DpiY) / 96d;
        var size = NotificationPlacementCalculator.ToPhysicalSize(
            new Size(Math.Max(1, ActualWidth), Math.Max(1, ActualHeight)), monitor.DpiX, monitor.DpiY);
        var point = NotificationPlacementCalculator.Calculate(
            monitor.WorkArea, size, settings.Position, 24 * Math.Max(scaleX, scaleY),
            settings.OffsetX * scaleX, settings.OffsetY * scaleY);
        NativeMethods.SetWindowPos(handle, -1, (int)Math.Round(point.X), (int)Math.Round(point.Y), 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private void StartEntry(int version, double startX, double startOpacity)
    {
        _state = PresentationState.Entering;
        var animated = _currentSettings.AnimationEnabled == true && MotionPolicy.ResolveCurrent().UseTransitions;
        if (!animated)
        {
            EntryTransform.X = 0;
            AnimatedRoot.Opacity = 1;
            CompleteEntry(version);
            return;
        }

        EntryTransform.X = startX;
        AnimatedRoot.Opacity = startOpacity;
        var duration = new Duration(TimeSpan.FromMilliseconds(300));
        var ease = new PowerEase { Power = 3, EasingMode = EasingMode.EaseOut };
        EntryTransform.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(0, duration) { EasingFunction = ease });
        var fade = new DoubleAnimation(1, duration) { EasingFunction = ease };
        fade.Completed += (_, _) => CompleteEntry(version);
        AnimatedRoot.BeginAnimation(OpacityProperty, fade);
    }

    private void CompleteEntry(int version)
    {
        if (version != _presentationVersion || !IsVisible)
            return;
        _state = PresentationState.Visible;
        _hideTimer.Interval = TimeSpan.FromMilliseconds(_currentSettings.DurationMilliseconds);
        _hideTimer.Start();
    }

    private void HideTimer_Tick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();
        var version = _presentationVersion;
        if (_currentSettings.AnimationEnabled != true || !MotionPolicy.ResolveCurrent().UseTransitions)
        {
            HideImmediately();
            return;
        }

        _state = PresentationState.Exiting;
        var duration = new Duration(TimeSpan.FromMilliseconds(250));
        EntryTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(16, duration));
        var fade = new DoubleAnimation(0, duration);
        fade.Completed += (_, _) =>
        {
            if (version == _presentationVersion)
                HideImmediately();
        };
        AnimatedRoot.BeginAnimation(OpacityProperty, fade);
    }

    private void OnNotificationSettingsChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnNotificationSettingsChanged(sender, e));
            return;
        }
        if (_state == PresentationState.Hidden)
            return;
        var settings = SettingsManager.Current.TrackChangeNotification.Normalize();
        if (!settings.Enabled)
        {
            HideImmediately();
            return;
        }
        _currentSettings = settings;
        ApplySettings(settings);
        UpdateLayout();
        RefreshPlacement();
        if (_state == PresentationState.Visible)
        {
            _hideTimer.Stop();
            _hideTimer.Interval = TimeSpan.FromMilliseconds(settings.DurationMilliseconds);
            _hideTimer.Start();
        }
    }

    private void OnMonitorsChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(RefreshPlacement);
            return;
        }
        RefreshPlacement();
    }

    private void RefreshPlacement()
    {
        if (_state == PresentationState.Hidden)
            return;
        var monitor = _monitorService.ResolveNotificationMonitor(
            _currentSettings.TargetMode, _currentSettings.FixedMonitorDeviceId);
        if (monitor is null)
            return;
        _currentMonitor = monitor;
        UpdateLayout();
        PositionOnMonitor(monitor, _currentSettings);
    }

    /// <summary>取消所有过渡和计时并立即隐藏，可在退出时重复调用。</summary>
    public void HideImmediately()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(HideImmediately);
            return;
        }
        ++_presentationVersion;
        _hideTimer.Stop();
        StopAnimations();
        _state = PresentationState.Hidden;
        if (IsVisible)
            Hide();
    }

    private void StopAnimations()
    {
        AnimatedRoot.BeginAnimation(OpacityProperty, null);
        EntryTransform.BeginAnimation(TranslateTransform.XProperty, null);
    }

    protected override void OnClosed(EventArgs e)
    {
        HideImmediately();
        _hideTimer.Tick -= HideTimer_Tick;
        SettingsManager.TrackChangeNotificationSettingsChanged -= OnNotificationSettingsChanged;
        _monitorService.MonitorsChanged -= OnMonitorsChanged;
        base.OnClosed(e);
    }
}
