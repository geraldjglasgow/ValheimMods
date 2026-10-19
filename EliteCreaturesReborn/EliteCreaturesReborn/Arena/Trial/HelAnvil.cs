using PatchGuard;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Hel's Anvil (features/arena.md section 3, "Hel's Anvil"): the hall puts one on each side of Hel's dais
    /// (<c>ecr_helarena_anvil</c>) and adds this to the model's root; its colliders make it hoverable. The hover shows
    /// "Hel's Anvil" and, for a player of the trial in the hall, during the countdown or a break "[E] Repair your gear
    /// (120 souls)" or "Not enough souls (120)", during a wave "The anvil is cold while the dead come". E asks the core's
    /// owner to take the souls (<see cref="AnvilUse"/>). The hover runs every frame while looked at: its text is built only
    /// when what it shows changes (<see cref="AnvilText"/>).
    /// </summary>
    public sealed class HelAnvil : MonoBehaviour, Hoverable, Interactable
    {
        public string GetHoverName() => Lang.Get(ArenaWords.Anvil);

        public string GetHoverText()
        {
            try
            {
                return AnvilText.For();
            }
            catch (System.Exception e)
            {
                AnvilText.Fail(e);
                return GetHoverName();
            }
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || user != Player.m_localPlayer)
            {
                return false;
            }
            bool taken = false;
            Guard.Run("Hel's Anvil", () => taken = AnvilUse.Ask(transform.position));
            return taken;
        }

        /// <summary>The game's own offset for hovering: none, the anvil is reached as any piece.</summary>
        public float GetHoverOffset() => 0f;

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
