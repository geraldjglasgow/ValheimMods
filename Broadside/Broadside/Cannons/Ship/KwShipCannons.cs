using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The four ship cannons, each on a naval truck carriage (<see cref="KwShipSpec"/>, <see cref="KwShipCarriage"/>), with
    /// their shots (<see cref="KwCannons"/>), each a breech-loading gun (its shape: <see cref="KwSmallIronCannonShape"/>,
    /// <see cref="KwSmallCannonShape"/>, <see cref="KwLargeCannonShape"/>) with a long barrel (the shape's <c>Long</c>
    /// stretch) that reaches back over the carriage and overhangs well past its front, as a naval gun's does, each a piece
    /// of its own (<c>BS_cannon_bronze</c>, Black Forest, the small iron cannon's design cast in bronze on fine wood and
    /// firing stone shot; <c>BS_cannon_small_iron</c>, Swamp; <c>BS_cannon_small</c>, Plains; <c>BS_cannon_large</c>,
    /// Ashlands; only the bronze one offered for now, <see cref="KwTiers.Offered"/>) built only on a launched Broadside
    /// ship's deck (<see cref="KwArmPlace"/>). The whole carriage is levered round on its
    /// trucks only <see cref="Arc"/> each way, the gun elevating in it with the quoin following under the breech; further
    /// round it is hauled about the deck by its rear (<see cref="KwArmTow"/>). At each shot the carriage rolls back on its trucks until the breeching
    /// is taut, then runs out again (<see cref="KwShipRecoil"/>). The gunner aims and reloads close behind the low breech
    /// and a little to its right, bent well over it (<see cref="KwArmDef.MostLean"/>), turned a little toward it; his stand
    /// point turns with the carriage, so he follows the breech round; as he yanks the lanyard he steps out to the right,
    /// out of the recoil's way, and back again once the gun has run out. The carriages are one design in proportion to
    /// their front trucks: the three small guns' alike, the large one's bigger.
    /// </summary>
    public sealed class KwShipCannons : IKwArmSet
    {
        /// <summary>Degrees the carriage is levered round each way on its trucks.</summary>
        public const float Arc = 20f;

        // How far the gunner turns from the way the gun points, toward it, standing behind the breech on its right; how far
        // he bends over the low breech to aim and to reload, at most, and further a degree the gun is raised.
        private const float Facing = 20f, AimLean = 50f, ReloadLean = 55f, MostLean = 65f, TiltLean = 2f;

        // How far the T-handle's crossbar reaches below the barrel's axis, its knob included.
        private const float Knob = 0.068f;

        public IEnumerable<KwArmDef> Arms()
        {
            yield return Bronze();
            yield return SmallIron();
            yield return Small();
            yield return Large();
        }

        // The small iron cannon's gun and carriage cast in bronze on fine wood (the Black Forest rudder's two looks), loading
        // stone shot (its ball in the breech given the stone, KwStone); its handling the small iron cannon's in every way.
        private static KwArmDef Bronze()
        {
            KwShipSpec ship = SmallIronSpec(KwLook.FineWood, KwLook.Bronze);
            KwArmDef arm = Ship(KwCannons.Bronze, "Bronze ship cannon", KwTiers.BlackForest, KwCannons.BronzeShot(), ship, KwSmallIronCannonShape.ShipFace + 0.05f);
            arm.Description = "A cast bronze breech-loading cannon on a naval truck carriage of fine wood: long stepped brackets on four solid trucks, bronze cap-squares and a breeching rope that takes its recoil. Fired by a pull cord; each shot burns one black powder.";
            (arm.TurnSpeed, arm.TiltSpeed, arm.Health) = (22f, 30f, 400f);
            arm.Cost.AddRange(new[] { ("Bronze", 10), ("FineWood", 20) });
            KwSmallIronCannonShape.AddShip(arm, ship, stone: true);
            arm.Finish = prefab => KwStone.Wear(prefab.transform, KwArmMover.Prefix + "ball");
            Stance(arm, ship.Behind(KwSmallIronCannonShape.ShipBreech));
            return arm;
        }

        private static KwArmDef SmallIron()
        {
            KwShipSpec ship = SmallIronSpec(KwLook.Bark, KwLook.Iron);
            KwArmDef arm = Ship(KwCannons.SmallIron, "Small iron ship cannon", KwTiers.Swamp, KwCannons.SmallIronShot(), ship, KwSmallIronCannonShape.ShipFace + 0.05f);
            arm.Description = "A cast-iron breech-loading cannon on a naval truck carriage of ancient bark: long stepped brackets on four solid trucks, iron cap-squares and a breeching rope that takes its recoil. Fired by a pull cord; each shot burns one black powder.";
            (arm.TurnSpeed, arm.TiltSpeed, arm.Health) = (22f, 30f, 500f);
            arm.Cost.AddRange(new[] { ("Iron", 14), ("ElderBark", 22) });
            KwSmallIronCannonShape.AddShip(arm, ship);
            Stance(arm, ship.Behind(KwSmallIronCannonShape.ShipBreech));
            return arm;
        }

        private static KwArmDef Small()
        {
            KwShipSpec ship = new KwShipSpec(KwLook.DarkWood, KwLook.BlackMetal)
            {
                Size = 0.35f, Height = 0.68f, Width = 0.1f, Rope = 0.016f, Recoil = 0.8f, Down = -4f, Up = 14f, Slope = 0.9f,
                Bears = Bearing(KwSmallCannonShape.Long.X(-0.43f), 0.158f, 0.12f, KwSmallCannonShape.ShipBreech),
            };
            KwArmDef arm = Ship(KwCannons.Small, "Small ship cannon", KwTiers.Plains, KwCannons.SmallShot(), ship, KwSmallCannonShape.ShipFace + 0.05f);
            arm.Description = "A long hooped black metal breech-loading cannon on a tarred naval truck carriage with black metal fittings, on four solid trucks; a breeching rope takes its recoil. Fired by a pull cord; each shot burns two black powder.";
            (arm.TurnSpeed, arm.TiltSpeed, arm.Health) = (22f, 30f, 700f);
            arm.Cost.AddRange(new[] { ("BlackMetal", 14), ("Tar", 8), ("Wood", 24), ("LinenThread", 4) });
            KwSmallCannonShape.AddShip(arm, ship);
            Stance(arm, ship.Behind(KwSmallCannonShape.ShipBreech));
            return arm;
        }

        private static KwArmDef Large()
        {
            KwShipSpec ship = new KwShipSpec(KwLook.Ashwood, KwLook.Flametal)
            {
                Size = 0.41f, Height = 0.8f, Width = 0.12f, Rope = 0.02f, Recoil = 0.95f, Down = -4f, Up = 14f, Slope = 0.9f,
                Bears = Bearing(KwLargeCannonShape.Long.X(-0.55f), 0.228f, 0.18f, KwLargeCannonShape.ShipBreech),
            };
            KwArmDef arm = Ship(KwCannons.Large, "Large ship cannon", KwTiers.Ashlands, KwCannons.LargeShot(), ship, KwLargeCannonShape.ShipFace + 0.06f);
            arm.Description = "A long, heavy flametal breech-loading cannon fired from a surtling core by a pull cord, on an ashwood naval truck carriage with flametal fittings, on four great trucks; a breeching rope takes its recoil. Each shot burns three black powder.";
            (arm.TurnSpeed, arm.TiltSpeed, arm.Health) = (16f, 22f, 1000f);
            arm.Cost.AddRange(new[] { ("FlametalNew", 18), ("SurtlingCore", 6), ("Blackwood", 26), ("LinenThread", 6) });
            KwLargeCannonShape.AddShip(arm, ship);
            Stance(arm, ship.Behind(KwLargeCannonShape.ShipBreech));
            return arm;
        }

        // The small iron cannon's carriage in a wood and a metal (the bronze cannon's too).
        private static KwShipSpec SmallIronSpec(KwLook wood, KwLook metal) => new KwShipSpec(wood, metal)
        {
            Size = 0.35f, Height = 0.68f, Width = 0.1f, Rope = 0.016f, Recoil = 0.7f, Down = -4f, Up = 14f, Slope = 0.9f,
            Bears = Bearing(KwSmallIronCannonShape.Long.X(-0.385f), 0.165f, 0.13f, KwSmallIronCannonShape.ShipBreech),
        };

        // What the four share: the whole carriage the yaw part, its pivot on the deck under its middle; a narrow arc; the
        // trunnions, muzzle and elevation the carriage was drawn for; the gunner turned a little toward the gun and bending
        // further at its low breech.
        private static KwArmDef Ship(string id, string label, int tier, KwShot shot, KwShipSpec ship, float muzzle) =>
            new KwArmDef(id, label, tier, shot)
            {
                Ship = ship,
                YawPivot = Vector3.zero,
                YawLimit = Arc,
                PitchPivot = ship.Trunnions,
                Muzzle = new Vector3(muzzle, 0f, 0f),
                PitchDown = ship.Down,
                PitchUp = ship.Up,
                Facing = Facing,
                MostLean = MostLean,
                TiltLean = TiltLean,
            };

        // The gunner's stand point behind the breech, and how far he bends over it to aim and to reload (the gun's shape
        // gave its grips a breech-loader's leans, for a breech up at his waist).
        private static void Stance(KwArmDef arm, Vector3 stand)
        {
            arm.Stand = stand;
            if (arm.Hands != null)
                arm.Hands.Lean = AimLean;
            if (arm.ReloadHands != null)
                arm.ReloadHands.Lean = ReloadLean;
        }

        // The breech's underside the quoin bears on: the ring (behind the trunnions, its radius), then the barrel's back
        // edge (the back face's radius), the breech door's foot and the T-handle's lower knob (the quoin drawn out under
        // the low breech at full elevation stays under them all).
        private static Vector2[] Bearing(float x, float radius, float back, KwBreech breech) => new[]
        {
            new Vector2(x, radius), new Vector2(breech.Rear, back), new Vector2(breech.Rear - breech.Thick, breech.Block), new Vector2(breech.Tee.x, Knob),
        };
    }
}
