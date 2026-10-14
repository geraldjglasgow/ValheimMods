using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Looking at Hel (features/arena.md section 1, "Hel"), clients only: her hover shows "Hel" and, while no trial runs
    /// and the local player stands in the hall, "[E] Begin the trial"; E then asks the trial to start
    /// (<see cref="ArenaTrial.RequestStart"/>, which sends it to the arena core's owner). During a wave whose creatures
    /// have all come out, "[E] Call the next wave" (<see cref="ArenaTrial.RequestNextWave"/>). While a trial runs, what it
    /// would pay this player now: "Current rewards: ..." (<see cref="Rewards.RewardPreview"/>). Her box collider is on this
    /// object. The game asks for the text every frame the player looks at her, so it is rebuilt and localized at most
    /// four times a second. She is 6 m tall on a 1.76 m dais, so she can be hovered from 3 m further than the game's
    /// usual reach: from the foot of the dais.
    /// </summary>
    internal sealed class HelHover : MonoBehaviour, Hoverable, Interactable
    {
        private const string BeginLine = "\n[<color=yellow><b>$KEY_Use</b></color>] $" + HallWords.Begin;
        private const string CallLine = "\n[<color=yellow><b>$KEY_Use</b></color>] $" + ArenaTrial.CallNextWord;
        private const float Every = 0.25f;
        private const float Reach = 3f;

        private string _text = "";
        private string _name = "";
        private float _builtAt = float.NegativeInfinity;

        public string GetHoverText()
        {
            if (Time.time - _builtAt < Every)
            {
                return _text;
            }
            _builtAt = Time.time;
            TrialView.Refresh();
            _name = Localization.instance.Localize("$" + HallWords.Hel);
            _text = CanBegin(TrialView.Running) ? _name + Localization.instance.Localize(BeginLine)
                : ArenaTrial.CanCallNext ? _name + Localization.instance.Localize(CallLine) : _name;
            string rewards = TrialView.Running ? Rewards.RewardPreview.Line() : "";
            _text += rewards.Length > 0 ? "\n" + rewards : "";
            return _text;
        }

        public string GetHoverName()
        {
            if (_name.Length == 0)
            {
                _name = Localization.instance.Localize("$" + HallWords.Hel);
            }
            return _name;
        }

        public float GetHoverOffset() => Reach;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || user != Player.m_localPlayer)
            {
                return false;
            }
            if (CanBegin(ArenaTrial.Running))
            {
                ArenaTrial.RequestStart();
            }
            else if (ArenaTrial.CanCallNext)
            {
                ArenaTrial.RequestNextWave();
            }
            else
            {
                return false;
            }
            _builtAt = float.NegativeInfinity;
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        private static bool CanBegin(bool running)
        {
            Player? player = Player.m_localPlayer;
            return !running && player != null && ArenaHall.Built && ArenaHall.Contains(player.transform.position);
        }
    }
}
