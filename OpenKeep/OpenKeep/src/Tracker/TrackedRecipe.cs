using OpenKeep.Recipes;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// One tracked recipe: its key (<see cref="RecipeKeys"/>), the quality it makes (1 for a new item, the next level for
    /// an upgrade), how many crafts are wanted, and whether it was tracked at an upgrader station (which takes other
    /// materials, <see cref="RecipeNeeds"/>). Saved as <c>key|quality|amount</c>, with <c>|u</c> for an upgrader.
    /// </summary>
    public sealed class TrackedRecipe
    {
        public const int MaxAmount = 9999;
        private const string UpgraderMark = "u";

        public string Key { get; }
        public int Quality { get; }
        public bool Upgrader { get; }
        public int Amount { get; set; }

        public TrackedRecipe(string key, int quality, int amount, bool upgrader)
        {
            Key = key;
            Quality = quality < 1 ? 1 : quality;
            Amount = amount < 1 ? 1 : amount > MaxAmount ? MaxAmount : amount;
            Upgrader = upgrader;
        }

        /// <summary>The loaded recipe, or null while it is unknown (its mod is missing, or the item database is not up yet).</summary>
        public Recipe Recipe => RecipeKeys.Find(Key);

        public bool Is(string key, int quality, bool upgrader) => quality == Quality && upgrader == Upgrader && key == Key;

        public override string ToString() => Key + "|" + Quality + "|" + Amount + (Upgrader ? "|" + UpgraderMark : "");

        /// <summary>A saved entry, or null when it is not one.</summary>
        public static TrackedRecipe Parse(string text)
        {
            string[] parts = (text ?? "").Split('|');
            if (parts.Length < 3 || parts.Length > 4 || parts[0].Length == 0)
                return null;
            if (!int.TryParse(parts[1], out int quality) || !int.TryParse(parts[2], out int amount))
                return null;
            return new TrackedRecipe(parts[0], quality, amount, parts.Length == 4 && parts[3] == UpgraderMark);
        }
    }
}
