// 验证新版通知默认值和旧设置文件兼容；文件测试只使用独立临时目录。
using AFMediaBar.Classes.Services;
using AFMediaBar.Classes.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace AFMediaBar.Layout.Tests;

/// <summary>极简通知设置的默认值和旧配置兼容回归。</summary>
[TestClass]
public sealed class MinimalNotificationSettingsTests
{
    [TestMethod]
    public void NewDefaultsUsePrimaryBottomRightAndIndependentTextStyles()
    {
        var settings = TrackChangeNotificationSettings.Default;
        Assert.AreEqual(TrackChangeNotificationPosition.BottomRight, settings.Position);
        Assert.AreEqual(NotificationTargetMode.Fixed, settings.TargetMode);
        Assert.AreEqual(3500, settings.DurationMilliseconds);
        Assert.AreEqual(22, settings.TitleFontSize);
        Assert.AreEqual(15, settings.ArtistFontSize);
        Assert.AreEqual(72, settings.CoverSize);
        Assert.AreEqual("#202020", settings.TitleColor);
        Assert.AreEqual("#666666", settings.ArtistColor);
    }

    [TestMethod]
    public void ExistingNotificationSettingsWithoutNewFieldsKeepOldChoicesAndGainStyleDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AFMediaBar-notification-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                SchemaVersion = SettingsPersistenceService.CurrentSchemaVersion,
                Settings = new
                {
                    TrackChangeNotification = new
                    {
                        Enabled = true,
                        ShowWhenFullscreen = false,
                        DurationMilliseconds = 5000,
                        Position = TrackChangeNotificationPosition.TopLeft,
                        TargetMode = NotificationTargetMode.ForegroundWindow,
                        FixedMonitorDeviceId = (string?)null
                    }
                }
            });
            File.WriteAllText(Path.Combine(directory, "user-defaults.json"), json);
            using var persistence = new SettingsPersistenceService(directory);
            var settings = persistence.LoadUserDefaults()!.TrackChangeNotification;
            Assert.AreEqual(TrackChangeNotificationPosition.TopLeft, settings.Position);
            Assert.AreEqual(NotificationTargetMode.ForegroundWindow, settings.TargetMode);
            Assert.AreEqual(5000, settings.DurationMilliseconds);
            Assert.AreEqual(22, settings.TitleFontSize);
            Assert.AreEqual(15, settings.ArtistFontSize);
            Assert.AreEqual(72, settings.CoverSize);
            Assert.AreEqual("#202020", settings.TitleColor);
            Assert.AreEqual("#666666", settings.ArtistColor);
            Assert.IsTrue(settings.AnimationEnabled);
        }
        finally { Directory.Delete(directory, true); }
    }
}
