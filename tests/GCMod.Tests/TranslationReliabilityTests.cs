using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using GCMod.Services;
using Xunit;

namespace GCMod.Tests;

public sealed class TranslationReliabilityTests : IDisposable
{
    private const string Data = """{"名前":"姓名"}""";
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "GCMod.Tests",
        Guid.NewGuid().ToString("N")
    );

    [Fact]
    public async Task CorruptCacheIsRedownloadedAndReplaced()
    {
        string path = CachePath("names.json");
        await File.WriteAllTextAsync(path, "{broken");
        using var client = new HttpClient(
            new Handler(
                (request, _) =>
                    Task.FromResult(
                        Json(
                            request.RequestUri.AbsolutePath.EndsWith("manifest.json")
                                ? JsonSerializer.Serialize(
                                    new
                                    {
                                        names = Utility.Cryptography.StringTableHash.Compute(Data),
                                    }
                                )
                                : Data
                        )
                    )
            )
        );
        var cache = CreateCache(client);
        await cache.FetchManifestAsync();
        var result = await cache.LoadAsync(TranslationPaths.Names);
        Assert.Equal("姓名", result["名前"]);
        Assert.Equal(Data, await File.ReadAllTextAsync(path));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SaveFailureDoesNotDiscardDownloadedDataOrLeaveTemporaryFiles()
    {
        Directory.CreateDirectory(CachePath("names.json")); // 目录占据目标文件，强制提交失败。
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(Json(Data))));
        var result = await CreateCache(client).LoadAsync(TranslationPaths.Names);
        Assert.Equal("姓名", result["名前"]);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ConcurrentLoadsShareOneRequestPerResourceBeyondOldCleanupThreshold()
    {
        var requests = new ConcurrentDictionary<string, int>();
        using var client = new HttpClient(
            new Handler(
                async (request, token) =>
                {
                    requests.AddOrUpdate(
                        request.RequestUri.AbsolutePath,
                        1,
                        (_, count) => count + 1
                    );
                    await Task.Delay(20, token).ConfigureAwait(false);
                    return Json(Data);
                }
            )
        );
        var cache = CreateCache(client);
        var results = await Task.WhenAll(
            Enumerable
                .Range(0, 160)
                .Select(i => cache.LoadAsync(TranslationPaths.Novels, (i % 4).ToString()))
        );
        Assert.All(results, result => Assert.Equal("姓名", result["名前"]));
        Assert.Equal(4, requests.Count);
        Assert.All(requests.Values, count => Assert.Equal(1, count));
    }

    [Fact]
    public async Task CancelledDownloadPreservesExistingFileAndReleasesResourceLock()
    {
        string path = CachePath("names.json");
        const string old = """{"名前":"旧译文"}""";
        await File.WriteAllTextAsync(path, old);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        using var client = new HttpClient(
            new Handler(
                async (_, token) =>
                {
                    if (Interlocked.Increment(ref requests) == 1)
                    {
                        entered.SetResult();
                        await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
                    }
                    return Json(Data);
                }
            )
        );
        var cache = CreateCache(client);
        using var cancellation = new CancellationTokenSource();
        var cancelled = cache.LoadAsync(
            TranslationPaths.Names,
            cancellationToken: cancellation.Token
        );
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        Assert.Equal(old, await File.ReadAllTextAsync(path));
        var result = await cache
            .LoadAsync(TranslationPaths.Names)
            .WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal("姓名", result["名前"]);
    }

    [Fact]
    public async Task FailedLoadsHaveCooldownAndFreshSessionCanRetryImmediately()
    {
        int requests = 0;
        using var client = new HttpClient(
            new Handler(
                (_, _) =>
                    Task.FromResult(
                        Interlocked.Increment(ref requests) == 1
                            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                            : Json(Data)
                    )
            )
        );
        var cache = CreateCache(client);
        Assert.Null(await cache.LoadAsync(TranslationPaths.Names));
        Assert.Null(await cache.LoadAsync(TranslationPaths.Names));
        Assert.Equal(1, requests);
        Assert.Equal("姓名", (await CreateCache(client).LoadAsync(TranslationPaths.Names))["名前"]);
    }

    [Fact]
    public async Task RemoteHashMismatchDoesNotOverwriteUsableLocalCache()
    {
        string path = CachePath("names.json");
        const string old = """{"名前":"旧译文"}""";
        await File.WriteAllTextAsync(path, old);
        using var client = new HttpClient(
            new Handler(
                (request, _) =>
                    Task.FromResult(
                        Json(
                            request.RequestUri.AbsolutePath.EndsWith("manifest.json")
                                ? JsonSerializer.Serialize(
                                    new
                                    {
                                        names = Utility.Cryptography.StringTableHash.Compute(Data),
                                    }
                                )
                                : """{"名前":"错误版本"}"""
                        )
                    )
            )
        );
        var cache = CreateCache(client);
        await cache.FetchManifestAsync();
        Assert.Equal("旧译文", (await cache.LoadAsync(TranslationPaths.Names))["名前"]);
        Assert.Equal(old, await File.ReadAllTextAsync(path));
    }

    private TranslationCache CreateCache(HttpClient client) =>
        new("http://local", _directory, "zh-Hans", client);

    private string CachePath(string file)
    {
        string language = Path.Combine(_directory, "zh-Hans");
        Directory.CreateDirectory(language);
        return Path.Combine(language, file);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    internal static HttpResponseMessage Json(string text) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(text, Encoding.UTF8, "application/json"),
        };

    internal sealed class Handler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond
    ) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => respond(request, cancellationToken);
    }
}
