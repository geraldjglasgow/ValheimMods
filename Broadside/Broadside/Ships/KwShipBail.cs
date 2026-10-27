using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Bailing a launched ship's flooded hold: B held (read through the Hotkeys library, never while typing) by a player in
    /// the hold (inside the hull, under its lowest deck, the water of the compartment he stands in no more than
    /// <see cref="Reach"/> under his feet) scoops a bucket (<see cref="Bucket"/>) out of that compartment every
    /// <see cref="Every"/> seconds for a little stamina, bending as the game's players bend to drop an item, and a message
    /// says how full the hold still is. His game asks the ship's owner (<see cref="Rpc"/>, the bucket and the compartment),
    /// who takes the water out (<see cref="KwShipFlood.Bail"/>). B pressed elsewhere aboard a flooded Broadside ship, in a
    /// dry compartment, or too tired, says why nothing is bailed; on a dry ship B does nothing. B is free in the game's own
    /// keys (only its debug mode uses it).
    /// </summary>
    public sealed class KwShipBail : MonoBehaviour
    {
        public const string Rpc = "KW_Bail";
        private const float Every = 1f, Bucket = 0.012f, Stamina = 3f, Reach = 1.2f, UnderDeck = 0.3f, Feet = 0.3f;

        private ZNetView? view;
        private Ship? ship;
        private KwShipFlood? flood;
        private float deckLow = float.MaxValue, last = -99f;

        public void Set(KwShipFlood flood, KwBpHull hull)
        {
            (view, ship, this.flood, deckLow) = (GetComponent<ZNetView>(), GetComponent<Ship>(), flood, hull.DeckLow);
            if (view != null && view.IsValid())
                view.Register<float, int>(Rpc, (sender, bucket, hold) => Guard.Run("Broadside bailing", () => flood.Bail(Mathf.Clamp(bucket, 0f, Bucket), hold)));
        }

        // The local player's game: B held aboard this ship, a bucket every Every seconds while he can bail.
        private void Update()
        {
            Player? me = Player.m_localPlayer;
            if (me == null || ship == null || flood == null || !Hotkey.KeyHeld(KeyCode.B) || Typing.Active || !ship.IsPlayerInBoat(me) || !me.TakeInput())
                return;
            string? why = Why(me, out int hold);
            if (why == null && Time.time - last >= Every)
                Scoop(me, hold);
            else if (!string.IsNullOrEmpty(why) && Input.GetKeyDown(KeyCode.B))
                me.Message(MessageHud.MessageType.Center, why);
        }

        // Why the player cannot bail now (and the compartment he stands in): null when he can, nothing to say when the
        // hold is dry.
        private string? Why(Player me, out int hold)
        {
            hold = -1;
            KwFloodHolds? holds = flood != null ? flood.Holds : null;
            if (holds == null || !flood!.Solved || flood.Volume < Bucket / 4f)
                return "";
            Vector3 local = transform.InverseTransformPoint(me.transform.position);
            if (!InHold(local))
                return "Stand in the flooded hold to bail";
            hold = holds.At(local.z);
            if (!holds.Wet(hold) || holds.VolumeOf(hold) < Bucket / 4f || holds.LevelOf(hold) - Vector3.Dot(holds.Up, local) <= -Reach)
                return "No water to bail here: stand where the hold is flooded";
            return me.HaveStamina(Stamina) ? null : "Too tired to bail";
        }

        // Inside the hull at his feet, under the lowest deck.
        private bool InHold(Vector3 local) =>
            local.y <= deckLow - UnderDeck && KwFloodLevel.Inside(flood!.Slices, local + Vector3.up * Feet);

        private void Scoop(Player me, int hold)
        {
            last = Time.time;
            view!.InvokeRPC(Rpc, Bucket, hold);
            me.UseStamina(Stamina);
            me.m_zanim.SetTrigger("interact");
            int left = Mathf.RoundToInt(100f * Mathf.Max(0f, flood!.Volume - Bucket) / Mathf.Max(flood.Capacity, 1e-3f));
            me.Message(MessageHud.MessageType.Center, $"Bailing: the hold is {left}% flooded");
        }
    }
}
