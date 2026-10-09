using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The component on a Blueprint Bench (<see cref="BenchPrefab"/>): hovering names it with the game's Use key, E opens
    /// the bench window (<see cref="BenchWindow"/>) for the local player, as a crafting station opens, behind the same ward
    /// check. While blueprints are off the bench says so and stays shut.
    /// </summary>
    public sealed class BlueprintBench : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView view;

        private void Awake() => view = GetComponent<ZNetView>();

        private bool Valid => view != null && view.IsValid();

        public string GetHoverText()
        {
            if (!Valid)
                return "";
            string action = BlueprintSettings.Enabled ? "[<color=yellow><b>$KEY_Use</b></color>] $piece_use" : BlueprintWords.Disabled;
            return Language.Localize(BenchWords.Name + "\n" + action);
        }

        public string GetHoverName() => Language.Localize(BenchWords.Name);

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || !Valid || user == null || user != Player.m_localPlayer)
                return false;
            if (!BlueprintSettings.Enabled)
            {
                Messages.Center(Language.Localize(BlueprintWords.Disabled));
                return false;
            }
            if (!PrivateArea.CheckAccess(transform.position))
                return false;
            BlueprintSafe.Run("OpenKeep blueprint bench", () => BenchWindow.Open(this));
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
