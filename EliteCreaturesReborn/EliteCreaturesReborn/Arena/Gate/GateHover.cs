using EliteCreaturesReborn.Config;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Looking at Helgrind: its name, and "Walk through into Hel's hall" while Arena is on (the hall is built on demand,
    /// on the way in), or
    /// "Hel's gate is closed." (<see cref="GateWords"/>). The game asks for the text every frame the player looks at it, so
    /// it is rebuilt and localized at most four times a second.
    /// </summary>
    internal sealed class GateHover : MonoBehaviour, Hoverable
    {
        private const float Every = 0.25f;

        private string _text = "";
        private float _builtAt = float.NegativeInfinity;

        public string GetHoverText()
        {
            if (Time.time - _builtAt < Every)
            {
                return _text;
            }
            _builtAt = Time.time;
            string state = ArenaSettings.Enabled ? GateWords.Open : GateWords.Closed;
            _text = Localization.instance.Localize("$" + GateWords.Piece + "\n$" + state);
            return _text;
        }

        public string GetHoverName() => Localization.instance.Localize("$" + GateWords.Piece);

        public float GetHoverOffset() => 0f;
    }
}
