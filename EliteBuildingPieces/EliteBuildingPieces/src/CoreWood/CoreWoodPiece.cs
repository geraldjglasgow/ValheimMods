using UnityEngine;

namespace EliteBuildingPieces.CoreWood
{
    /// <summary>
    /// One core wood piece: its prefab name (a save key: never renamed), the game piece it copies, the embedded bundle
    /// holding its model, its word key, its core wood cost and its width in stakewalls (its health scales with it).
    /// </summary>
    public sealed class CoreWoodPiece
    {
        public CoreWoodPiece(string prefab, string source, string bundle, string word, int cost, int width)
        {
            Prefab = prefab;
            Source = source;
            Bundle = bundle;
            Word = word;
            Cost = cost;
            Width = width;
        }

        public string Prefab { get; }
        public string Source { get; }
        public string Bundle { get; }
        public string Word { get; }
        public int Cost { get; }
        public int Width { get; }

        /// <summary>The piece prefab; null until the scene first woke (or when its game piece is missing).</summary>
        public GameObject Built { get; set; }

        public bool IsDoor => Source == CoreWoodPieces.Gate;
        public string NameToken => "$ebp_" + Word;
        public string DescriptionToken => "$ebp_" + Word + "_desc";
    }

    /// <summary>
    /// The four pieces. Walls copy the game's stakewall (<c>stake_wall</c>: placement, support, wear, health), doors its
    /// wood gate (<c>wood_gate</c>: the game's Door, sounds and open animator). Models from ValheimAssets
    /// <c>Assets/Props/CoreWoodWall</c> (v002, Variants/DoubleWidth_v003) and <c>Assets/Props/CoreWoodGate</c> (v002).
    /// </summary>
    public static class CoreWoodPieces
    {
        public const string Wall = "stake_wall";
        public const string Gate = "wood_gate";

        public static readonly CoreWoodPiece[] All =
        {
            new CoreWoodPiece("EBP_CoreWoodWall", Wall, "va_corewoodwall_v002", "corewood_wall", 4, 1),
            new CoreWoodPiece("EBP_CoreWoodWallLarge", Wall, "va_corewoodwall_double_v003", "corewood_wall_large", 8, 2),
            new CoreWoodPiece("EBP_CoreWoodDoor", Gate, "va_corewoodgate_single_v002", "corewood_door", 6, 1),
            new CoreWoodPiece("EBP_CoreWoodDoubleDoor", Gate, "va_corewoodgate_double_v002", "corewood_door_double", 12, 2),
        };
    }
}
