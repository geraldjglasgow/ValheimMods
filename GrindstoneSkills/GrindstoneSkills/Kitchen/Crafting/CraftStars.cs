using System.Collections.Generic;

namespace GrindstoneSkills
{
    /// <summary>
    /// The stars of items crafted at a kitchen crafting station. Every unit rolls on its own (<see cref="StarOdds"/>)
    /// at the crafter's Cooking level plus the ingredient bonus (<see cref="CookLevel.Effective"/>), plus the size of a
    /// fish being cleaned (<see cref="Fillets"/>). What the station's trash filter throws away is told to the crafter in
    /// one line, e.g. "Trashed: 2 x 1★ Deer stew".
    /// </summary>
    public static class CraftStars
    {
        /// <summary>How many of <paramref name="units"/> rolled each star count (index = stars).</summary>
        public static int[] Roll(int units, float averageIngredientStars)
        {
            float fish = HookGuard.Run("fillet stars", () => Fillets.Levels(CraftRecord.Lots), 0f);
            float level = CookLevel.Effective(CookLevel.Local(), averageIngredientStars) + fish;
            int[] rolled = new int[Stars.Max + 1];
            for (int i = 0; i < units; i++)
                rolled[StarOdds.Roll(level)]++;
            return rolled;
        }

        /// <summary>Tells the crafter what fell below <paramref name="minStars"/>, if anything.</summary>
        public static void ReportTrashed(KitchenCraftContext craft, int[] rolled, int minStars)
        {
            List<string> parts = new List<string>();
            for (int stars = 0; stars < minStars && stars <= Stars.Max; stars++)
            {
                if (rolled[stars] > 0)
                    parts.Add($"{rolled[stars]} x {stars}★");
            }
            if (parts.Count == 0)
                return;
            string item = Localization.instance.Localize(craft.Shared.m_name);
            craft.Player.Message(MessageHud.MessageType.Center, $"Trashed: {string.Join(", ", parts)} {item}");
        }
    }
}
