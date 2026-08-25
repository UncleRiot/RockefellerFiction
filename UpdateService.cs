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
    private const string ReleasesApiUrl =
     "https://api.github.com/repos/UncleRiot/RockefellerFiction/releases?per_page=100";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            using HttpResponseMessage response =
             await HttpClient.GetAsync(ReleasesApiUrl);

            if (!response.IsSuccessStatusCode)
                return null;

            await using Stream stream =
             await response.Content.ReadAsStreamAsync();

            using JsonDocument document =
             await JsonDocument.ParseAsync(stream);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            SemanticVersion? currentVersion =
             GetCurrentVersion();

            if (currentVersion == null)
                return null;

            JsonElement? latestRelease = null;
            SemanticVersion? latestVersion = null;

            foreach (JsonElement release in document.RootElement.EnumerateArray())
            {
                bool isDraft =
                 release.TryGetProperty("draft", out JsonElement draftElement) &&
                 draftElement.ValueKind == JsonValueKind.True;

                if (isDraft)
                    continue;

                string tagName =
                 release.TryGetProperty("tag_name", out JsonElement tagElement)
                  ? tagElement.GetString() ?? ""
                  : "";

                if (!SemanticVersion.TryParse(tagName, out SemanticVersion? candidate))
                    continue;

                if (latestVersion == null ||
                    candidate.CompareTo(latestVersion) > 0)
                {
                    latestVersion = candidate;
                    latestRelease = release.Clone();
                }
            }

            if (latestVersion == null ||
                latestRelease == null ||
                latestVersion.CompareTo(currentVersion) <= 0)
                return null;

            JsonElement root = latestRelease.Value;

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
             currentVersion.ToString(),
             latestVersion.ToString(),
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

    private static SemanticVersion? GetCurrentVersion()
    {
        Assembly assembly =
         Assembly.GetExecutingAssembly();

        string informationalVersion =
         assembly
          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
          .InformationalVersion ??
         "";

        if (SemanticVersion.TryParse(
             informationalVersion,
             out SemanticVersion? semanticVersion))
            return semanticVersion;

        Version? assemblyVersion =
         assembly.GetName().Version;

        if (assemblyVersion == null)
            return null;

        return new SemanticVersion(
         assemblyVersion.Major,
         assemblyVersion.Minor,
         Math.Max(0, assemblyVersion.Build),
         []);
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

    private sealed class SemanticVersion : IComparable<SemanticVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public IReadOnlyList<string> PreRelease { get; }

        public SemanticVersion(
         int major,
         int minor,
         int patch,
         IReadOnlyList<string> preRelease)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            PreRelease = preRelease;
        }

        public static bool TryParse(
         string value,
         out SemanticVersion? version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string normalized = value.Trim();

            if (normalized.StartsWith(
                 "v",
                 StringComparison.OrdinalIgnoreCase))
                normalized = normalized[1..];

            int buildMetadataIndex =
             normalized.IndexOf('+');

            if (buildMetadataIndex >= 0)
                normalized =
                 normalized[..buildMetadataIndex];

            string corePart;
            string? preReleasePart = null;

            int preReleaseIndex =
             normalized.IndexOf('-');

            if (preReleaseIndex >= 0)
            {
                corePart =
                 normalized[..preReleaseIndex];

                preReleasePart =
                 normalized[(preReleaseIndex + 1)..];
            }
            else
            {
                corePart = normalized;
            }

            string[] coreParts =
             corePart.Split('.');

            if (coreParts.Length != 3 ||
                !int.TryParse(coreParts[0], out int major) ||
                !int.TryParse(coreParts[1], out int minor) ||
                !int.TryParse(coreParts[2], out int patch) ||
                major < 0 ||
                minor < 0 ||
                patch < 0)
                return false;

            List<string> preRelease = [];

            if (preReleasePart != null)
            {
                if (string.IsNullOrWhiteSpace(preReleasePart))
                    return false;

                foreach (string identifier in preReleasePart.Split('.'))
                {
                    if (string.IsNullOrWhiteSpace(identifier) ||
                        identifier.Any(character =>
                         !char.IsLetterOrDigit(character) &&
                         character != '-'))
                        return false;

                    if (identifier.All(char.IsDigit) &&
                        identifier.Length > 1 &&
                        identifier[0] == '0')
                        return false;

                    preRelease.Add(identifier);
                }
            }

            version =
             new SemanticVersion(
              major,
              minor,
              patch,
              preRelease);

            return true;
        }

        public int CompareTo(SemanticVersion? other)
        {
            if (other == null)
                return 1;

            int coreComparison =
             Major.CompareTo(other.Major);

            if (coreComparison != 0)
                return coreComparison;

            coreComparison =
             Minor.CompareTo(other.Minor);

            if (coreComparison != 0)
                return coreComparison;

            coreComparison =
             Patch.CompareTo(other.Patch);

            if (coreComparison != 0)
                return coreComparison;

            bool thisIsStable =
             PreRelease.Count == 0;

            bool otherIsStable =
             other.PreRelease.Count == 0;

            if (thisIsStable && otherIsStable)
                return 0;

            if (thisIsStable)
                return 1;

            if (otherIsStable)
                return -1;

            int commonLength =
             Math.Min(
              PreRelease.Count,
              other.PreRelease.Count);

            for (int index = 0;
                 index < commonLength;
                 index++)
            {
                string left =
                 PreRelease[index];

                string right =
                 other.PreRelease[index];

                bool leftIsNumeric =
                 long.TryParse(
                  left,
                  out long leftNumber);

                bool rightIsNumeric =
                 long.TryParse(
                  right,
                  out long rightNumber);

                if (leftIsNumeric &&
                    rightIsNumeric)
                {
                    int numericComparison =
                     leftNumber.CompareTo(
                      rightNumber);

                    if (numericComparison != 0)
                        return numericComparison;

                    continue;
                }

                if (leftIsNumeric)
                    return -1;

                if (rightIsNumeric)
                    return 1;

                int textComparison =
                 string.CompareOrdinal(
                  left,
                  right);

                if (textComparison != 0)
                    return textComparison;
            }

            return
             PreRelease.Count.CompareTo(
              other.PreRelease.Count);
        }

        public override string ToString()
        {
            string version =
             $"{Major}.{Minor}.{Patch}";

            if (PreRelease.Count > 0)
                version +=
                 "-" +
                 string.Join(
                  ".",
                  PreRelease);

            return version;
        }
    }
}
