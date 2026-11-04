using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Trawling from a ship (2026-11-03, the user's words: "we need to have some kind of mechanism for casting out a net on
    /// a boat while sailing to trawl, then some mechanism to hoist the net up onto the deck and the fish drop out onto the
    /// deck for players to pick up", and "the net winch or hoist needs to be placeable on a ship"; <c>Trawl/TRAWL.md</c> has
    /// it in full). The net winch (<see cref="Prefab"/>) is a hammer piece at a workbench, built on a launched ship's stern
    /// or side rail, which it then rides (<see cref="KwShipRiderRules"/>); E casts its net astern while the ship is afloat
    /// and hauls it in again (<see cref="KwTrawlWinch"/>), and the fish it caught fall onto the deck and ride the ship until
    /// they are picked up (<see cref="KwTrawlDeck"/>). Made once when the network scene first wakes, after the ammunition
    /// piles, on every peer (the dedicated server too, which draws nothing and loads no net), registered every time it
    /// wakes; then the hammer is refreshed (<see cref="KwTrawlHammer"/>).
    /// </summary>
    public static class KwTrawl
    {
        public const string Prefix = "BS_trawl_", Prefab = Prefix + "winch", Label = "Trawl net winch";
        private const string Source = "wood_floor";
        private static readonly List<GameObject> built = new List<GameObject>();

        public const string Description =
            "Build it on a launched ship at the stern or a side rail, its arm reaching out over the water. E casts the trawl net astern while the ship is afloat; " +
            "sail or row slowly (1.5 to 4.5 m/s) to fill it, then E hauls it in and the fish drop onto the deck.";

        /// <summary>Its recipe (Meadows and Black Forest items, by the game's prefab names), given back when it is removed.</summary>
        public static readonly (string Item, int Amount)[] Cost = { ("Wood", 10), ("FineWood", 6), ("Bronze", 2), ("LeatherScraps", 12) };

        /// <summary>The winch piece, once made.</summary>
        public static IReadOnlyList<GameObject> All => built;

        /// <summary>Once, from the plugin, after the ammunition piles (<see cref="KwPiles.Install"/>).</summary>
        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside trawl net winch", () => Build(scene)));

        private static void Build(ZNetScene scene)
        {
            if (built.Count == 0)
                Create(scene);
            foreach (GameObject prefab in built)
                NetPrefabs.Register(scene, prefab);
            KwHammer.Refresh();
        }

        private static void Create(ZNetScene scene)
        {
            GameObject? floor = scene.GetPrefab(Source);
            if (floor == null || floor.GetComponent<Piece>() == null || floor.GetComponent<ZNetView>() == null)
            {
                Log.Warn($"Broadside: the game's {Source} was not found; no trawl net winch.");
                return;
            }
            KwTrawlNet.Load(scene);
            built.Add(KwTrawlBuild.Make(floor));
            Log.Info($"Broadside: the trawl net winch built ({KwIcons.RenderAll(built)} hammer icon rendered).");
        }
    }
}
