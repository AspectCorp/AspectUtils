using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace AspectUtils.Desktop.Services;

public class UpdateService
{
    private static readonly HttpClient _httpClient = new();
    
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
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AspectUtils-UpdateChecker");
            var response = await _httpClient.GetStringAsync("https://api.github.com/repos/AspectCorp/AspectUtils/releases/latest");
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);
            
            if (release == null || string.IsNullOrEmpty(release.tag_name)) 
                return (false, "", "");
            
            string latestTag = release.tag_name.TrimStart('v');
            string currentVersion = BrandInfo.Version;
            
            if (Version.TryParse(latestTag, out var latestVer) && Version.TryParse(currentVersion, out var currentVer))
            {
                if (latestVer > currentVer)
                {
                    return (true, release.tag_name, release.html_url);
                }
            }
            
            return (false, release.tag_name, release.html_url);
        }
        catch
        {
            return (false, "", "");
        }
    }
    
    public static void ShowUpdateDialog(string latestVersion, string url)
    {
        var result = MessageBox.Show(
            $"A new version of A$PECT UTILS is available!\n\nCurrent version: v{BrandInfo.Version}\nLatest version: {latestVersion}\n\nWould you like to download it from GitHub?",
            "Update Available",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        
        if (result == MessageBoxResult.Yes)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
