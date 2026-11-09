#Requires -RunAsAdministrator
<#
    Installs the DgtAngel shim as the machine-wide DGT driver.

    DGT clients disagree about where the driver comes from: Arena honours its
    own folder, Fritz 14 ignores its own folder entirely and resolves
    DGTEBDLL.dll out of System32. Because of Fritz this has to be machine-wide,
    and because WOW64 redirects a 32-bit process's System32 the two bitnesses
    need different files in different places:

        32-bit clients -> C:\Windows\SysWOW64\DGTEBDLL.dll
        64-bit clients -> C:\Windows\System32\DGTEBDLL.dll

    Release builds are statically linked, so each install is a single DLL with a
    system-only import table. The installer removes the third-party runtime DLLs
    that the earlier dynamic build needed.

    Every touched location gets a DGTEBDLL.dll.previous-<stamp> backup and a
    DgtAngel.installed.txt manifest. See README.md for the background.
#>

[CmdletBinding()]
param(
    [string]$X86 = (Join-Path $PSScriptRoot '..\Release\DgtAngelNative.dll'),
    [string]$X64 = (Join-Path $PSScriptRoot '..\x64\Release\DgtAngelNative.dll'),
    [string]$Arena = '',           # optional: also place the x86 shim here
    [string]$Log = (Join-Path $env:TEMP 'dgtangel-install.log'),
    [switch]$Skip32,
    [switch]$Skip64
)

$ErrorActionPreference = 'Stop'
$lines = New-Object System.Collections.Generic.List[string]
function Say([string]$m) { $script:lines.Add($m); Write-Host $m }

$elevated = (New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $elevated) { throw 'Run this from an elevated PowerShell.' }

# runtime DLLs that only the old dynamic build needed
$dynamicDeps = @(
    'abseil_dll.dll', 'cares.dll', 'libcrypto-3.dll', 'libssl-3.dll',
    'libprotobuf.dll', 'libprotobuf-lite.dll', 're2.dll', 'zlib1.dll'
)

function Install-Shim([string]$dir, [string]$payload, [string]$label)
{
    Say ''
    Say "== $label : $dir =="

    if (-not (Test-Path $dir)) { Say '   skipped, folder does not exist'; return }
    if (-not (Test-Path $payload)) { Say "   skipped, build output missing: $payload"; return }

    $target = Join-Path $dir 'DGTEBDLL.dll'
    if (Test-Path $target)
    {
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        Copy-Item $target "$target.previous-$stamp" -Force
        Say ("   backed up existing {0:n0} bytes" -f (Get-Item $target).Length)
    }

    Copy-Item $payload $target -Force
    Say ("   installed {0:n0} bytes  sha256:{1}" -f (Get-Item $target).Length,
        (Get-FileHash $target -Algorithm SHA256).Hash.Substring(0, 16))

    foreach ($d in $dynamicDeps)
    {
        $p = Join-Path $dir $d
        if (Test-Path $p)
        {
            try { Remove-Item $p -Force -ErrorAction Stop; Say "   removed $d" }
            catch { Say ("   could not remove {0}: {1}" -f $d, $_.Exception.Message) }
        }
    }

    @("installed $(Get-Date -Format o)", "source $payload", 'DGTEBDLL.dll') |
        Set-Content (Join-Path $dir 'DgtAngel.installed.txt') -Encoding UTF8
}

# nothing may hold the driver open while we replace it
foreach ($n in @('ChessProgram14', 'DgtCherub', 'Arena', 'ChessBase'))
{
    Get-Process -Name $n -ErrorAction SilentlyContinue | ForEach-Object {
        Stop-Process -Id $_.Id -Force
        Say "stopped $n ($($_.Id))"
    }
}

if (-not $Skip32) { Install-Shim 'C:\Windows\SysWOW64' $X86 '32-bit clients' }
if (-not $Skip64) { Install-Shim 'C:\Windows\System32' $X64 '64-bit clients' }
if ($Arena)       { Install-Shim $Arena              $X86 'Arena' }

Say ''
Say '== verification =='
foreach ($p in @('C:\Windows\SysWOW64\DGTEBDLL.dll', 'C:\Windows\System32\DGTEBDLL.dll'))
{
    if (-not (Test-Path $p)) { continue }
    $b = [IO.File]::ReadAllBytes($p)
    $pe = [BitConverter]::ToInt32($b, 0x3C)
    $arch = switch ([BitConverter]::ToUInt16($b, $pe + 4)) { 0x14c { 'x86' } 0x8664 { 'x64' } default { '?' } }
    Say ("   {0,-46} {1,-3} {2,10:n0}  sha256:{3}" -f $p, $arch, $b.Length,
        (Get-FileHash $p -Algorithm SHA256).Hash.Substring(0, 16))
}

Say ''
Say 'Start Cherub first: it owns the real driver.'
$lines | Set-Content -Path $Log -Encoding UTF8
Write-Host "log -> $Log"
