using System.Collections.Generic;
using EliteCreaturesPack.Arsenal;
using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// How the Bone Ballista and its missiles are made, while the switch is on (<see cref="BallistaSettings"/>): the
    /// ballista in the hammer right after the game's ballista (3 spines and 25 bone fragments; at the workbench, the game
    /// ballista's station), and Bone Missiles at the workbench, level 2
    /// like the skeleton arsenal, twenty at a time like the game's wooden missiles. Off: both leave; built ballistas
    /// stay. Put in every ObjectDB the game builds or copies, once the prefabs exist, and again when the switch changes.
    /// Every peer.
    /// </summary>
    [HarmonyPatch]
    public static class BallistaCrafting
    {
        private const string Hammer = "Hammer", Workbench = "piece_workbench";
        private const int MissilesPerCraft = 20, WorkbenchLevel = 2;
        private static readonly (string item, int amount)[] PieceCost = { (ArsenalItems.SpineName, 3), ("BoneFragments", 25) };
        private static readonly (string item, int amount)[] MissileCost = { ("BoneFragments", 5), ("Feathers", 2) };

        private static Recipe? recipe;

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Built(ObjectDB __instance) => SafeCall.Run("bone ballista crafting", Refresh, __instance);

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Copied(ObjectDB __instance) => SafeCall.Run("bone ballista crafting", Refresh, __instance);

        /// <summary>The hammer and the missile recipe in step with the switch (idempotent).</summary>
        public static void Refresh(ObjectDB? db)
        {
            if (db == null)
            {
                return;
            }
            if (BallistaPiece.Prefab != null)
            {
                Hammered(db, BallistaPiece.Prefab);
            }
            if (BallistaMissile.Item != null)
            {
                Recipe(db, BallistaMissile.Item);
            }
        }

        private static void Hammered(ObjectDB db, GameObject ballista)
        {
            List<GameObject>? pieces = db.GetItemPrefab(Hammer)?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces?.m_pieces;
            if (pieces == null)
            {
                return;
            }
            pieces.Remove(ballista);
            ballista.GetComponent<Piece>().m_resources = Cost(db, PieceCost);
            if (BallistaSettings.Enabled)
            {
                int game = pieces.FindIndex(p => p != null && p.name == BallistaPiece.GamePiece);
                pieces.Insert(game < 0 ? pieces.Count : game + 1, ballista);
            }
        }

        private static void Recipe(ObjectDB db, GameObject missile)
        {
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<Recipe>();
                (recipe.name, recipe.m_item, recipe.m_amount) = ("Recipe_" + missile.name, missile.GetComponent<ItemDrop>(), MissilesPerCraft);
            }
            GameObject? bench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Workbench) : null;
            recipe.m_craftingStation = bench != null ? bench.GetComponent<CraftingStation>() : null;
            recipe.m_minStationLevel = WorkbenchLevel;
            recipe.m_resources = Cost(db, MissileCost);
            recipe.m_enabled = BallistaSettings.Enabled && recipe.m_craftingStation != null;
            if (!db.m_recipes.Contains(recipe))
            {
                db.m_recipes.Add(recipe);
            }
        }

        private static Piece.Requirement[] Cost(ObjectDB db, (string item, int amount)[] cost)
        {
            var made = new List<Piece.Requirement>();
            foreach ((string name, int amount) in cost)
            {
                ItemDrop? item = ArsenalItems.Find(db, name);
                if (item == null)
                {
                    Log.Warn($"Bone ballista: the game has no {name}; it is left out of the cost.");
                    continue;
                }
                made.Add(new Piece.Requirement { m_resItem = item, m_amount = amount, m_recover = true });
            }
            return made.ToArray();
        }
    }
}
