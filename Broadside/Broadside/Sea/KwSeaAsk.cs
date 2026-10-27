using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The editing player's side of a sea edit, on their own game: the game's building rules at the part
    /// (<see cref="KwSeaRules"/>), the cost tried on a copy of the blueprint and checked against the inventory, the edit sent
    /// to the ship's owner with a token (one at a time); and the owner's answer (<see cref="KwSeaShip.EditedRpc"/>): the
    /// items it worked out taken or given back exactly once (none with the world's free build key; nothing taken with the
    /// no-cost cheat), the player told what was done or why not.
    /// </summary>
    public static class KwSeaAsk
    {
        private const float Wait = 6f, Remember = 60f;
        private static readonly Dictionary<int, (KwSeaEdit Edit, float Sent)> asked = new Dictionary<int, (KwSeaEdit, float)>();
        private static float lastSent = -100f;

        /// <summary>Whether an edit is waiting for its answer.</summary>
        public static bool Waiting => Time.time - lastSent < Wait && asked.Count > 0;

        /// <summary>
        /// The edit asked of the ship's owner, the rules checked at a point of the ship's frame (the part's) and the cost
        /// against the player's inventory first; false (the player told why) when not sent.
        /// </summary>
        public static bool Send(Player player, KwSeaShip ship, KwSeaEdit edit, Vector3 shipPoint)
        {
            string? why = Waiting ? "Wait for the last change to the ship" : KwSeaRules.Why(player, ship, ship.World(shipPoint));
            if (why == null && !player.NoCostCheat() && !Free(ship))
            {
                string? missing = KwBpCost.Missing(player, KwSeaCost.Paid(Tried(ship, edit)));
                why = missing != null ? "That needs " + missing : null;
            }
            if (why != null)
                return Say(player, why);
            Forget();
            int token = Random.Range(1, int.MaxValue);
            asked[token] = (edit, Time.time);
            lastSent = Time.time;
            ship.View.InvokeRPC(KwSeaShip.EditRpc, edit.Write(), token);
            return true;
        }

        /// <summary>What the edit would cost, tried on a copy of the ship's blueprint (as the owner works it out).</summary>
        public static List<(string Item, int Amount)> Tried(KwSeaShip ship, KwSeaEdit edit)
        {
            KwBlueprint after = KwSeaApply.Copy(ship.Blueprint);
            KwSeaChanges changes = KwSeaApply.Apply(after, edit);
            return KwSeaCost.Of(ship, ship.Blueprint, after, changes);
        }

        /// <summary>The owner's answer: the cost settled once for an edit this game asked, the player told.</summary>
        public static void Answered(KwSeaShip ship, int token, bool ok, string words, ZPackage pkg)
        {
            Player? player = Player.m_localPlayer;
            if (!asked.Remove(token) || player == null)
                return;
            lastSent = -100f;
            if (!ok)
            {
                Say(player, words);
                return;
            }
            List<(string Item, int Amount)> cost = KwSeaCost.Read(pkg);
            if (!Free(ship))
            {
                if (!player.NoCostCheat())
                    KwBpCost.Take(player, KwSeaCost.Paid(cost));
                KwSeaCost.Give(player, KwSeaCost.Back(cost));
            }
            Say(player, Words(words, cost, Free(ship)));
        }

        // What the player reads: what was done, what it took and gave back.
        private static string Words(string done, List<(string Item, int Amount)> cost, bool free)
        {
            List<(string Item, int Amount)> paid = KwSeaCost.Paid(cost), back = KwSeaCost.Back(cost);
            string text = string.IsNullOrEmpty(done) ? "Done" : done;
            if (!free && paid.Count > 0)
                text += "\nUsed " + KwBpCost.Words(paid);
            if (!free && back.Count > 0)
                text += "\nGot back " + KwBpCost.Words(back);
            return text;
        }

        // Whether the world builds for free (its free build key, as the game's own pieces and the ship's launch).
        private static bool Free(KwSeaShip ship)
        {
            Piece? piece = ship.GetComponent<Piece>();
            return piece != null && ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey());
        }

        // Tokens whose answer never came are let go after a minute.
        private static void Forget()
        {
            List<int> old = new List<int>();
            foreach (KeyValuePair<int, (KwSeaEdit Edit, float Sent)> entry in asked)
            {
                if (Time.time - entry.Value.Sent > Remember)
                    old.Add(entry.Key);
            }
            old.ForEach(t => asked.Remove(t));
        }

        private static bool Say(Player player, string text)
        {
            player.Message(MessageHud.MessageType.Center, text);
            return false;
        }
    }
}
