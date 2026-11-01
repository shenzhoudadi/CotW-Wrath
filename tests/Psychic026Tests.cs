using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
internal static class Psychic026Tests {
 internal static int Count;static BlueprintBuff donor;static UnitViewLink emptyView;static PrefabLink emptyFx;
 static object Get(object o,string n)=>AccessTools.Field(o.GetType(),n).GetValue(o);
 static void Set(object o,string n,object v)=>AccessTools.Field(o.GetType(),n).SetValue(o,v);
 static void Check(bool ok,string message){if(!ok)throw new Exception("026 clone/load: "+message);Count++;}
 internal static int BeforeRegistration(Assembly assembly,BlueprintsCache cache){Count=0;donor=(BlueprintBuff)cache.Load(BlueprintGuid.Parse("0c0afabcfddeecc40a1545a282f2bec8"));var poly=donor.GetComponent<Polymorph>();
 // The plain JSON fixture keeps null links. Reproduce the empty resource-link
 // objects implicated by the user's live-game stack, without loading prefabs.
 emptyView=new UnitViewLink {AssetId=""};emptyFx=new PrefabLink {AssetId=""};Set(poly,"m_PrefabFemale",emptyView);
 foreach(string key in new[]{"m_EnterTransition","m_ExitTransition"}){var transition=(Polymorph.VisualTransitionSettings)Get(poly,key);transition.OldPrefabFX=emptyFx;transition.NewPrefabFX=new PrefabLink {AssetId=""};}
 Check(emptyView.Equals(emptyFx)&&!ReferenceEquals(emptyView,emptyFx),"Native resource equality conflates distinct link types with empty IDs");
 return Count;}
 // Equal-valued mutable objects must not be merged; actual aliasing and cycles must survive.
 sealed class Node {public int Value;public Node Next;public override bool Equals(object other)=>other is Node;public override int GetHashCode()=>1;}
 sealed class Probe:BlueprintComponent {public UnitViewLink View;public PrefabLink Fx;public PrefabLink OtherFx;public WeakResourceLink[] Links;public Node Left,Right;public List<Node> List;}
 internal static int AfterRegistration(Assembly assembly,BlueprintsCache cache){int before=Count;var factory=assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicAssets",true);var maker=Activator.CreateInstance(factory,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{cache},null);var clone=AccessTools.Method(factory,"Clone").MakeGenericMethod(typeof(BlueprintBuff));
 foreach(string id in new[]{"","same-resource-id",null}){
 var a=new Node{Value=1};var b=new Node{Value=2};a.Next=b;b.Next=a;
 var src=new Probe {View=new UnitViewLink{AssetId=id},Fx=new PrefabLink{AssetId=id},OtherFx=new PrefabLink{AssetId=id},Left=a,Right=b,List=new List<Node>{a,b,a}};src.Links=new WeakResourceLink[]{src.View,src.Fx,src.OtherFx,src.View};
 var source=new BlueprintBuff {ComponentsArray=new BlueprintComponent[]{src}};
 var copied=(BlueprintBuff)clone.Invoke(maker,new object[]{source,"CopyProbe"+(id==null?"Null":id==""?"Empty":"Same"),"Clone probe","Private test graph"});var dst=(Probe)copied.ComponentsArray.Single();
 Check(!ReferenceEquals(src,dst)&&!ReferenceEquals(src.View,dst.View)&&!ReferenceEquals(src.Fx,dst.Fx),"Clone through actual factory independently owns mutable component/links");
 Check(dst.View.GetType()==typeof(UnitViewLink)&&dst.Fx.GetType()==typeof(PrefabLink)&&dst.View.AssetId==id&&dst.Fx.AssetId==id,"Different typed links retain their declared types and values, even null IDs");
 Check(!ReferenceEquals(dst.Fx,dst.OtherFx)&&ReferenceEquals(dst.Links[0],dst.View)&&ReferenceEquals(dst.Links[3],dst.View)&&ReferenceEquals(dst.Links[1],dst.Fx),"Separate equal links remain separate; true aliases preserved");
 Check(!ReferenceEquals(dst.Left,dst.Right)&&dst.Left.Value==1&&dst.Right.Value==2&&ReferenceEquals(dst.Left.Next,dst.Right)&&ReferenceEquals(dst.Right.Next,dst.Left),"Equal nodes not merged and cycles preserved");
 Check(!ReferenceEquals(dst.List,src.List)&&ReferenceEquals(dst.List[0],dst.Left)&&ReferenceEquals(dst.List[2],dst.Left),"List ownership and repeated-reference identity preserved");
 dst.Fx.AssetId="changed";dst.Left.Value=99;Check(src.Fx.AssetId==id&&src.Left.Value==1&&dst.OtherFx.AssetId==id,"Changing clone leaves donor and distinct equal links unchanged");
 }
 var poly=donor.GetComponent<Polymorph>();Check(ReferenceEquals(Get(poly,"m_PrefabFemale"),emptyView)&&ReferenceEquals(((Polymorph.VisualTransitionSettings)Get(poly,"m_EnterTransition")).OldPrefabFX,emptyFx)&&emptyFx.AssetId=="","Registration does not mutate normalized donor");
 var idMethod=AccessTools.Method(factory,"Id");Func<string,BlueprintGuid> guid=t=>(BlueprintGuid)idMethod.Invoke(null,new object[]{t});
 foreach(string token in new[]{"MagicalBeastShapeBulette","MagicalBeastShapeHydra"}){var b=(BlueprintBuff)cache.Load(guid(token+"Buff"));var p=b.GetComponent<Polymorph>();var tr=(Polymorph.VisualTransitionSettings)Get(p,"m_EnterTransition");Check(Get(p,"m_Prefab") is UnitViewLink&&tr.OldPrefabFX is PrefabLink&&!ReferenceEquals(tr.OldPrefabFX,emptyFx),"Both real form blueprints publish with intact independent transition links");}
 foreach(string token in new[]{"MagicalBeastShape","SenseSpiritMagic","SpiritCall","FontOfSpiritMagic","TemporalStasis","FlamingSphere","CondensedEther"}){var a=cache.Load(guid(token)) as BlueprintAbility;Check(a!=null&&a.GetComponents<SpellListComponent>().Any(x=>x.SpellList.SpellsByLevel[x.SpellLevel].Spells.Contains(a)),"Published spell is reachable from its declared class list: "+token);}
 Check(cache.Load(guid("DisciplineSelection")) is BlueprintFeatureSelection,"Discipline selection published after full expansion");
 bool success=false;Exception failure=null;Action register=()=>AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.PsychicExpansion"),"Register").Invoke(null,new object[]{cache});Action<string,bool,Exception> report=(name,ok,error)=>{success=ok;failure=error;};AccessTools.Method(assembly.GetType("CallOfTheWild.Wrath.Prototype.BlueprintRegistration"),"RegisterModule").Invoke(null,new object[]{"Psychic spells and disciplines",register,report});Check(success&&failure==null,"Actual module wrapper reports success with runtime-empty-link donor");
 Console.WriteLine("PASS: "+Count+" clone/load checks; native link equality regression, reference graph isolation, full expansion registration and module status callback. No Unity prefab loading.");return Count-before;
 }
}
