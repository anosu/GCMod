using System.Reflection;
using BepInEx.Configuration;
using GCMod.Patches;
using Xunit;

namespace GCMod.Tests;

public sealed class RuntimeControllerTests : IDisposable
{
    private readonly RuntimeController _runtime = new();

    public RuntimeControllerTests()
    {
        Config.Initialize(
            new ConfigFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cfg"), false)
            {
                SaveOnConfigSet = false,
            }
        );
        Plugin.Trans = new();
        VisualPatch.Applied = 0;
        EnhancePatch.Applied = 0;
        Invoke("OnEnable");
    }

    [Fact]
    public void ConfigChangesWaitForUpdateAndAreCoalesced()
    {
        Config.NormalAlpha.Value = 0.4f;
        Config.CgModeAlpha.Value = 0.8f;
        Config.FrameRate.Value = 144;
        Config.Translation.Value = false;
        Config.TranslationCDN.Value = "https://example.test/new";
        Assert.Equal(0, VisualPatch.Applied);
        Assert.Equal(0, EnhancePatch.Applied);
        Assert.Equal(0, Plugin.Trans.Refreshes);

        Invoke("Update");
        Assert.Equal(1, VisualPatch.Applied);
        Assert.Equal(1, EnhancePatch.Applied);
        Assert.Equal(1, Plugin.Trans.Refreshes);
        Assert.Equal(0, Plugin.Trans.Loads);
        Invoke("Update");
        Assert.Equal(1, VisualPatch.Applied);
        Assert.Equal(1, Plugin.Trans.Refreshes);
    }

    [Fact]
    public void DisableRemovesSubscriptionsAndQueuedWork()
    {
        Config.FrameRate.Value = 120;
        Invoke("OnDisable");
        Config.NormalAlpha.Value = 0.5f;
        Invoke("Update");
        Assert.Equal(0, EnhancePatch.Applied);
        Assert.Equal(0, VisualPatch.Applied);
    }

    [Theory]
    [InlineData("#abc", "AABBCCFF")]
    [InlineData("abcd", "AABBCCDD")]
    [InlineData("123456", "123456FF")]
    [InlineData("12345678", "12345678")]
    [InlineData("invalid", "FFFFFFFF")]
    public void ColorsRemainCompatibleWithHexConfig(string source, string expected)
    {
        Config.NameTextColorHex.Value = source;
        Assert.Equal(expected, Config.NameTextColorHex.Value);
        Assert.InRange(Config.NameTextColor.a, 0f, 1f);
    }

    [Fact]
    public void NumericConstraintsClampOnlyTheirSupportedDomain()
    {
        Config.FrameRate.Value = -10;
        Assert.Equal(1, Config.FrameRate.Value);
        Config.FrameRate.Value = 1000;
        Assert.Equal(1000, Config.FrameRate.Value);
        Config.NormalAlpha.Value = 2;
        Assert.Equal(1f, Config.NormalAlpha.Value);
        Config.FaceDilate.Value = -2;
        Assert.Equal(-1f, Config.FaceDilate.Value);
    }

    private void Invoke(string method) =>
        typeof(RuntimeController)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_runtime, null);

    public void Dispose()
    {
        Invoke("OnDisable");
        Config.Shutdown();
    }
}
