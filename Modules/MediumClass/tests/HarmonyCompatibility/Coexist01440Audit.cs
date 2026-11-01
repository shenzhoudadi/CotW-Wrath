using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;
using System.Text.Json;

// Static analysis of actual provider release DLLs, never provider initialization.
internal static class Coexist01440Audit
{
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots) => roots.SelectMany(t => new[] { t }.Concat(Types(t.NestedTypes)));
    static string[] IL(MethodDefinition m) => m.HasBody ? m.Body.Instructions.Select(i => i.ToString()).ToArray() : [];
    static object FileInfo(string p, AssemblyDefinition a) => new { path=Path.GetFullPath(p), assembly=a.Name.FullName, sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))) };
    public static int Run(string[] args)
    {
        if (args.Length != 6) return 2;
        const string id="d10496e92d0799a40bb3930b8f4fda0d";
        using var b=AssemblyDefinition.ReadAssembly(args[0]);
        using var c=AssemblyDefinition.ReadAssembly(args[1]);
        using var e=AssemblyDefinition.ReadAssembly(args[2]);
        using var ours=AssemblyDefinition.ReadAssembly(args[3]);
        using var native=AssemblyDefinition.ReadAssembly(args[4]);
        MethodDefinition[] Hits(AssemblyDefinition a) => Types(a.MainModule.Types).SelectMany(t=>t.Methods).Where(m=>m.HasBody && m.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldstr && Equals(i.Operand,id))).ToArray();
        var baseHits=Hits(b); var ecHits=Hits(e);
        string[] TargetBlock(MethodDefinition m) {
            var code=m.Body.Instructions;
            int start=code.ToList().FindIndex(i=>i.OpCode==OpCodes.Ldstr && Equals(i.Operand,id));
            return code.Skip(start).TakeWhile((i,n)=>n==0 || !(i.OpCode==OpCodes.Ldstr && i.Operand is string s && s.Length==32 && s.All(Uri.IsHexDigit))).Select(i=>i.ToString()).ToArray();
        }
        var converter=Types(c.MainModule.Types).SelectMany(t=>t.Methods).Single(m=>m.Name=="ConvertVanillaDamageResistanceToRework");
        var init=Types(c.MainModule.Types).Single(t=>t.Name=="TTAddDamageResistanceBase").Methods.Single(m=>m.Name=="InitFromVanillaDamageResistance");
        var production=Types(ours.MainModule.Types).Single(t=>t.Name=="IncreasedDamageReduction01439");
        var write=production.Methods.Single(m=>m.Name=="WriteDouble");
        var doubled=production.Methods.Single(m=>m.Name=="IsDoubled");
        var providerType=Types(c.MainModule.Types).Single(t=>t.Name=="TTAddDamageResistancePhysical");
        var copy=providerType.Methods.Single(m=>m.Name=="AdditionalInitFromVanillaDamageResistance");
        var shared=Types(native.MainModule.Types).Single(t=>t.Name=="UnitPartDamageReduction").Methods.Single(m=>m.Name=="GetBestAndStackableDR");
        var checks=new Dictionary<string,bool> {
            ["oneTTTTargetWriter"]=baseHits.Length==1,
            ["oneExpandedContentTargetWriter"]=ecHits.Length==1,
            ["TTTConvertsPhysicalComponent"]=baseHits.Any(m=>IL(m).Any(s=>s.Contains("ConvertVanillaDamageResistanceToRework") && s.Contains("TTAddDamageResistancePhysical"))),
            ["converterPreservesRankConfig"]=!IL(converter).Any(s=>s.Contains("ContextRankConfig")),
            ["converterCopiesNativeValue"]=IL(init).Any(s=>s.Contains("::Value")),
            ["TTTPreservesStackableFlag"]=IL(copy).Any(s=>s.Contains("m_IsStackable")) && IL(copy).Any(s=>s.Contains("AddToAllStacks")),
            ["CotWOnlyWritesRankAndDescription"]=!production.Methods.SelectMany(IL).Any(s=>s.Contains("AddDamageResistancePhysical") || s.Contains("TTAddDamageResistancePhysical")),
            ["CotWDoublesUsingExactlyTwoReferences"]=IL(write).Any(s=>s.Contains("FeatureListRanks")) && IL(write).Any(s=>s.Contains("ldc.i4.2")) && IL(write).Any(s=>s.Contains("m_FeatureList")),
            ["CotWRecognizesAlreadyDoubledShape"]=IL(doubled).Any(s=>s.Contains("ldc.i4.2")) && IL(doubled).Any(s=>s.Contains("FeatureListRanks")),
            ["ExpandedContentDoesNotWriteRank"]=ecHits.All(m=>!TargetBlock(m).Any(s=>s.Contains("ContextRankConfig"))),
            ["nativeStackableAccountingIdentified"]=IL(shared).Any(s=>s.Contains("get_IsStackable")) && IL(shared).Any(s=>s.Contains("get_RemainReduction")) && IL(shared).Any(s=>s.Contains("Bypassed"))
        };
        // The prerequisite's compiler-generated delegate carries Level/Group.
        var ecDelegateNames=ecHits.SelectMany(TargetBlock).Where(s=>s.Contains("ldftn ")).Select(s=>s.Split("ldftn ")[1]).ToHashSet();
        var ecDelegates=Types(e.MainModule.Types).SelectMany(t=>t.Methods).Where(m=>ecDelegateNames.Contains(m.FullName)).ToArray();
        checks["ExpandedContentLevel12DelegatePresent"]=ecDelegates.Length==1 && IL(ecDelegates[0]).Any(s=>s.Contains("PrerequisiteClassLevel::Level")) && IL(ecDelegates[0]).Any(s=>s.Contains("ldc.i4.s 12"));
        checks["ExpandedContentAnyGroupPreserved"]=ecDelegates.Length==1 && IL(ecDelegates[0]).Any(s=>s.Contains("Prerequisite::Group")) && IL(ecDelegates[0]).Any(s=>s.Contains("ldc.i4.1"));
        checks["ExpandedContentAddsExactlyOnePrerequisite"]=ecHits.Sum(m=>TargetBlock(m).Count(s=>s.Contains("::AddComponent<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel>")))==1;
        var report=new {
            passed=checks.Values.All(v=>v), checks,
            providers=new[]{FileInfo(args[0],b),FileInfo(args[1],c),FileInfo(args[2],e)},
            production=FileInfo(args[3],ours),native=FileInfo(args[4],native),
            target=id,
            combinations=new[]{"providers absent / TTT DR switch off: native component retained", "TTT on, CotW first: conversion copies Value and leaves doubled rank config", "TTT on, TTT first: CotW writes rank config without touching replacement component", "ExpandedContent before/after: additive prerequisite retained; it writes no rank config", "repeated registration: already-doubled shape is accepted without another multiplier"},
            baseTarget=baseHits.Select(m=>new{method=m.FullName,il=IL(m)}),
            expandedTarget=ecHits.Select(m=>new{method=m.FullName,il=TargetBlock(m)}),
            expandedLevel12Delegates=ecDelegates.Select(m=>new{method=m.FullName,il=IL(m)}),
            converter=new{method=converter.FullName,il=IL(converter),copyValue=IL(init),copyPhysical=IL(copy)},
            nativeSharedAccounting=IL(shared),
            boundary="Static IL and signature checks of official release DLLs. No complete provider startup, settings UI, Unity scene, event bus or save lifecycle is executed by this audit. The real native damage-part contract runs separately."
        };
        File.WriteAllText(args[5],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        foreach(var pair in checks) Console.WriteLine((pair.Value?"PASS: ":"FAIL: ")+pair.Key);
        return report.passed?0:1;
    }
}
