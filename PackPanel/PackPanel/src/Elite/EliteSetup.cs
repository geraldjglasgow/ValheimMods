using System.Collections.Generic;
using EliteCraftingLink;
using PackPanel.Backpacks;

namespace PackPanel.Elite
{
    /// <summary>
    /// PackPanel with EliteCrafting (the user's request, 2026-10-05), through EliteCrafting's public API bound by the
    /// merged <c>EliteCraftingLink</c> library: nothing of EliteCrafting is referenced, and without it (or with one older
    /// than API version 1) nothing here runs. Called from the plugin's Awake, which BepInEx runs after EliteCrafting's
    /// (soft dependency): the backpack item class with the eight packs and their item levels (the catalog lists them by
    /// biome, Meadows 1 to Deep North 8), the <c>pack_slots</c> effect and the Deep Pockets inscription
    /// (<see cref="DeepPockets"/> applies it), six of EliteCrafting's inscriptions added to the class's pool, the worn
    /// pack as equipment (<see cref="WornPackLink"/>). Registrations are code: every peer that runs both mods makes the
    /// same ones, the dedicated server included, and the server's EliteCrafting files still override them. The words are
    /// in <c>translations/English.yml</c>: the class's and the inscription's names, and the inscription's tooltip sentence
    /// under the key EliteCrafting looks for in the game's localization, <c>ecf_affix_deep_pockets_line</c> ($1 the value).
    /// </summary>
    public static class EliteSetup
    {
        public static void Register()
        {
            if (!CraftingLink.Present)
                return;
            List<string> refused = new List<string>();
            Expect(CraftingClasses.RegisterItemClass(EliteDefinitions.ClassJson), "the backpack class", refused);
            Expect(CraftingClasses.ClaimItems(EliteDefinitions.ClassId, PackIds()), "the backpacks' class", refused);
            SetLevels(refused);
            Expect(CraftingInscriptions.RegisterExternalEffect(EliteDefinitions.EffectJson), "the pack_slots effect", refused);
            Expect(CraftingInscriptions.RegisterInscription(EliteDefinitions.DeepPocketsJson), "Deep Pockets", refused);
            foreach ((string id, bool best) in EliteDefinitions.Pool)
                Expect(CraftingInscriptions.AddToPool(EliteDefinitions.ClassId, id, best), id + " on backpacks", refused);
            WornPackLink.Register(refused);
            Plugin.Log.LogInfo($"EliteCrafting {CraftingLink.PluginVersion}: backpacks are a class of their own"
                + (refused.Count > 0 ? $", refused: {string.Join(", ", refused)} (see EliteCrafting's log)" : ""));
        }

        private static string[] PackIds()
        {
            IReadOnlyList<BackpackKind> all = BackpackCatalog.All;
            string[] ids = new string[all.Count];
            for (int i = 0; i < all.Count; i++)
                ids[i] = all[i].Id;
            return ids;
        }

        /// <summary>Item level by biome: the catalog's order, Deerhide Satchel 1 to Moosehide Pack 8.</summary>
        private static void SetLevels(List<string> refused)
        {
            IReadOnlyList<BackpackKind> all = BackpackCatalog.All;
            for (int i = 0; i < all.Count; i++)
                Expect(CraftingClasses.SetItemLevel(all[i].Id, i + 1), all[i].Id + "'s level", refused);
        }

        internal static void Expect(bool accepted, string what, List<string> refused)
        {
            if (!accepted)
                refused.Add(what);
        }
    }
}
