using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// E at a net winch, on the pressing player's game: a stowed net is cast and a net out hauled in, each asked of the
    /// winch's owner (<see cref="KwTrawlWinch.UseCall"/>), with a word on the screen; while the net pays out, comes in or is
    /// emptied the winch is busy. Before a cast this game checks, as it sees the ship (<see cref="Why"/>): the winch rides
    /// a launched ship here; its davit's sheave reaches out over open water, nothing of the ship under it (built at the
    /// stern or a side rail, facing out); and there is water enough astern (<see cref="KwTrawlTune.MinWater"/>).
    /// </summary>
    public static class KwTrawlCast
    {
        private const float Below = 40f, Astern = 4f;
        private static readonly RaycastHit[] hits = new RaycastHit[16];
        private static int mask;

        public static bool Use(KwTrawlWinch winch, Humanoid user)
        {
            KwTrawlStep step = KwTrawlState.Step(winch.Zdo!);
            if (step == KwTrawlStep.Out)
                return Ask(winch, user, KwTrawlWinch.Haul, KwTrawlWords.Hauled);
            if (step != KwTrawlStep.Stowed)
            {
                user.Message(MessageHud.MessageType.Center, KwTrawlWords.Busy);
                return false;
            }
            string? why = Why(winch);
            if (why == null)
                return Ask(winch, user, KwTrawlWinch.Cast, KwTrawlWords.Cast);
            user.Message(MessageHud.MessageType.Center, why);
            return false;
        }

        private static bool Ask(KwTrawlWinch winch, Humanoid user, int want, string words)
        {
            winch.Net.InvokeRPC(KwTrawlWinch.UseCall, want);
            user.Message(MessageHud.MessageType.Center, words);
            return true;
        }

        /// <summary>Why the net cannot be cast now, or null when it can.</summary>
        public static string? Why(KwTrawlWinch winch)
        {
            KwShipRiders? ship = winch.Ship;
            if (ship == null)
                return KwTrawlWords.NotAboard;
            Vector3 sheave = winch.Sheave.position;
            if (!OverWater(sheave, ship))
                return KwTrawlWords.ArmInboard;
            Vector3 astern = sheave - ship.transform.forward * Astern;
            return KwTrawlSea.Floor(astern) > KwTrawlSea.Water - KwTrawlTune.MinWater ? KwTrawlWords.Shallow : null;
        }

        // Nothing of the ship straight under the sheave: the ray cast where the ship's colliders are (its body's pose).
        private static bool OverWater(Vector3 sheave, KwShipRiders ship)
        {
            Rigidbody? body = ship.GetComponent<Rigidbody>();
            (Vector3 at, Quaternion turn) = ship.Pose;
            Vector3 from = KwShipRiderPose.Carry(ship.transform.position, ship.transform.rotation, at, turn, sheave);
            if (mask == 0)
                mask = LayerMask.GetMask("Default", "piece", "vehicle");
            int count = Physics.RaycastNonAlloc(from, Vector3.down, hits, Below, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider != null && hits[i].collider.attachedRigidbody == body)
                    return false;
            }
            return true;
        }
    }
}
