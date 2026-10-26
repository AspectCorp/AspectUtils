using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AspectUtils.Desktop.Theme;

public class ThemedWindow : Window
{
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            var enabled = 1;

            // 20 is DWMWA_USE_IMMERSIVE_DARK_MODE on Windows 10 2004+; 19 is the older value.
            if (DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
            }
        }
        catch
        {
            // The system decides the title bar when DWM is unavailable.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
}
