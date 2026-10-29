namespace AspectUtils.Desktop.Services;

public static class PowerShellCompat
{
    /// <summary>
    /// Tweak scripts in the shared configuration call two explorer helpers that normally come from
    /// the WinUtil module. Desktop runs each script in a fresh PowerShell process, so define the
    /// helpers first instead of failing partway through a tweak.
    /// </summary>
    private const string Prelude = """
function Invoke-WinUtilExplorerUpdate {
    param([string]$action = 'refresh')

    if ($action -eq 'restart') {
        taskkill.exe /F /IM explorer.exe 2>$null | Out-Null
        Start-Process explorer.exe
        return
    }

    if (-not ([System.Management.Automation.PSTypeName]'AspectUtilsWin32').Type) {
        Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class AspectUtilsWin32 {
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, IntPtr wParam, string lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
}
"@
    }

    [AspectUtilsWin32]::SendMessageTimeout(
        [IntPtr]0xffff, 0x1A, [IntPtr]::Zero, 'ImmersiveColorSet', 0x2, 100, [ref]([IntPtr]::Zero)) | Out-Null
}

function Invoke-WinutilThemeChange {
    param([string]$theme = 'Auto')
    # The desktop shell owns its own theme; the tweak's registry values are what matter here.
}
""";

    public static string Wrap(string script) => Prelude + Environment.NewLine + script;
}
