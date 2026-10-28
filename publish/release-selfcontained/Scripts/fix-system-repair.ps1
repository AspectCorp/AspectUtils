$ErrorActionPreference = 'Continue'

$steps = @(
    @{
        Label = 'Checking the disk for errors'
        Arguments = '/c chkdsk /scan /perf'
        SuccessCodes = @(0, 1, 2)
    },
    @{
        Label = 'Scanning protected system files'
        Arguments = '/c sfc /scannow'
        SuccessCodes = @(0)
    },
    @{
        Label = 'Repairing the Windows image'
        Arguments = '/c dism /online /cleanup-image /restorehealth'
        SuccessCodes = @(0, 3010)
    }
)

foreach ($step in $steps) {
    Write-Host "$($step.Label)..."
    $process = Start-Process cmd.exe -ArgumentList $step.Arguments -NoNewWindow -Wait -PassThru
    if ($step.SuccessCodes -notcontains $process.ExitCode) {
        Write-Error "$($step.Label) failed with exit code $($process.ExitCode)."
        exit 1
    }

    Write-Host "$($step.Label) finished with exit code $($process.ExitCode)."
}

Write-Host 'System repair finished. Restart Windows if DISM requested it.'
