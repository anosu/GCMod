using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Utility.Notifications;

namespace GCMod.Services;

/// <summary>通过 GitHub 最新 Release 的重定向检查更新，不使用 GitHub API。</summary>
internal static class UpdateChecker
{
    private const string LatestReleaseUrl = "https://github.com/anosu/GCMod/releases/latest";
    private const string TagPath = "/anosu/GCMod/releases/tag/";

    public static async Task CheckAsync(
        HttpClient httpClient,
        string currentVersion,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, LatestReleaseUrl);
            using var response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn(
                    $"Update check failed: {(int)response.StatusCode} {response.StatusCode}"
                );
                return;
            }

            Uri releaseUri = response.RequestMessage?.RequestUri;
            string path = releaseUri?.AbsolutePath.TrimEnd('/');
            if (
                releaseUri?.Host != "github.com"
                || path == null
                || !path.StartsWith(TagPath, StringComparison.OrdinalIgnoreCase)
            )
            {
                Logger.Warn($"Update check returned an unexpected URL: {releaseUri}");
                return;
            }

            string tag = Uri.UnescapeDataString(path[TagPath.Length..]);
            if (
                !TryParseVersion(tag, out var latest)
                || !TryParseVersion(currentVersion, out var current)
            )
            {
                Logger.Warn($"Update check could not compare versions: {currentVersion} / {tag}");
                return;
            }
            if (latest <= current)
                return;

            Logger.Info(
                $"New GCMod version available: {currentVersion} -> {tag}. Release: {releaseUri}"
            );
            Toast.Info(
                "GCMod 发现新版本",
                $"当前：{currentVersion} → 最新：{tag}\n请到 GitHub Releases 下载更新"
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (TaskCanceledException)
        {
            Logger.Warn("Update check timed out");
        }
        catch (Exception e)
        {
            Logger.Warn($"Update check failed: {e.Message}");
        }
    }

    private static bool TryParseVersion(string text, out Version version)
    {
        version = null;
        text = text?.Trim();
        if (!string.IsNullOrEmpty(text) && (text[0] == 'v' || text[0] == 'V'))
            text = text[1..];
        if (!Version.TryParse(text, out var parsed))
            return false;

        // 将 6.2、6.2.0、6.2.0.0 视为同一版本。
        version = new Version(
            parsed.Major,
            parsed.Minor,
            Math.Max(0, parsed.Build),
            Math.Max(0, parsed.Revision)
        );
        return true;
    }
}
