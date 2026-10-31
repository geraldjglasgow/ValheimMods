using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// A creature's animator on the server always animates while the server runs it. Unity skips animators nobody sees,
    /// and a dedicated server sees nothing (a hosting player sees only their own area), but attacks land from animation
    /// events and the animation the players see is the owner's: a culled animator would never strike and would stand
    /// frozen for everyone. Without "Creatures only where shared" that is every creature the server loads, as it runs them
    /// all. With it, only those it owns: a copy it does not run goes back to the prefab's own culling on a hosted game
    /// (the host may be looking at it), and on a dedicated server to none at all (nothing there reads a copy's animation;
    /// its owner's game drives what players see). Ownership HighSeat moves itself is followed at once (<see cref="Handover"/>),
    /// ownership moved any other way at the next ownership pass (<see cref="Check"/>), and switching the setting re-applies
    /// every loaded creature. Applied when a creature wakes (<see cref="CreatureWake"/>).
    /// </summary>
    internal static class AlwaysAnimate
    {
        private static readonly List<Animator> Found = new List<Animator>();
        private static readonly List<Animator> Shipped = new List<Animator>();
        private static readonly Dictionary<int, AnimatorCullingMode[]> PrefabModes = new Dictionary<int, AnimatorCullingMode[]>();
        private static readonly HashSet<ZDOID> Animated = new HashSet<ZDOID>();
        private static readonly List<ZDOID> Gone = new List<ZDOID>();
        private static bool _hooked;

        /// <summary>A creature woken on the server.</summary>
        public static void OnWake(BaseAI ai)
        {
            Hook();
            ZNetView nview = ai.m_nview;
            if (!Settings.OnlyWhereShared.Value || nview == null || nview.GetZDO() == null)
            {
                SetAll(ai.gameObject, AnimatorCullingMode.AlwaysAnimate);
                return;
            }
            Apply(ai.gameObject, nview.GetZDO(), nview.IsOwner());
        }

        /// <summary>Follows an ownership change the server just made itself.</summary>
        public static void Follow(ZDO zdo, bool owned)
        {
            ZNetView nview = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(zdo) : null;
            if (nview == null)
            {
                Animated.Remove(zdo.m_uid);
                return;
            }
            Apply(nview.gameObject, zdo, owned);
        }

        /// <summary>From the ownership pass, for every creature it meets: re-applies when its owner changed another way.</summary>
        public static void Check(ZDO zdo)
        {
            bool owned = zdo.IsOwner();
            if (owned != Animated.Contains(zdo.m_uid))
            {
                Follow(zdo, owned);
            }
        }

        /// <summary>Forgets creatures no longer loaded (once a pass).</summary>
        public static void Prune()
        {
            foreach (ZDOID id in Animated)
            {
                ZDO zdo = ZDOMan.instance.GetZDO(id);
                if (zdo == null || ZNetScene.instance.FindInstance(zdo) == null)
                {
                    Gone.Add(id);
                }
            }
            foreach (ZDOID id in Gone)
            {
                Animated.Remove(id);
            }
            Gone.Clear();
        }

        private static void Apply(GameObject go, ZDO zdo, bool owned)
        {
            AnimatorCullingMode[] modes = owned ? Array.Empty<AnimatorCullingMode>() : ModesOf(zdo.GetPrefab());
            go.GetComponentsInChildren(true, Found);
            for (int i = 0; i < Found.Count; i++)
            {
                Found[i].cullingMode = owned ? AnimatorCullingMode.AlwaysAnimate : Rest(modes, i);
            }
            Found.Clear();
            if (owned)
            {
                Animated.Add(zdo.m_uid);
            }
            else
            {
                Animated.Remove(zdo.m_uid);
            }
        }

        // A copy the server does not run: no animation at all on a dedicated server, the prefab's own culling when hosted.
        private static AnimatorCullingMode Rest(AnimatorCullingMode[] modes, int index)
        {
            if (ZNet.instance.IsDedicated())
            {
                return AnimatorCullingMode.CullCompletely;
            }
            return index < modes.Length ? modes[index] : AnimatorCullingMode.CullUpdateTransforms;
        }

        private static AnimatorCullingMode[] ModesOf(int prefab)
        {
            if (PrefabModes.TryGetValue(prefab, out AnimatorCullingMode[] modes))
            {
                return modes;
            }
            GameObject go = ZNetScene.instance.GetPrefab(prefab);
            if (go != null)
            {
                go.GetComponentsInChildren(true, Shipped);
            }
            modes = new AnimatorCullingMode[Shipped.Count];
            for (int i = 0; i < Shipped.Count; i++)
            {
                modes[i] = Shipped[i].cullingMode;
            }
            Shipped.Clear();
            PrefabModes[prefab] = modes;
            return modes;
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

        // Once: when "Creatures only where shared" is switched, every loaded creature is set again for the new rule.
        private static void Hook()
        {
            if (_hooked)
            {
                return;
            }
            _hooked = true;
            Settings.OnlyWhereShared.SettingChanged += (_, _) => Reapply();
        }

        private static void Reapply()
        {
            if (!ServerRun.Active)
            {
                return;
            }
            foreach (BaseAI ai in BaseAI.BaseAIInstances)
            {
                if (ai != null)
                {
                    OnWake(ai);
                }
            }
        }
    }
}
