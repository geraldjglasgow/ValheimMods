using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>The trawl net's steps, in order: on the drum, paying out, out trawling, hauling in, swung in and emptied.</summary>
    public enum KwTrawlStep
    {
        Stowed, Casting, Out, Hauling, Landing,
    }

    /// <summary>How the net out fares, as its winch's owner last found it (the hover says it).</summary>
    public enum KwTrawlFare
    {
        None, Slack, Trawling, Lifting, Snagged, Full, NoFish,
    }

    /// <summary>
    /// A net winch's state in its ZDO (<see cref="KwTrawlWinch"/>), written only by the winch's owner, read by every peer
    /// (a late joiner sees it at once): the step (<see cref="StepKey"/>) and when it began (<see cref="AtKey"/>, the game's
    /// network clock in ticks, the same on every peer), the catch so far (<see cref="CatchKey"/>: the fish's prefab
    /// names, comma separated; kept in the net until it is landed), the way to the next fish (<see cref="PartKey"/>) and how
    /// the net fares (<see cref="FareKey"/>).
    /// </summary>
    public static class KwTrawlState
    {
        public const string StepKey = "bs_trawl_state", AtKey = "bs_trawl_at", CatchKey = "bs_trawl_catch", PartKey = "bs_trawl_part", FareKey = "bs_trawl_fare";
        public static readonly int StepHash = StepKey.GetStableHashCode(), AtHash = AtKey.GetStableHashCode(), CatchHash = CatchKey.GetStableHashCode();
        public static readonly int PartHash = PartKey.GetStableHashCode(), FareHash = FareKey.GetStableHashCode();

        public static KwTrawlStep Step(ZDO zdo) => (KwTrawlStep)Mathf.Clamp(zdo.GetInt(StepHash), (int)KwTrawlStep.Stowed, (int)KwTrawlStep.Landing);

        public static KwTrawlFare Fare(ZDO zdo) => (KwTrawlFare)Mathf.Clamp(zdo.GetInt(FareHash), (int)KwTrawlFare.None, (int)KwTrawlFare.NoFish);

        /// <summary>Seconds since the step began, by the network clock (0 before it is known).</summary>
        public static float Since(ZDO zdo)
        {
            long at = zdo.GetLong(AtHash, 0L);
            return at == 0L || ZNet.instance == null ? 0f : Mathf.Max(0f, (float)((ZNet.instance.GetTime().Ticks - at) / 1e7));
        }

        /// <summary>Whether the net is in the water: paying out, out or hauling in.</summary>
        public static bool InWater(KwTrawlStep step) => step == KwTrawlStep.Casting || step == KwTrawlStep.Out || step == KwTrawlStep.Hauling;

        /// <summary>On the owner: a step begun now.</summary>
        public static void Begin(ZDO zdo, KwTrawlStep step)
        {
            zdo.Set(StepHash, (int)step);
            zdo.Set(AtHash, ZNet.instance != null ? ZNet.instance.GetTime().Ticks : 1L);
        }

        /// <summary>On the owner: how the net fares, written only when it changes.</summary>
        public static void Fares(ZDO zdo, KwTrawlFare fare)
        {
            if (Fare(zdo) != fare)
                zdo.Set(FareHash, (int)fare);
        }

        /// <summary>How many fish are in the net (the commas counted, nothing made).</summary>
        public static int Count(ZDO zdo)
        {
            string text = zdo.GetString(CatchHash, "");
            if (text.Length == 0)
                return 0;
            int count = 1;
            foreach (char c in text)
                count += c == ',' ? 1 : 0;
            return count;
        }

        /// <summary>The fish in the net, by prefab name.</summary>
        public static List<string> Catch(ZDO zdo)
        {
            string text = zdo.GetString(CatchHash, "");
            return text.Length == 0 ? new List<string>() : new List<string>(text.Split(','));
        }

        /// <summary>On the owner: one more fish in the net.</summary>
        public static void Add(ZDO zdo, string fish)
        {
            string text = zdo.GetString(CatchHash, "");
            zdo.Set(CatchHash, text.Length == 0 ? fish : text + "," + fish);
        }

        /// <summary>On the owner: the net emptied.</summary>
        public static void Empty(ZDO zdo)
        {
            zdo.Set(CatchHash, "");
            zdo.Set(PartHash, 0f);
        }
    }
}
