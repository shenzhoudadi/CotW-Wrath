#Requires -Version 7.4
param([Parameter(Mandatory = $true)][string]$ArchivePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$caseRoot = Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))) ('work/package-negative-tests/' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $caseRoot -Force
$prefix = 'CallOfTheWildWrathPrototype/'
$archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
try {
    $reader = [IO.StreamReader]::new($archive.GetEntry($prefix + 'Localization/zhCN.json').Open())
    try { $language = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
} finally { $archive.Dispose() }
# A Unicode sentinel is deliberately invalid gameplay text; no translation.
$language.strings | Add-Member -NotePropertyName 'cotw-wrath.english-only-negative' -NotePropertyValue ([string][char]0x4E2D)
$invalidLanguage = $language | ConvertTo-Json -Depth 20
$cases = @(
    @{ Name='custom-cjk-gameplay'; Entry='Localization/zhCN.json'; Value=$invalidLanguage; Replace=$true },
    @{ Name='invalid-settings'; Entry='OwlcatModificationSettings.json'; Value='not-json'; Replace=$true },
    @{ Name='text-dll'; Entry='Assemblies/CallOfTheWild.Wrath.Prototype.dll'; Value='not a DLL'; Replace=$true },
    @{ Name='empty-assembly-extra'; Entry='Assemblies/README.txt'; Value=''; Replace=$false },
    @{ Name='missing-bundles-directory'; Entry='Bundles/'; Remove=$true }
)
foreach ($case in $cases) {
    $path = Join-Path $caseRoot ($case.Name + '.zip')
    Copy-Item -LiteralPath $ArchivePath -Destination $path
    $zip = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Update)
    try {
        if ($case['Replace'] -or $case['Remove']) { $zip.GetEntry($prefix + $case.Entry).Delete() }
        if (!$case['Remove']) {
            $entry = $zip.CreateEntry($prefix + $case.Entry)
            if ($case.Value.Length -gt 0) {
                $writer = [IO.StreamWriter]::new($entry.Open())
                try { $writer.Write($case.Value) } finally { $writer.Dispose() }
            }
        }
    } finally { $zip.Dispose() }
    $rejected = $false
    try { & (Join-Path $PSScriptRoot 'Verify-Package.ps1') -ArchivePath $path | Out-Null }
    catch { $rejected = $true }
    if (!$rejected) { throw "Invalid package was accepted: $($case.Name)" }
}
Write-Output 'PASS: 5 broken-package scenarios rejected (invalid JSON, fake DLL, empty assembly extra, missing directory, forbidden custom CJK gameplay text).'
