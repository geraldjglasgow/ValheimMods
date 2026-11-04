using UnityEngine;

namespace Broadside
{
    /// <summary>A crate's place on a cart's bed: its cells (<see cref="KwCartShape"/>) and its turn in the cart's frame (degrees).</summary>
    public readonly struct KwCartPlace
    {
        public readonly int Mask;
        public readonly float Yaw;

        public KwCartPlace(int mask, float yaw) => (Mask, Yaw) = (mask, yaw);
    }

    /// <summary>Whether a crate goes onto a cart: it fits a free place, the places it fits are taken, it fits none, or the cart has no id yet.</summary>
    public enum KwCartFit
    {
        Fits,
        Full,
        TooBig,
        NotReady,
    }

    /// <summary>
    /// Loading a crate onto a cart, on the crate's owner (the operator's game only asks, for its refusal). A crate let go over a
    /// cart takes the smallest shape its footprint fits (<see cref="KwCartShape.Shapes"/>), turned square to the cart, the one
    /// of the two square turns nearest how it hung, at the free place of that shape nearest where it hung; its world pose and
    /// its link to the cart (<see cref="KwCargoCart.CartHash"/>, <see cref="KwCargoCart.SlotHash"/>, <see cref="KwCargoCart.YawHash"/>) are
    /// written in one ZDO write and it rides the cart at once (<see cref="KwCartRider"/>). The operator's Block over a cart
    /// with no room is refused with a message (<see cref="Refusal"/>); a crate let go there any other way (the operator
    /// leaving, the crane gone) goes down beside the cart instead, or as a last resort straight under it, the cart pushed aside.
    /// </summary>
    public static class KwCartLoad
    {
        /// <summary>The place a crate takes on this cart, nearest where it is now, or why none.</summary>
        public static KwCartFit Choose(KwCrate crate, KwCartBed bed, out KwCartPlace place)
        {
            place = default;
            if (bed.Id == 0L || KwCartBed.Gone(bed.Id))
                return KwCartFit.NotReady;
            Vector2 half = new Vector2(crate.Half.x, crate.Half.z);
            foreach ((int across, int along, int[] masks) in KwCartShape.Shapes)
            {
                float turn = bed.Shape.Fit(across, along, half);
                if (turn >= 0f)
                    return Nearest(crate, bed, masks, turn, out place) ? KwCartFit.Fits : KwCartFit.Full;
            }
            return KwCartFit.TooBig;
        }

        private static bool Nearest(KwCrate crate, KwCartBed bed, int[] masks, float turn, out KwCartPlace place)
        {
            place = default;
            int used = bed.Used(crate.gameObject);
            Vector3 here = bed.transform.InverseTransformPoint(crate.transform.TransformPoint(crate.BoxCentre));
            float best = float.MaxValue;
            foreach (int mask in masks)
            {
                Vector3 middle = bed.Shape.Middle(mask);
                float far = new Vector2(middle.x - here.x, middle.z - here.z).sqrMagnitude;
                if ((mask & used) == 0 && far < best)
                    (best, place) = (far, new KwCartPlace(mask, Facing(crate, bed, turn)));
            }
            return best < float.MaxValue;
        }

        // Square to the cart: of the two turns its shape allows, the one nearest how it hangs now.
        private static float Facing(KwCrate crate, KwCartBed bed, float turn)
        {
            float now = Mathf.DeltaAngle(bed.transform.eulerAngles.y, crate.transform.eulerAngles.y);
            return Mathf.Abs(Mathf.DeltaAngle(now, turn)) <= 90f ? turn : turn + 180f;
        }

        /// <summary>The crate's pose in the cart's frame at its place: its box's bottom in the middle of its cells, on the floor.</summary>
        public static (Vector3 At, Quaternion Turn) Pose(KwCartShape shape, KwCartPlace place, Vector3 boxCentre, Vector3 half)
        {
            Quaternion turn = Quaternion.Euler(0f, place.Yaw, 0f);
            Vector3 bottom = new Vector3(boxCentre.x, boxCentre.y - half.y, boxCentre.z);
            return (shape.Middle(place.Mask) - turn * bottom, turn);
        }

        /// <summary>The crate's pose in the world at its place on this cart as the cart is drawn now.</summary>
        public static (Vector3 At, Quaternion Turn) World(KwCrate crate, KwCartBed bed, KwCartPlace place)
        {
            (Vector3 local, Quaternion turn) = Pose(bed.Shape, place, crate.BoxCentre, crate.Half);
            return (bed.transform.TransformPoint(local), bed.transform.rotation * turn);
        }

        /// <summary>On the owner: the crate onto the cart at its place, its world pose and its link written together; it rides here at once.</summary>
        public static void Board(KwCrate crate, KwCartBed bed, KwCartPlace place)
        {
            (Vector3 at, Quaternion turn) = World(crate, bed, place);
            ZDO zdo = crate.View!.GetZDO();
            zdo.SetPosition(at);
            zdo.SetRotation(turn);
            zdo.Set(KwCargoCart.SlotHash, place.Mask);
            zdo.Set(KwCargoCart.YawHash, place.Yaw);
            zdo.Set(KwCargoCart.CartHash, bed.Id);
            KwCartRider.Agree(crate.gameObject, zdo);
        }

        /// <summary>Why the operator's Block is refused, or null: the crate hangs over a cart that has no id yet, no room, or is too small for it.</summary>
        public static string? Refusal(KwCrate? crate)
        {
            KwCartBed? bed = crate != null ? KwCrateRest.Below(crate).Cart : null;
            if (bed == null)
                return null;
            return Choose(crate!, bed, out _) switch
            {
                KwCartFit.Full => KwCartText.Full,
                KwCartFit.TooBig => KwCartText.TooBig,
                KwCartFit.NotReady => KwCartText.NotReady,
                _ => null,
            };
        }

        /// <summary>
        /// Where a crate let go over a cart comes to rest: its place on the bed; beside the cart when it has none; straight
        /// down past the cart as a last resort (on the ground under it, the cart pushed aside).
        /// </summary>
        public static KwCrateTarget Onto(KwCrate crate, KwCartBed bed)
        {
            if (Choose(crate, bed, out KwCartPlace place) == KwCartFit.Fits)
            {
                (Vector3 at, Quaternion turn) = World(crate, bed, place);
                return new KwCrateTarget(at, turn, false, null, bed, place);
            }
            return KwCartUnload.Spot(crate, bed, crate.transform.position) ?? KwCrateRest.Plain(crate, bed.transform);
        }
    }
}
