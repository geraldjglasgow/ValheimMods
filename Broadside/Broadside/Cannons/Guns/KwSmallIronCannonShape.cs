using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The small iron ship cannon's gun: a thick cast-iron breech-loader on a naval truck carriage of ancient bark
    /// (<see cref="KwShipCarriage"/>). On the pitch part (from the trunnions, the barrel along +x): the trunnion axle and
    /// the iron band round the barrel that holds it. Moving (<see cref="KwCannonWork"/>): the barrel, which slides back
    /// through the band when it fires: a flat back face, the breech with a cast base ring, a vent on top, the reinforce
    /// (drawn long, <see cref="Long"/>), a straight run through the band, a chase tapering to a small ring and a swelled
    /// muzzle round the dark bore; at the back the breech door and T-handle of its <see cref="Breech"/>. The same gun is
    /// cast in its carriage's metal (<see cref="KwShipSpec.Metal"/>): the bronze ship cannon's, on fine wood, loading
    /// stone shot.
    /// </summary>
    public static class KwSmallIronCannonShape
    {
        /// <summary>How far ahead the trunnions sit in their cheeks; the muzzle's face along the barrel (before the stretch).</summary>
        public const float Ahead = 0.06f, Face = 0.69f;

        /// <summary>The breech, its pull handle and loads, and the gunner's grips on it.</summary>
        public static readonly KwBreech Breech = new KwBreech
        {
            Rear = -0.46f, Block = 0.11f, Thick = 0.04f, Bore = 0.07f, Charge = 0.13f, Metal = KwLook.Iron, Recoil = 0.14f,
            Aim = new Vector3(-0.28f, 0.07f, 0.17f), Midway = new Vector3(-0.18f, -0.26f, 0.09f), Pull = new Vector3(-0.25f, -0.38f, 0.18f),
        };

        /// <summary>The ship cannon's long barrel (<see cref="KwStretch"/>): the reinforce drawn out 0.45 m behind the trunnions and the chase 0.4 m ahead of them; its breech, moved back with it; its muzzle's face.</summary>
        public static readonly KwStretch Long = new KwStretch(-0.2f, 0.45f, 0.4f, 0.4f);
        public static readonly KwBreech ShipBreech = Long.Breech(Breech);
        public static float ShipFace => Long.X(Face);

        // The carriage's brackets across (inner and outer faces).
        private const float Inner = 0.17f, Outer = 0.24f;

        // (x, radius) back to front: the flat back face, the breech, base ring, reinforce, the straight run the band
        // holds, the chase, a small ring, the swelled muzzle.
        private static readonly Vector2[] Barrel =
        {
            new Vector2(-0.46f, 0.13f), new Vector2(-0.44f, 0.146f), new Vector2(-0.41f, 0.152f),
            new Vector2(-0.41f, 0.165f), new Vector2(-0.36f, 0.165f), new Vector2(-0.36f, 0.15f),
            new Vector2(0f, 0.14f), new Vector2(0f, 0.128f), new Vector2(0.22f, 0.128f),
            new Vector2(0.54f, 0.104f), new Vector2(0.54f, 0.114f), new Vector2(0.57f, 0.114f), new Vector2(0.58f, 0.106f),
            new Vector2(0.62f, 0.112f), new Vector2(0.67f, 0.126f), new Vector2(Face, 0.118f),
        };

        /// <summary>
        /// The ship cannon: the gun with its long barrel (<see cref="Long"/>), cast in the carriage's metal, on a naval truck
        /// carriage whose brackets are its cheeks across (<see cref="KwShipCarriage"/>), and its breeching rings; its breech
        /// loading stone shot or iron balls.
        /// </summary>
        public static void AddShip(KwArmDef arm, KwShipSpec ship, bool stone = false)
        {
            KwShipCarriage.Add(arm, ship, new KwCheeks(Inner, Outer, Ahead, 0.056f, 0.012f));
            Ordnance(arm, Long, ship.Metal, Long.Breech(Breech).Cast(ship.Metal, stone));
            KwShipCarriage.Breeching(arm, ship);
        }

        // The trunnion band on the pitch part, the barrel (and its vent) that slides through it, and its breech's work, all
        // in the metal; the barrel as long as the stretch draws it.
        private static void Ordnance(KwArmDef arm, KwStretch stretch, KwLook metal, KwBreech breech)
        {
            arm.Part(KwJoint.Pitch, metal, Mount);
            KwArmMover gun = arm.Mover("gun", KwJoint.Pitch, Vector3.zero).Part(metal, b => Gun(b, stretch));
            gun.Part(KwLook.Coal, b => KwCannonParts.Bore(b, stretch.X(Face), Breech.Bore));
            KwCannonWork.Add(arm, gun, breech);
        }

        // The trunnion axle and the band round the barrel that holds it, which the barrel slides through as it kicks.
        private static void Mount(MeshBuilder b)
        {
            Solid.Log(b, new Vector3(-0.03f, 0f, 0f), new Vector3(0.03f, 0f, 0f), 0.152f, 12);
            KwCannonParts.Trunnions(b, 0.04f, Outer + 0.01f);
        }

        // The barrel and the vent on top.
        private static void Gun(MeshBuilder b, KwStretch stretch)
        {
            KwCannonParts.Lathe(b, Vector3.zero, stretch.Outline(Barrel), 10);
            Solid.Box(b, stretch.At(new Vector3(-0.33f, -0.014f, 0.142f)), stretch.At(new Vector3(-0.3f, 0.014f, 0.162f)));
        }
    }
}
