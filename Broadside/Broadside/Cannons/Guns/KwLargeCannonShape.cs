using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The large ship cannon's gun: a heavy flametal breech-loader fired from a surtling core, on an ashwood naval truck
    /// carriage with flametal fittings (<see cref="KwShipCarriage"/>). On the pitch part (from the trunnions, the barrel
    /// along +x): the thick trunnion axle and the flametal band round the barrel that holds it. Moving
    /// (<see cref="KwCannonWork"/>): the barrel, which slides back through the band when it fires: a flat back face, a
    /// broad breech chamber in three flametal hoops with the ember glow of its fire showing between them, a glowing core
    /// held on top of the chamber in a cage of four flametal bars, the reinforce (drawn long, <see cref="Long"/>), a
    /// straight run through the band to a ring, a long chase and a heavy swelled muzzle round the dark bore; at the back
    /// the breech door and T-handle of its <see cref="Breech"/>.
    /// </summary>
    public static class KwLargeCannonShape
    {
        /// <summary>How far ahead the trunnions sit in their cheeks; the muzzle's face along the barrel (before the stretch).</summary>
        public const float Ahead = 0.28f, Face = 0.94f;

        /// <summary>The breech, its pull handle and loads, and the gunner's grips on it.</summary>
        public static readonly KwBreech Breech = new KwBreech
        {
            Rear = -0.62f, Block = 0.155f, Thick = 0.05f, Bore = 0.105f, Charge = 0.17f, Metal = KwLook.Flametal, Flametal = true, Recoil = 0.2f,
            Aim = new Vector3(-0.55f, 0.08f, 0.25f), Midway = new Vector3(-0.23f, -0.31f, 0.09f), Pull = new Vector3(-0.3f, -0.4f, 0.18f), ReloadLean = 42f,
        };

        /// <summary>The ship cannon's long barrel (<see cref="KwStretch"/>): the reinforce drawn out 0.48 m behind the trunnions (the chamber, core and glow moved back with the breech) and the chase 0.34 m ahead of them; its breech, moved back with it; its muzzle's face.</summary>
        public static readonly KwStretch Long = new KwStretch(-0.12f, 0.48f, 0.56f, 0.34f);
        public static readonly KwBreech ShipBreech = Long.Breech(Breech);
        public static float ShipFace => Long.X(Face);

        // The carriage's brackets across (inner and outer faces); the core's place along the chamber.
        private const float Inner = 0.24f, Outer = 0.33f, Core = -0.41f;

        // (x, radius) back to front: the flat back face, the breech chamber in three hoops, the reinforce, the straight
        // run the band holds, a ring, the chase, the heavy muzzle.
        private static readonly Vector2[] Barrel =
        {
            new Vector2(-0.62f, 0.18f), new Vector2(-0.6f, 0.19f), new Vector2(-0.58f, 0.205f),
            new Vector2(-0.58f, 0.228f), new Vector2(-0.52f, 0.228f), new Vector2(-0.52f, 0.2f),
            new Vector2(-0.44f, 0.2f), new Vector2(-0.44f, 0.228f), new Vector2(-0.38f, 0.228f), new Vector2(-0.38f, 0.2f),
            new Vector2(-0.3f, 0.2f), new Vector2(-0.3f, 0.228f), new Vector2(-0.24f, 0.228f), new Vector2(-0.24f, 0.205f),
            new Vector2(0f, 0.196f), new Vector2(0.28f, 0.19f), new Vector2(0.28f, 0.2f), new Vector2(0.32f, 0.2f), new Vector2(0.32f, 0.168f),
            new Vector2(0.8f, 0.14f), new Vector2(0.84f, 0.15f), new Vector2(0.9f, 0.168f), new Vector2(Face, 0.158f),
        };

        /// <summary>The ship cannon: the gun with its long barrel (<see cref="Long"/>) on a naval truck carriage whose brackets are its cheeks across (<see cref="KwShipCarriage"/>), and its breeching rings.</summary>
        public static void AddShip(KwArmDef arm, KwShipSpec ship)
        {
            KwShipCarriage.Add(arm, ship, new KwCheeks(Inner, Outer, Ahead, 0.085f, 0.018f));
            Ordnance(arm, Long);
            KwShipCarriage.Breeching(arm, ship);
        }

        // The trunnion band on the pitch part, the barrel with its core and glow that slides through it, and its breech's
        // work; the barrel as long as the stretch draws it.
        private static void Ordnance(KwArmDef arm, KwStretch stretch)
        {
            arm.Part(KwJoint.Pitch, KwLook.Flametal, Mount);
            KwArmMover gun = arm.Mover("gun", KwJoint.Pitch, Vector3.zero).Part(KwLook.Flametal, b => Gun(b, stretch)).Part(KwLook.Ember, b => Glow(b, stretch));
            gun.Part(KwLook.Coal, b => KwCannonParts.Bore(b, stretch.X(Face), Breech.Bore));
            KwCannonWork.Add(arm, gun, stretch.Breech(Breech));
        }

        // The thick trunnion axle and the band round the barrel that holds it, which the barrel slides through as it kicks.
        private static void Mount(MeshBuilder b)
        {
            Solid.Log(b, new Vector3(-0.035f, 0f, 0f), new Vector3(0.035f, 0f, 0f), 0.21f, 14);
            KwCannonParts.Trunnions(b, 0.065f, Outer + 0.012f);
        }

        // The barrel, and the cage on top of the chamber's middle hoop: four bars out round the core and in to a knob.
        private static void Gun(MeshBuilder b, KwStretch stretch)
        {
            KwCannonParts.Lathe(b, Vector3.zero, stretch.Outline(Barrel), 12);
            float core = stretch.X(Core);
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) * Mathf.PI / 2f;
                Vector3 way = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 root = new Vector3(core, 0f, 0.2f) + way * 0.045f, bulge = new Vector3(core, 0f, 0.29f) + way * 0.065f;
                Solid.Log(b, root, bulge, 0.012f, 4);
                Solid.Log(b, bulge, new Vector3(core, 0f, 0.35f), 0.012f, 4);
            }
            KwCannonParts.Ball(b, new Vector3(core, 0f, 0.356f), 0.02f, 3, 6);
        }

        // The fire's glow between the chamber's hoops, and the core in its cage.
        private static void Glow(MeshBuilder b, KwStretch stretch)
        {
            Solid.Log(b, stretch.At(new Vector3(-0.52f, 0f, 0f)), stretch.At(new Vector3(-0.44f, 0f, 0f)), 0.206f, 12);
            Solid.Log(b, stretch.At(new Vector3(-0.38f, 0f, 0f)), stretch.At(new Vector3(-0.3f, 0f, 0f)), 0.206f, 12);
            KwCannonParts.Ball(b, new Vector3(stretch.X(Core), 0f, 0.29f), 0.05f, 5, 8);
        }
    }
}
