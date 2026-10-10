using System;
using System.IO;
using BepInEx.Configuration;
using Utility.Notifications;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace GCMod.Tests;

public sealed class ConfigurationTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "GCMod.Tests",
        Guid.NewGuid().ToString("N")
    );
    private readonly ConfigFile _file;

    public ConfigurationTests()
    {
        Directory.CreateDirectory(_directory);
        _file = new ConfigFile(Path.Combine(_directory, "legacy.cfg"), false)
        {
            SaveOnConfigSet = false,
        };
        Config.Initialize(_file);
        Toast.Count = 0;
    }

    [Fact]
    public void LegacyValuesRoundTripThroughBepInEx()
    {
        File.WriteAllText(
            _file.ConfigFilePath,
            "[General]\nFrameRate = 144\n[Translation]\nEnabled = false\n[Translation.MasterData]\nEnabledTables = mUnitWords, mSubunitWords\n[Message.Text]\nNameColor = #abc\n"
        );
        _file.Reload();
        Assert.Equal(144, Config.FrameRate.Value);
        Assert.False(Config.Translation.Value);
        Assert.Equal(new[] { "mUnitWords", "mSubunitWords" }, Config.MasterDataTables.Value);
        Assert.Equal("AABBCCFF", Config.NameTextColorHex.Value);
        Assert.Equal(0f, Config.NormalAlpha.Value);
        _file.Save();
        Assert.Contains(
            "EnabledTables = mUnitWords, mSubunitWords",
            File.ReadAllText(_file.ConfigFilePath)
        );
    }

    [Theory]
    [InlineData("  https://example.test/assets  ", "https://example.test/assets")]
    [InlineData("file:///tmp/assets", null)]
    [InlineData("invalid", null)]
    [InlineData("   ", null)]
    public void ResourceUrlIsNormalizedBeforeBeingSaved(string input, string expected)
    {
        var entry = Config.TranslationCDN;
        entry.Value = input;
        Assert.Equal(expected ?? (string)entry.DefaultValue, entry.Value);
        Assert.Equal(0, Toast.Count);
    }

    [Fact]
    public void EmptyFontPathFallsBackAndBooleanNotificationsStopOnShutdown()
    {
        Config.FontBundlePath.Value = "  ";
        Assert.Equal(Config.FontBundlePath.DefaultValue, Config.FontBundlePath.Value);
        Assert.Equal(0, Toast.Count);
        var enabled = Config.Translation;
        enabled.Value = !enabled.Value;
        Assert.Equal(1, Toast.Count);
        Config.Shutdown();
        enabled.Value = !enabled.Value;
        Assert.Equal(1, Toast.Count);
    }

    public void Dispose()
    {
        Config.Shutdown();
        Directory.Delete(_directory, true);
    }
}
