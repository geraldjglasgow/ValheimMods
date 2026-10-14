using EliteCreaturesReborn.Patches;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// A grave out of Hel's river (features/arena.md section 1). The game makes a dead player's grave where they died
    /// (<c>Player.CreateTombStone</c>: the tombstone at the body's centre), which for a death in the river is in the air
    /// over nothing, 5,000 m up. While the local player's grave is made under the river (<see cref="HallLake.Under"/>),
    /// the new tombstone is put on the arena's floor at the walls' <c>gate_inside</c> point, a metre up, as it wakes:
    /// before its own Awake records where it was made (the spot it returns to if pushed more than 4 m away), and with its
    /// fresh ZDO there too, so every peer and every reload finds it on the floor. The death point and map pin stay where
    /// the player fell. Every other grave is the game's own; a failure leaves the grave where the game made it.
    /// </summary>
    internal static class HallLakeGrave
    {
        private const string Gate = "gate_inside";
        private const float Raise = 1f;

        private static bool _moving;

        [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
        private static class Making
        {
            private static void Prefix(Player __instance) =>
                _moving = ArenaHall.Built && __instance == Player.m_localPlayer && HallLake.Under(__instance.transform.position);

            private static void Finalizer() => _moving = false;
        }

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.Awake))]
        private static class Placing
        {
            private static void Prefix(TombStone __instance)
            {
                if (_moving)
                {
                    _moving = false;
                    SafeCall.Run("Hel's river grave", static t => Move(t), __instance);
                }
            }
        }

        private static void Move(TombStone grave)
        {
            if (!ArenaHall.TryPoint(Gate, out Vector3 gate, out _))
            {
                return;
            }
            Vector3 spot = gate + Vector3.up * Raise;
            grave.transform.position = spot;
            ZNetView? view = grave.GetComponent<ZNetView>();
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo != null)
            {
                zdo.SetPosition(spot);
            }
        }
    }
}
