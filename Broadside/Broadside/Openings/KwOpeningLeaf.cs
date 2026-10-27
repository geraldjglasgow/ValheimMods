using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One leaf of an opening's cover or door on a part on the dock (<see cref="KwOpeningPart"/>): on its hinge, with a box to
    /// bump into, its wood and iron drawn where there is a screen. It swings toward its opening's state in the part's ZDO
    /// (<see cref="KwLeafSwing"/>; a double door's two leaves share it), with the game's wooden door's sound; E asks the
    /// part's owner to open it (a door away from the player, a cover outwards) or shut it, where the player may build (a
    /// ward's access, as a hatch).
    /// </summary>
    public sealed class KwOpeningLeaf : MonoBehaviour, Interactable, Hoverable
    {
        private KwOpeningPart? part;
        private KwLeafPlan? plan;
        private Quaternion shut;
        private float stage, aim;

        /// <summary>A leaf hung under a part's openings holder.</summary>
        public static void Hang(Transform holder, KwLeafPlan leaf, KwOpeningPart part, KwBiomeTier tier)
        {
            GameObject made = KwLeafSwing.Make(holder, leaf, "leaf" + leaf.Opening.Id, tier);
            KwOpeningLeaf use = made.AddComponent<KwOpeningLeaf>();
            (use.part, use.plan, use.shut) = (part, leaf, made.transform.localRotation);
            use.stage = use.aim = part.State(leaf.Opening.Id);
            KwLeafSwing.Pose(made.transform, use.shut, leaf, use.stage);
        }

        private void Update()
        {
            if (part == null || plan == null)
                return;
            float target = part.State(plan.Opening.Id);
            if (target != aim)
                KwLeafSwing.Sound(transform, plan, (aim = target) != 0f);
            if (Mathf.Approximately(stage, target))
                return;
            stage = Mathf.MoveTowards(stage, target, KwLeafSwing.PerSecond * Time.deltaTime);
            KwLeafSwing.Pose(transform, shut, plan, stage);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || part == null || plan == null)
                return false;
            if (PrivateArea.CheckAccess(transform.position))
                part.Ask(plan.Opening.Id, KwLeafSwing.Way(transform, shut, plan, user.transform.position));
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText() => plan == null || part == null ? "" : KwLeafSwing.Hover(plan, part.State(plan.Opening.Id) != 0, "");

        public string GetHoverName() => plan != null ? KwLeafSwing.Name(plan) : "";

        public float GetHoverOffset() => 0f;
    }
}
