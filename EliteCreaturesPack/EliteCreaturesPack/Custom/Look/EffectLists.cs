using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// The <c>effects:</c> block: each list it names replaces the creature's own, in this pass (a later definition of the
    /// chain replaces it again), an empty list clearing it. The game's fields: hit = <c>Character.m_hitEffects</c>, death
    /// = <c>Character.m_deathEffects</c>, alert = <c>BaseAI.m_alertedEffects</c>, idle = <c>BaseAI.m_idleSound</c>. Every
    /// list is a new EffectList with new entries: the base's lists are never touched. An entry for a prefab the creature
    /// (or its base) already had in that list keeps that entry's settings (attached, scaled, on a bone...); a new one is
    /// placed where the game plays the list. An unknown name fails the creature, naming the field.
    /// <para>
    /// <b>Effects that spawn creatures.</b> The game instantiates every prefab of a list, so a creature (or any saved object)
    /// named there is spawned by the game itself, as a networked object of its own, on the machine that plays the list.
    /// The hit list plays in <c>Character.ApplyDamage</c>, the death list in <c>Character.OnDeath</c> and the alert list in
    /// <c>BaseAI.SetAlerted</c>, all three only on the creature's owner, so such a creature is made once and every peer
    /// sees it. The idle list plays in <c>BaseAI.DoIdleSound</c>, on a timer of its own on every peer that has the creature
    /// loaded: a saved object there is taken out of the list and spawned on the owner by <see cref="IdleSpawns"/>. A custom
    /// creature named is needed (<see cref="CreatureBuild.Needs"/>); its shell exists already, so loops resolve.
    /// </para>
    /// </summary>
    internal static class EffectLists
    {
        private sealed class Slot
        {
            public Slot(string key, Func<GameObject, EffectList?> get, Action<GameObject, EffectList> set, bool everyPeer = false)
            {
                Key = key;
                Get = get;
                Set = set;
                EveryPeer = everyPeer;
            }

            public string Key { get; }
            public Func<GameObject, EffectList?> Get { get; }
            public Action<GameObject, EffectList> Set { get; }

            /// <summary>Played on every peer that has the creature loaded, not only on its owner.</summary>
            public bool EveryPeer { get; }
        }

        private static readonly Slot Hit = new Slot("hit", go => Body(go)?.m_hitEffects, (go, list) => Body(go)!.m_hitEffects = list);
        private static readonly Slot Death = new Slot("death", go => Body(go)?.m_deathEffects, (go, list) => Body(go)!.m_deathEffects = list);
        private static readonly Slot Alert = new Slot("alert", go => Mind(go)?.m_alertedEffects, (go, list) => Mind(go)!.m_alertedEffects = list);
        private static readonly Slot Idle = new Slot("idle", go => Mind(go)?.m_idleSound, (go, list) => Mind(go)!.m_idleSound = list, true);

        public static void Apply(CreatureBuild build)
        {
            EffectsBlock? effects = build.Definition.Effects;
            if (effects == null)
            {
                return;
            }
            Replace(build, Hit, effects.Hit);
            Replace(build, Death, effects.Death);
            Replace(build, Alert, effects.Alert);
            Replace(build, Idle, effects.Idle);
        }

        private static void Replace(CreatureBuild build, Slot slot, List<string>? names)
        {
            if (names == null || build.Report.Failed)
            {
                return;
            }
            EffectList? current = slot.Get(build.Shell);
            if (current == null)
            {
                build.Report.Warn($"it has no AI, so it has no {slot.Key} effects to replace", "effects." + slot.Key);
                return;
            }
            List<EffectList.EffectData>? entries = Entries(build, slot, names, current);
            if (entries == null)
            {
                return;
            }
            if (slot.EveryPeer)
            {
                entries = IdleSpawns.TakeSpawns(build.Shell, entries);
            }
            slot.Set(build.Shell, new EffectList { m_effectPrefabs = entries.ToArray() });
        }

        private static List<EffectList.EffectData>? Entries(CreatureBuild build, Slot slot, List<string> names, EffectList current)
        {
            EffectList? original = build.Base != null ? slot.Get(build.Base) : null;
            List<EffectList.EffectData> entries = new List<EffectList.EffectData>(names.Count);
            for (int i = 0; i < names.Count; i++)
            {
                GameObject? prefab = build.Find.Prefab(names[i]);
                if (prefab == null)
                {
                    build.Report.Fail($"unknown effect '{names[i]}': no prefab or built custom creature has that name",
                        $"effects.{slot.Key}[{i}]");
                    return null;
                }
                if (build.Find.Custom(names[i]) != null)
                {
                    build.Needs(names[i]);
                }
                entries.Add(EffectEntries.For(prefab, current, original));
            }
            return entries;
        }

        private static Character? Body(GameObject prefab) => prefab.GetComponent<Character>();

        private static BaseAI? Mind(GameObject prefab) => prefab.GetComponent<BaseAI>();
    }
}
