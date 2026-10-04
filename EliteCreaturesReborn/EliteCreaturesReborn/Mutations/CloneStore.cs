using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Cloning's state, all of it in the two creatures' ZDOs, so every machine reads the same thing and a new owner carries
    /// on where the old one stopped. On the creature: the decoy it hides behind (<see cref="Decoy"/>, None while it shows),
    /// how many times it has done it, and when the current trick began or the last one ended. On the decoy: the creature it
    /// stands in for (<see cref="RealOf"/>), whose presence is what makes it a decoy. Each is written only by its own
    /// owner. The two ZDOID keys are hashed once here: the creature's is read every frame on every machine that holds it.
    /// </summary>
    internal static class CloneStore
    {
        private static readonly KeyValuePair<int, int> DecoyKey = ZDO.GetHashZDOID(TraitKeys.CloneDecoy);
        private static readonly KeyValuePair<int, int> OfKey = ZDO.GetHashZDOID(TraitKeys.CloneOf);

        /// <summary>On the creature: the decoy it hides behind right now, or None while it shows.</summary>
        public static ZDOID Decoy(ZDO zdo) => zdo.GetZDOID(DecoyKey);

        /// <summary>On the creature: true while it hides behind a decoy.</summary>
        public static bool Hiding(ZDO? zdo) => zdo != null && Decoy(zdo) != ZDOID.None;

        /// <summary>On a decoy: the creature it stands in for; None on anything that is not a decoy.</summary>
        public static ZDOID RealOf(ZDO zdo) => zdo.GetZDOID(OfKey);

        /// <summary>True for a Cloning creature's decoy, read from its ZDO, so it holds before its controller has resolved.</summary>
        public static bool IsDecoy(Character? character)
        {
            ZNetView? view = character != null ? character.m_nview : null;
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            return zdo != null && RealOf(zdo) != ZDOID.None;
        }

        /// <summary>On the creature: how many tricks it has done in its life.</summary>
        public static int Done(ZDO zdo) => zdo.GetInt(TraitKeys.Clones);

        /// <summary>On the creature: seconds since its current trick began, or since its last one ended.</summary>
        public static float SecondsSinceMark(ZDO zdo) => NetTime.SecondsSince(zdo.GetLong(TraitKeys.CloneAt));

        /// <summary>
        /// Owner: a trick is spent - it hides behind <paramref name="decoy"/> from now, counted and stamped. A decoy that
        /// could not be made is passed as None: the trick still counts, so a failure is never retried every frame.
        /// </summary>
        public static void Begin(ZDO zdo, ZDOID decoy)
        {
            zdo.Set(DecoyKey, decoy);
            zdo.Set(TraitKeys.Clones, Done(zdo) + 1);
            zdo.Set(TraitKeys.CloneAt, NetTime.NowMs());
        }

        /// <summary>
        /// Owner: the trick is over - it shows again, and the cooldown counts from now. The id is written as None rather
        /// than removed, because a removal alone does not reach the other machines.
        /// </summary>
        public static void End(ZDO zdo)
        {
            zdo.Set(DecoyKey, ZDOID.None);
            zdo.Set(TraitKeys.CloneAt, NetTime.NowMs());
        }

        /// <summary>The decoy's owner, in the frame it is made: whose decoy it is.</summary>
        public static void MarkDecoy(ZDO decoy, ZDOID real) => decoy.Set(OfKey, real);
    }
}
