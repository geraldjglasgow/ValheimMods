using UnityEngine;

namespace OpenKeep.Recipes
{
    /// <summary>Records, late in every frame, whether one of the crafting panel's fields is focused (see <see cref="TypingGuard"/>).</summary>
    public class TypingWatch : MonoBehaviour
    {
        public static bool WasTyping { get; private set; }

        private void Awake() => WasTyping = false;

        private void LateUpdate() => WasTyping = TypingGuard.TypingNow;
    }
}
