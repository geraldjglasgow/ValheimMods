using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The server's pass over its creatures' animation, once a frame after the game's MonoUpdaters.Update: a share of the
    /// game's own list of creatures (BaseAI.BaseAIInstances) each frame, so every creature is looked at about every
    /// <see cref="CheckSeconds"/> however many there are, with no allocation. One the server runs animates in full while
    /// the wake rule says its bones may be needed (<see cref="AnimRule"/>) and for <see cref="QuietSeconds"/> after,
    /// so it does not flip at the edge of a rule; then it goes quiet (<see cref="CreatureAnim"/>). The changes take effect
    /// at the animators' update later in the same frame. A copy the server does not run, with "Creatures only where
    /// shared" on, rests. The moments that cannot wait for the pass are taken at once (<see cref="AnimWake"/>). In a
    /// debug build, with AnimStats.QuietAllowed off it puts every creature it runs in full; once something here failed it
    /// stops (<see cref="AlwaysAnimate.Fail"/>). Destroyed creatures' records are dropped at the end of each full scan.
    /// </summary>
    [HarmonyPatch(typeof(MonoUpdaters), nameof(MonoUpdaters.Update))]
    internal static class AnimScan
    {
        /// <summary>Seconds between two looks at the same creature.</summary>
        public const float CheckSeconds = 0.5f;

        /// <summary>Seconds a creature must have needed nothing before it goes quiet.</summary>
        public const float QuietSeconds = 5f;

#if DEBUG
        private static bool QuietAllowed => AnimStats.QuietAllowed;
#else
        private const bool QuietAllowed = true;
#endif

        private static int _cursor;
        private static float _owed;

        private static void Postfix()
        {
            if (AlwaysAnimate.Failed || !ServerRun.Active)
            {
                return;
            }
            try
            {
                Step(BaseAI.BaseAIInstances, Time.time);
            }
            catch (Exception e)
            {
                AlwaysAnimate.Fail(e);
            }
        }

        // This frame's share: the creatures due since the last frame, carried over in fractions, at most all of them.
        private static void Step(List<BaseAI> all, float now)
        {
            #if DEBUG
            AnimStats.Tick(now);
            #endif
            _owed = Mathf.Min(_owed + all.Count * Time.deltaTime / CheckSeconds, all.Count);
            int due = (int)_owed;
            if (due == 0)
            {
                return;
            }
            _owed -= due;
            AnimPlayers.Refresh();
            for (int i = 0; i < due; i++)
            {
                if (_cursor >= all.Count)
                {
                    _cursor = 0;
                    #if DEBUG
                    AnimStats.ScanDone();
                    #endif
                    CreatureAnim.Prune();
                }
                Visit(all[_cursor++], now);
            }
        }

        private static void Visit(BaseAI ai, float now)
        {
            ZNetView nview = ai != null ? ai.m_nview : null;
            if (nview == null || !nview.IsValid())
            {
                return;
            }
            CreatureAnim anim = CreatureAnim.Of(ai);
            if (!nview.IsOwner())
            {
                anim.Set(CreatureAnim.Mode.Rest);
            }
            else
            {
                if (AnimRule.NeedsBones(anim))
                {
                    anim.LastNeed = now;
                }
                bool quiet = QuietAllowed && now - anim.LastNeed >= QuietSeconds;
                anim.Set(quiet ? CreatureAnim.Mode.Quiet : CreatureAnim.Mode.Full);
            }
            #if DEBUG
            AnimStats.Count(anim.Current);
            #endif
        }
    }
}
