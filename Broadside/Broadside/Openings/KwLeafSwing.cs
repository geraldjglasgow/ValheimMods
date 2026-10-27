using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a leaf on the dock (<see cref="KwOpeningLeaf"/>) and one on a launched ship share: the leaf's object on its hinge
    /// with its box, and its wood and iron drawn where there is a screen (a door's in the game's door wood and its tier's
    /// style, a cover's in the plank wood); its pose as it swings (eased, a share of its angle, either way for a door;
    /// a double door's two leaves the same way, as they share one state); which way E opens it (a door away from the
    /// player, a cover outwards); its name and hover words.
    /// </summary>
    public static class KwLeafSwing
    {
        /// <summary>How fast a leaf swings: its whole angle in half a second.</summary>
        public const float PerSecond = 2f;

        /// <summary>A leaf's object under a holder, at its hinge, with its box and (on a screen) its look in its part's tier.</summary>
        public static GameObject Make(Transform holder, KwLeafPlan leaf, string name, KwBiomeTier tier)
        {
            GameObject made = new GameObject(name) { layer = holder.gameObject.layer };
            made.transform.SetParent(holder, false);
            made.transform.SetLocalPositionAndRotation(KwMesh.ToUnity(leaf.Hinge), leaf.Turn);
            BoxCollider box = made.AddComponent<BoxCollider>();
            (box.center, box.size) = leaf.Box;
            if (!KwShipBody.Drawn)
                return made;
            MeshBuilder board = new MeshBuilder(Vector3.zero), straps = new MeshBuilder(Vector3.zero);
            KwOpeningLeafMesh.Draw(board, straps, leaf, KwOpeningLeafMesh.Own, tier);
            Material? wood = leaf.Door ? KwDoorLooks.Wood(tier) : KwOpeningLooks.Wood(tier);
            KwOpeningPart.Drawn(made.transform, "wood", board.ToMesh("KW_opening_leaf"), wood);
            KwOpeningPart.Drawn(made.transform, "iron", straps.ToMesh("KW_opening_straps"), KwOpeningLooks.Iron);
            return made;
        }

        /// <summary>The leaf turned a share of its way open (stage -1 to 1, 0 shut) about its pivot (<see cref="KwLeafPlan.Pivot"/>), eased.</summary>
        public static void Pose(Transform leaf, Quaternion shut, KwLeafPlan plan, float stage)
        {
            Quaternion swing = Swing(plan, stage);
            Vector3 pivot = plan.Pivot(stage);
            leaf.localRotation = shut * swing;
            leaf.localPosition = KwMesh.ToUnity(plan.Hinge) + shut * (pivot - swing * pivot);
        }

        /// <summary>A leaf's turn about its hinge's line a share of its way open (stage -1 to 1), eased.</summary>
        public static Quaternion Swing(KwLeafPlan plan, float stage) =>
            KwTurns.About(Mathf.Sign(stage) * Mathf.SmoothStep(0f, plan.Angle, Mathf.Abs(stage)), Vector3.right);

        /// <summary>Which way E opens a leaf: a door away from the side the player stands on (1 out, -1 in), a cover out.</summary>
        public static int Way(Transform leaf, Quaternion shut, KwLeafPlan plan, Vector3 player)
        {
            if (!plan.Door)
                return 1;
            Vector3 outward = (leaf.parent != null ? leaf.parent.rotation : Quaternion.identity) * shut * Vector3.forward;
            return Vector3.Dot(outward, player - leaf.position) > 0f ? -1 : 1;
        }

        /// <summary>
        /// The game's wooden door's sound as a leaf starts to open or shut (where there is a screen; none without the game's
        /// door; none for a quiet leaf, whose partner sounds for both).
        /// </summary>
        public static void Sound(Transform leaf, KwLeafPlan plan, bool open)
        {
            if (!KwShipBody.Drawn || ZNetScene.instance == null || plan.Quiet)
                return;
            door ??= ZNetScene.instance.GetPrefab("wood_door")?.GetComponent<Door>();
            (open ? door?.m_openEffects : door?.m_closeEffects)?.Create(leaf.position, leaf.rotation);
        }

        private static Door? door;

        /// <summary>What a leaf is called on its hover.</summary>
        public static string Name(KwLeafPlan plan) => plan.Opening.Cover switch
        {
            KwOpening.Leaf => "Door",
            KwOpening.DoubleDoor => "Double door",
            KwOpening.Up => "Gunport lid",
            KwOpening.Down => "Drop lid",
            KwOpening.Shutters => "Shutters",
            _ => "Porthole lid",
        };

        /// <summary>The hover: its name, E to open or shut it ("Open the double door"), and a note under them (none for "").</summary>
        public static string Hover(KwLeafPlan plan, bool open, string note)
        {
            string act = plan.Door ? $"{(open ? "Close" : "Open")} the {Name(plan).ToLowerInvariant()}" : open ? "Shut" : "Open";
            string text = $"{Name(plan)}\n[<color=yellow><b>$KEY_Use</b></color>] {act}";
            return Localization.instance.Localize(note.Length > 0 ? text + "\n" + note : text);
        }
    }
}
