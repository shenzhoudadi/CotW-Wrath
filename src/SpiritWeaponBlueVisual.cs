using System.Linq;
using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Visual.MaterialEffects.ColorTint;
using Kingmaker.Visual.MaterialEffects.RimLighting;
using Kingmaker.Visual.Particles.FxSpawnSystem;
using UnityEngine;

namespace CallOfTheWild.Wrath.Prototype
{
    // Only the three requested actor buffs carry this component. Mutate the
    // spawned instance, never the shared prefab, a weapon enchantment or rule.
    [TypeId("b042db82028b4d93ad53f594704df142")]
    public sealed class SpiritWeaponBlueVisual : UnitBuffComponentDelegate, IBuffFxComponent
    {
        internal static Color Blue(Color source)
        {
            float brightness = Mathf.Max(source.r, Mathf.Max(source.g, source.b));
            return new Color(.12f * brightness, .48f * brightness, brightness, source.a);
        }
        internal static Gradient BlueGradient(Gradient source)
        {
            if (source == null) return null;
            var gradient = new Gradient { mode = source.mode };
            gradient.SetKeys(source.colorKeys.Select(k => new GradientColorKey(Blue(k.color), k.time)).ToArray(), source.alphaKeys);
            return gradient;
        }
        internal static ParticleSystem.MinMaxGradient BlueParticles(ParticleSystem.MinMaxGradient source)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color: return new ParticleSystem.MinMaxGradient(Blue(source.color));
                case ParticleSystemGradientMode.TwoColors: return new ParticleSystem.MinMaxGradient(Blue(source.colorMin), Blue(source.colorMax));
                case ParticleSystemGradientMode.TwoGradients: return new ParticleSystem.MinMaxGradient(BlueGradient(source.gradientMin), BlueGradient(source.gradientMax));
                default:
                    var value = new ParticleSystem.MinMaxGradient(BlueGradient(source.gradient)); value.mode = source.mode; return value;
            }
        }
        public void OnSpawnFx()
        {
            var handle = AccessTools.Field(typeof(Kingmaker.UnitLogic.Buffs.Buff), "m_ParticleEffect").GetValue(Buff) as IFxHandle;
            // The engine can defer spawning to a later frame. Its callback also
            // runs immediately for an already spawned instance.
            handle?.RunAfterSpawn(Apply);
        }
        internal static void Apply(GameObject effect)
        {
            if (!effect) return;
            var restore=effect.GetComponent<SpiritBlueFxRestore>()??effect.AddComponent<SpiritBlueFxRestore>();
            restore.Restore();
            foreach (var setup in effect.GetComponentsInChildren<RimLightingAnimationSetup>(true))
                if (setup.Settings != null) {
                    var settings=setup.Settings;var original=settings.ColorOverLifetime;var current=settings.CurrentColor;
                    restore.Add(()=>{settings.ColorOverLifetime=original;settings.CurrentColor=current;});
                    settings.ColorOverLifetime = BlueGradient(original); settings.CurrentColor = Blue(current);
                }
            foreach (var setup in effect.GetComponentsInChildren<ColorTintAnimationSetup>(true))
                if (setup.Settings != null) {
                    var settings=setup.Settings;var original=settings.ColorOverLifetime;var current=settings.CurrentColor;
                    restore.Add(()=>{settings.ColorOverLifetime=original;settings.CurrentColor=current;});
                    settings.ColorOverLifetime = BlueGradient(original); settings.CurrentColor = Blue(current);
                }
            foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;var initial=main.startColor;main.startColor = BlueParticles(initial);
                var lifetime = particle.colorOverLifetime;var overLife=lifetime.color;if (lifetime.enabled) lifetime.color = BlueParticles(overLife);
                var speed = particle.colorBySpeed;var overSpeed=speed.color;if (speed.enabled) speed.color = BlueParticles(overSpeed);
                restore.Add(()=>{if(particle){main.startColor=initial;lifetime.color=overLife;speed.color=overSpeed;}});
            }
            foreach (var light in effect.GetComponentsInChildren<Light>(true)) {var original=light.color;light.color=Blue(original);restore.Add(()=>{if(light)light.color=original;});}
        }
    }
    // FX objects are pooled by the engine. Restore instance settings when they
    // are returned so an ordinary Brilliant Energy effect retains its colors.
    public sealed class SpiritBlueFxRestore:MonoBehaviour
    {
        private readonly List<Action> actions=new List<Action>();
        internal void Add(Action action)=>actions.Add(action);
        internal void Restore(){foreach(var action in actions)action();actions.Clear();}
        private void OnDisable()=>Restore();
    }
}
