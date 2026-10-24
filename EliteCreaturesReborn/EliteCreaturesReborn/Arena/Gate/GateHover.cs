using EliteCreaturesReborn.Arena.Trial;
using EliteCreaturesReborn.Config;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Looking at Helgrind: its name, and while Arena is on the trial it is set to ("[E] Trial: Swamp", Shift + E back,
    /// <see cref="GateChoice"/>) and "Walk through into Hel's hall" (the hall is built on demand, on the way in), or
    /// "Hel's gate is closed." (<see cref="GateWords"/>). The game asks for the text every frame the player looks at it, so
    /// it is rebuilt and localized at most four times a second, and at once after E.
    /// </summary>
    internal sealed class GateHover : MonoBehaviour, Hoverable, Interactable
    {
        private const float Every = 0.25f;
        private const string UseKey = "\n[<color=yellow><b>$KEY_Use</b></color>] ";

        private string _text = "";
        private float _builtAt = float.NegativeInfinity;
        private ZNetView? _view;

        public string GetHoverText()
        {
            if (Time.time - _builtAt < Every)
            {
                return _text;
            }
            _builtAt = Time.time;
            string name = Localization.instance.Localize("$" + GateWords.Piece);
            if (!ArenaSettings.Enabled)
            {
                _text = name + "\n" + Localization.instance.Localize("$" + GateWords.Closed);
                return _text;
            }
            string trial = Lang.Format(GateWords.Trial, ArenaModes.Name(GateChoice.Shown(View())));
            _text = name + Localization.instance.Localize(UseKey) + trial + "\n" + Lang.Get(GateWords.Open);
            return _text;
        }

        public string GetHoverName() => Localization.instance.Localize("$" + GateWords.Piece);

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || user != Player.m_localPlayer || !ArenaSettings.Enabled || !GateChoice.Step(View(), alt))
            {
                return false;
            }
            _builtAt = float.NegativeInfinity;
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        private ZNetView? View() => _view != null ? _view : _view = GetComponent<ZNetView>();
    }
}
