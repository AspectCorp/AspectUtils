using System.Runtime.InteropServices;

namespace AspectUtils.Desktop.Services;

public static class ResolutionService
{
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const uint DM_PELSWIDTH = 0x00080000;
    private const uint DM_PELSHEIGHT = 0x00100000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

    public static (int Width, int Height) GetCurrent()
    {
        var dm = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
        if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm) == 0)
        {
            return (0, 0);
        }
        return ((int)dm.dmPelsWidth, (int)dm.dmPelsHeight);
    }

    public static bool TrySet(int width, int height, out string message)
    {
        if (width <= 0 || height <= 0)
        {
            message = "Width and height must be positive numbers.";
            return false;
        }

        var dm = new DEVMODE
        {
            dmSize = (short)Marshal.SizeOf<DEVMODE>(),
            dmPelsWidth = (uint)width,
            dmPelsHeight = (uint)height,
            dmFields = DM_PELSWIDTH | DM_PELSHEIGHT
        };

        int result = ChangeDisplaySettings(ref dm, 0);
        message = result switch
        {
            0 => $"Applied {width}x{height}.",
            1 => "The graphics mode is not supported.",
            -1 => "The display driver failed to apply the mode.",
            -2 => "The settings were saved in the registry but not applied.",
            _ => $"ChangeDisplaySettings returned {result}."
        };
        return result == 0;
    }
}
