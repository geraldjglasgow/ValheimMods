using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// On the captain's game, a cannon's load from the piles another peer owns (the player who has one open, or whoever the
    /// game made its owner, a dedicated server too): his game never writes their ZDOs. Before a load
    /// (<see cref="KwBroadsideReload.Begin"/>) it is planned over every pile aboard the cannon's ship (<see cref="KwPileStore.Plan"/>);
    /// what would come from piles another peer owns is asked of each pile's owner (<see cref="KwPile.TakeCall"/>: an id from
    /// a counter, the item, how many), who takes what it holds of it and answers what it took (<see cref="KwPile.TookCall"/>).
    /// The load waits for the answers (a round, <see cref="Asking"/>) at most <see cref="Patience"/> seconds, then goes on
    /// with what came (<see cref="KwPileCredit"/>) and takes the rest from this game's own piles and the captain's pack. An
    /// answer is taken only for an id this game asked, for that item and no more than asked, once; one that comes late is
    /// held all the same and goes into his pack once nothing waits (nothing taken is lost), and a pile whose owner never
    /// answers (a peer without its object) is left out of the asks for <see cref="QuietFor"/> seconds. One round a cannon at
    /// a time; nothing is asked of a pile on another ship. Nothing is saved.
    /// </summary>
    public static class KwPileAsk
    {
        /// <summary>Seconds a load waits for the piles' answers.</summary>
        public const float Patience = 1.5f;

        /// <summary>Seconds a pile whose owner never answered is left out of the asks.</summary>
        public const float QuietFor = 10f;

        // A round nobody looked at again for so long is dropped; an ask unanswered for so long is forgotten.
        private const float Linger = 5f, Forget = 60f;

        private sealed class Round
        {
            public float Until;
            public int Out;
        }

        private static readonly Dictionary<KwArm, Round> rounds = new Dictionary<KwArm, Round>();
        private static readonly Dictionary<int, (Round Round, KwPile Pile, string Item, int Amount, float At)> asks = new Dictionary<int, (Round, KwPile, string, int, float)>();
        private static readonly List<KwArm> stale = new List<KwArm>();
        private static readonly List<int> old = new List<int>();
        private static int lastId;
        private static float swept;

        /// <summary>The launched ship a cannon rides, or null (parked, or on none).</summary>
        public static KwShipRiders? ShipOf(KwArm arm)
        {
            KwShipRider? rider = arm.GetComponent<KwShipRider>();
            return rider != null ? rider.On : null;
        }

        /// <summary>
        /// Whether the cannon's load waits for piles another peer owns: a round already out and neither answered nor past its
        /// patience, or one asked now (the load planned over every pile aboard asks something of them). False: load now.
        /// </summary>
        public static bool Asking(KwArm arm, Player captain, KwShipRiders? ship, int kind)
        {
            if (rounds.TryGetValue(arm, out Round round))
            {
                if (round.Out > 0 && Time.time < round.Until)
                    return true;
                Close(arm);
                return false;
            }
            if (ship == null)
                return false;
            KwPileStore plan = KwPileStore.Plan(ship, captain);
            if (KwArmReload.Pay(arm.Def, plan, kind, out _) < 0 || plan.Asks.Count == 0)
                return false;
            Send(arm, plan.Asks);
            return true;
        }

        /// <summary>The cannon's round closed (loaded, or no longer the captain's to load): its piles that let its patience run out go quiet.</summary>
        public static void Close(KwArm arm)
        {
            if (!rounds.TryGetValue(arm, out Round round))
                return;
            rounds.Remove(arm);
            if (round.Out <= 0 || Time.time < round.Until)
                return;
            foreach (var ask in asks.Values)
            {
                if (ask.Round == round && ask.Pile != null)
                    ask.Pile.QuietUntil = Time.time + QuietFor;
            }
        }

        /// <summary>On the captain's game, a pile's owner answered: what it took held for the load, if this game asked it.</summary>
        public static void Answered(KwPile pile, int id, string item, int amount)
        {
            if (!asks.TryGetValue(id, out var ask) || ask.Pile != pile || ask.Item != item)
                return;
            asks.Remove(id);
            KwPileCredit.Add(item, Mathf.Clamp(amount, 0, ask.Amount));
            ask.Round.Out--;
            pile.QuietUntil = 0f;
        }

        /// <summary>Each frame on the captain's game: rounds nobody looks at dropped, old asks forgotten, what is held and unused into his pack.</summary>
        public static void Tick()
        {
            if (rounds.Count > 0)
                Drop();
            if (asks.Count > 0 && Time.time > swept + 1f)
                Sweep();
            if (rounds.Count == 0)
                KwPileCredit.Flush();
        }

        // One ask a pile and item, each with its own id; the round waits for them all.
        private static void Send(KwArm arm, List<(KwPile Pile, string Item, int Amount)> wanted)
        {
            Round round = new Round { Until = Time.time + Patience };
            foreach ((KwPile pile, string item, int amount) in wanted)
            {
                int id = ++lastId;
                asks[id] = (round, pile, item, amount, Time.time);
                pile.Net.InvokeRPC(KwPile.TakeCall, id, item, amount);
                round.Out++;
            }
            rounds[arm] = round;
        }

        // Rounds whose cannon was not looked at again since their patience ran out (the captain left the helm, the cannon went).
        private static void Drop()
        {
            stale.Clear();
            foreach (KeyValuePair<KwArm, Round> round in rounds)
            {
                if (Time.time > round.Value.Until + Linger)
                    stale.Add(round.Key);
            }
            foreach (KwArm arm in stale)
                Close(arm);
        }

        private static void Sweep()
        {
            swept = Time.time;
            old.Clear();
            foreach (var ask in asks)
            {
                if (Time.time > ask.Value.At + Forget)
                    old.Add(ask.Key);
            }
            foreach (int id in old)
                asks.Remove(id);
        }
    }
}
