using System.Net;
using GCMod.Services;
using Utility.Notifications;
using Xunit;

namespace GCMod.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [InlineData("6.2.0", "v6.3.0", true)]
    [InlineData("6.2.0", "v6.10.0", true)]
    [InlineData("6.2.0", "V6.2.1", true)]
    [InlineData("6.2.0", "v6.2.0", false)]
    [InlineData("6.2.0", "v6.1.9", false)]
    [InlineData("6.2", "6.2.0.0", false)]
    [InlineData("6.2.0", "not-a-version", false)]
    [InlineData("6.2.0", "v6.3.0-rc1", false)]
    public async Task OnlyNewerReleaseProducesNotification(
        string current,
        string tag,
        bool expected
    )
    {
        Toast.InfoMessages.Clear();
        using var client = new HttpClient(
            new Handler(
                (request, _) =>
                {
                    Assert.Equal(HttpMethod.Head, request.Method);
                    Assert.Equal(
                        "https://github.com/anosu/GCMod/releases/latest",
                        request.RequestUri.AbsoluteUri
                    );
                    return Task.FromResult(
                        Response($"https://github.com/anosu/GCMod/releases/tag/{tag}")
                    );
                }
            )
        );

        await UpdateChecker.CheckAsync(client, current);
        Assert.Equal(expected ? 1 : 0, Toast.InfoMessages.Count);
        if (expected)
            Assert.Contains(tag, Assert.Single(Toast.InfoMessages).Message);
    }

    [Theory]
    [InlineData("https://github.com/anosu/GCMod/releases/latest")]
    [InlineData("https://github.com/login")]
    [InlineData("https://github.com/another/repo/releases/tag/v99.0")]
    [InlineData("https://example.com/anosu/GCMod/releases/tag/v99.0")]
    public async Task UnexpectedRedirectDoesNotNotify(string url)
    {
        Toast.InfoMessages.Clear();
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(Response(url))));
        await UpdateChecker.CheckAsync(client, "6.2.0");
        Assert.Empty(Toast.InfoMessages);
    }

    [Fact]
    public async Task NetworkFailureDoesNotEscapeOrNotify()
    {
        Toast.InfoMessages.Clear();
        using var client = new HttpClient(
            new Handler((_, _) => throw new HttpRequestException("offline"))
        );
        await UpdateChecker.CheckAsync(client, "6.2.0");
        Assert.Empty(Toast.InfoMessages);
    }

    [Fact]
    public async Task UnloadCancellationDoesNotNotify()
    {
        Toast.InfoMessages.Clear();
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(
            new Handler(
                (_, _) =>
                {
                    cancellation.Cancel();
                    return Task.FromResult(
                        Response("https://github.com/anosu/GCMod/releases/tag/v99.0")
                    );
                }
            )
        );
        await UpdateChecker.CheckAsync(client, "6.2.0", cancellation.Token);
        Assert.Empty(Toast.InfoMessages);
    }

    private static HttpResponseMessage Response(string url) =>
        new(HttpStatusCode.OK) { RequestMessage = new HttpRequestMessage(HttpMethod.Head, url) };

    private sealed class Handler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond
    ) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => respond(request, cancellationToken);
    }
}
