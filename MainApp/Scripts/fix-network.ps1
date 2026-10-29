$ErrorActionPreference = 'Continue'

Write-Host 'Resetting Winsock...'
netsh winsock reset | Out-Host

Write-Host 'Resetting the IP stack...'
netsh int ip reset | Out-Host

Write-Host 'Flushing the DNS resolver cache...'
ipconfig /flushdns | Out-Host

Write-Host 'Network configuration reset. Restart Windows to finish applying the changes.'
