using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Black powder (<c>BS_BlackPowder</c>), the Black Forest's gunpowder for the cannons: a little open keg of plank wood
    /// with two iron hoops, heaped with dark powder (the coal's own material). Ground at the forge from coal and resin, four
    /// at a time (no swamp guck since 2026-10-26, when the cannons came down to the Black Forest); stacks like the game's
    /// materials. A cannon loads it as a charge sewn in a linen bag
    /// (<see cref="Cartridge"/>, drawn going into a breech by <see cref="KwCannonWork"/>).
    /// </summary>
    public sealed class KwBlackPowder : IKwItemSet
    {
        public const string Name = "BS_BlackPowder";

        private const float Bottom = 0.1f, Belly = 0.12f, Height = 0.22f;

        public IEnumerable<KwItemDef> Items()
        {
            KwItemDef powder = new KwItemDef(Name, "Black powder", KwTiers.BlackForest)
            {
                Description = "Coal and resin ground fine and kept dry in a little keg. Ship's cannons fire with it.",
                Stack = 50,
                Weight = 0.5f,
                Station = "forge",
                StationLevel = 1,
                Amount = 4,
            };
            powder.Cost.AddRange(new[] { ("Coal", 2), ("Resin", 1) });
            yield return powder.Part(KwLook.Planks, Keg).Part(KwLook.Iron, Hoops).Part(KwLook.Coal, Heap);
        }

        // Staves bellied out from the foot to the middle and in again to the rim, a floor and a rim inside the top.
        private static void Keg(MeshBuilder b)
        {
            KwRound.Taper(b, Vector3.zero, new Vector3(0f, 0f, Height / 2f), Bottom, Belly, 12);
            KwRound.Taper(b, new Vector3(0f, 0f, Height / 2f), new Vector3(0f, 0f, Height), Belly, Bottom, 12);
            KwRound.Ring(b, new Vector3(0f, 0f, Height - 0.005f), Vector3.forward, Bottom - 0.008f, 0.008f, 12, 4);
        }

        private static void Hoops(MeshBuilder b)
        {
            foreach (float z in new[] { 0.035f, Height - 0.035f })
                KwRound.Ring(b, new Vector3(0f, 0f, z), Vector3.forward, Radius(z) + 0.004f, 0.007f, 14, 4);
        }

        // The powder heaped a little over the rim.
        private static void Heap(MeshBuilder b) =>
            KwRound.Taper(b, new Vector3(0f, 0f, Height - 0.02f), new Vector3(0f, 0f, Height + 0.035f), Bottom - 0.012f, 0.012f, 10);

        /// <summary>A charge sewn in a linen bag, as a cannon loads it: round along x from its back end, so long and so thick, its ends drawn in.</summary>
        public static void Cartridge(MeshBuilder b, Vector3 from, float length, float radius)
        {
            Vector2[] outline = { new Vector2(0f, radius * 0.55f), new Vector2(0.02f, radius), new Vector2(length - 0.02f, radius), new Vector2(length, radius * 0.55f) };
            KwCannonParts.Lathe(b, from, outline, 10);
        }

        /// <summary>The cord tied round a cartridge near its back end.</summary>
        public static void Tie(MeshBuilder b, Vector3 from, float radius) =>
            KwRound.Ring(b, from + new Vector3(0.035f, 0f, 0f), new Vector3(1f, 0f, 0f), radius + 0.002f, 0.005f, 10, 3);

        private static float Radius(float z) => Mathf.Lerp(Bottom, Belly, 1f - Mathf.Abs(z / (Height / 2f) - 1f));
    }
}
