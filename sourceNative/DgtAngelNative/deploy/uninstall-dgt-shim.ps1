#Requires -RunAsAdministrator
<#
    Removes the DgtAngel shim and puts the previous driver back.

    Preference order for the file to restore:
      1. an explicit -Backup name
      2. a *.genuine-* copy, which the installer used when it replaced DGT's own
         driver
      3. the oldest *.previous-* / *.bak-* copy

    Nothing here can restore a driver that was never backed up, so the first run
    of install-dgt-shim.ps1 is the one that matters.
#>

[CmdletBinding()]
param(
    [string]$Backup = '',
    [string]$Arena = '',
    [string]$Log = (Join-Path $env:TEMP 'dgtangel-uninstall.log')
)

$ErrorActionPreference = 'Stop'
$lines = New-Object System.Collections.Generic.List[string]
function Say([string]$m) { $script:lines.Add($m); Write-Host $m }

$elevated = (New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $elevated) { throw 'Run this from an elevated PowerShell.' }

foreach ($n in @('ChessProgram14', 'DgtCherub', 'Arena', 'ChessBase'))
{
    Get-Process -Name $n -ErrorAction SilentlyContinue | ForEach-Object {
        Stop-Process -Id $_.Id -Force
        Say "stopped $n ($($_.Id))"
    }
}
Start-Sleep -Milliseconds 800

function Restore-Shim([string]$dir, [string]$label)
{
    Say ''
    Say "== $label : $dir =="
    if (-not (Test-Path $dir)) { Say '   skipped, folder does not exist'; return }

    $target = Join-Path $dir 'DGTEBDLL.dll'

    $candidates = @()
    if ($Backup) { $candidates = @(Join-Path $dir $Backup) }
    else
    {
        $candidates = @(
            Get-ChildItem $dir -Filter 'DGTEBDLL.dll.genuine-*' -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending
            Get-ChildItem $dir -Filter 'DGTEBDLL.dll.previous-*' -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime
            Get-ChildItem $dir -Filter 'DGTEBDLL.dll.bak-*' -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime
        ) | Where-Object { $_ } | ForEach-Object { $_.FullName }
    }

    $restore = $candidates | Select-Object -First 1
    if (-not $restore -or -not (Test-Path $restore))
    {
        Say '   no backup found; leaving DGTEBDLL.dll alone'
        return
    }

    Copy-Item $restore $target -Force
    Say ("   restored {0} ({1:n0} bytes)" -f (Split-Path $restore -Leaf), (Get-Item $target).Length)

    Remove-Item (Join-Path $dir 'DgtAngel.installed.txt') -Force -ErrorAction SilentlyContinue
}

Restore-Shim 'C:\Windows\SysWOW64' '32-bit clients'
Restore-Shim 'C:\Windows\System32' '64-bit clients'
if ($Arena) { Restore-Shim $Arena 'Arena' }

Say ''
Say 'Backups (genuine-*, previous-*, bak-*) are left in place to be deleted by hand.'
$lines | Set-Content -Path $Log -Encoding UTF8
Write-Host "log -> $Log"
