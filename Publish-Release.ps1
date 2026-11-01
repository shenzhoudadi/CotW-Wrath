#Requires -Version 7.4
param(
    [Parameter(Mandatory = $true)][string]$CertificatePath,
    [string]$DeliveryDirectory = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference = 'Stop'
# The certificate is written LAST in staging, after both ZIPs and all required
# release checks are finished. This command never constructs a delivery ZIP.
$certificate = Get-Content -LiteralPath $CertificatePath -Raw | ConvertFrom-Json
if ($certificate.deliveryReady -cne $true) { throw 'Release certificate is not marked deliveryReady. Finish all release work first.' }
if ($certificate.requiredGameHarmony.version -cne '2.0.4.0' -or $certificate.requiredGameHarmony.mandatoryRegression -cne $true -or
    @($certificate.independentChecks.harmonyRuntimes) -cnotcontains '2.0.4.0') {
    throw 'Release requires the actual game Harmony 2.0.4 regression evidence.'
}
$requiredRuntimePath = [IO.Path]::GetFullPath($certificate.requiredGameHarmony.path)
if (([Reflection.AssemblyName]::GetAssemblyName($requiredRuntimePath)).Version -ne [Version]'2.0.4.0' -or
    (Get-FileHash -LiteralPath $requiredRuntimePath).Hash -ine $certificate.requiredGameHarmony.sha256) {
    throw 'The actual game Harmony differs from the certified mandatory runtime.'
}
$destinationRoot = [IO.Path]::GetFullPath($DeliveryDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
if (!(Test-Path -LiteralPath $destinationRoot -PathType Container)) { throw 'Delivery directory must already exist.' }
if (!$IsWindows) { throw 'This release workflow requires Windows same-volume rename.' }
if (!('CotWRelease.AtomicMove' -as [type])) {
    Add-Type -TypeDefinition @'
using System.ComponentModel;
using System.Runtime.InteropServices;
namespace CotWRelease {
    public static class AtomicMove {
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
        private static extern bool MoveFileExW(string source, string destination, uint flags);
        public static void Move(string source, string destination) {
            // No COPY_ALLOWED and no REPLACE_EXISTING: even volume mount points
            // cannot turn publication into a visible incremental copy.
            if (!MoveFileExW(source, destination, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
'@
}
$plans = @()
$heldStreams = @()
try {
    foreach ($entry in @($certificate.package, $certificate.source)) {
        if (!$entry.path -or $entry.sha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw 'Certificate must identify both completed ZIPs and SHA256 hashes.' }
        $sourcePath = [IO.Path]::GetFullPath($entry.path)
        $name = [IO.Path]::GetFileName($sourcePath)
        if ($name -notmatch '^CallOfTheWildWrathPrototype-\d+\.\d+\.\d+-(experimental|源码)\.zip$') { throw 'Unexpected delivery ZIP filename.' }
        if ($sourcePath.StartsWith($destinationRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Source ZIP must be staged outside the watched delivery directory.' }
        if ([IO.Path]::GetPathRoot($sourcePath) -ine [IO.Path]::GetPathRoot($destinationRoot)) { throw 'Stage on the delivery volume, outside the watched folder; cross-volume publication could expose a partial copy.' }
        $destination = Join-Path $destinationRoot $name
        if (Test-Path -LiteralPath $destination) { throw "Refusing to replace an already published file: $destination" }
        # Reject an unfinished writer and keep the verified bytes read-only until
        # rename completes. Readers and same-volume rename remain allowed.
        $stream = [IO.File]::Open($sourcePath, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::Read -bor [IO.FileShare]::Delete))
        $heldStreams += $stream
        $hasher = [Security.Cryptography.SHA256]::Create()
        try { $hash = [Convert]::ToHexString($hasher.ComputeHash($stream)) } finally { $hasher.Dispose() }
        if ($hash -ine $entry.sha256) { throw "Staged ZIP differs from its final certificate: $name" }
        $plans += [pscustomobject]@{ Source = $sourcePath; Destination = $destination; Name = $name }
    }
    if ($plans.Count -ne 2 -or @($plans.Name | Select-Object -Unique).Count -ne 2) { throw 'Expected one installation ZIP and one source ZIP.' }
    $installName = $plans[0].Name -replace '-experimental\.zip$', ''
    $sourceName = $plans[1].Name -replace '-源码\.zip$', ''
    if ($installName -ceq $plans[0].Name -or $sourceName -ceq $plans[1].Name -or $installName -cne $sourceName) { throw 'Installation/source ZIP versions do not match.' }
    # Each ZIP appears with complete, verified bytes. Two files are not a single
    # filesystem transaction; neither file is copied/written in the watched folder.
    foreach ($plan in $plans) {
        [CotWRelease.AtomicMove]::Move($plan.Source, $plan.Destination)
        Write-Output "Published complete ZIP: $($plan.Destination)"
    }
} finally {
    foreach ($stream in $heldStreams) { $stream.Dispose() }
}
