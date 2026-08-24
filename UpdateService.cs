using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace RockefellerFiction;

public sealed record UpdateInfo(
 string CurrentVersion,
 string LatestVersion,
 string ReleaseUrl,
 string DownloadUrl,
 string Changelog);

public static class UpdateService
{
    private const string LatestReleaseApiUrl =
     "https://api.github.com/repos/UncleRiot/RockefellerFiction/releases/latest";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            using HttpResponseMessage response =
             await HttpClient.GetAsync(LatestReleaseApiUrl);

            if (!response.IsSuccessStatusCode)
                return null;

            await using Stream stream =
             await response.Content.ReadAsStreamAsync();

            using JsonDocument document =
             await JsonDocument.ParseAsync(stream);

            JsonElement root = document.RootElement;

            string tagName =
             root.TryGetProperty("tag_name", out JsonElement tagElement)
              ? tagElement.GetString() ?? ""
              : "";

            if (!TryParseVersion(tagName, out Version latestVersion))
                return null;

            Version currentVersion =
             Assembly.GetExecutingAssembly().GetName().Version ??
             new Version(0, 0, 0, 0);

            if (latestVersion <= currentVersion)
                return null;

            string releaseUrl =
             root.TryGetProperty("html_url", out JsonElement releaseUrlElement)
              ? releaseUrlElement.GetString() ?? ""
              : "";

            string changelog =
             root.TryGetProperty("body", out JsonElement bodyElement)
              ? bodyElement.GetString() ?? ""
              : "";

            string downloadUrl = FindZipDownloadUrl(root);

            return new UpdateInfo(
             FormatVersion(currentVersion),
             FormatVersion(latestVersion),
             releaseUrl,
             downloadUrl,
             string.IsNullOrWhiteSpace(changelog)
              ? "Für dieses Release wurde kein Changelog hinterlegt."
              : changelog.Trim());
        }
        catch
        {
            return null;
        }
    }

    private static string FindZipDownloadUrl(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out JsonElement assets) ||
            assets.ValueKind != JsonValueKind.Array)
            return "";

        var zipAssets = assets
         .EnumerateArray()
         .Select(asset => new
         {
             Name = asset.TryGetProperty("name", out JsonElement nameElement)
           ? nameElement.GetString() ?? ""
           : "",
             Url = asset.TryGetProperty("browser_download_url", out JsonElement urlElement)
           ? urlElement.GetString() ?? ""
           : ""
         })
         .Where(asset =>
          asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
          !string.IsNullOrWhiteSpace(asset.Url))
         .OrderByDescending(asset =>
          asset.Name.Contains("RockefellerFiction", StringComparison.OrdinalIgnoreCase))
         .ThenBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
         .ToList();

        return zipAssets.FirstOrDefault()?.Url ?? "";
    }

    private static bool TryParseVersion(
     string value,
     out Version version)
    {
        string normalized = value.Trim();

        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[1..];

        int suffixIndex = normalized.IndexOf('-');
        if (suffixIndex >= 0)
            normalized = normalized[..suffixIndex];

        return Version.TryParse(normalized, out version!);
    }

    private static string FormatVersion(Version version)
    {
        return version.Revision > 0
         ? version.ToString(4)
         : version.Build >= 0
          ? version.ToString(3)
          : version.ToString(2);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
         "RockefellerFiction-UpdateCheck");

        client.DefaultRequestHeaders.Accept.ParseAdd(
         "application/vnd.github+json");

        return client;
    }
}