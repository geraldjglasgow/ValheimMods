using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// The Shipping Crate build piece: a copy of the game's wooden chest (<see cref="ClonedPiece"/>), 6 x 3 slots, in the
    /// hammer's Misc tab at a workbench for 10 Wood and 4 Resin, buildable while the "Shipping Crate" switch is on. The
    /// switch is watched, so the hammer follows a synced change at once. Built crates stay chests while it is off.
    /// </summary>
    public static class ShippingPiece
    {
        private const string Source = "piece_chest_wood";
        private const int Width = 6;
        private const int Height = 3;

        private static bool watching;

        public static readonly ClonedPiece Piece = new ClonedPiece(ShippingKeys.Prefab, Source, "Shipping Crate",
            "What you leave in it is bought at dawn for coins. Starred food pays more.", ("Wood", 10), ("Resin", 4))
        {
            Dress = DressCopy,
            Buildable = () => On,
        };

        /// <summary>Whether the Shipping Crate switch is on.</summary>
        public static bool On => Settings.ShippingCrate != null && Settings.ShippingCrate.Value;

        private static void DressCopy(GameObject copy)
        {
            Container container = copy.GetComponent<Container>();
            if (container == null)
            {
                Hearthhold.Log.LogWarning($"The game's {Source} has no container: the Shipping Crate cannot sell.");
                return;
            }
            container.m_name = "Shipping Crate";
            container.m_width = Width;
            container.m_height = Height;
            copy.AddComponent<ShippingCrate>();
        }

        private static void Watch()
        {
            if (watching || Settings.ShippingCrate == null)
                return;
            watching = true;
            Settings.ShippingCrate.SettingChanged += (sender, args) => HookGuard.Run("shipping crate switch", Piece.RefreshHammer);
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPrefix]
            private static void Prefix()
            {
                ClonedPieces.Add(Piece);
                Watch();
            }
        }
    }
}
