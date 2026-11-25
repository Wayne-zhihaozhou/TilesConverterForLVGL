Get-Process -Name lvglConverter -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
$p = Start-Process 'bin\Release\net472\lvglConverter.exe' -PassThru
Start-Sleep -Seconds 3
try { $alive = [bool](Get-Process -Id $p.Id -ErrorAction SilentlyContinue) } catch { $alive = False }
Write-Host ('at 3s -> alive: ' + $alive)
if ($alive) { Stop-Process $p.Id -Force; Write-Host 'killed after confirming ALIVE (window open, message loop running)' } else { Write-Host 'process exited early (problem)' }
