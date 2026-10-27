using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A breech-loading cannon's breech, pull handle and loads (<see cref="KwCannonWork"/> and <see cref="KwCannonFiring"/>
    /// animate them), in its pitch part's kit axes (from the trunnions, the barrel along +x): the barrel's flat back face
    /// with the bore's dark mouth in it, a round breech door over it, hung on an upright hinge down its right side and
    /// opened sideways by an upright handle on its left, as an oven door is; a small metal T-handle sitting in the boss at
    /// the door's middle, its upright crossbar for the gunner's fist and its stem on a cord that comes out of the door
    /// there; and where the loads lie, the powder bag at the back of the bore and the ball ahead of it.
    /// </summary>
    public sealed class KwBreech
    {
        // The boss's thickness; the T-handle's crossbar (half its length, its radius, its swelled ends) and how far it
        // stands off the boss at rest, room for the fingers; its stem's length (into the boss) and radius.
        private const float BossThick = 0.012f, TeeHalf = 0.05f, TeeRadius = 0.011f, Knob = 0.015f, Standoff = 0.03f;
        private const float Stem = 0.04f, StemRadius = 0.007f;

        /// <summary>The barrel's back face (x); the door's radius and its thickness; the bore's radius; the powder bag's length.</summary>
        public float Rear { get; set; }
        public float Block { get; set; }
        public float Thick { get; set; }
        public float Bore { get; set; }
        public float Charge { get; set; }

        /// <summary>The door's and the T-handle's look; whether it fires flametal balls or stone shot (else iron balls).</summary>
        public KwLook Metal { get; set; }
        public bool Flametal { get; set; }
        public bool Stone { get; set; }

        /// <summary>How far the barrel kicks back through its trunnion band at a shot, metres.</summary>
        public float Recoil { get; set; }

        /// <summary>The gunner's left fist while he aims (on the breech), and how far he bends in to aim and to load.</summary>
        public Vector3 Aim { get; set; }
        public float AimLean { get; set; } = 45f;
        public float ReloadLean { get; set; } = 50f;

        /// <summary>Where his right fist has drawn the T-handle halfway through the pull (the shot leaves then) and at the pull's end, from where it sits.</summary>
        public Vector3 Midway { get; set; }
        public Vector3 Pull { get; set; }

        public float BallRadius => Flametal ? KwCannonballs.FlametalRadius : Stone ? KwCannonballs.StoneRadius : KwCannonballs.IronRadius;

        /// <summary>The door's upright hinge, at its right edge.</summary>
        public Vector3 Hinge => new Vector3(Rear - Thick / 2f - 0.002f, -Block - 0.005f, 0f);

        /// <summary>The middle of the door's upright handle, near its left edge.</summary>
        public Vector3 Handle => new Vector3(Rear - Thick - 0.03f, Block * 0.68f, 0f);

        /// <summary>Where the cord comes out of the door: the middle of the boss's back face.</summary>
        public Vector3 CordExit => new Vector3(Rear - Thick - BossThick, 0f, 0f);

        /// <summary>The middle of the T-handle's crossbar as it sits in the boss, where the gunner's fist closes round it; the end of its stem, where the cord is tied.</summary>
        public Vector3 Tee => CordExit - new Vector3(Standoff, 0f, 0f);
        public Vector3 StemEnd => Tee + new Vector3(Stem, 0f, 0f);

        /// <summary>The loaded powder bag's back end, and the loaded ball's middle ahead of it.</summary>
        public Vector3 BagFrom => new Vector3(Rear + 0.012f, 0f, 0f);
        public Vector3 BallCentre => new Vector3(Rear + 0.017f + Charge + BallRadius, 0f, 0f);

        /// <summary>The same breech on a barrel drawn longer (<see cref="KwStretch"/>): its back face and the aiming fist on it moved along the barrel by so much (back is negative).</summary>
        public KwBreech Moved(float by) => new KwBreech
        {
            Rear = Rear + by, Block = Block, Thick = Thick, Bore = Bore, Charge = Charge, Metal = Metal, Flametal = Flametal, Stone = Stone, Recoil = Recoil,
            Aim = Aim + new Vector3(by, 0f, 0f), AimLean = AimLean, ReloadLean = ReloadLean, Midway = Midway, Pull = Pull,
        };

        /// <summary>The same breech cast in another metal, loading stone shot or not (the bronze ship cannon's, from the small iron cannon's).</summary>
        public KwBreech Cast(KwLook metal, bool stone)
        {
            KwBreech copy = Moved(0f);
            (copy.Metal, copy.Stone) = (metal, stone);
            return copy;
        }

        /// <summary>The breech's fixed parts, on the barrel: the hinge's upright knuckle at the door's right edge and the lug holding it to the barrel's side.</summary>
        public void Fixed(MeshBuilder b)
        {
            Vector3 h = Hinge;
            float r = Block * 0.13f, tall = Block * 0.55f;
            Solid.Log(b, new Vector3(h.x, h.y, -tall), new Vector3(h.x, h.y, tall), r, 8);
            Solid.Box(b, new Vector3(Rear - 0.012f, h.y - r, -tall * 0.8f), new Vector3(Rear + 0.04f, h.y + 0.02f, tall * 0.8f));
        }

        /// <summary>The bore's dark mouth in the back face, standing just proud of it (the door shuts over it).</summary>
        public void Mouth(MeshBuilder b) => Solid.Log(b, new Vector3(Rear - 0.003f, 0f, 0f), new Vector3(Rear + 0.03f, 0f, 0f), Bore, 10);

        /// <summary>The round door over the back face with a boss in its middle, two hinge straps across it from the hinge, and its upright handle on two stand-offs.</summary>
        public void Door(MeshBuilder b)
        {
            float back = Rear - Thick;
            Solid.Log(b, new Vector3(back, 0f, 0f), new Vector3(Rear - 0.004f, 0f, 0f), Block, 16);
            Solid.Log(b, new Vector3(back - BossThick, 0f, 0f), new Vector3(back, 0f, 0f), Block * 0.42f, 12);
            foreach (float z in new[] { -Block * 0.45f, Block * 0.45f })
                Solid.Box(b, new Vector3(back - 0.007f, Hinge.y, z - 0.018f), new Vector3(back, Block * 0.2f, z + 0.018f));
            Vector3 h = Handle;
            Solid.Log(b, h + new Vector3(0f, 0f, -0.07f), h + new Vector3(0f, 0f, 0.07f), 0.012f, 8);
            foreach (float z in new[] { -0.055f, 0.055f })
                Solid.Log(b, new Vector3(back, h.y, h.z + z), new Vector3(h.x, h.y, h.z + z), 0.009f, 6);
        }

        /// <summary>The dark hole in the boss's middle the cord comes out of (the T-handle's stem sits in it).</summary>
        public void Socket(MeshBuilder b) => Solid.Log(b, CordExit - new Vector3(0.002f, 0f, 0f), CordExit + new Vector3(0.004f, 0f, 0f), 0.013f, 8);

        /// <summary>The T-handle: an upright crossbar with swelled ends, and its stem forward from the crossbar's middle.</summary>
        public void TeeHandle(MeshBuilder b)
        {
            Vector3 t = Tee, half = new Vector3(0f, 0f, TeeHalf);
            Solid.Log(b, t - half, t + half, TeeRadius, 8);
            foreach (float side in new[] { 1f, -1f })
                Solid.Log(b, t + half * side * 0.72f, t + half * side * 1.08f, Knob, 8);
            Solid.Log(b, t, StemEnd, StemRadius, 6);
        }
    }

    /// <summary>
    /// The cannons' motion (<c>Arms/ANIMATION.md</c>), the same on all four, from each one's <see cref="KwBreech"/>; the
    /// barrel is the moving part "gun", drawn by the cannon's shape and sliding through its trunnion band. Aiming, the
    /// gunner bends over the breech, his left fist on it and his right on the T-handle in the door. The shot is
    /// <see cref="KwCannonFiring"/>'s: he draws the handle out on its cord, the shot leaves halfway, the barrel kicks
    /// back, he lets go and the handle is reeled home. The reload: the barrel runs out again, heavily; he pulls the round
    /// breech door open sideways on its hinge by its handle, a ball is laid in at the breech and goes in, then a bag of
    /// powder, which he pushes home, and he swings the door shut. The loads are gone from the shot until they are laid in.
    /// </summary>
    public static class KwCannonWork
    {
        /// <summary>How far the door swings open about its upright hinge, degrees (its handle side comes back and round to the right).</summary>
        private const float Open = 105f;

        // The reload, as fractions of it: the run out, the door opened, the fists at the breech, the ball laid in, the
        // powder laid in and pushed home, the fists back on the door's handle, the door shut.
        private const float RunOut = 0.03f, RanOut = 0.15f, Opening = 0.18f, Opened = 0.32f, Reached = 0.37f;
        private const float BallShown = 0.38f, BallMouth = 0.45f, BallIn = 0.54f, BagShown = 0.56f, Pushed = 0.62f, BagIn = 0.68f;
        private const float Back = 0.72f, Shutting = 0.74f, Shut = 0.86f;

        /// <summary>Gives a cannon whose barrel is the moving part <paramref name="gun"/> its breech, pull handle, loads, motion and grips.</summary>
        public static void Add(KwArmDef arm, KwArmMover gun, KwBreech breech)
        {
            gun.Part(breech.Metal, breech.Fixed).Part(KwLook.Coal, breech.Mouth);
            KwArmMover block = arm.Mover("block", KwJoint.Pitch, breech.Hinge, "gun").Part(breech.Metal, breech.Door).Part(KwLook.Coal, breech.Socket);
            KwCannonFiring.Add(arm, gun, breech);
            Reload(gun, block, breech.Recoil);
            Bag(arm, breech);
            Ball(arm, breech);
            Hands(arm, breech);
        }

        // The barrel, held back (run in) after the shot until the reload runs it out again, slow to start and slow to stop;
        // the breech door, shut through the shot, pulled open sideways on its upright hinge, held open while the loads go
        // in, and swung shut.
        private static void Reload(KwArmMover gun, KwArmMover block, float back)
        {
            Vector3 run = new Vector3(-back, 0f, 0f), open = new Vector3(0f, 0f, Open);
            gun.Reload.Key(0f, run).Key(RunOut, run).Key(RanOut);
            block.Reload.Key(Opening).Key(Opened, turn: open).Key(Shutting, turn: open).Key(Shut);
        }

        // The powder in its linen bag: gone at the shot; laid behind the open breech after the ball, and pushed home.
        private static void Bag(KwArmDef arm, KwBreech breech)
        {
            Vector3 from = breech.BagFrom, behind = new Vector3(-(0.042f + breech.Charge), -0.03f, 0.02f);
            float radius = breech.Bore - 0.012f, length = breech.Charge;
            KwArmMover bag = arm.Mover("powder", KwJoint.Pitch, from, "gun");
            bag.Part(KwLook.Linen, b => KwBlackPowder.Cartridge(b, from, length, radius)).Part(KwLook.Resin, b => KwBlackPowder.Tie(b, from, radius));
            bag.Fire.Key(0f, shown: false);
            bag.Reload.Key(0f, behind, shown: false).Key(BagShown, behind).Key(BagIn);
        }

        // The ball: gone at the shot (the flying one leaves the muzzle); shown behind the open breech a little high and
        // to the right, brought to its mouth and sent in. A stone ball is drawn in the stone's stand-in look and given the
        // stone by its cannon's last touch (KwStone).
        private static void Ball(KwArmDef arm, KwBreech breech)
        {
            Vector3 at = breech.BallCentre;
            float r = breech.BallRadius;
            KwArmMover ball = arm.Mover("ball", KwJoint.Pitch, at, "gun");
            if (breech.Flametal)
                ball.Part(KwLook.Flametal, b => KwCannonballs.FlametalBall(b, at)).Part(KwLook.Ember, b => KwCannonballs.Seam(b, at));
            else if (breech.Stone)
                ball.Part(KwStone.Stand, b => KwCannonballs.StoneBall(b, at));
            else
                ball.Part(KwLook.Iron, b => KwCannonballs.IronBall(b, at));
            Vector3 behind = new Vector3(breech.Rear - 0.075f - r - at.x, -0.03f, 0.03f), mouth = new Vector3(breech.Rear - r - 0.004f - at.x, 0f, 0f);
            ball.Fire.Key(0f, shown: false);
            ball.Reload.Key(0f, behind, shown: false).Key(BallShown, behind).Key(BallMouth, mouth).Key(BallIn);
        }

        // The gunner's fists. Aiming: the left on the breech, the right on the T-handle (its fist part, which goes with the
        // handle through the pull and stays where he lets go). Reloading: both on the door's handle, carried by a moving
        // part of no mesh that turns on the door's hinge as the door does, swings the fists to the open breech while the
        // loads go in (pushing the bag home), and back to the handle to shut it.
        private static void Hands(KwArmDef arm, KwBreech breech)
        {
            Vector3 handle = breech.Handle, open = new Vector3(0f, 0f, Open);
            Vector3 load = new Vector3(breech.Rear - 0.05f, 0f, breech.Bore + 0.04f) - handle, push = load + new Vector3(0.04f, 0f, -0.01f);
            KwArmMover hands = arm.Mover("hands", KwJoint.Pitch, breech.Hinge, "gun");
            hands.Reload.Key(Opening).Key(Opened, turn: open).Key(Reached, load).Key(BagShown, load).Key(Pushed, push).Key(BagIn, load)
                .Key(Back, turn: open).Key(Shutting, turn: open).Key(Shut);
            KwArmPoint fist = new KwArmPoint(KwJoint.Pitch, breech.Tee, KwCannonFiring.Fist);
            arm.Hands = new KwArmGrip(new KwArmPoint(KwJoint.Pitch, breech.Aim, "gun"), fist) { Lean = breech.AimLean };
            arm.ReloadHands = new KwArmGrip(Fist(handle, 1f), Fist(handle, -1f)) { Lean = breech.ReloadLean };
        }

        // A fist on the door's upright handle, carried by the hands' moving part: above (+1) or below (-1) its middle.
        private static KwArmPoint Fist(Vector3 handle, float side) => new KwArmPoint(KwJoint.Pitch, handle + new Vector3(0f, 0f, side * 0.035f), "hands");
    }
}
