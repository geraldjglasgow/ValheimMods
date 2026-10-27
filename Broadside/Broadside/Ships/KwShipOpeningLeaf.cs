using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One leaf of a covered opening's cover or door on a launched ship (<see cref="KwShipOpenings"/>): on its hinge, with
    /// its box on its own object (the ship's body is one rigidbody, so the game hovers and uses the collider's own object
    /// only when that object can be hovered), its wood and straps drawn where there is a screen (<see cref="KwLeafSwing"/>).
    /// It swings toward its opening's state in the ship's ZDO (the game's door sound as it starts, on screens); E asks the ship's owner to open it (a door away from the
    /// player, a cover outwards) or shut it. Its hover says when the opening is below the waterline.
    /// </summary>
    public sealed class KwShipOpeningLeaf : MonoBehaviour, Interactable, Hoverable
    {
        private KwShipOpenings? openings;
        private KwLeafPlan? plan;
        private int number;
        private string note = "";
        private Quaternion shut;
        private float stage, aim;

        /// <summary>A leaf hung under its part's holder on the ship, drawn in its part's tier.</summary>
        public static void Hang(Transform part, KwLeafPlan leaf, KwShipOpenings openings, int number, string note, KwBiomeTier tier)
        {
            GameObject made = KwLeafSwing.Make(part, leaf, "leaf" + number, tier);
            KwShipOpeningLeaf use = made.AddComponent<KwShipOpeningLeaf>();
            (use.openings, use.plan, use.number, use.note, use.shut) = (openings, leaf, number, note, made.transform.localRotation);
            use.stage = use.aim = openings.State(number);
            KwLeafSwing.Pose(made.transform, use.shut, leaf, use.stage);
        }

        /// <summary>Its opening's number among the ship's covered openings.</summary>
        public int Number => number;

        /// <summary>Its opening numbered again (an edit at sea numbered the ship's openings anew); kept as it is when not in the map.</summary>
        public void Renumber(Dictionary<int, int> map)
        {
            if (map.TryGetValue(number, out int now))
            {
                number = now;
                gameObject.name = "leaf" + now;
            }
        }

        // Toward the state the ship's ZDO holds, with the game's door sound as it starts to open or shut.
        private void Update()
        {
            if (openings == null || plan == null)
                return;
            float target = openings.State(number);
            if (target != aim)
                KwLeafSwing.Sound(transform, plan, (aim = target) != 0f);
            if (Mathf.Approximately(stage, target))
                return;
            stage = Mathf.MoveTowards(stage, target, KwLeafSwing.PerSecond * Time.deltaTime);
            KwLeafSwing.Pose(transform, shut, plan, stage);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || openings == null || plan == null)
                return false;
            openings.Ask(number, KwLeafSwing.Way(transform, shut, plan, user.transform.position));
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText() => plan == null || openings == null ? "" : KwLeafSwing.Hover(plan, openings.State(number) != 0, note);

        public string GetHoverName() => plan != null ? KwLeafSwing.Name(plan) : "";

        public float GetHoverOffset() => 0f;
    }
}
