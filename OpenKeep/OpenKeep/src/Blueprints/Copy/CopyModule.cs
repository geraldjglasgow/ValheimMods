using OpenKeep.Blueprints.Sites;
using SyncedConfig;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// Entry point of the Copy building tool (the blueprint menu's Copy entry): aim at buildings in the world and select
    /// them - a click takes a building, Shift + click one piece, G + click the joined pieces of one type, as many
    /// buildings as wanted (a whole compound) - then Enter saves exactly the selected pieces as a blueprint under a name
    /// of the player's choice, with their ground; Backspace clears the selection. Registers the words, the click, the
    /// per-frame work and the claim on Enter; the glow patches are ordinary patch classes. No settings of its own: the
    /// keys are fixed (<see cref="CopyKeys"/>), the switch is the Blueprints one. Nothing is networked: the selection and
    /// its glow are this machine's.
    /// </summary>
    public static class CopyModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            CopyWords.Register();
            SiteHooks.CopyClick = CopyClicks.OnClick;
            SiteHooks.OnUpdate("OpenKeep copy building", CopySession.Tick);
            ChatEnter.Claim("OpenKeep copy Enter", CopyKeys.TakesEnter);
        }
    }
}
