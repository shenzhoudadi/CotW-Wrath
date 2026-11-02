#Requires -Version 7.4
param([Parameter(Mandatory = $true)][string]$ArchivePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Reflection.Metadata
$zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($ArchivePath))
$prefix = 'CallOfTheWildWrathPrototype/'
try {
    $preReader = [IO.StreamReader]::new($zip.GetEntry($prefix+'OwlcatModificationManifest.json').Open())
    try { $manifest = $preReader.ReadToEnd() | ConvertFrom-Json } finally { $preReader.Dispose() }
    $required = @('Assemblies/', 'Bundles/', 'Blueprints/', 'Localization/',
        'Assemblies/CallOfTheWild.Wrath.Prototype.dll', 'OwlcatModificationManifest.json',
        'OwlcatModificationSettings.json', 'Localization/enGB.json', 'Localization/zhCN.json',
        'LICENSE-CotW.txt', 'README.zh-CN.md', '测试者必读.txt', '百宝袋快速测试.txt',
        'Modules/Medium/MediumClass.dll', 'Modules/Medium/mediumclass_assets', 'Modules/Medium/LICENSE-CotW.txt',
        'Modules/WayOfTheShield/Way of the shield.dll', 'Modules/WayOfTheShield/NOTICE-WayOfTheShield.txt', 'Modules/WayOfTheShield/upstream-provenance.json')
    if ([Version]$manifest.Version -ge [Version]'0.14.62') { $required += @('0.14.62PsychicFeedback-test-guide.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.63') { $required += @('0.14.63UrbanBloodrager-test-guide.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.64') { $required += @('0.14.64Amnesiac-test-guide.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.65') { $required += @('0.14.65UrbanBloodrager-feedback-test-guide.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.59') { $required += @('LICENSE-LongArm-TTT.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.60') { $required += @('Localization-QL使用说明.txt', 'Localization-QL/CallOfTheWildWrathPrototypeLocalized.json') }
    foreach ($name in $required) {
        if (!$zip.GetEntry($prefix + $name)) { throw "Missing required archive entry: $name" }
    }
    $allowed = @($required | ForEach-Object { $prefix + $_ })
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName -cnotin $allowed) { throw "Unexpected archive entry: $($entry.FullName)" }
        if (!$entry.FullName.EndsWith('/') -and $entry.Length -eq 0) { throw "Empty required file: $($entry.FullName)" }
    }
    $duplicates = $zip.Entries | Group-Object FullName | Where-Object Count -gt 1
    if ($duplicates) { throw 'Duplicate archive entries.' }
    $assemblies = @($zip.Entries | Where-Object { $_.FullName.StartsWith($prefix + 'Assemblies/') -and !$_.FullName.EndsWith('/') })
    if ($assemblies.Count -ne 1 -or $assemblies[0].Name -ne 'CallOfTheWild.Wrath.Prototype.dll') {
        throw 'Native loader Assemblies directory contains an unexpected file.'
    }
    $locales = @{}
    foreach ($name in @('Localization/enGB.json', 'Localization/zhCN.json')) {
        $reader = [IO.StreamReader]::new($zip.GetEntry($prefix + $name).Open())
        try { $locale = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($locale.strings -is [array]) { throw "Locale must use current dictionary schema: $name" }
        $locales[$name] = $locale
        foreach ($key in @('650aef18-9d51-4af7-9609-23e0e53c5dd9', '3dfbe98a-aa2f-49d0-a116-b12b2ad44e14',
            'd8b9a823-2bb4-41e0-a203-133c733aaef7', 'af58b74c-64c9-47be-b69e-319ed85bff9b',
            'f07eef7e-4545-4ac8-996c-8d3d9d7b2321', 'f21c1e3e-56df-4d8c-845e-658bec53fecc',
        'd6b7ef6f-aeca-45a2-bd3a-5f469ed08856', '12e5f12d-29bc-445c-9305-d76930dc91ca')) {
            if ([string]::IsNullOrWhiteSpace($locale.strings.$key)) { throw "Missing localization key $key in $name" }
        }
    }
    foreach ($key in @('650aef18-9d51-4af7-9609-23e0e53c5dd9', '3dfbe98a-aa2f-49d0-a116-b12b2ad44e14',
        'd8b9a823-2bb4-41e0-a203-133c733aaef7', 'af58b74c-64c9-47be-b69e-319ed85bff9b',
        'f07eef7e-4545-4ac8-996c-8d3d9d7b2321', 'f21c1e3e-56df-4d8c-845e-658bec53fecc',
        'd6b7ef6f-aeca-45a2-bd3a-5f469ed08856', '12e5f12d-29bc-445c-9305-d76930dc91ca')) {
        if ($locales['Localization/zhCN.json'].strings.$key -cne $locales['Localization/enGB.json'].strings.$key) {
            throw "In-game text must remain English in both supported language settings: $key"
        }
    }
    foreach ($token in @('QuickReflexes', 'UnrestrainedRage', 'AcidResistance', 'ColdResistance', 'FireResistance', 'ElectricityResistance', 'SonicResistance', 'SharpenedAccuracy')) {
        foreach ($suffix in @('name', 'description')) {
            $key = "cotw-wrath-prototype.rage.$token.$suffix"
            $english = $locales['Localization/enGB.json'].strings.$key
            if ([string]::IsNullOrWhiteSpace($english) -or $english -match '[\p{IsCJKUnifiedIdeographs}]' -or $english -cne $locales['Localization/zhCN.json'].strings.$key) {
                throw "Missing or non-English rage localization: $key"
            }
        }
    }
    foreach ($token in @('PsychicClass', 'PsychicSpellbook', 'PsychicSpellcasting', 'CenterSelf', 'CenterSelfBuff', 'LogicalSpell', 'PhrenicAbilitySelection', 'WisdomPhrenicPool', 'CharismaPhrenicPool')) {
        foreach ($suffix in @('name', 'description')) {
            $key = "cotw-wrath.psychic.$token.$suffix"
            $english = $locales['Localization/enGB.json'].strings.$key
            if ([string]::IsNullOrWhiteSpace($english) -or $english -match '[\p{IsCJKUnifiedIdeographs}]' -or $english -cne $locales['Localization/zhCN.json'].strings.$key) { throw "Missing or non-English Psychic foundation text: $key" }
        }
    }
    if ([Version]$manifest.Version -ge [Version]'0.14.60') {
        $qlEntry=$zip.GetEntry($prefix+'Localization-QL/CallOfTheWildWrathPrototypeLocalized.json')
        $qlStream=[IO.MemoryStream]::new()
        try { $inputStream=$qlEntry.Open(); try { $inputStream.CopyTo($qlStream) } finally { $inputStream.Dispose() }; $qlBytes=$qlStream.ToArray() } finally { $qlStream.Dispose() }
        if ($qlBytes.Length -ge 3 -and $qlBytes[0] -eq 239 -and $qlBytes[1] -eq 187 -and $qlBytes[2] -eq 191) { throw '01460 QL source must be UTF-8 without BOM.' }
        $qlText=[Text.UTF8Encoding]::new($false,$true).GetString($qlBytes)
        if (!$qlText.TrimStart().StartsWith('[')) { throw '01460 QL source requires an array root.' }
        $qlRows=@($qlText | ConvertFrom-Json)
        $expectedQL = if ([Version]$manifest.Version -ge [Version]'0.14.65') { 6749 } elseif ([Version]$manifest.Version -ge [Version]'0.14.64') { 6679 } elseif ([Version]$manifest.Version -ge [Version]'0.14.63') { 5516 } elseif ([Version]$manifest.Version -ge [Version]'0.14.61') { 5482 } else { 5130 }
        if ($qlRows.Count -ne $expectedQL) { throw "Exact versioned complete QL catalog requires $expectedQL entries." }
        $qlMap=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
        foreach ($row in $qlRows) {
            if ($row.Key -isnot [string] -or [string]::IsNullOrEmpty($row.Key) -or $row.enGB -isnot [string] -or $row.zhCN -isnot [string] -or $row.ProcessTemplates -isnot [bool] -or $qlMap.ContainsKey($row.Key)) { throw '01460 invalid or duplicated QL row.' }
            $qlMap.Add($row.Key,$row)
        }
        foreach ($pair in $locales['Localization/enGB.json'].strings.PSObject.Properties) {
            if (!$qlMap.ContainsKey($pair.Name) -or $qlMap[$pair.Name].enGB -cne $pair.Value) { throw "01460 QL catalog lost or changed an original native text key: $($pair.Name)" }
        }
    }
    $cotwOriginal = (Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'Localization01444.json') | ConvertFrom-Json).strings
    foreach ($pair in $cotwOriginal.PSObject.Properties) {
        foreach ($name in @('Localization/enGB.json', 'Localization/zhCN.json')) {
            if (!$locales[$name].strings.PSObject.Properties[$pair.Name] -or $locales[$name].strings.($pair.Name) -cne $pair.Value) { throw "CotW original text mismatch: $($pair.Name) in $name" }
        }
    }
    $idrEn = $locales['Localization/enGB.json'].strings.'cotw-wrath.stalwart.increased-damage-reduction.description'
    $idrZh = $locales['Localization/zhCN.json'].strings.'cotw-wrath.stalwart.increased-damage-reduction.description'
    if ($idrEn -cne 'The stalwart defender''s damage reduction from this class increases by 2/—. This increase is always active while the stalwart defender is in a defensive stance. He can select this power up to two times. Its effects stack. The stalwart defender must be at least 6th level before selecting this defensive power.') { throw 'Increased Damage Reduction text differs from original CotW Rebalance.cs.' }
    if ($idrZh -cne $idrEn) { throw 'Increased Damage Reduction text differs between language packs.' }
    $psychicKeys = @($locales['Localization/enGB.json'].strings.PSObject.Properties | Where-Object Name -like 'cotw-wrath.psychic.full.*')
    if ($psychicKeys.Count -lt 90) { throw 'Missing Psychic extension localization definitions.' }
    foreach ($locale in $locales.Values) {
        foreach ($entry in $locale.strings.PSObject.Properties) {
            if ($entry.Value -match '[\p{IsCJKUnifiedIdeographs}]') { throw "Custom Chinese gameplay text is forbidden: $($entry.Name)" }
        }
    }
    foreach ($pair in $psychicKeys) {
        $emptySave = $pair.Value -is [string] -and $pair.Value -ceq '' -and $pair.Name.EndsWith('.savingThrow')
        $emptyCotW = $pair.Name -match '^cotw-wrath\.psychic\.full\.BravadosBlade[1-5]Reduction01444\.(name|description)$'
        if ((!$emptySave -and !$emptyCotW -and [string]::IsNullOrWhiteSpace($pair.Value)) -or $pair.Value -match '[\p{IsCJKUnifiedIdeographs}]' -or !$locales['Localization/zhCN.json'].strings.PSObject.Properties[$pair.Name] -or $pair.Value -cne $locales['Localization/zhCN.json'].strings.($pair.Name)) { throw "Missing or non-English Psychic extension text: $($pair.Name)" }
    }
    $reader = [IO.StreamReader]::new($zip.GetEntry($prefix + 'OwlcatModificationManifest.json').Open())
    try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($manifest.UniqueName -ne 'CallOfTheWildWrathPrototype') { throw 'Unexpected manifest identity.' }
    $reader = [IO.StreamReader]::new($zip.GetEntry($prefix + 'OwlcatModificationSettings.json').Open())
    try { $settings = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($null -eq $settings -or $settings.BlueprintPatches -isnot [array] -or $settings.BlueprintPatches.Count -ne 0) {
        throw 'Expected an empty BlueprintPatches array in native settings.'
    }
    # Parse CLR metadata without loading or executing the assembly.
    $binary = [IO.MemoryStream]::new()
    $entryStream = $assemblies[0].Open()
    try { $entryStream.CopyTo($binary) } finally { $entryStream.Dispose() }
    $binary.Position = 0
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($binary)
    try {
        if (!$pe.HasMetadata -or $null -eq $pe.PEHeaders.CorHeader) { throw 'Expected a managed DLL.' }
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        if (!$metadata.IsAssembly -or $metadata.GetString($metadata.GetAssemblyDefinition().Name) -ne 'CallOfTheWild.Wrath.Prototype') {
            throw 'Unexpected assembly identity.'
        }
        if ($metadata.GetAssemblyDefinition().Version.ToString(3) -ne $manifest.Version) {
            throw 'Manifest version does not match the packaged DLL.'
        }
        $harmonyReferences = @()
        foreach ($handle in $metadata.AssemblyReferences) {
            $reference = $metadata.GetAssemblyReference($handle)
            if ($metadata.GetString($reference.Name) -in @('ModMenu', 'UnityModManager', 'BrawlerClassWrath')) {
                throw 'Optional menu integration must not introduce a hard dependency on ModMenu or UMM.'
            }
            if ($metadata.GetString($reference.Name) -eq '0Harmony') { $harmonyReferences += $reference.Version.ToString() }
        }
        if ($harmonyReferences.Count -ne 1 -or $harmonyReferences[0] -ne '2.0.4.0') {
            throw 'Harmony compile baseline must match this release target 2.0.4.0.'
        }
    } finally { $pe.Dispose(); $binary.Dispose() }
    $moduleBinary = [IO.MemoryStream]::new()
    $moduleStream = $zip.GetEntry($prefix + 'Modules/Medium/MediumClass.dll').Open()
    try { $moduleStream.CopyTo($moduleBinary) } finally { $moduleStream.Dispose() }
    $moduleBinary.Position = 0
    $modulePe = [System.Reflection.PortableExecutable.PEReader]::new($moduleBinary)
    try {
        if (!$modulePe.HasMetadata) { throw 'Bundled Medium is not a managed assembly.' }
        $moduleMetadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($modulePe)
        $definition = $moduleMetadata.GetAssemblyDefinition()
        if ($moduleMetadata.GetString($definition.Name) -ne 'MediumClass' -or $definition.Version.ToString() -ne '0.4.3.0') {
            throw 'Bundled Medium identity/version does not match this release.'
        }
        $moduleHarmony = @($moduleMetadata.AssemblyReferences | ForEach-Object {
            $reference = $moduleMetadata.GetAssemblyReference($_)
            if ($moduleMetadata.GetString($reference.Name) -eq '0Harmony') { $reference.Version.ToString() }
        })
        # Both the compiler and the merged module must retain the exact game identity.
        if ($moduleHarmony.Count -ne 1 -or $moduleHarmony[0] -ne '2.0.4.0') { throw 'Medium must have exactly one pinned Harmony 2.0.4.0 API reference.' }
    } finally { $modulePe.Dispose(); $moduleBinary.Dispose() }
    $wayBinary = [IO.MemoryStream]::new()
    $wayStream = $zip.GetEntry($prefix + 'Modules/WayOfTheShield/Way of the shield.dll').Open()
    try { $wayStream.CopyTo($wayBinary) } finally { $wayStream.Dispose() }
    $wayBinary.Position = 0
    $wayPe = [System.Reflection.PortableExecutable.PEReader]::new($wayBinary)
    try {
        if (!$wayPe.HasMetadata) { throw 'Bundled Way of the Shield is not managed.' }
        $wayMetadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($wayPe)
        $definition = $wayMetadata.GetAssemblyDefinition()
        if ($wayMetadata.GetString($definition.Name) -cne 'Way of the shield' -or $definition.Version.ToString() -cne '4.0.13.0') { throw 'Original shield module assembly identity must be retained.' }
        $wayHarmony = @()
        foreach ($handle in $wayMetadata.AssemblyReferences) {
            $ref = $wayMetadata.GetAssemblyReference($handle); $name = $wayMetadata.GetString($ref.Name)
            if ($name -in @('UnityModManager', 'ModMenu', 'TabletopTweaks-Core', 'TabletopTweaks-Base')) { throw 'Shield integration must not require an external loader/provider.' }
            if ($name -eq '0Harmony') { $wayHarmony += $ref.Version.ToString() }
        }
        if ($wayHarmony.Count -ne 1 -or $wayHarmony[0] -cne '2.0.4.0') { throw 'Shield module must retain one exact game Harmony reference.' }
    } finally { $wayPe.Dispose(); $wayBinary.Dispose() }
    if ($zip.Entries | Where-Object Name -in @('Assembly-CSharp.dll','0Harmony.dll','UnityModManager.dll')) {
        throw 'Package must not redistribute game or loader assemblies.'
    }
    Write-Output 'PASS: strict archive whitelist, managed DLL identity/Harmony 2.0.4.0 baseline, English-only gameplay strings in both language packs, JSON settings, and license.'
} finally { $zip.Dispose() }

