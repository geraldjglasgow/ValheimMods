using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// <c>look: overlay</c>: the game's own look of a body on fire (the Burning status effect's, <c>vfx_Burning</c>: pixel
    /// flames, gradient flames, cinders, a glow and a light) or wreathed in smoke (the Smoked status effect's,
    /// <c>vfx_Smoked</c>), which the game wears on a character's whole body while it burns or stands in smoke, taken with
    /// the settings the game wears it with. A copy is made into the overlay's template (a part of the creature, kept with it
    /// and never registered, so never networked), silenced (a creature that always burns should not always crackle), its
    /// particles looping, and recoloured once to <c>overlay colour</c> where the creature is drawn
    /// (<see cref="OverlayColours"/>). <see cref="BodyOverlay"/> wears it. The template is made on every peer alike (its
    /// name is taken from the creature's part names), and prepared only where something draws.
    /// </summary>
    internal static class OverlaySource
    {
        public static void Apply(CreatureBuild build, LookPlan plan)
        {
            if (plan.Overlay == null)
            {
                WithoutOverlay(build, plan);
                return;
            }
            EffectList.EffectData? source = Source(build.Find, plan.Overlay.Value);
            if (source == null)
            {
                build.Report.Warn($"the game's {plan.Overlay.Value.ToString().ToLowerInvariant()} look was not found, so it wears no overlay", "look.overlay");
                return;
            }
            GameObject template = build.CopyPart(source.m_prefab, "overlay", networked: false);
            if (!Drawing.Headless)
            {
                // this machine's drawing alone: a failure here must not leave the creature out on one peer only
                SafeCall.Run("custom creature overlay (drawing)", static (t, c) => Prepare(t, c), template, plan.OverlayColour);
            }
            BodyOverlay wear = build.Shell.AddComponent<BodyOverlay>();
            wear.m_template = template;
            wear.m_scaleToBody = source.m_scale;
            wear.m_child = source.m_childTransform ?? "";
        }

        private static void WithoutOverlay(CreatureBuild build, LookPlan plan)
        {
            if (plan.OverlayColour != null)
            {
                build.Report.Warn("an overlay colour without an overlay does nothing: add overlay: smoke or flame", "look.overlay colour");
            }
        }

        /// <summary>The status effect's own entry (its prefab and how the game wears it), or the effect prefab by name.</summary>
        private static EffectList.EffectData? Source(PrefabLookup find, OverlayKind kind)
        {
            StatusEffect? status = find.StatusEffect(kind == OverlayKind.Flame ? "Burning" : "Smoked");
            foreach (EffectList.EffectData entry in status?.m_startEffects?.m_effectPrefabs ?? new EffectList.EffectData[0])
            {
                if (entry != null && entry.m_prefab != null && entry.m_prefab.GetComponentInChildren<ParticleSystem>(true) != null)
                {
                    return entry;
                }
            }
            GameObject? prefab = find.Prefab(kind == OverlayKind.Flame ? "vfx_Burning" : "vfx_Smoked");
            return prefab != null ? new EffectList.EffectData { m_prefab = prefab, m_attach = true, m_scale = true } : null;
        }

        /// <summary>No sound, particles that loop for as long as the copy lives, and the overlay's colour.</summary>
        private static void Prepare(GameObject template, Color? colour)
        {
            foreach (ZSFX sound in template.GetComponentsInChildren<ZSFX>(true))
            {
                Object.DestroyImmediate(sound); // it would play as the copy wakes, whatever else is switched off
            }
            foreach (AudioSource audio in template.GetComponentsInChildren<AudioSource>(true))
            {
                audio.playOnAwake = false;
                audio.enabled = false;
            }
            foreach (ParticleSystem system in template.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.loop = true;
            }
            if (colour != null)
            {
                OverlayColours.Paint(template, colour.Value);
            }
        }
    }
}
