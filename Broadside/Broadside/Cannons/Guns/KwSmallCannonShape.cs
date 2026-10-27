using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The small ship cannon's gun: a long, slim black metal breech-loader, hooped, on a tarred naval truck carriage with
    /// black metal fittings (<see cref="KwShipCarriage"/>). On the pitch part (from the trunnions, the barrel along +x):
    /// the trunnion axle and the black metal band round the barrel that holds it. Moving (<see cref="KwCannonWork"/>): the
    /// barrel, which slides back through the band when it fires: a flat back face, the breech with a hoop, the reinforce
    /// (drawn long, <see cref="Long"/>) tapering through the band, two hoops down the chase and a flared muzzle round the
    /// dark bore; at the back the breech door and T-handle of its <see cref="Breech"/>.
    /// </summary>
    public static class KwSmallCannonShape
    {
        /// <summary>How far ahead the trunnions sit in their cheeks; the muzzle's face along the barrel (before the stretch).</summary>
        public const float Ahead = 0.1f, Face = 0.86f;

        /// <summary>The breech, its pull handle and loads, and the gunner's grips on it.</summary>
        public static readonly KwBreech Breech = new KwBreech
        {
            Rear = -0.5f, Block = 0.105f, Thick = 0.04f, Bore = 0.07f, Charge = 0.13f, Metal = KwLook.BlackMetal, Recoil = 0.15f,
            Aim = new Vector3(-0.32f, 0.07f, 0.155f), Midway = new Vector3(-0.18f, -0.26f, 0.09f), Pull = new Vector3(-0.25f, -0.38f, 0.18f),
        };

        /// <summary>The ship cannon's long barrel (<see cref="KwStretch"/>): the reinforce drawn out 0.41 m behind the trunnions and the chase 0.3 m between its hoops; its breech, moved back with it; its muzzle's face.</summary>
        public static readonly KwStretch Long = new KwStretch(-0.2f, 0.41f, 0.48f, 0.3f);
        public static readonly KwBreech ShipBreech = Long.Breech(Breech);
        public static float ShipFace => Long.X(Face);

        // The carriage's brackets across (inner and outer faces).
        private const float Inner = 0.17f, Outer = 0.24f;

        // (x, radius) back to front: the flat back face, the breech and its hoop, the reinforce tapering through the band,
        // two hoops down the chase, the flared muzzle.
        private static readonly Vector2[] Barrel =
        {
            new Vector2(-0.5f, 0.12f), new Vector2(-0.49f, 0.135f), new Vector2(-0.45f, 0.142f),
            new Vector2(-0.45f, 0.158f), new Vector2(-0.4f, 0.158f), new Vector2(-0.4f, 0.14f),
            new Vector2(0.3f, 0.12f), new Vector2(0.3f, 0.132f), new Vector2(0.34f, 0.132f), new Vector2(0.34f, 0.116f),
            new Vector2(0.62f, 0.104f), new Vector2(0.62f, 0.118f), new Vector2(0.66f, 0.118f), new Vector2(0.66f, 0.103f),
            new Vector2(0.76f, 0.1f), new Vector2(0.82f, 0.118f), new Vector2(0.85f, 0.132f), new Vector2(Face, 0.124f),
        };

        /// <summary>The ship cannon: the gun with its long barrel (<see cref="Long"/>) on a naval truck carriage whose brackets are its cheeks across (<see cref="KwShipCarriage"/>), and its breeching rings.</summary>
        public static void AddShip(KwArmDef arm, KwShipSpec ship)
        {
            KwShipCarriage.Add(arm, ship, new KwCheeks(Inner, Outer, Ahead, 0.058f, 0.012f));
            Ordnance(arm, Long);
            KwShipCarriage.Breeching(arm, ship);
        }

        // The trunnion band on the pitch part, the barrel that slides through it, and its breech's work; the barrel as long
        // as the stretch draws it.
        private static void Ordnance(KwArmDef arm, KwStretch stretch)
        {
            arm.Part(KwJoint.Pitch, KwLook.BlackMetal, Mount);
            KwArmMover gun = arm.Mover("gun", KwJoint.Pitch, Vector3.zero).Part(KwLook.BlackMetal, b => KwCannonParts.Lathe(b, Vector3.zero, stretch.Outline(Barrel), 10));
            gun.Part(KwLook.Coal, b => KwCannonParts.Bore(b, stretch.X(Face), Breech.Bore));
            KwCannonWork.Add(arm, gun, stretch.Breech(Breech));
        }

        // The trunnion axle and the band round the barrel that holds it, which the barrel slides through as it kicks.
        private static void Mount(MeshBuilder b)
        {
            Solid.Log(b, new Vector3(-0.04f, 0f, 0f), new Vector3(0.04f, 0f, 0f), 0.148f, 12);
            KwCannonParts.Trunnions(b, 0.042f, Outer + 0.012f);
        }
    }
}
