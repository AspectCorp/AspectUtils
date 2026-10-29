function Invoke-WPFActivateMAS {
    <#
    .SYNOPSIS
        Launches Microsoft Activation Scripts (MAS) in an elevated PowerShell window.
    #>

    Write-WinUtilLog -Component "Activation" -Message "Launching Microsoft Activation Scripts (MAS)."
    Start-Process -FilePath "powershell.exe" -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", "irm https://get.activated.win | iex") -Verb RunAs -NoNewWindow
}
