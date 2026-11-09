using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using static CallOfTheWild.Wrath.Prototype.PsychicAssets;

namespace CallOfTheWild.Wrath.Prototype
{
    internal sealed class PsychicCommonSpellLists
    {
        private sealed class Link {internal BlueprintSpellList List;internal BlueprintAbility Spell;internal int Level;}
        private readonly List<Link> links=new List<Link>();
        private readonly Dictionary<BlueprintSpellList,SpellLevelList[]> before=new Dictionary<BlueprintSpellList,SpellLevelList[]>();
        internal PsychicCommonSpellLists(PsychicAssets f)
        {
            Add(f,"UndeadAnatomy1",3,"ba0401fdeb4062f40a7aa95b6f07fe89","4d72e1e7bd6bc4f4caaea7aa43a14639","f60d0cd93edc65c42ad31e34a905fb2f");
            Add(f,"UndeadAnatomy2",5,"ba0401fdeb4062f40a7aa95b6f07fe89","4d72e1e7bd6bc4f4caaea7aa43a14639","f60d0cd93edc65c42ad31e34a905fb2f");
            Add(f,"UndeadAnatomy3",6,"ba0401fdeb4062f40a7aa95b6f07fe89","4d72e1e7bd6bc4f4caaea7aa43a14639","f60d0cd93edc65c42ad31e34a905fb2f");
            Add(f,"GiantForm1",7,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"GiantForm1",6,"f60d0cd93edc65c42ad31e34a905fb2f");
            Add(f,"GiantForm2",8,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"Suffocation",5,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"SuffocationMass",9,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"ControlConstruct",7,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"ScouringWinds",7,"ba0401fdeb4062f40a7aa95b6f07fe89","bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"FlamingSphere",2,"bad8638d40639d04fa2f80a1cac67d6b","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"FlamingSphereGreater",4,"bad8638d40639d04fa2f80a1cac67d6b","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"AggressiveThundercloud",2,"bad8638d40639d04fa2f80a1cac67d6b","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"AggressiveThundercloudGreater",4,"bad8638d40639d04fa2f80a1cac67d6b","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"BallLightning",4,"bad8638d40639d04fa2f80a1cac67d6b","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"AirWalk",4,"f60d0cd93edc65c42ad31e34a905fb2f","8443ce803d2d31347897a3d85cc32f53","bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"AirWalkCommunal",5,"f60d0cd93edc65c42ad31e34a905fb2f","8443ce803d2d31347897a3d85cc32f53","bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"OverlandFlight",5,"f60d0cd93edc65c42ad31e34a905fb2f","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"WindWalk",6,"f60d0cd93edc65c42ad31e34a905fb2f","8443ce803d2d31347897a3d85cc32f53");
            Add(f,"WindWalk",7,"bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"EarthTremor",3,"bad8638d40639d04fa2f80a1cac67d6b","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"CondensedEther",5,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"BarrowHaze",3,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"BowSpirit",4,"29f3c338532390546bc5347826a655c4");
            Add(f,"AlliedCloak",3,"ba0401fdeb4062f40a7aa95b6f07fe89","25a5013493bdcf74bb2424532214d0c8","4d72e1e7bd6bc4f4caaea7aa43a14639");
            Add(f,"SmiteAbomination",4,"57c894665b7895c499b3dce058c284b3");
            Add(f,"SmiteAbomination",5,"8443ce803d2d31347897a3d85cc32f53");
            Add(f,"Screech",3,"e17df9977b879b64e8a8cbb4b3569f19");
            Add(f,"CurseMajor",5,"8443ce803d2d31347897a3d85cc32f53");
            Add(f,"CurseMajor",6,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"ObscuringMist",1,"bad8638d40639d04fa2f80a1cac67d6b","8443ce803d2d31347897a3d85cc32f53","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"FogCloud",2,"bad8638d40639d04fa2f80a1cac67d6b","8443ce803d2d31347897a3d85cc32f53","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"SolidFog",4,"4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"SleetStorm",3,"bad8638d40639d04fa2f80a1cac67d6b","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"RiverOfWind",4,"bad8638d40639d04fa2f80a1cac67d6b","4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"FickleWinds",5,"8443ce803d2d31347897a3d85cc32f53","bad8638d40639d04fa2f80a1cac67d6b","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"FickleWinds",3,"29f3c338532390546bc5347826a655c4");
            Add(f,"ThreefoldAspect",5,"bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"BlisteringInvective",2,"f60d0cd93edc65c42ad31e34a905fb2f","57c894665b7895c499b3dce058c284b3","25a5013493bdcf74bb2424532214d0c8");
            Add(f,"WallOfNausea",3,"ba0401fdeb4062f40a7aa95b6f07fe89","25a5013493bdcf74bb2424532214d0c8");
            Add(f,"WallOfBlindness",4,"ba0401fdeb4062f40a7aa95b6f07fe89","25a5013493bdcf74bb2424532214d0c8");
            Add(f,"WallOfBlindness",5,"8443ce803d2d31347897a3d85cc32f53");
            Add(f,"RighteousVigor",2,"9f5be2f7ea64fe04eb40878347b147bc");
            Add(f,"RighteousVigor",3,"57c894665b7895c499b3dce058c284b3");
            Add(f,"PathOfGlory",2,"8443ce803d2d31347897a3d85cc32f53","25a5013493bdcf74bb2424532214d0c8");
            Add(f,"PathOfGloryGreater",4,"8443ce803d2d31347897a3d85cc32f53","25a5013493bdcf74bb2424532214d0c8");
            Add(f,"Flashfire",3,"bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"ForcefulStrike",4,"8443ce803d2d31347897a3d85cc32f53","4d72e1e7bd6bc4f4caaea7aa43a14639","57c894665b7895c499b3dce058c284b3","9f5be2f7ea64fe04eb40878347b147bc");
            Add(f,"AuraOfDoom",4,"8443ce803d2d31347897a3d85cc32f53");
            Add(f,"LitanyOfTruth",6,"57c894665b7895c499b3dce058c284b3");
            Add(f,"LitanyOfTruth",4,"9f5be2f7ea64fe04eb40878347b147bc");
            Add(f,"DazzlingBlade",1,"25a5013493bdcf74bb2424532214d0c8","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"DazzlingBladeMass",3,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"VineStrike",2,"bad8638d40639d04fa2f80a1cac67d6b","f60d0cd93edc65c42ad31e34a905fb2f","29f3c338532390546bc5347826a655c4","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"FlamesOfTheFaithful",2,"57c894665b7895c499b3dce058c284b3");
            Add(f,"WrathfulWeapon",4,"8443ce803d2d31347897a3d85cc32f53");
            Add(f,"FlurryOfSnowballs",2,"4d72e1e7bd6bc4f4caaea7aa43a14639","bad8638d40639d04fa2f80a1cac67d6b","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"TouchOfBloodletting",1,"8443ce803d2d31347897a3d85cc32f53","bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"TouchOfBloodletting",2,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"FlameArrow",3,"4d72e1e7bd6bc4f4caaea7aa43a14639","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"Rebuke",4,"57c894665b7895c499b3dce058c284b3");
            Add(f,"SheetLightning",3,"bad8638d40639d04fa2f80a1cac67d6b","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"BloodyClaws",4,"bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"BloodyClaws",3,"29f3c338532390546bc5347826a655c4");
            Add(f,"HexVulnerability",1,"e17df9977b879b64e8a8cbb4b3569f19","c0c40e42f07ff104fa85492da464ac69");
            Add(f,"OrbOfTheVoid",8,"8443ce803d2d31347897a3d85cc32f53","ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"FieryShuriken",2,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"SpiritBoundBlade",4,"c0c40e42f07ff104fa85492da464ac69");Add(f,"Contingency",6,"ba0401fdeb4062f40a7aa95b6f07fe89");Add(f,"Spite",4,"e17df9977b879b64e8a8cbb4b3569f19");Add(f,"DelayedConsumption",5,"f60d0cd93edc65c42ad31e34a905fb2f");Add(f,"EmblemOfGreed",6,"ba0401fdeb4062f40a7aa95b6f07fe89","8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3","4d72e1e7bd6bc4f4caaea7aa43a14639");
            Add(f,"SpiritualWeapon",2,"8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3");Add(f,"SpiritualAlly",4,"8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3");Add(f,"MagesSword",7,"ba0401fdeb4062f40a7aa95b6f07fe89");Add(f,"TwilightKnife",3,"ba0401fdeb4062f40a7aa95b6f07fe89","e17df9977b879b64e8a8cbb4b3569f19");Add(f,"ArcaneEye",4,"ba0401fdeb4062f40a7aa95b6f07fe89","f60d0cd93edc65c42ad31e34a905fb2f");
            Add(f,"Sanctuary",1,"8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3");
            Add(f,"BattlemindLink",6,"ba0401fdeb4062f40a7aa95b6f07fe89");Add(f,"BattlemindLink",4,"57c894665b7895c499b3dce058c284b3");
            Add(f,"FireSeeds",6,"bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"SenseSpiritMagic",1,"c0c40e42f07ff104fa85492da464ac69");
            Add(f,"SpiritCall",1,"c0c40e42f07ff104fa85492da464ac69","bad8638d40639d04fa2f80a1cac67d6b");
            Add(f,"FontOfSpiritMagic",3,"c0c40e42f07ff104fa85492da464ac69");
            Add(f,"TemporalStasis",8,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"LendJudgment",1,"57c894665b7895c499b3dce058c284b3");
            Add(f,"LendJudgmentGreater",5,"57c894665b7895c499b3dce058c284b3");
            Add(f,"Babble",2,"25a5013493bdcf74bb2424532214d0c8");
            Add(f,"Babble",3,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"MagicalBeastShape",7,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"GhostbaneDirge",2,"25a5013493bdcf74bb2424532214d0c8","8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3");
            Add(f,"GhostbaneDirge",1,"9f5be2f7ea64fe04eb40878347b147bc");
            Add(f,"GhostbaneDirgeMass",4,"25a5013493bdcf74bb2424532214d0c8");
            Add(f,"GhostbaneDirgeMass",5,"8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3");
            Add(f,"GhostbaneDirgeMass",3,"9f5be2f7ea64fe04eb40878347b147bc");
            Add(f,"SecondWind",3,"8443ce803d2d31347897a3d85cc32f53","9f5be2f7ea64fe04eb40878347b147bc","57c894665b7895c499b3dce058c284b3","29f3c338532390546bc5347826a655c4");
            Add(f,"BladeTutor",1,"4d72e1e7bd6bc4f4caaea7aa43a14639");
            Add(f,"BladeTutor",2,"ba0401fdeb4062f40a7aa95b6f07fe89","9f5be2f7ea64fe04eb40878347b147bc");
            Add(f,"ChannelVigor",3,"f60d0cd93edc65c42ad31e34a905fb2f","8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3","4d72e1e7bd6bc4f4caaea7aa43a14639");
            Add(f,"FluidForm",6,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"FluidForm",4,"f60d0cd93edc65c42ad31e34a905fb2f");
            Add(f,"DebilitatingPortent",4,"8443ce803d2d31347897a3d85cc32f53");
            Add(f,"IrresistibleDance",8,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"IrresistibleDance",6,"25a5013493bdcf74bb2424532214d0c8");
            Add(f,"GlobeOfInvulnerabilityLesser",4,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"GlobeOfInvulnerability",6,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"WardingWeapon",2,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"BloodArmor",2,"ba0401fdeb4062f40a7aa95b6f07fe89","f60d0cd93edc65c42ad31e34a905fb2f");
            Add(f,"SavageMaw",2,"bad8638d40639d04fa2f80a1cac67d6b","8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3","4d72e1e7bd6bc4f4caaea7aa43a14639");
            Add(f,"SavageMaw",1,"29f3c338532390546bc5347826a655c4");
            Add(f,"WardingWeapon",1,"4d72e1e7bd6bc4f4caaea7aa43a14639");
            Add(f,"DazeMass",4,"ba0401fdeb4062f40a7aa95b6f07fe89","25a5013493bdcf74bb2424532214d0c8","57c894665b7895c499b3dce058c284b3");
            Add(f,"HaltUndead",3,"ba0401fdeb4062f40a7aa95b6f07fe89","57c894665b7895c499b3dce058c284b3");
            Add(f,"ControlUndead",7,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"Invigorate",1,"25a5013493bdcf74bb2424532214d0c8");
            Add(f,"InvigorateMass",3,"25a5013493bdcf74bb2424532214d0c8");
            Add(f,"HowlingAgony",2,"57c894665b7895c499b3dce058c284b3");
            Add(f,"HowlingAgony",3,"ba0401fdeb4062f40a7aa95b6f07fe89","e17df9977b879b64e8a8cbb4b3569f19","98c05aeff6e3d384f8aec6d584973642");
            Add(f,"LocateWeakness",3,"ba0401fdeb4062f40a7aa95b6f07fe89","4d72e1e7bd6bc4f4caaea7aa43a14639","57c894665b7895c499b3dce058c284b3","e17df9977b879b64e8a8cbb4b3569f19","98c05aeff6e3d384f8aec6d584973642");
            Add(f,"LocateWeakness",2,"29f3c338532390546bc5347826a655c4");
            Add(f,"Silence",2,"25a5013493bdcf74bb2424532214d0c8","8443ce803d2d31347897a3d85cc32f53","57c894665b7895c499b3dce058c284b3");
            Add(f,"CountlessEyes",3,"ba0401fdeb4062f40a7aa95b6f07fe89","f60d0cd93edc65c42ad31e34a905fb2f","57c894665b7895c499b3dce058c284b3");
            Add(f,"ForceSword",2,"ba0401fdeb4062f40a7aa95b6f07fe89","4d72e1e7bd6bc4f4caaea7aa43a14639");
            Add(f,"PainStrike",3,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"PainStrikeMass",5,"ba0401fdeb4062f40a7aa95b6f07fe89");
            Add(f,"BurstOfForce",5,"ba0401fdeb4062f40a7aa95b6f07fe89");
        }
        private void Add(PsychicAssets f,string token,int level,params string[] ids)
        {
            var spell=f.Find<BlueprintAbility>(token);
            foreach(var id in ids) {
                var list=f.Native<BlueprintSpellList>(id);
                if(!before.ContainsKey(list)) before.Add(list,list.SpellsByLevel);
                var c=new SpellListComponent {name="CotWOriginalSpellList"+id,SpellLevel=level};Set(c,"m_SpellList",Ref<BlueprintSpellListReference>(list));
                spell.ComponentsArray=spell.ComponentsArray.Concat(new BlueprintComponent[]{c}).ToArray();links.Add(new Link {List=list,Spell=spell,Level=level});
            }
        }
        internal void Verify(bool present)
        {
            foreach(var link in links) {
                bool found=(link.List.SpellsByLevel??Array.Empty<SpellLevelList>()).Where(l=>l!=null&&l.SpellLevel==link.Level)
                    .SelectMany(l=>(List<BlueprintAbilityReference>)Get(l,"m_Spells")??new List<BlueprintAbilityReference>()).Any(s=>s!=null&&s.Guid==link.Spell.AssetGuid);
                if(found!=present)throw new InvalidOperationException("Shared CotW spell list link has unexpected presence: "+link.Spell.name);
            }
        }
        internal void Apply()
        {
            foreach(var pair in before) {
                var additions=links.Where(l=>l.List==pair.Key).ToArray();
                var merged=SpellListCompatibility.Build(pair.Value,additions.Select(l=>l.Level).DefaultIfEmpty(0).Max());
                if(merged==null)throw new InvalidOperationException("Unsupported spell-list levels: "+pair.Key.AssetGuid);
                foreach(var group in additions.GroupBy(l=>l.Level)) {
                    var old=(List<BlueprintAbilityReference>)Get(merged[group.Key],"m_Spells");var seen=new HashSet<BlueprintGuid>(old.Select(s=>s.Guid));
                    Replace(merged[group.Key],old.Concat(group.Where(l=>seen.Add(l.Spell.AssetGuid)).Select(l=>Ref<BlueprintAbilityReference>(l.Spell))).ToList());
                }
                pair.Key.SpellsByLevel=merged;
            }
        }
        internal void Restore(){foreach(var pair in before)pair.Key.SpellsByLevel=pair.Value;}
        private static void Replace(SpellLevelList level,List<BlueprintAbilityReference> spells) {
            Set(level,"m_Spells",spells);Set(level,"m_SpellsProxy",null);Set(level,"m_SpellsFiltered",null);
        }
    }
}
