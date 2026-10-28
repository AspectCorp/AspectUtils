using System.Management;
using System.Windows;

namespace AspectUtils.Desktop.Services;

public class DriverCategory
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public bool Detected { get; set; }
    public string DetectedText { get; set; } = "";
    public List<string> Packages { get; set; } = new();
}

public class DriverService
{
    public List<DriverCategory> DetectCategories()
    {
        var categories = new List<DriverCategory>();
        try
        {
            // Chipset / Motherboard (Win32_PNPDevice with motherboard class GUID)
            var chipsetDetected = false;
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PNPDevice WHERE ClassGuid LIKE '%{4d36e97d-e325-11ce-bfc1-08002be10318}%'");
                chipsetDetected = searcher.Get().Count > 0;
            }
            catch { }

            categories.Add(new DriverCategory
            {
                Key = "chipset",
                Label = "Chipset / Motherboard",
                Detected = chipsetDetected,
                Packages = new List<string> { "Intel-Chipset-Device-Software", "AMD-Chipset-Software" }
            });

            // Audio
            var audioDetected = false;
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
                audioDetected = searcher.Get().Count > 0;
            }
            catch { }
            categories.Add(new DriverCategory
            {
                Key = "audio",
                Label = "Audio / Sound",
                Detected = audioDetected,
                Packages = new List<string> { "Realtek.Audio.Driver", "Realtek-HD-Audio-Driver" }
            });

            // GPU
            var gpuDetected = false;
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController WHERE Name IS NOT NULL");
                gpuDetected = searcher.Get().Count > 0;
            }
            catch { }
            categories.Add(new DriverCategory
            {
                Key = "gpu",
                Label = "GPU / Graphics",
                Detected = gpuDetected,
                Packages = new List<string> { "NVIDIA.Graphics.Driver", "AMD.Radeon.Driver" }
            });

            // Network / Internet
            var networkDetected = false;
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionStatus IS NOT NULL");
                networkDetected = searcher.Get().Count > 0;
            }
            catch { }
            categories.Add(new DriverCategory
            {
                Key = "network",
                Label = "Network / Internet",
                Detected = networkDetected,
                Packages = new List<string> { "Intel.Network.Driver", "Realtek-Network-Driver" }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Driver detection failed: {ex.Message}", "A$PECT UTILS",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return categories;
    }
}
