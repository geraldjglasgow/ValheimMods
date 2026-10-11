using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// One Raiders Chest's hover text, kept between frames. The game asks for it every frame the player looks at the chest;
    /// building it costs a survey of the players near (the heat), the game's base test (a physics query) and a walk of the
    /// raid hosts' ZDOs around (the cooldown and the 200 m spacing), so it is rebuilt only when something it shows can have
    /// moved: the chest's ZDO changed (coins in or out, the raid stepping on; at most four times a second), the hold on E
    /// moved a tenth, or a second passed (players coming and going, the clocks counting down). Every other frame returns
    /// the same string, localized once per rebuild.
    /// </summary>
    internal sealed class ChestHover
    {
        private const float MaxAge = 1f;
        private const float MinAge = 0.25f;

        private string _text = "";
        private uint _revision = uint.MaxValue;
        private int _holdStep = -2;
        private float _builtAt = float.NegativeInfinity;

        public string Text(RaidChest chest)
        {
            uint revision = chest.View.GetZDO().DataRevision;
            int step = chest.Hold.Step;
            float age = Time.time - _builtAt;
            if (step == _holdStep && age < MaxAge && (revision == _revision || age < MinAge))
            {
                return _text;
            }
            _revision = revision;
            _holdStep = step;
            _builtAt = Time.time;
            _text = Localization.instance.Localize(ChestHoverText.Build(chest, step));
            return _text;
        }
    }
}
