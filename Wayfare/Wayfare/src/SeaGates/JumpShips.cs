using System.Collections.Generic;
using HarmonyLib;

namespace Wayfare.SeaGates
{
    /// <summary>The ships loaded on this machine, for the owner's side of a jump (<see cref="ShipJump.Tick"/>). A ship
    /// joins on Awake; the game's <c>Ship</c> has no OnDestroy, so dead entries (a zone unloaded, a ship moved out of
    /// this machine's area mid-jump) are dropped whenever the list is read.</summary>
    public static class JumpShips
    {
        private static readonly List<Ship> ships = new List<Ship>();

        /// <summary>The live ships, dead entries dropped first.</summary>
        public static List<Ship> Live
        {
            get
            {
                ships.RemoveAll(ship => ship == null);
                return ships;
            }
        }

        internal static void Add(Ship ship)
        {
            if (!ships.Contains(ship))
                ships.Add(ship);
        }
    }

    /// <summary>Every ship with a ZDO registers <see cref="SeaGateFields.LandedRpc"/> (jump id, player id) and joins
    /// <see cref="JumpShips"/>, whatever the settings say: a handler and a list entry change nothing in the game, and a
    /// ship loaded while sea gates are off must still be known when they are turned on. The handler itself checks
    /// ownership and the jump. A placement ghost has no ZDO (its <c>ZNetView</c> was disabled) and is skipped.</summary>
    [HarmonyPatch(typeof(Ship), "Awake")]
    public static class ShipJumpAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Ship __instance)
        {
            ZNetView view = __instance != null ? __instance.m_nview : null;
            if (view == null || view.GetZDO() == null)
                return;
            Ship ship = __instance;
            view.Register<int, long>(SeaGateFields.LandedRpc, (sender, jumpId, playerId) => ShipJump.OnLanded(ship, jumpId, playerId));
            JumpShips.Add(ship);
        }
    }
}
