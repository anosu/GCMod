using System.Diagnostics;
using System.Text.Json;
using DMM.OLG.Unity.Extensions.Novel;
using GCMod.Patches;
using GCMod.Services;
using TMPro;
using Utility.Assets;
using Xunit;
using static GCMod.Tests.TranslationReliabilityTests;

namespace GCMod.Tests;

public sealed class TranslationManagerTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "GCMod.Tests",
        Guid.NewGuid().ToString("N")
    );

    public TranslationManagerTests()
    {
        Config.Translation.Value = true;
        Config.AsyncMode.Value = false;
        Config.ModifyText.Value = false;
        Config.MasterDataTables.Value = [];
    }

    [Fact]
    public async Task RefreshReloadsTheCurrentlyPlayingNovel()
    {
        string translation = "first";
        using var client = new HttpClient(
            new Handler(
                (request, _) =>
                    Task.FromResult(
                        Json(
                            request.RequestUri.AbsolutePath.Contains("/novels/")
                                ? JsonSerializer.Serialize(new { jp = translation })
                                : "{}"
                        )
                    )
            )
        );
        using var manager = CreateManager(client);
        TranslationPatch.SetupTranslation("novel", "101");
        Assert.Equal("first", RenderMessage());

        translation = "refreshed";
        manager.Refresh();
        await manager.LoadTranslationAsync();
        Assert.Equal("refreshed", RenderMessage());
    }

    [Fact]
    public async Task ReenablingTranslationLoadsTheNovelEnteredWhileDisabled()
    {
        using var client = new HttpClient(
            new Handler(
                (request, _) =>
                    Task.FromResult(
                        Json(
                            request.RequestUri.AbsolutePath.EndsWith("/novels/101.json")
                                ? """{"jp":"old novel"}"""
                            : request.RequestUri.AbsolutePath.EndsWith("/novels/202.json")
                                ? """{"jp":"current novel"}"""
                            : "{}"
                        )
                    )
            )
        );
        using var manager = CreateManager(client);
        TranslationPatch.SetupTranslation("novel", "101");
        Assert.Equal("old novel", RenderMessage());

        Config.Translation.Value = false;
        TranslationPatch.SetupTranslation("novel", "202");
        Assert.Equal(202, manager.CurrentNovelId);
        Assert.Equal("jp", RenderMessage());
        Config.Translation.Value = true;
        await manager.LoadTranslationAsync(); // F8 开启时使用的加载入口。
        Assert.Equal("current novel", RenderMessage());
    }

    [Fact]
    public async Task CachedMasterTranslatesBeforeNetworkManifestCompletes()
    {
        Config.MasterDataTables.Value = ["*"];
        string language = Path.Combine(_directory, "zh-Hans");
        Directory.CreateDirectory(language);
        const string master = """{"mTest":{"name":{"jp":"cached translation"}}}""";
        await File.WriteAllTextAsync(
            Path.Combine(language, "manifest.json"),
            JsonSerializer.Serialize(
                new { master = Utility.Cryptography.StringTableHash.Compute(master) }
            )
        );
        await File.WriteAllTextAsync(Path.Combine(language, "master.json"), master);
        using var client = new HttpClient(
            new Handler(
                async (_, token) =>
                {
                    await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
                    return Json("{}");
                }
            )
        )
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
        using var manager = CreateManager(client);
        var elapsed = Stopwatch.StartNew();
        string result = manager.TranslateMasterData("mTest", """{"name":"jp"}""");
        Assert.Equal("cached translation", JsonNodeValue(result));
        Assert.True(
            elapsed.Elapsed < TimeSpan.FromSeconds(5),
            "Local cache should not wait for the 10-second network budget"
        );
    }

    private TranslationManager CreateManager(HttpClient client)
    {
        var manager = new TranslationManager(
            () => new TranslationCache("http://local", _directory, "zh-Hans", client),
            new AssetBundleLoader<TMP_FontAsset>()
        );
        Plugin.Trans = manager;
        return manager;
    }

    private static string RenderMessage()
    {
        string message = "jp";
        TranslationPatch.SetMessageText(new EventText(), ref message);
        return message;
    }

    private static string JsonNodeValue(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("name").GetString();
    }

    public void Dispose()
    {
        Plugin.Trans = null;
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
