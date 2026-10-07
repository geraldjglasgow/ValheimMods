using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// The Aging Cask build piece: a copy of the game's barrel (piece_chest_barrel) named "Aging Cask", in the hammer's
    /// Misc tab at a workbench, for 10 fine wood, 10 bronze nails and 5 resin. Always buildable (no setting). Its
    /// container gets an <see cref="AgingCask"/>, which ages the meads and wines inside. Registered with
    /// <see cref="ClonedPieces"/> from a ZNetScene.Awake prefix, so it is listed before ClonedPieces' postfix installs it.
    /// </summary>
    public static class CaskPiece
    {
        public const string DisplayName = "Aging Cask";
        public const string Description = "Meads and wines stored inside gain a star every 2 days, up to gold.";

        public static readonly ClonedPiece Piece = new ClonedPiece(CaskKeys.Prefab, "piece_chest_barrel", DisplayName, Description,
            ("FineWood", 10), ("BronzeNails", 10), ("Resin", 5))
        {
            Dress = DressCopy,
        };

        private static void DressCopy(GameObject copy)
        {
            Container container = copy.GetComponentInChildren<Container>(true);
            if (container == null)
            {
                Hearthhold.Log.LogWarning("The game's barrel has no container: the Aging Cask ages nothing.");
                return;
            }
            container.m_name = DisplayName;
            container.gameObject.AddComponent<AgingCask>();
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPrefix]
            private static void Prefix() => ClonedPieces.Add(Piece);
        }
    }
}
