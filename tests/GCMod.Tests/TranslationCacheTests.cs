using System.Net;
using System.Text;
using System.Text.Json;
using GCMod.Services;
using Xunit;

namespace GCMod.Tests;

public class TranslationCacheTests : IDisposable
{
    private const string MasterJson =
        """{"mItems":{"ml_name[]":{"薬":"药"}},"mActionPatterns":{"name":{"AI":"人工智能"}}}""";
    private const string MasterHash = "a9e145ac31f671352213856d150da307";
    private readonly string _cacheDir = Path.Combine(
        Path.GetTempPath(),
        "GCMod.Tests",
        Guid.NewGuid().ToString("N")
    );

    [Fact]
    public async Task NestedMasterUsesNewRoutesAndManifestValidatedCache()
    {
        var requests = new List<string>();
        using var client = new HttpClient(
            new Handler(request =>
            {
                requests.Add(request.RequestUri.AbsolutePath);
                return JsonResponse(
                    request.RequestUri.AbsolutePath.EndsWith("manifest.json")
                        ? $"{{\"master\":\"{MasterHash}\"}}"
                        : MasterJson
                );
            })
        );
        var cache = new TranslationCache("http://local", _cacheDir, "zh-Hans", client);
        await cache.FetchManifestAsync();
        var first = await cache.LoadAsync<
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        >(TranslationPaths.Master);
        var second = await cache.LoadAsync<
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        >(TranslationPaths.Master);

        Assert.Equal("药", first["mItems"]["ml_name[]"]["薬"]);
        Assert.Equal("药", second["mItems"]["ml_name[]"]["薬"]);
        Assert.Equal(
            new[] { "/translations/zh-Hans/manifest.json", "/translations/zh-Hans/master.json" },
            requests
        );
    }

    [Fact]
    public async Task UnavailableRemoteFallsBackToNestedLocalCache()
    {
        Directory.CreateDirectory(Path.Combine(_cacheDir, "zh-Hans"));
        await File.WriteAllTextAsync(Path.Combine(_cacheDir, "zh-Hans", "master.json"), MasterJson);
        using var client = new HttpClient(
            new Handler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
        );
        var cache = new TranslationCache("http://local", _cacheDir, "zh-Hans", client);
        var table = await cache.LoadAsync<
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        >(TranslationPaths.Master);
        Assert.Equal("药", table["mItems"]["ml_name[]"]["薬"]);
    }

    // 预期哈希由新翻译仓库的 scripts/build.py 生成。
    [Theory]
    [InlineData(MasterJson, MasterHash)]
    [InlineData("{\"名前\":\"姓名\",\"台詞\":\"台词\"}", "1c270dad5856196dae8a51b3314e7448")]
    [InlineData(
        "{\"\uffff\":\"尾\",\"😀\":\"笑\",\"a\":{\"x\":\"y\"}}",
        "a76b988d75acae8f66d39976d4714454"
    )]
    public void HashMatchesPythonIncludingUnicodeKeys(string json, string expected)
    {
        Assert.Equal(expected, TranslationHash.Compute(json));
    }

    [Theory]
    [InlineData("names", null, "names.json")]
    [InlineData("master", null, "master.json")]
    [InlineData("manifest", null, "manifest.json")]
    [InlineData("novels", "10005", "novels/10005.json")]
    public void ResourcePathsFollowNewRepository(string type, string id, string suffix)
    {
        Assert.Equal(
            $"http://local/translations/zh-Hans/{suffix}",
            TranslationPaths.BuildRemoteUrl("http://local/", type, "zh-Hans", id)
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDir))
            Directory.Delete(_cacheDir, true);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(respond(request));
    }
}
