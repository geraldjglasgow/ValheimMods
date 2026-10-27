using System.Text;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The helm's hover on a Broadside ship says what the ship is: its hull's tier and health, how fire and the Ashlands
    /// sea treat it (<see cref="KwShipPerks"/>), the cargo aboard against the most it floats (<see cref="KwShipCargo"/>)
    /// and whether it lies at anchor (<see cref="KwShipAnchor"/>), where the wind is and how well its sails draw in it
    /// (<see cref="KwSailWords.Now"/>), and, when it is damaged, its holes and how full of water
    /// its hold is (<see cref="KwHullHoles"/>, <see cref="KwShipFlood"/>), under the game's own line. Worked out at most
    /// once a second per ship (the hover is read every frame).
    /// </summary>
    [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.GetHoverText))]
    public static class KwShipCard
    {
        private const string Grey = "<color=#B8B8B8>", Warning = "<color=#E8A050>", End = "</color>";
        private static Ship? last;
        private static string card = "";
        private static float made = -99f;

        [HarmonyPostfix]
        private static void Postfix(ShipControlls __instance, ref string __result)
        {
            Ship? ship = __instance.m_ship;
            if (ship != null && ship.TryGetComponent(out KwShipPerks perks))
                __result += Guard.Run("Broadside ship card", Cached, perks);
        }

        private static string Cached(KwShipPerks perks)
        {
            Ship ship = perks.GetComponent<Ship>();
            if (ship == last && Time.time - made < 1f)
                return card;
            (last, made) = (ship, Time.time);
            return card = Lines(perks);
        }

        private static string Lines(KwShipPerks perks)
        {
            StringBuilder text = new StringBuilder("\n" + Grey);
            text.Append($"{TierName(perks.Tier)} hull: {perks.Health:0} health, {Fire(perks.FireModifier)}");
            text.Append("\nAshlands sea: " + (perks.Share <= 0.001f ? "does not burn it" : perks.Share >= 0.999f ? "burns it" : $"burns it {perks.Share * 100f:0}% as much"));
            KwShipCargo? cargo = perks.GetComponent<KwShipCargo>();
            if (cargo != null)
                text.Append($"\nCargo {cargo.Load:0} of {cargo.Most:0} kg" + (cargo.Overloaded ? ": overloaded" : ""));
            KwShipAnchor? anchor = perks.GetComponent<KwShipAnchor>();
            if (anchor != null && anchor.Down)
                text.Append("\nAt anchor");
            KwShipWind? wind = perks.GetComponent<KwShipWind>();
            if (wind != null && wind.Mix.Best > 0f)
                text.Append("\n" + KwSailWords.Now(wind, perks.transform.forward));
            Damage(text, perks);
            return text.Append(End).ToString();
        }

        // The hull's holes and the water in the hold, when there are any (KwHullHoles and KwShipFlood, read from the
        // ship's kw_holes and kw_flood), in a warning colour.
        private static void Damage(StringBuilder text, KwShipPerks perks)
        {
            int holes = perks.TryGetComponent(out KwHullHoles hull) ? hull.Holes.Count : 0;
            float flooded = perks.TryGetComponent(out KwShipFlood flood) ? flood.Share : 0f;
            if (holes > 0)
                text.Append($"\n{Warning}Holed: {holes} {(holes == 1 ? "hole" : "holes")} in the hull{End}");
            if (flooded >= 0.005f)
                text.Append($"\n{Warning}Hold {flooded * 100f:0}% flooded{End}");
        }

        private static string TierName(float tier) => KwBiomeTiers.All[Mathf.Clamp(Mathf.RoundToInt(tier), 0, KwBiomeTiers.All.Count - 1)].Name;

        private static string Fire(HitData.DamageModifier fire) => fire switch
        {
            HitData.DamageModifier.SlightlyResistant => "slightly fire resistant",
            HitData.DamageModifier.Resistant => "fire resistant",
            HitData.DamageModifier.VeryResistant => "very fire resistant",
            HitData.DamageModifier.Immune => "fireproof",
            HitData.DamageModifier.Weak or HitData.DamageModifier.SlightlyWeak or HitData.DamageModifier.VeryWeak => "burns easily",
            _ => "burns as wood does",
        };
    }
}
