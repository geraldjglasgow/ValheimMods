using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cannons' balls (<see cref="KwCannons"/>), of an ammunition type of their own so no game bow or turret takes
    /// them; a cannon fires a ball and burns black powder with it. The stone cannonball (<c>BS_CannonballStone</c>) is for
    /// the bronze cannon: a ball of hewn stone, a little faceted, in the game's stone (<see cref="KwStone"/>), four cut at
    /// the workbench from two stone. The iron cannonball (<c>BS_CannonballIron</c>) is for
    /// the two small cannons: a plain ball of cast iron, four cast at the Swamp's forge from one iron bar. The flametal
    /// cannonball (<c>BS_CannonballFlametal</c>) is for the large cannon: a bigger ball of flametal cast round a grausten
    /// core, its seam still glowing, four cast at the black forge from one flametal and two grausten. The balls fly in
    /// the cannons' shots wearing these models, and the same shapes are drawn going in at a breech as a cannon is loaded
    /// (<see cref="KwCannonWork"/>). Only the stone ball is crafted for now (<see cref="KwTiers.Offered"/>); the iron and
    /// flametal ones are still made, so a world's stacks of them load.
    /// </summary>
    public sealed class KwCannonballs : IKwItemSet
    {
        public const string Stone = "BS_CannonballStone", Iron = "BS_CannonballIron", Flametal = "BS_CannonballFlametal", AmmoType = "$bs_ammo_cannonball";

        /// <summary>The balls' radii (stone shot fills the same bore as iron).</summary>
        public const float StoneRadius = 0.065f, IronRadius = 0.065f, FlametalRadius = 0.095f;

        public IEnumerable<KwItemDef> Items()
        {
            yield return StoneShot();
            KwItemDef iron = Ball(Iron, "Iron cannonball", KwTiers.Swamp, 1.2f,
                "A ball of cast iron for the small cannons. Each shot takes one ball and black powder.");
            iron.Cost.Add(("Iron", 1));
            yield return iron.Part(KwLook.Iron, b => IronBall(b, new Vector3(0f, 0f, IronRadius)));
            KwItemDef flametal = Ball(Flametal, "Flametal cannonball", KwTiers.Ashlands, 2f,
                "A heavy ball of flametal cast round a grausten core, for the large cannon. Each shot takes one ball and black powder.");
            flametal.Cost.AddRange(new[] { ("FlametalNew", 1), ("Grausten", 2) });
            Vector3 middle = new Vector3(0f, 0f, FlametalRadius);
            yield return flametal.Part(KwLook.Flametal, b => FlametalBall(b, middle)).Part(KwLook.Ember, b => Seam(b, middle));
        }

        // Stone shot: drawn in the stone's stand-in look and given the game's stone as its last touch (KwStone), before its
        // icon is rendered and its shot copies the model.
        private static KwItemDef StoneShot()
        {
            KwItemDef stone = Ball(Stone, "Stone cannonball", KwTiers.Meadows, 1f,
                "A ball of hewn stone for the bronze cannon. Each shot takes one ball and black powder.");
            stone.Cost.Add(("Stone", 2));
            stone.Finish = item => KwStone.Wear(item.transform, "part");
            return stone.Part(KwStone.Stand, b => StoneBall(b, new Vector3(0f, 0f, StoneRadius)));
        }

        // A ball as the game keeps its catapult stones: ammunition no hand weapon equips, four to a craft.
        private static KwItemDef Ball(string name, string label, int tier, float weight, string description) => new KwItemDef(name, label, tier)
        {
            Description = description,
            Base = "Catapult_ammo",
            Type = ItemDrop.ItemData.ItemType.AmmoNonEquipable,
            AmmoType = AmmoType,
            Stack = 50,
            Weight = weight,
            Amount = 4,
        };

        /// <summary>A stone ball round a centre, hewn: fewer, broader faces than a cast ball.</summary>
        public static void StoneBall(MeshBuilder b, Vector3 centre) => KwCannonParts.Ball(b, centre, StoneRadius, 5, 8);

        /// <summary>An iron ball round a centre.</summary>
        public static void IronBall(MeshBuilder b, Vector3 centre) => KwCannonParts.Ball(b, centre, IronRadius);

        /// <summary>A flametal ball round a centre (its seam drawn apart, <see cref="Seam"/>).</summary>
        public static void FlametalBall(MeshBuilder b, Vector3 centre) => KwCannonParts.Ball(b, centre, FlametalRadius, 7, 12);

        /// <summary>The glowing seam round a flametal ball's middle, where the casting closed over its core.</summary>
        public static void Seam(MeshBuilder b, Vector3 centre) =>
            KwRound.Ring(b, centre, new Vector3(1f, 0f, 0f), FlametalRadius - 0.002f, 0.007f, 16, 3);
    }
}
