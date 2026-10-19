using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Rules;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>
    /// Opens and closes the altar window, where a player chooses the fight a boss altar offers, building it on first use
    /// and again after the screen it lives beside is rebuilt (a new world). While open, the game treats it like its own
    /// windows (free cursor, no attacks, Esc closes; see the WindowInput library).
    /// </summary>
    internal static class AltarWindow
    {
        private static AltarWindowView? _view;

        public static bool IsOpen => _view != null && _view.gameObject.activeSelf;

        /// <summary>From E on the bowl. False when nothing can be chosen there, so the game's own interaction goes on.</summary>
        public static bool Open(OfferingBowl bowl)
        {
            AltarAspect? altar = bowl.GetComponent<AltarAspect>();
            if (altar == null || !CanChoose(altar) || Player.m_localPlayer == null)
            {
                return false;
            }
            if (_view == null)
            {
                _view = AltarWindowBuilder.Build();
            }
            if (_view == null)
            {
                return false;
            }
            _view.Show(altar);
            return true;
        }

        /// <summary>The setting is on, boss stars or aspects are on, and the offering has not been made yet.</summary>
        public static bool CanChoose(AltarAspect altar) =>
            AltarSettings.Enabled && !altar.Offered && (RuleState.Active.Boss.Enabled || RuleState.Active.Boss.Aspects.Enabled);

        public static void Close()
        {
            if (IsOpen)
            {
                _view!.Hide();
            }
        }
    }
}
