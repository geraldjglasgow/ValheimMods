using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ladder in the world, used as the game's own ship ladders are: E within reach of it puts the player at its other
    /// end at once (a hatch's ladder and an open edge's alike). From below, on the deck beside the hatch or behind the edge,
    /// facing away from the opening (a hatch opens first if it is
    /// shut), whichever side of the ladder the player stood and whichever way the ladder faces: the first spot round its
    /// top with deck under it (<see cref="Top"/>); from the deck, at the ladder's foot, facing into the room below, standing on the first thing under the
    /// opening there (a hold's floor laid since the ladder was made stands over its foot, <see cref="KwSole"/>). Local to
    /// the climbing player; the game sends the player's position as it always does.
    /// </summary>
    public sealed class KwLadderClimb : MonoBehaviour, Interactable, Hoverable
    {
        // In front of the ladder at its foot; behind its side on the deck; how near the ladder a player must be; how far under
        // the deck's top the way down is looked along (clear of the deck and the hatch's coaming).
        private const float Out = 0.45f, Behind = 0.55f, Reach = 2.5f, Under = 0.3f;

        // Spots round the ladder's top to land on, in kit axes (x into the opening, y to the side): behind it first, then
        // to either side, then before it, then the corners; then further in behind it, for when the player fits at none of those.
        private static readonly Vector2[] Spots =
        {
            new Vector2(-Behind, 0f), new Vector2(0f, 0.7f), new Vector2(0f, -0.7f), new Vector2(Behind, 0f),
            new Vector2(-Behind, 0.6f), new Vector2(-Behind, -0.6f), new Vector2(Behind, 0.6f), new Vector2(Behind, -0.6f),
            new Vector2(-0.85f, 0f), new Vector2(-0.85f, 0.5f), new Vector2(-0.85f, -0.5f), new Vector2(-1.2f, 0f), new Vector2(-1.6f, 0f),
        };

        /// <summary>A ladder with no ZDO of its own (one below a launched ship's deck): its length.</summary>
        public float FixedLength;

        /// <summary>On a launched ship, the blueprint piece it was made from (<see cref="KwSeaLadderLook"/>).</summary>
        public KwBpPiece? Piece;

        private float Length
        {
            get
            {
                if (FixedLength > 0f)
                    return FixedLength;
                ZDO? zdo = GetComponent<ZNetView>()?.GetZDO();
                return zdo != null && KwLadder.TryLength(zdo.GetString(KwGenerated.Key), out float length) ? length : 0f;
            }
        }

        public bool Interact(Humanoid user, bool hold, bool alt) => Guard.Run("Broadside ladder", () => Climb(user, hold));

        private bool Climb(Humanoid user, bool hold)
        {
            float length = Length;
            if (hold || !(user is Player player) || length <= 0f || player.IsAttached() || !Near(player, length))
                return false;
            bool up = Up(player);
            if (up)
                OpenAbove();
            Vector3 to = up ? Top(player) : Foot(player, length);
            Vector3 facing = up ? Away(to) : transform.forward;
            player.transform.rotation = Quaternion.LookRotation(facing);
            player.SetLookDir(facing);
            KwClimbRoom.Place(player, to);
            return false;
        }

        // Where climbing down lands: on whatever stands first under the opening in front of the ladder, when that is over its
        // foot; else at its foot.
        private Vector3 Foot(Player player, float length)
        {
            Vector3 from = Point(Out, -Under), foot = Point(Out, -length + 0.02f);
            float nearest = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, length, player.m_placeRayMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < nearest && !hit.collider.transform.IsChildOf(transform))
                    nearest = hit.distance;
            }
            return nearest < float.MaxValue && from.y - nearest > foot.y ? from + Vector3.down * (nearest - 0.02f) : foot;
        }

        // Where climbing up lands: the first spot round the ladder's top with deck under it (a flat surface near the deck's
        // top, not the opening, not the ladder itself) where the player fits standing (KwClimbRoom); the first with deck
        // under it when the player fits at none; behind it as before when none is found.
        private Vector3 Top(Player player)
        {
            float deck = Point(0f, 0f).y;
            Vector3? first = null;
            foreach (Vector2 spot in Spots)
            {
                Vector3? at = Floor(player, spot, deck);
                if (at != null && KwClimbRoom.Fits(player, at.Value))
                    return at.Value;
                first ??= at;
            }
            return first ?? Point(-Behind, 0.02f);
        }

        // The deck under a spot round the ladder's top, at about the deck's height (not the ladder itself), or null.
        private Vector3? Floor(Player player, Vector2 spot, float deck)
        {
            Vector3 at = At(spot.x, spot.y, 0.6f);
            if (Physics.Raycast(at, Vector3.down, out RaycastHit hit, 1.2f, player.m_placeRayMask, QueryTriggerInteraction.Ignore)
                && hit.normal.y > 0.7f && Mathf.Abs(hit.point.y - deck) < 0.35f && !hit.collider.transform.IsChildOf(transform))
                return hit.point + Vector3.up * 0.02f;
            return null;
        }

        // Level, from the opening out over the spot landed on.
        private Vector3 Away(Vector3 landed)
        {
            Vector3 way = landed - Point(Out, 0f);
            way.y = 0f;
            return way.sqrMagnitude > 0.01f ? way.normalized : -transform.forward;
        }

        // The hatch at the ladder's top opens first: a placed one, or a launched ship's.
        private void OpenAbove()
        {
            KwHatchCover? hatch = KwHatchCover.Nearest(transform.position, 1.6f);
            if (hatch != null)
                hatch.OpenUp();
            else
                KwShipHatch.Nearest(transform.position, 1.6f)?.OpenUp();
        }

        private bool Up(Player player) => player.transform.position.y < transform.position.y - 0.5f;

        // Within reach of the ladder anywhere along it, from its foot to its top.
        private bool Near(Player player, float length)
        {
            float height = Mathf.Clamp(transform.InverseTransformPoint(player.transform.position).y, -length, 0.3f);
            return Vector3.Distance(player.transform.position, Point(0.05f, height)) < Reach;
        }

        // A point on the ladder's kit axes (x into the opening, z up from the deck's top) in the world.
        private Vector3 Point(float x, float z) => At(x, 0f, z);

        private Vector3 At(float x, float y, float z) => transform.TransformPoint(KwMesh.ToUnity(new Vector3(x, y, z)));

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText()
        {
            Player? me = Player.m_localPlayer;
            if (me != null && !Near(me, Length))
                return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
            string way = me != null && Up(me) ? "Climb up" : "Climb down";
            return Localization.instance.Localize($"Ladder\n[<color=yellow><b>$KEY_Use</b></color>] {way}");
        }

        public string GetHoverName() => "Ladder";

        public float GetHoverOffset() => 0f;
    }
}
