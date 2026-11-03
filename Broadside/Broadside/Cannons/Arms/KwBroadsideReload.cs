using System.Collections.Generic;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The captain's broadside reloads what it fired, on his own game. Each cannon his called shot fired
    /// (<see cref="Fired"/>, from <see cref="KwArm"/>'s called shot: his game owns the cannon's ZDO from the call on) is
    /// queued, and once it has rested after the shot as a gunner's would (<see cref="ReloadCycle.Rest"/>, the shot's
    /// motion) its reload begins there, a gunner's reload in every way (<see cref="Begin"/>: the ball and powder for the
    /// shot it fired, else its other shot's, taken from the ammunition piles riding the cannon's ship first and the rest
    /// from his pack, <see cref="KwPileStore"/>, and the load written to the ZDO with the step and its start, which every
    /// peer then draws, the breech's reload motion included, and counts loaded once the step has run). A pile another peer
    /// owns is asked of its owner first (<see cref="KwPileAsk"/>): the load waits for the answer briefly (<see cref="KwReload.Asked"/>),
    /// then goes on with what came and his pack. It takes only while his game still owns the cannon, nobody mans
    /// it and it is empty and not loading: one a gunner took meanwhile (his hold wins, his own reload following from his
    /// pack), one broken, gone, or whose ZDO went elsewhere, takes nothing. A gunner who takes hold of one mid-reload finds
    /// it loading, loaded when the step has run (nothing given back, nothing taken again). Short of a load it stays empty.
    /// </summary>
    public static class KwBroadsideReload
    {
        /// <summary>Seconds past its due time a queued reload is given up (its cannon never settled).</summary>
        private const float GiveUp = 10f;

        private static readonly List<(KwArm Arm, Player Captain, int Kind, float At)> queued = new List<(KwArm, Player, int, float)>();

        /// <summary>What the broadside's reloads count on, for its forecast: the ammunition piles riding the ship (every one, whoever owns it) and the captain's pack.</summary>
        public static KwAmmoStore Store(Player captain, KwShipRiders? ship) => KwPileStore.Plan(ship, captain);

        /// <summary>On the captain's game, a called shot of the trigger `kind` fired: its reload queued for when the cannon has rested.</summary>
        public static void Fired(KwArm arm, Player captain, int kind)
        {
            queued.RemoveAll(q => q.Arm == arm);
            queued.Add((arm, captain, kind, Time.time + arm.Design.Reload.Rest));
        }

        /// <summary>Each frame on the captain's game: each queued reload whose time has come begun, or dropped.</summary>
        public static void Tick()
        {
            for (int i = queued.Count - 1; i >= 0; i--)
            {
                (KwArm arm, Player captain, int kind, float at) = queued[i];
                if (Time.time < at)
                    continue;
                KwReload made = Begin(arm, captain, kind);
                if (made != KwReload.Wait && made != KwReload.Asked || Time.time > at + GiveUp)
                    queued.RemoveAt(i);
            }
        }

        /// <summary>Whether a reload is queued for the cannon (fired by the broadside, not yet begun).</summary>
        public static bool Queued(KwArm arm) => queued.Exists(q => q.Arm == arm);

        /// <summary>Whether any reload is queued (the empty cannons' loads wait for them, <see cref="KwBroadsideLoad"/>).</summary>
        public static bool Pending => queued.Count > 0;

        /// <summary>
        /// On the captain's game: the cannon's reload begun when it can take one now, its load taken from the piles aboard
        /// and his pack; what came of it (wait: not rested yet; asked: waiting for a pile another peer owns).
        /// </summary>
        public static KwReload Begin(KwArm arm, Player captain, int kind)
        {
            KwReload ready = Ready(arm, captain);
            if (ready != KwReload.Begun)
            {
                if (ready == KwReload.Gone && !ReferenceEquals(arm, null))
                    KwPileAsk.Close(arm);
                return ready;
            }
            KwShipRiders? ship = KwPileAsk.ShipOf(arm);
            if (KwPileAsk.Asking(arm, captain, ship, kind))
                return KwReload.Asked;
            if (!KwArmReload.Load(arm, KwPileStore.Take(ship, captain), kind))
                return KwReload.Short;
            WeaponKeys keys = arm.Design.Keys;
            arm.Link.Zdo.Set(keys.Stage, 0);
            arm.Link.Zdo.Set(keys.Step, 1);
            arm.Link.Zdo.Set(keys.StepAt, WeaponClock.Now());
            return KwReload.Begun;
        }

        // Whether the cannon can take a reload on the captain's game now: begun (it can), gone, or wait (not rested yet).
        private static KwReload Ready(KwArm arm, Player captain)
        {
            if (arm == null || captain == null || captain.IsDead() || !arm.Link.Valid || !arm.Net.IsOwner() || arm.Frame == null)
                return KwReload.Gone;
            WeaponState s = arm.State;
            if (arm.Manned || !arm.Tower.IsNone() || s.Loaded || s.Busy || s.Step != 0)
                return KwReload.Gone;
            return s.ShotAge < arm.Design.Reload.Rest ? KwReload.Wait : KwReload.Begun;
        }

        /// <summary>
        /// The forecast for the broadside's message, as U is pressed: how many of the cannons it fires (bow first, the order
        /// they reload in) the store could reload, one after another, and the first item the rest are short of.
        /// </summary>
        public static int Forecast(List<KwArm> firing, KwAmmoStore store, out string? shortOf)
        {
            KwAmmoTally tally = new KwAmmoTally(store);
            int reloads = 0;
            shortOf = null;
            foreach (KwArm arm in firing)
            {
                int kind = arm.Link.Zdo.GetInt(arm.Link.LoadKind);
                if (KwArmReload.Pay(arm.Def, tally, kind, out _) >= 0)
                    reloads++;
                else
                    shortOf ??= KwArmReload.Short(arm.Def, tally, kind);
            }
            return reloads;
        }
    }

    /// <summary>
    /// What came of a queued reload: begun, short of a load, the cannon no longer the captain's to reload, not rested yet,
    /// or waiting for the answer of a pile another peer owns (<see cref="KwPileAsk"/>).
    /// </summary>
    public enum KwReload
    {
        Begun,
        Short,
        Gone,
        Wait,
        Asked,
    }

    /// <summary>The broadside's count, for the captain's top line (<see cref="KwArmWords.Broadside"/>).</summary>
    public struct KwVolley
    {
        /// <summary>The side fired: port, else starboard (the side the captain looked toward).</summary>
        public bool Port;

        /// <summary>Cannons on that side; those fired, manned or not; those not fired: still reloading, empty.</summary>
        public int Aboard, Fired, Reloading, Empty;

        /// <summary>Of those fired that nobody mans (a gunner reloads his own), how many will reload from the piles aboard and the captain's pack, how many are short, and of what (an item prefab).</summary>
        public int Reloads, Short;
        public string? ShortOf;
    }
}
