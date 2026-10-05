using EliteCraftingLink;
using EliteCreaturesPack.Core;

namespace EliteCreaturesPack.Crafting
{
    /// <summary>
    /// Elite Crafting, optional. When it is installed (it loads first: the plugin's soft dependency) the mod tells it, in
    /// the plugin's Awake, what its gear is (<see cref="CraftingGear"/>: item class and item level of every bone weapon and
    /// the Kraken shield) and what its creatures drop of Elite Crafting's runes and rolled gear (<see cref="CraftingLoot"/>),
    /// through the merged EliteCraftingLink library: Elite Crafting's public API found by reflection, no reference either
    /// way, nothing at all without it. Registrations are code, so every peer (server and clients) makes the same ones; a
    /// server's Elite Crafting files still override any of them by prefab name. Loot that follows a setting (the spawn
    /// biomes) is sent again when the settings change, the server's values included.
    /// </summary>
    internal static class CraftingHandOff
    {
        public static void Install()
        {
            if (!CraftingLink.Present)
            {
                return;
            }
            int gear = CraftingGear.Register();
            int creatures = CraftingLoot.Register();
            Settings.Changed += () => SafeCall.Run("Elite Crafting loot", () => CraftingLoot.Register());
            Log.Info($"Elite Crafting {CraftingLink.PluginVersion} found: {gear} weapons and shields given a class and a level, "
                + $"loot set for {creatures} creatures.");
        }
    }
}
