using System.Net;
using GCMod.Services;
using Xunit;
using static GCMod.Tests.TranslationReliabilityTests;

namespace GCMod.Tests;

public sealed class TranslationSessionTests : IDisposable
{
    private const string Master = """{"mTest":{"name":{"名前":"名称"}}}""";
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "GCMod.Tests",
        Guid.NewGuid().ToString("N")
    );

    [Fact]
    public async Task MasterCanRetryAfterFirstFailure()
    {
        int attempts = 0;
        using var client = new HttpClient(
            new Handler(
                (request, _) =>
                    Task.FromResult(
                        request.RequestUri.AbsolutePath.EndsWith("manifest.json") ? Json("{}")
                        : Interlocked.Increment(ref attempts) == 1
                            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                        : Json(Master)
                    )
            )
        );
        await using var session = CreateSession(client);
        Assert.Null(await session.GetMasterAsync());
        var translator = await session.GetMasterAsync();
        Assert.NotNull(translator);
        Assert.Contains("名称", translator.Translate("mTest", """{"name":"名前"}""", ["*"]));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ConcurrentMasterCallersShareTaskAndCompiledRules()
    {
        var release = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using var client = new HttpClient(
            new Handler(
                (request, _) =>
                    request.RequestUri.AbsolutePath.EndsWith("manifest.json")
                        ? Task.FromResult(Json("{}"))
                        : release.Task
            )
        );
        await using var session = CreateSession(client);
        var first = session.GetMasterAsync();
        var second = session.GetMasterAsync();
        Assert.Same(first, second);
        release.SetResult(Json(Master));
        Assert.Same(await first, await second);
        Assert.Same(first, session.GetMasterAsync());
    }

    [Fact]
    public async Task LoadingDoesNotPostContinuationsToCallingSynchronizationContext()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using var client = new HttpClient(
            new Handler(
                (request, _) =>
                    request.RequestUri.AbsolutePath.EndsWith("manifest.json")
                        ? Task.FromResult(Json("{}"))
                        : response.Task
            )
        );
        await using var session = CreateSession(client);
        var previous = SynchronizationContext.Current;
        var context = new UnpumpedContext();
        Task<Dictionary<string, string>> task;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            task = session.GetNovelAsync(1);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        response.SetResult(Json("""{"text":"译文"}"""));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal("译文", result["text"]);
        Assert.Equal(0, context.Posts);
    }

    [Fact]
    public async Task ReplacingSessionCancelsOldRequestAndKeepsNewLanguageSnapshot()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(
            new Handler(
                async (request, token) =>
                {
                    if (request.RequestUri.AbsolutePath.EndsWith("manifest.json"))
                        return Json("{}");
                    if (request.RequestUri.Host == "old")
                    {
                        started.SetResult();
                        await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
                    }
                    return Json("""{"text":"new language"}""");
                }
            )
        );
        var old = CreateSession(client, "http://old", "old-language");
        var pending = old.GetNovelAsync(1);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await using var current = CreateSession(client, "http://new", "new-language");
        await old.DisposeAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await current.GetNovelAsync(1);
        Assert.False(old.TryGetNovel(1, out _));
        Assert.True(current.TryGetNovel(1, out var translations));
        Assert.Equal("new language", translations["text"]);
        Assert.Throws<ObjectDisposedException>(() =>
        {
            _ = old.GetNovelAsync(2);
        });
    }

    private TranslationSession CreateSession(
        HttpClient client,
        string cdn = "http://local",
        string language = "zh-Hans"
    ) => new(new TranslationCache(cdn, _directory, language, client, TimeSpan.Zero));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    private sealed class UnpumpedContext : SynchronizationContext
    {
        public int Posts;

        public override void Post(SendOrPostCallback callback, object state) =>
            Interlocked.Increment(ref Posts);
    }
}
