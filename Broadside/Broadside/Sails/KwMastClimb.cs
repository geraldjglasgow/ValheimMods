using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Climbing a mast's pegs to its platforms (<see cref="KwMastTops"/>), as the hatch ladders are climbed: E puts the player
    /// on the next floor up at once (the deck at the mast's foot, the fighting top, the crow's nest), Shift and E on the next
    /// one down, beside the mast on the side they climbed from, facing the bow. The climb is found on the pegs: on the dock
    /// two thin boxes along the mast's sides, so its fore and aft faces still raise and lower the sails
    /// (<see cref="KwSailRig"/>); on a launched ship, whose mast takes no other use, one box round it. Within reach of the
    /// mast only. Local to the climbing player; the game sends the player's position as it always does. Not made on a
    /// dedicated server, where nobody climbs.
    /// </summary>
    public sealed class KwMastClimb : MonoBehaviour, Interactable, Hoverable
    {
        public const string Name = "climb";
        private const float Reach = 2.5f, Out = 0.22f;

        private float[] levels = new float[0];
        private Vector3[] stands = new Vector3[0];
        private string[] names = new string[0];

        /// <summary>The mast's climbing boxes under it (the old ones gone), when it has a platform; around it on a ship.</summary>
        public static void Add(Transform mast, KwMastSpec spec, bool ship)
        {
            for (int i = mast.childCount - 1; i >= 0; i--)
            {
                Transform child = mast.GetChild(i);
                if (child.name == Name)
                {
                    child.name = Name + "_gone";
                    Destroy(child.gameObject);
                }
            }
            if (!spec.Tops.Any || !KwShipBody.Drawn)
                return;
            float r = KwSailLayout.RadiusOf(spec.Mast), high = spec.Tops.Highest + 1.6f;
            if (ship)
                Box(mast, spec, new Vector3(0f, 0f, high / 2f), new Vector3(2f * (r + Out), high, 2f * r + 0.3f));
            else
                foreach (float side in new[] { -1f, 1f })
                    Box(mast, spec, new Vector3(0f, side * (r + Out / 2f), high / 2f), new Vector3(Out + 0.04f, high, 0.26f));
        }

        // One climbing box at a kit point of the mast, sized in Unity axes (across, up, along).
        private static void Box(Transform mast, KwMastSpec spec, Vector3 kit, Vector3 size)
        {
            GameObject box = new GameObject(Name) { layer = LayerMask.NameToLayer("piece_nonsolid") };
            box.transform.SetParent(mast, false);
            box.transform.localPosition = KwMesh.ToUnity(kit);
            box.AddComponent<BoxCollider>().size = size;
            KwMastClimb climb = box.AddComponent<KwMastClimb>();
            climb.levels = spec.Tops.Levels().ToArray();
            climb.names = Names(spec.Tops, climb.levels);
            climb.stands = System.Array.ConvertAll(climb.levels, level => Stand(spec, level));
        }

        // Where a climber stands on a floor, on the port side (the starboard side mirrors it): beside the mast on the deck,
        // halfway round the crow's nest's ring, or in the middle of the fighting top's half.
        private static Vector3 Stand(KwMastSpec spec, float level)
        {
            float r = KwMastTops.RadiusAt(spec.Mast, level);
            KwMastTops tops = spec.Tops;
            if (level <= 0f)
                return new Vector3(0f, KwSailLayout.RadiusOf(spec.Mast) + 0.6f, 0.05f);
            if (tops.Nest && Mathf.Approximately(level, tops.NestAt))
                return new Vector3(0f, (r + KwMastTops.NestRadius(spec.Mast) - 0.04f) / 2f, level + 0.05f);
            float h = KwMastTops.TopHalf(spec.Mast);
            return new Vector3(KwMastTops.TopWay(spec) * 0.6f * h, 0.5f * h, level + 0.05f);
        }

        // What each floor is called on the hover: the deck, the fighting top, the crow's nest.
        private static string[] Names(KwMastTops tops, float[] levels)
        {
            string[] names = new string[levels.Length];
            for (int i = 0; i < levels.Length; i++)
                names[i] = i == 0 ? "the deck" : tops.Nest && Mathf.Approximately(levels[i], tops.NestAt) ? "the crow's nest" : "the fighting top";
            return names;
        }

        public bool Interact(Humanoid user, bool hold, bool alt) => Guard.Run("Broadside mast climb", () => Climb(user, hold, alt));

        private bool Climb(Humanoid user, bool hold, bool alt)
        {
            if (hold || !(user is Player player) || player.IsAttached() || !Near(player))
                return false;
            int at = Level(player), to = alt ? at - 1 : at + 1;
            if (to < 0 || to >= levels.Length)
                return false;
            Transform frame = transform.parent;
            Vector3 stand = stands[to];
            stand.y *= Kit(player.transform.position).y < 0f ? -1f : 1f;
            Vector3 facing = new Vector3(frame.forward.x, 0f, frame.forward.z).normalized;
            player.transform.rotation = Quaternion.LookRotation(facing);
            player.SetLookDir(facing);
            KwClimbRoom.Place(player, frame.TransformPoint(KwMesh.ToUnity(stand)));
            return false;
        }

        // The floor the player stands on (or over): the highest at most half a metre over their feet.
        private int Level(Player player)
        {
            float z = Kit(player.transform.position).z;
            int at = 0;
            for (int i = 1; i < levels.Length; i++)
                at = levels[i] <= z + 0.5f ? i : at;
            return at;
        }

        // Within reach of the mast, between its foot and a little over its highest floor.
        private bool Near(Player player)
        {
            Vector3 kit = Kit(player.transform.position);
            return new Vector2(kit.x, kit.y).magnitude < Reach && kit.z > -1f && kit.z < levels[levels.Length - 1] + 2.5f;
        }

        // A point in the mast's kit axes.
        private Vector3 Kit(Vector3 world) => KwMesh.ToKit(transform.parent.InverseTransformPoint(world));

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText()
        {
            Player? me = Player.m_localPlayer;
            if (me == null || levels.Length < 2)
                return "";
            if (!Near(me))
                return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
            int at = Level(me);
            List<string> lines = new List<string> { "Mast" };
            if (at + 1 < levels.Length)
                lines.Add($"[<color=yellow><b>$KEY_Use</b></color>] Climb up to {names[at + 1]}");
            if (at > 0)
                lines.Add($"[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] Climb down to {names[at - 1]}");
            return Localization.instance.Localize(string.Join("\n", lines));
        }

        public string GetHoverName() => "Mast";

        public float GetHoverOffset() => 0f;
    }
}
