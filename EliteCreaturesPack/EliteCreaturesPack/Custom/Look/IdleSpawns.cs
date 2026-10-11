using System.Collections.Generic;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// The saved objects (creatures, custom ones included, items) a creature's idle effects name. The game plays a
    /// creature's idle sound on every peer that has it loaded (<c>BaseAI.DoIdleSound</c>, a timer of its own on each),
    /// which is right for a sound, whose copies come and go, but would make a creature once per peer. So the look step takes
    /// them out of the idle list (<see cref="TakeSpawns"/>) and they are made here instead: on the owner only, at the
    /// game's idle pace and chance (<c>m_idleSoundInterval</c>, <c>m_idleSoundChance</c>), never while it sleeps or is
    /// dead. Each is instantiated as the game instantiates an effect, so its network view makes it a networked object of
    /// its own and every peer sees it once. Costs nothing on a creature with none (the timer never starts).
    /// </summary>
    public sealed class IdleSpawns : MonoBehaviour
    {
        private const float GameInterval = 5f, GameChance = 0.5f;

        /// <summary>What it spawns, set at build; the prefab instances carry it.</summary>
        public GameObject[] m_spawns = new GameObject[0];

        private ZNetView? nview;
        private BaseAI? ai;
        private Character? character;

        /// <summary>
        /// At build: the saved objects of a new idle list go to the creature's <see cref="IdleSpawns"/> (replacing what an
        /// earlier definition put there), the rest stay in the list, which the game plays as before.
        /// </summary>
        public static List<EffectList.EffectData> TakeSpawns(GameObject shell, List<EffectList.EffectData> entries)
        {
            List<GameObject> spawns = new List<GameObject>();
            List<EffectList.EffectData> kept = new List<EffectList.EffectData>(entries.Count);
            foreach (EffectList.EffectData entry in entries)
            {
                if (Saved(entry.m_prefab))
                {
                    spawns.Add(entry.m_prefab);
                }
                else
                {
                    kept.Add(entry);
                }
            }
            IdleSpawns? holder = shell.GetComponent<IdleSpawns>();
            if (spawns.Count > 0 || holder != null)
            {
                (holder != null ? holder : shell.AddComponent<IdleSpawns>()).m_spawns = spawns.ToArray();
            }
            return kept;
        }

        /// <summary>A prefab whose network view is saved with the world: a creature, an item, a piece.</summary>
        private static bool Saved(GameObject prefab)
        {
            ZNetView? view = prefab != null ? prefab.GetComponent<ZNetView>() : null;
            return view != null && view.m_persistent;
        }

        private void Start() => SafeCall.Run("custom creature idle spawns", static me => me.Begin(), this);

        private void Begin()
        {
            nview = GetComponent<ZNetView>();
            ai = GetComponent<BaseAI>();
            character = GetComponent<Character>();
            if (m_spawns.Length == 0 || nview == null)
            {
                return;
            }
            float every = ai != null && ai.m_idleSoundInterval > 0.5f ? ai.m_idleSoundInterval : GameInterval;
            InvokeRepeating(nameof(Tick), every, every);
        }

        private void Tick() => SafeCall.Run("custom creature idle spawns", static me => me.Spawn(), this);

        private void Spawn()
        {
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || (character != null && character.IsDead()))
            {
                return;
            }
            if ((ai != null && ai.IsSleeping()) || Random.value > (ai != null ? ai.m_idleSoundChance : GameChance))
            {
                return;
            }
            foreach (GameObject prefab in m_spawns)
            {
                if (prefab != null)
                {
                    Instantiate(prefab, transform.position, Quaternion.identity);
                }
            }
        }
    }
}
