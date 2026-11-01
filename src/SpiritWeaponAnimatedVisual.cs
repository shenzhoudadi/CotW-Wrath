using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.View;
using Kingmaker.Visual.Animation.Kingmaker;
using UnityEngine;

namespace CallOfTheWild.Wrath.Prototype {
 public sealed class SpiritAnimatedViewState { [JsonIgnore] public SpiritWeaponView View; }
 [TypeId("a74a734fa89c4e499bf538b7c5cf7dcd")]
 public sealed class SpiritWeaponAnimatedVisual:UnitBuffComponentDelegate<SpiritAnimatedViewState> {
  public int Kind;
  void Attach(){var view=Owner.View;if(!view||Kind!=1&&!view.CharacterAvatar)return;if(Data.View&&Data.View.Unit==Owner)return;Detach();Data.View=view.gameObject.AddComponent<SpiritWeaponView>();Data.View.Initialize(Owner,Kind==1);}
  void Detach(){if(Data.View){Data.View.Restore();UnityEngine.Object.Destroy(Data.View);}Data.View=null;}
  protected override void OnTurnOn()=>Attach();protected override void OnTurnOff()=>Detach();
  protected override void OnViewDidAttach()=>Attach();protected override void OnViewWillDetach()=>Detach();
  internal static void Swing(UnitEntityData unit,UnitEntityData target){if(unit?.View)unit.View.GetComponent<SpiritWeaponView>()?.Swing(target);}
 }
 // A private humanoid carries the private weapon on its real hand bone. Its
 // skeleton/animator stays active; only body rendering is hidden. Native combat
 // poses unsheathe the weapon and native attack clips move it. No UnitAttack
 // command is queued, so animation events cannot deliver another attack rule.
 public sealed class SpiritWeaponView:MonoBehaviour {
  internal UnitEntityData Unit;bool ally;
  readonly Dictionary<Renderer,bool> visibility=new Dictionary<Renderer,bool>();
  readonly Dictionary<Renderer,MaterialPropertyBlock> colors=new Dictionary<Renderer,MaterialPropertyBlock>();
  readonly Dictionary<Renderer,Material[]> bodyMaterials=new Dictionary<Renderer,Material[]>();
  readonly List<Material> privateMaterials=new List<Material>();
  readonly Queue<UnitEntityData> targets=new Queue<UnitEntityData>();
  UnitAnimationActionHandle animation;
  GameObject fist;
  internal void Initialize(UnitEntityData unit,bool visibleAlly){Unit=unit;ally=visibleAlly;Refresh();}
  internal void Swing(UnitEntityData target){if(target!=null&&targets.Count<16){targets.Enqueue(target);Play();}}
  void LateUpdate(){if(Unit==null||Unit.IsDisposed||!Unit.View){Restore();return;}Refresh();Play();}
  void Refresh(){
   var view=Unit?.View;if(!view||!ally&&!view.CharacterAvatar)return;
   var hands=view.HandsEquipment;if(hands!=null&&!hands.InCombat)hands.ForceSwitch(true);
   if(Unit.Body.PrimaryHand.MaybeWeapon?.Blueprint.Category==Kingmaker.Enums.WeaponCategory.UnarmedStrike&&!fist){
    // Irori's favored weapon has no item mesh: show a small blue force fist on
    // the native right-hand bone, which follows the same unarmed strike clip.
    var hand=hands?.Sets.Values.FirstOrDefault(set=>set.MainHand.Slot.Active)?.MainHand.MainHandTransform;
    if(hand){fist=GameObject.CreatePrimitive(PrimitiveType.Sphere);fist.name="CotW private force fist";fist.transform.SetParent(hand,false);fist.transform.localScale=Vector3.one*.16f;Destroy(fist.GetComponent<Collider>());var renderer=fist.GetComponent<Renderer>();var material=new Material(renderer.sharedMaterial){color=new Color(.12f,.48f,1f)};material.SetColor("_EmissionColor",new Color(.12f,.48f,1f));material.EnableKeyword("_EMISSION");renderer.sharedMaterial=material;privateMaterials.Add(material);}
   }
   var weapons=hands?.Sets.Values.SelectMany(set=>new[]{set.MainHand.VisualModel,set.OffHand.VisualModel}).Where(model=>model).Select(model=>model.transform).ToArray()??Array.Empty<Transform>();
   var sheaths=hands?.Sets.Values.SelectMany(set=>new[]{set.MainHand.SheathVisualModel,set.OffHand.SheathVisualModel}).Where(model=>model).Select(model=>model.transform).ToArray()??Array.Empty<Transform>();
   // Native baked human prefabs have no CharacterAvatar. Their real skinned
   // renderer lives under the view root, just like their weapon hand bones.
   foreach(var renderer in ally?view.GetComponentsInChildren<Renderer>(true):view.CharacterAvatar.GetComponentsInChildren<Renderer>(true)){
    if(!renderer||renderer is ParticleSystemRenderer)continue;
    bool held=weapons.Any(t=>renderer.transform==t||renderer.transform.IsChildOf(t))||fist&&renderer.transform.IsChildOf(fist.transform);
    bool sheath=sheaths.Any(t=>renderer.transform==t||renderer.transform.IsChildOf(t));
    if(ally&&!held&&!sheath&&!(renderer is SkinnedMeshRenderer)&&!renderer.name.StartsWith("Renderer_Character",StringComparison.Ordinal))continue;
    bool show=held||ally&&!sheath;
    if(!show){if(!visibility.ContainsKey(renderer))visibility.Add(renderer,renderer.enabled);renderer.enabled=false;}
    if(show&&!colors.ContainsKey(renderer)){
     if(ally&&!held){
      var originals=renderer.sharedMaterials;bodyMaterials.Add(renderer,originals);renderer.sharedMaterials=originals.Select(original=>{if(!original)return original;var copy=SpiritMaterial(original);privateMaterials.Add(copy);return copy;}).ToArray();
     }
     var old=new MaterialPropertyBlock();renderer.GetPropertyBlock(old);colors.Add(renderer,old);
     var blue=new MaterialPropertyBlock();renderer.GetPropertyBlock(blue);
     foreach(string property in new[]{"_Color","_BaseColor","_ColorTint"})if(renderer.sharedMaterials.Any(m=>m&&m.HasProperty(property)))blue.SetColor(property,new Color(.12f,.48f,1f,ally&&!held?.6f:.7f));
     renderer.SetPropertyBlock(blue);
    }
   }
  }
  internal static Material SpiritMaterial(Material original){
   var copy=new Material(original);var blue=new Color(.12f,.48f,1f,.6f);
   foreach(string property in new[]{"_Color","_BaseColor","_TintColor"})if(copy.HasProperty(property))copy.SetColor(property,blue);
   // The game's ghost donor is CUTOUT, so alpha tint alone stayed opaque.
   // Set the audited character shader's real transparent blend state on a
   // private copy, retaining the human mesh, textures and native animation.
   foreach(var pair in new[]{new KeyValuePair<string,float>("_Surface",1),new KeyValuePair<string,float>("_Mode",2),new KeyValuePair<string,float>("_Alphatest",0),new KeyValuePair<string,float>("_SrcBlend",5),new KeyValuePair<string,float>("_DstBlend",10),new KeyValuePair<string,float>("_ZWrite",0),new KeyValuePair<string,float>("_ZTest",4)})if(copy.HasProperty(pair.Key))copy.SetFloat(pair.Key,pair.Value);
   copy.DisableKeyword("CUTOUT_ON");copy.DisableKeyword("_ALPHATEST_ON");copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");copy.EnableKeyword("_ALPHABLEND_ON");copy.SetOverrideTag("RenderType","Transparent");copy.renderQueue=3000;return copy;
  }
  void Play(){
   if(targets.Count==0||Unit?.View?.AnimationManager==null)return;
   if(animation!=null&&!animation.IsFinished&&!animation.IsReleased)return;
   animation?.Release();animation=null;var target=targets.Dequeue();if(target==null||target.IsDisposed)return;
   var direction=target.Position-Unit.Position;if(direction.sqrMagnitude>.001f)Unit.Orientation=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
   var hand=Unit.Body?.PrimaryHand;if(hand?.MaybeWeapon==null)return;
   var attack=new AttackHandInfo(hand,0){Target=target};attack.CreateAnimationHandleForAttack(new[]{attack},false);
   animation=attack.AnimationHandle;if(animation!=null){animation.AttackTargetDistance=direction.magnitude;Unit.View.AnimationManager.Execute(animation);}
  }
  internal void Restore(){animation?.Release();animation=null;targets.Clear();foreach(var pair in visibility)if(pair.Key)pair.Key.enabled=pair.Value;visibility.Clear();foreach(var pair in colors)if(pair.Key)pair.Key.SetPropertyBlock(pair.Value);colors.Clear();foreach(var pair in bodyMaterials)if(pair.Key)pair.Key.sharedMaterials=pair.Value;bodyMaterials.Clear();if(fist)Destroy(fist);fist=null;foreach(var material in privateMaterials)if(material)Destroy(material);privateMaterials.Clear();}
  void OnDisable()=>Restore();void OnDestroy()=>Restore();
 }
}
