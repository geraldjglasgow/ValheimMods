using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// How the server animates creatures. Unity skips the bone work of an animator nobody sees, and a dedicated server sees
    /// nothing (a hosting player sees only their own area), but attacks land from animation events at bone positions and a
    /// rider sits on a bone. So a creature the server runs animates in full (AlwaysAnimate) while anything could need its
    /// bones (<see cref="AnimRule"/>: a boss, dying, alerted, hurt, a target, attacking, ridden, aggravated, a player close)
    /// and for a few seconds after; a quiet one is set to CullUpdateTransforms, which keeps its state machine, parameters,
    /// animation events and root motion running and skips only IK and the bone writes while no renderer is seen (always,
    /// on a dedicated server; a hosting player's camera keeps the ones it sees fully updated). Decided by a pass over every
    /// creature about twice a second (<see cref="AnimScan"/>) and at once when one begins an attack or takes damage
    /// (<see cref="AnimWake"/>, <see cref="Wake"/>). A creature starts in full when it wakes, until its first look.
    /// A copy the server does not run goes back to the prefab's own culling on a hosted game (the host may be looking at it), and on a dedicated server
    /// to none at all (nothing there reads a copy's animation; its owner's game drives what players see). Ownership
    /// HighSeat moves itself is followed at once (<see cref="Handover"/>), ownership moved any other way at the next
    /// ownership pass (<see cref="Check"/>) or look; switching "Server runs creatures" re-applies every loaded creature
    /// (off gives each its own culling back). Never throws into the game: a failure is logged once
    /// and every creature animates in full from then on, as it did before quiet ones were culled (<see cref="Fail"/>).
    /// </summary>
    internal static class AlwaysAnimate
    {
        private static readonly List<Animator> Found = new List<Animator>();
        private static readonly HashSet<ZDOID> Running = new HashSet<ZDOID>();
        private static readonly List<ZDOID> Gone = new List<ZDOID>();
        private static bool _hooked;

        /// <summary>True once something here failed; every creature animates in full from then on.</summary>
        public static bool Failed { get; private set; }

        /// <summary>A creature woken on the server.</summary>
        public static void OnWake(BaseAI ai)
        {
            Hook();
            try
            {
                Begin(ai);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        /// <summary>Follows an ownership change the server just made itself.</summary>
        public static void Follow(ZDO zdo, bool owned)
        {
            try
            {
                FollowNow(zdo, owned);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        /// <summary>From the ownership pass, for every creature it meets: re-applies when its owner changed another way.</summary>
        public static void Check(ZDO zdo)
        {
            bool owned = zdo.IsOwner();
            if (!Failed && owned != Running.Contains(zdo.m_uid))
            {
                Follow(zdo, owned);
            }
        }

        /// <summary>Forgets creatures no longer loaded (once a pass).</summary>
        public static void Prune()
        {
            foreach (ZDOID id in Running)
            {
                ZDO zdo = ZDOMan.instance.GetZDO(id);
                if (zdo == null || ZNetScene.instance.FindInstance(zdo) == null)
                {
                    Gone.Add(id);
                }
            }
            foreach (ZDOID id in Gone)
            {
                Running.Remove(id);
            }
            Gone.Clear();
        }

        /// <summary>A creature that needs its bones this moment (an attack begun, damage taken): in full now, if the server runs it.</summary>
        public static void Wake(Character body)
        {
            BaseAI ai = body.m_baseAI;
            if (ai == null || Failed || !ServerRun.Active)
            {
                return;
            }
            try
            {
                WakeNow(ai);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        /// <summary>Logs once and animates every loaded creature in full; new ones wake in full from then on.</summary>
        public static void Fail(Exception e)
        {
            if (Failed)
            {
                return;
            }
            Failed = true;
            try
            {
                HighSeat.Log.LogError($"creature animation culling failed, so every creature animates in full for the rest of this session: {e}");
                foreach (BaseAI ai in BaseAI.BaseAIInstances)
                {
                    if (ai != null)
                    {
                        SetAll(ai.gameObject, AnimatorCullingMode.AlwaysAnimate);
                    }
                }
            }
            catch (Exception)
            {
                // Creatures set so far animate in full; the rest keep their mode until the world closes.
            }
        }

        private static void Begin(BaseAI ai)
        {
            ZNetView nview = ai.m_nview;
            if (Failed || nview == null || nview.GetZDO() == null)
            {
                SetAll(ai.gameObject, AnimatorCullingMode.AlwaysAnimate);
                return;
            }
            bool owned = nview.IsOwner();
            Track(nview.GetZDO().m_uid, owned);
            CreatureAnim.Of(ai).Set(owned ? CreatureAnim.Mode.Full : CreatureAnim.Mode.Rest);
        }

        // A creature taken over may be mid-fight: in full now, quiet only once the rule has let it go for a while.
        private static void FollowNow(ZDO zdo, bool owned)
        {
            ZNetView nview = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(zdo) : null;
            BaseAI ai = nview != null ? nview.GetComponent<BaseAI>() : null;
            if (ai == null || Failed)
            {
                Running.Remove(zdo.m_uid);
                return;
            }
            Track(zdo.m_uid, owned);
            CreatureAnim anim = CreatureAnim.Of(ai);
            if (owned)
            {
                anim.LastNeed = Time.time;
            }
            anim.Set(owned ? CreatureAnim.Mode.Full : CreatureAnim.Mode.Rest);
        }

        private static void WakeNow(BaseAI ai)
        {
            ZNetView nview = ai.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            CreatureAnim anim = CreatureAnim.Of(ai);
            anim.LastNeed = Time.time;
            anim.Set(CreatureAnim.Mode.Full);
        }

        private static void Track(ZDOID id, bool owned)
        {
            if (owned)
            {
                Running.Add(id);
            }
            else
            {
                Running.Remove(id);
            }
        }

        private static void SetAll(GameObject go, AnimatorCullingMode mode)
        {
            go.GetComponentsInChildren(true, Found);
            foreach (Animator animator in Found)
            {
                animator.cullingMode = mode;
            }
            Found.Clear();
        }

        // Once: when "Server runs creatures" is switched, every loaded creature is set again for the new rule.
        private static void Hook()
        {
            if (_hooked)
            {
                return;
            }
            _hooked = true;
            Settings.ServerRunsCreatures.SettingChanged += (_, _) => Reapply();
        }

        private static void Reapply()
        {
            try
            {
                ReapplyNow(ServerRun.Active);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Running creatures: as if just woken. Off: every creature HighSeat set gets its own culling back.
        private static void ReapplyNow(bool active)
        {
            if (Failed || ZNet.instance == null)
            {
                return;
            }
            foreach (BaseAI ai in BaseAI.BaseAIInstances)
            {
                if (ai == null)
                {
                    continue;
                }
                if (active)
                {
                    Begin(ai);
                }
                else
                {
                    CreatureAnim.Find(ai)?.Set(CreatureAnim.Mode.Shipped);
                }
            }
        }
    }
}
