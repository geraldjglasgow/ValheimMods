using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Wooden Repair Kit (the user, 2026-10-26: "Create an item, with an icon and a model, that players can craft: a ship
    /// repair kit. We'll only worry about the Meadows style, so it might be called the Wooden Repair Kit. It repairs ship
    /// hulls; the whole idea is to stop the water seeping in and sinking the ship further."): a crafted item
    /// (<see cref="Name"/>, made at the workbench, <see cref="KwRepairKitRecipe"/>) that patches one hole in a launched
    /// ship's hull (<see cref="KwHullPatchSpot"/>, <see cref="KwHullPatches"/>). A copy of the game's fine wood (its stack,
    /// pickup and floating), named, stacked and weighed as a kit; it wears the workshop's model and icon when the mod embeds
    /// the bundle (<see cref="KwRepairKitLook"/>), else fine wood's look. Made once and registered whenever the network scene
    /// wakes, on every peer (an item dropped on the ground is a networked object), and in every ObjectDB the game builds.
    /// </summary>
    public static class KwRepairKit
    {
        public const string Name = "KW_RepairKit_Wood", Label = "Wooden Repair Kit";
        public const string Description = "Boards, pegs and pine resin to stop a hole in a ship's hull. Aim at the hole and press the use key: "
            + "the sea stops coming in through it until the hole is mended with the hammer at a workbench.";
        private const string Base = "FineWood";
        private const int Stack = 10;
        private const float Weight = 2f;

        /// <summary>The kit's prefab; null until the network scene first wakes, or when the game has no fine wood.</summary>
        public static GameObject? Prefab { get; private set; }

        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside repair kit", () => Build(scene, harmony)));

        private static void Build(ZNetScene scene, Harmony harmony)
        {
            Prefab ??= Make(scene);
            if (Prefab == null)
                return;
            NetPrefabs.Register(scene, Prefab);
            ItemPrefabs.Register(harmony, Prefab);
            KwRepairKitRecipe.Install(ObjectDB.instance);
        }

        private static GameObject? Make(ZNetScene scene)
        {
            GameObject? game = scene.GetPrefab(Base);
            if (game == null || game.GetComponent<ItemDrop>() == null)
            {
                Log.Warn($"Broadside: the game's {Base} was not found; no {Label}.");
                return null;
            }
            GameObject copy = PrefabBench.Copy(game, Name);
            ItemDrop drop = copy.GetComponent<ItemDrop>();
            drop.m_itemData.m_dropPrefab = copy;
            Shared(drop.m_itemData.m_shared);
            KwRepairKitLook.Wear(copy, game);
            return copy;
        }

        private static void Shared(ItemDrop.ItemData.SharedData shared)
        {
            (shared.m_name, shared.m_description) = (Label, Description);
            (shared.m_maxStackSize, shared.m_weight, shared.m_value) = (Stack, Weight, 0);
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_teleportable = true;
        }

        /// <summary>How many kits a player carries.</summary>
        public static int Count(Player player) => player.GetInventory().CountItems(Label);

        /// <summary>One kit taken from a player's inventory; false when they had none.</summary>
        public static bool Take(Player player)
        {
            if (Count(player) <= 0)
                return false;
            player.GetInventory().RemoveItem(Label, 1);
            return true;
        }
    }
}
