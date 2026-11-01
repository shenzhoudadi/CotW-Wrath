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
        'LICENSE-CotW.txt', 'README.zh-CN.md', '测试者必读.txt', '百宝袋快速测试.txt', '0.3.0测试说明.txt', '0.3.1测试说明.txt', '0.3.3测试说明.txt', '0.3.4测试说明.txt', '0.4.0测试说明.txt', '0.4.1测试说明.txt', '0.5.0测试说明.txt', '0.5.1测试说明.txt', '0.6.0测试说明.txt', '0.6.1启动修复测试说明.txt', '0.6.2修复测试说明.txt', '0.6.3修复测试说明.txt', '0.6.4修复测试说明.txt', '0.6.5修复测试说明.txt', '0.7.0测试说明.txt', '0.7.1修复测试说明.txt', '0.7.2合并测试说明.txt', '0.7.0-spell-links.json', '0.8.0合并测试说明.txt', '0.9.0测试说明.txt', '0.9.1修复与诊断说明.txt', '0.9.2测试说明.txt', '0.9.3测试说明.txt', '0.9.4修复与诊断说明.txt', '0.9.5修复测试说明.txt', '0.10.0测试说明.txt', '0.10.1测试说明.txt', '0.11.0测试说明.txt', '0.11.1测试说明.txt', '0.12.0测试说明.txt', '0.12.1测试说明.txt', '0.12.2测试说明.txt', '0.13.0测试说明.txt', '0.13.1测试说明.txt', '0.13.2测试说明.txt', '0.13.3测试说明.txt', '0.13.4测试说明.txt', '0.13.5测试说明.txt', '0.13.11测试说明.txt', '0.13.12测试说明.txt', '0.13.13测试说明.txt', '0.13.14测试说明.txt', '0.13.15测试说明.txt', '0.13.16测试说明.txt', '0.13.17汇总测试说明.txt', '0.13.18测试说明.txt', '0.13.19测试说明.txt', '0.13.18-0.13.20测试说明.txt', '0.13.21测试说明.txt', '0.13.22测试说明.txt', '0.13.23测试说明.txt', '0.13.21-0.13.24测试说明.txt', '0.13.25修复测试说明.txt', '0.13.26修复测试说明.txt', '0.13.27修复测试说明.txt', '0.14.6专长反馈修复测试说明.txt', '0.14.8属性选择与召唤武器修复测试说明.txt', '0.14.7战斗专长列表修复测试说明.txt', '0.14.5信仰武器专长测试说明.txt', '0.14.3-0.14.5汇总测试说明.txt', '0.14.4施法资源专长测试说明.txt', '0.14.3训练与奥秘测试说明.txt', '0.14.2专长反馈修复测试说明.txt', '0.14.1专长与目标动作修复测试说明.txt', '0.14.0专长与蓝色灵体测试说明.txt', '0.13.35修复测试说明.txt', '0.13.34修复测试说明.txt', '0.13.33修复测试说明.txt', '0.13.33法术迁移定稿与后续路线.txt', '0.13.32修复测试说明.txt', '0.13.32剩余根法术清单.txt', '0.13.28测试说明.txt','0.13.28-0.13.31汇总测试说明.txt', 'Modules/Medium/MediumClass.dll', 'Modules/Medium/mediumclass_assets', 'Modules/Medium/LICENSE-CotW.txt')
    $required += @('Modules/WayOfTheShield/Way of the shield.dll', 'Modules/WayOfTheShield/NOTICE-WayOfTheShield.txt', 'Modules/WayOfTheShield/upstream-provenance.json', '0.14.9盾牌整合测试说明.txt')
    $required += @('0.14.9盾牌反馈修复测试说明.txt')
    $required += @('0.14.10盾牌回归与野性变身专长测试说明.txt')
    $required += @('0.14.11变形修复与协作专长测试说明.txt')
    $required += @('0.14.12附魔与灵体协作修复测试说明.txt')
    $required += @('0.14.13徒手灵体修复与新专长测试说明.txt')
    $required += @('0.14.14共享施法与诱敌修复测试说明.txt')
    $required += @('0.14.15移动性能与远程反应修复测试说明.txt')
    $required += @('0.14.16新专长第1批测试说明.txt')
    $required += @('0.14.17新专长第2批测试说明.txt', '0.14.18升级与菜单紧急修复测试说明.txt')
    $required += @('0.14.19反馈修复与新专长第3批测试说明.txt')
    $required += @('0.14.20兼容与参数显示修复测试说明.txt')
    $required += @('0.14.21法术表修复与单专长测试说明.txt')
    $required += @('0.14.21-spell-links.json')
    $required += @('0.14.22启动兼容紧急修复测试说明.txt')
    $required += @('0.14.23法术表结构兼容修复测试说明.txt')
    $required += @('0.14.24单专长测试说明.txt')
    $required += @('0.14.25进阶杀戮者天赋测试说明.txt')
    $required += @('0.14.26前置增强与五项移植测试说明.txt')
    $required += @('0.14.27嚎叫修复与五项真气能力测试说明.txt')
    $required += @('0.14.28嚎叫反馈与五项职业选项测试说明.txt')
    $required += @('0.14.29嚎叫清理与五项移植测试说明.txt')
    $required += @('0.14.30四项生命周期修正测试说明.txt')
    $required += @('0.14.31五项移植测试说明.txt')
    $required += @('0.14.32注册与资源修复测试说明.txt')
    $required += @('0.14.33五项移植测试说明.txt')
    $required += @('0.14.34五项移植测试说明.txt')
    $required += @('0.14.35五项移植测试说明.txt')
    $required += @('0.14.36选择入口与杰作可知修复测试说明.txt')
    $required += @('0.14.37两项职业选项移植测试说明.txt')
    $required += @('0.14.38时间凝滞与奥术发现修复测试说明.txt')
    $required += @('0.14.39伤害减免防御之力测试说明.txt')
    $required += @('0.14.40伤害减免与UMM兼容修复测试说明.txt')
    $required += @('0.14.41战鼓师测试说明.txt')
    $required += @('0.14.42存档加载紧急修复测试说明.txt')
    $required += @('0.14.43启动注册修复与职业变体测试说明.txt')
    $required += @('0.14.42都市歌者修复测试说明.txt')
    $required += @('0.14.43属性图标与乡野女巫测试说明.txt')
    $required += @('0.14.44三项职业变体测试说明.txt')
    $required += @('0.14.45五项职业变体测试说明.txt')
    $required += @('0.14.46血脉与Admixture测试说明.txt')
    if ([Version]$manifest.Version -ge [Version]'0.14.47') { $required += @('0.14.47施法修复与五项CotW内容测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.48') { $required += @('0.14.48目标与血脉及视觉反馈修复测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.49') { $required += @('0.14.49血脉兼容与重复能力移除测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.50') { $required += @('0.14.50植物目标与血脉选项修复测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.51') { $required += @('0.14.51血脉入口与能力交换修复测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.52') { $required += @('0.14.52术士血脉实际菜单修复测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.53') { $required += @('0.14.53血脉变异启动兼容修复测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.54') { $required += @('0.14.54心刃与盾法师及鞭飓风测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.55') { $required += @('0.14.55心刃与盾法师反馈修复测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.56') { $required += @('0.14.56盾法师施法战斗调整测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.57') { $required += @('0.14.57两项心灵变体测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.58') { $required += @('0.14.58异能者本体补齐与突变修复测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.59') { $required += @('0.14.59异能者登记与次数触及修复测试说明.txt', 'LICENSE-LongArm-TTT.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.60') { $required += @('0.14.60异能者反馈修复与QL文本测试说明.txt', 'Localization-QL使用说明.txt', 'Localization-QL/CallOfTheWildWrathPrototypeLocalized.json') }
    if ([Version]$manifest.Version -ge [Version]'0.14.61') { $required += @('0.14.61异能者菜单与猎人变体测试说明.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.62') { $required += @('0.14.62PsychicFeedback-test-guide.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.63') { $required += @('0.14.63UrbanBloodrager-test-guide.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.64') { $required += @('0.14.64Amnesiac-test-guide.txt') }
    if ([Version]$manifest.Version -ge [Version]'0.14.65') { $required += @('0.14.65UrbanBloodrager-feedback-test-guide.txt') }
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

