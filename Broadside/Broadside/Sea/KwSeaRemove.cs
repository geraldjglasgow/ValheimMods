using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammer's remove on a launched ship (the user's rule, 2026-10-26): aimed right at one of its sea-edited parts (a
    /// railing, a door or window, a ladder, the rig: the nearest <see cref="KwSeaKind.Pick"/> within <see cref="Take"/>)
    /// it removes only that part, as a sea edit (<see cref="KwSeaKind.Removal"/>, <see cref="KwSeaAsk"/>), its materials
    /// given back; a near miss (within <see cref="Near"/> of one) removes nothing and says so, so it never breaks a whole
    /// ship up; aimed anywhere else (the hull, a deck, the keel) the ship is broken up as before
    /// (<see cref="KwShipBreak"/>, whose prefix runs after this one). A piece built aboard (a rider) is the game's.
    /// </summary>
    [HarmonyPatch(typeof(Player), "RemovePiece")]
    public static class KwSeaRemove
    {
        public const float Take = 0.2f, Near = 0.75f;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        private static bool Prefix(Player __instance, ref bool __result)
        {
            if (__instance != Player.m_localPlayer)
                return true;
            int done = Guard.Run<Player, int>("Broadside sea remove", Decide, __instance);
            if (done == 0)
                return true;
            // the game plays its remove at the piece it hovers: none (a sail against the sky), no remove played
            __result = done == 1 && __instance.GetHoveringPiece() != null;
            Taken = Time.frameCount;
            return false;
        }

        /// <summary>The frame a sea part was removed or refused here: the ship's break-up prefix then leaves the click alone.</summary>
        public static int Taken { get; private set; } = -1;

        // 0: not a sea part (the game's removal, a break-up); 1: a removal sent; 2: refused.
        private static int Decide(Player player)
        {
            KwSeaAim? aim = KwSeaAim.Of(player, player.m_removeRayMask);
            if (aim == null || (aim.Hit && Rider(aim)))
                return 0;
            KwSeaPick? pick = Nearest(aim);
            if (pick == null || pick.Distance > Near || !aim.Within(player, pick.Point))
                return 0;
            if (pick.Distance > Take)
            {
                player.Message(MessageHud.MessageType.Center, KwSeaWords.NearMiss(pick.What));
                return 2;
            }
            KwSeaEdit? edit = pick.Kind.Removal(aim.Ship, pick);
            return edit != null && KwSeaAsk.Send(player, aim.Ship, edit, pick.Point) ? 1 : 2;
        }

        // Whether the collider hit is a piece built aboard (its own piece nearer than the ship's).
        private static bool Rider(KwSeaAim aim)
        {
            Piece? piece = aim.Collider != null ? aim.Collider.GetComponentInParent<Piece>() : null;
            return piece != null && piece.gameObject != aim.Ship.gameObject;
        }

        /// <summary>Every kind's part nearest the aim, the nearest of them; null for none.</summary>
        public static KwSeaPick? Nearest(KwSeaAim aim)
        {
            KwSeaPick? best = null;
            foreach (KwSeaKind kind in KwSeaKinds.All)
            {
                KwSeaPick? pick = null;
                KwShipBody.Step("Broadside sea pick " + kind.Name, () => pick = kind.Pick(aim));
                if (pick != null && (best == null || pick.Distance < best.Distance))
                    best = pick;
            }
            return best;
        }
    }
}
