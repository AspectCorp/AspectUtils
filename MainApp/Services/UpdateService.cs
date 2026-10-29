using System.Net.Http;
using System.Text.Json;

namespace AspectUtils.Desktop.Services;

public class UpdateService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public class GitHubRelease
    {
        public string tag_name { get; set; } = "";
        public string name { get; set; } = "";
        public string html_url { get; set; } = "";
        public bool prerelease { get; set; }
        public bool draft { get; set; }
    }

    public static async Task<(bool HasUpdate, string LatestVersion, string Url)> CheckForUpdateAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://api.github.com/repos/AspectCorp/AspectUtils/releases/latest");
            request.Headers.UserAgent.ParseAdd("AspectUtils-UpdateChecker");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return (false, "", "");
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release is null || string.IsNullOrWhiteSpace(release.tag_name))
            {
                return (false, "", "");
            }

            var latestTag = release.tag_name.TrimStart('v');
            if (Version.TryParse(latestTag, out var latestVersion) &&
                Version.TryParse(BrandInfo.Version, out var currentVersion) &&
                latestVersion > currentVersion)
            {
                return (true, release.tag_name, release.html_url);
            }

            return (false, release.tag_name, release.html_url);
        }
        catch
        {
            return (false, "", "");
        }
    }
}
