using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon hauled about a deck as a cart (<see cref="KwDeckMath"/>), in its ship's frame: where its middle stands
    /// (x and z; y unused) and the level way its muzzle points (<see cref="Ahead"/>). <see cref="KwCartMath"/> moves it.
    /// </summary>
    public struct KwCart
    {
        public Vector3 Axle;
        public Vector3 Ahead;
    }

    /// <summary>
    /// A cart's motion in plain arithmetic (no engine calls, so it is checked offline). Pulled or pushed by a hitch
    /// (<see cref="Follow"/>): it never slides sideways; its middle rolls only along its way (the way halfway through the
    /// step), to the hitch's reach, so a pull from the side swings it round about its middle and a straight pull rolls it
    /// straight. Pushed, a cart backs like a trailer and would swing off its line; a man pushing one by its handles swings
    /// his end the other way to steer it, and so does the hauler (<see cref="Pushed"/>).
    /// </summary>
    public static class KwCartMath
    {
        // How far a pushing hauler's walk turns for the cart's error (twice it, the other way: the cart then backs onto
        // his way as a pulled one follows), at most; and from what share of his walk toward the muzzle he pushes, fully.
        private const float Steer = 2f, Most = 60f * Mathf.Deg2Rad, Pushing = 0.2f, Pushes = 0.5f;

        /// <summary>The level way to the left of a level way.</summary>
        public static Vector3 Side(Vector3 ahead) => new Vector3(-ahead.z, 0f, ahead.x);

        /// <summary>
        /// The cart drawn after its hitch, to its reach behind the axle's middle: the middle rolls along the way halfway
        /// between the cart's way before and the line to the hitch now, never sideways; its way then is to the hitch.
        /// </summary>
        public static void Follow(ref KwCart cart, Vector3 hitch, float reach)
        {
            Vector3 axle = new Vector3(cart.Axle.x, 0f, cart.Axle.z), to = new Vector3(hitch.x, 0f, hitch.z) - axle;
            if (to.sqrMagnitude < 1e-8f)
                return;
            Vector3 halfway = to.normalized - cart.Ahead, roll = halfway.sqrMagnitude > 1e-4f ? halfway.normalized : to.normalized;
            float along = Vector3.Dot(to, roll), room = along * along - to.sqrMagnitude + reach * reach;
            Vector3 moved = room >= 0f ? axle + roll * (along - Mathf.Sqrt(room)) : axle + to - to.normalized * reach;
            Vector3 way = new Vector3(hitch.x - moved.x, 0f, hitch.z - moved.z);
            cart.Axle = new Vector3(moved.x, 0f, moved.z);
            cart.Ahead = way.sqrMagnitude > 1e-8f ? -way.normalized : cart.Ahead;
        }

        /// <summary>
        /// The puller's walk (his game's move direction, its length his speed) while he pushes the cart (walks toward its
        /// muzzle): turned the other way by twice the angle between the way he means to walk and the cart's way, at most
        /// 60 degrees, so his end swings out and the cart backs round onto his way, as a pulled one would follow him; less
        /// and less as he walks more across it than toward it; unchanged while he pulls.
        /// </summary>
        public static Vector3 Pushed(Vector3 walk, Vector3 ahead)
        {
            Vector3 level = new Vector3(walk.x, 0f, walk.z);
            float speed = level.magnitude;
            if (speed < 1e-3f)
                return walk;
            Vector3 way = level / speed;
            float toward = Vector3.Dot(way, ahead);
            if (toward <= Pushing)
                return walk;
            float off = Mathf.Atan2(Vector3.Dot(way, Side(ahead)), toward);
            float turn = Mathf.Clamp(-Steer * off, -Most, Most) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Pushing, Pushes, toward));
            Vector3 turned = way * Mathf.Cos(turn) + Side(way) * Mathf.Sin(turn);
            return new Vector3(turned.x * speed, walk.y, turned.z * speed);
        }

        /// <summary>A rotation from its axes (right, up, forward: orthonormal and right-handed as Unity's are), in plain arithmetic.</summary>
        public static Quaternion Rotation(Vector3 r, Vector3 u, Vector3 f)
        {
            float trace = r.x + u.y + f.z;
            if (trace > 0f)
            {
                float s = Mathf.Sqrt(trace + 1f) * 2f;
                return new Quaternion((u.z - f.y) / s, (f.x - r.z) / s, (r.y - u.x) / s, s / 4f);
            }
            if (r.x > u.y && r.x > f.z)
            {
                float s = Mathf.Sqrt(1f + r.x - u.y - f.z) * 2f;
                return new Quaternion(s / 4f, (u.x + r.y) / s, (f.x + r.z) / s, (u.z - f.y) / s);
            }
            if (u.y > f.z)
            {
                float s = Mathf.Sqrt(1f + u.y - r.x - f.z) * 2f;
                return new Quaternion((u.x + r.y) / s, s / 4f, (f.y + u.z) / s, (f.x - r.z) / s);
            }
            float t = Mathf.Sqrt(1f + f.z - r.x - u.y) * 2f;
            return new Quaternion((f.x + r.z) / t, (f.y + u.z) / t, t / 4f, (r.y - u.x) / t);
        }
    }
}
