// 验证旧允许列表升级后不会让未授权的浏览器会话接管媒体栏。
using AFMediaBar.Classes.Services;
using AFMediaBar.Classes.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AFMediaBar.Layout.Tests;

[TestClass]
public sealed class SmtcSourceFilterSettingsTests
{
    [TestMethod]
    public void FreshInstallAllowsNetEaseButNotAnUnapprovedBrowser()
    {
        var settings = SmtcSourceFilterSettings.Default.Normalize();
        Assert.IsTrue(settings.Enabled);
        Assert.IsTrue(MediaSourceFilterPolicy.IsAllowed("cloudmusic", settings));
        Assert.IsFalse(MediaSourceFilterPolicy.IsAllowed("msedge.exe", settings));
    }

    [TestMethod]
    public void LegacyDisabledFilterKeepsSelectedSourcesAndStartsEnforcingThem()
    {
        var settings = Load("{\"enabled\":false,\"allowedSourceIds\":[\"cloudmusic\"]}");
        Assert.IsTrue(settings.Enabled);
        CollectionAssert.AreEqual(new[] { "cloudmusic" }, settings.AllowedSourceIds!.ToArray());
        Assert.IsTrue(settings.DefaultRestrictionApplied);
        Assert.IsFalse(MediaSourceFilterPolicy.IsAllowed("msedge.exe", settings));
    }

    [TestMethod]
    public void LegacyEmptyListDefaultsToNetEaseWhileLaterExplicitOptOutPersists()
    {
        var migrated = Load("{\"enabled\":false,\"allowedSourceIds\":[]}");
        Assert.IsTrue(MediaSourceFilterPolicy.IsAllowed("cloudmusic", migrated));
        Assert.IsFalse(MediaSourceFilterPolicy.IsAllowed("msedge.exe", migrated));

        var previouslyBlockedAll = Load("{\"enabled\":true,\"allowedSourceIds\":[]}");
        Assert.IsTrue(previouslyBlockedAll.Enabled);
        Assert.IsFalse(MediaSourceFilterPolicy.IsAllowed("cloudmusic", previouslyBlockedAll));

        var optedOut = Load("{\"enabled\":false,\"allowedSourceIds\":[\"cloudmusic\"],\"defaultRestrictionApplied\":true}");
        Assert.IsFalse(optedOut.Enabled);
        Assert.IsTrue(MediaSourceFilterPolicy.IsAllowed("msedge.exe", optedOut));
    }

    private static SmtcSourceFilterSettings Load(string filterJson)
    {
        var directory = Path.Combine(Path.GetTempPath(), "afmb-source-filter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "user-defaults.json"),
                "{\"schemaVersion\":2,\"settings\":{\"smtcSourceFilter\":" + filterJson + "}}");
            using var store = new SettingsPersistenceService(directory);
            return store.LoadUserDefaults()!.SmtcSourceFilter;
        }
        finally { Directory.Delete(directory, true); }
    }
}
