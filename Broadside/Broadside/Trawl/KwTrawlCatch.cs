using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the net out catches, on the winch's owner once a second (<see cref="KwTrawlOwner"/>): how it fares (full, snagged
    /// on the sea floor, no fish in these waters, too slow, trawling, too fast and lifting; <see cref="Fare"/>) and, while it
    /// fishes, the way to the next fish grown by the share of a full rate its speed gives (<see cref="KwTrawlTune.Share"/>:
    /// a fish every <see cref="KwTrawlTune.SecondsPerFish"/> s in the band) up to <see cref="KwTrawlTune.Capacity"/>, each
    /// fish one of the water's own by chance (<see cref="KwTrawlFish"/>, the biome under the net). All in the winch's ZDO.
    /// </summary>
    public static class KwTrawlCatch
    {
        public static void Step(ZDO zdo, KwTrawlWinch winch, float seconds)
        {
            int count = KwTrawlState.Count(zdo);
            Heightmap.Biome biome = KwTrawlSea.Biome(winch.Tow.At);
            KwTrawlFare fare = Fare(count, winch.Tow.Snagged, KwTrawlFish.Has(biome), winch.Speed);
            KwTrawlState.Fares(zdo, fare);
            float share = Fishing(fare) ? KwTrawlTune.Share(winch.Speed) : 0f;
            if (share <= 0f)
                return;
            float part = zdo.GetFloat(KwTrawlState.PartHash) + share * seconds / KwTrawlTune.SecondsPerFish;
            if (part >= 1f && KwTrawlFish.Pick(biome, Random.value) is string fish)
            {
                KwTrawlState.Add(zdo, fish);
                part -= 1f;
            }
            zdo.Set(KwTrawlState.PartHash, Mathf.Clamp01(part));
        }

        /// <summary>How the net fares, as plain data.</summary>
        public static KwTrawlFare Fare(int count, bool snagged, bool fishHere, float speed)
        {
            if (count >= KwTrawlTune.Capacity)
                return KwTrawlFare.Full;
            if (snagged)
                return KwTrawlFare.Snagged;
            if (!fishHere)
                return KwTrawlFare.NoFish;
            if (speed < KwTrawlTune.Trawling)
                return KwTrawlFare.Slack;
            return speed > KwTrawlTune.Lifting ? KwTrawlFare.Lifting : KwTrawlFare.Trawling;
        }

        // Fishing at all: slow (a little above the slack speed), in the band, or lifting.
        private static bool Fishing(KwTrawlFare fare) => fare == KwTrawlFare.Slack || fare == KwTrawlFare.Trawling || fare == KwTrawlFare.Lifting;
    }
}
