using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.SeaGates
{
    /// <summary>A pillar's hover text. Unpaired: the pillar's name and how to pair it. Paired: the gate's name, how a
    /// ship uses it, then the E and Alt+E prompts in the look of the portals' own (<see cref="TeleportWorldHoverPatch"/>).
    /// Names players typed are shown without rich text and never run through the localizer.</summary>
    public static class SeaGatePillarHover
    {
        private const string UseKey = "[<color=yellow><b>$KEY_Use</b></color>] ";

        public static string Text(SeaGatePillar pillar, bool seaGatesOn)
        {
            if (Localization.instance == null)
                return "";
            string pillarName = Localize(SeaGateWords.PillarName);
            if (!seaGatesOn || pillar == null || !pillar.IsValid)
                return pillarName;
            LoadedGate gate = SeaGateRegistry.GateOf(pillar);
            if (gate == null)
                return pillarName + "\n" + UnpairedLines(pillar);
            return Shown(gate.Name) + "\n" + Localize(SeaGateWords.HoverSailIn) + "\n" + Localize(UseKey + SeaGateWords.HoverRename) +
                   "\n" + CycleLine(gate);
        }

        /// <summary>Why this pillar isn't in a gate, drawn on the water too (<see cref="SeaGatePreview"/>): the pillar it
        /// would pair with and what is wrong, with the numbers; pairing retries by itself once it is fixed.</summary>
        private static string UnpairedLines(SeaGatePillar pillar)
        {
            PairCheck check = SeaGatePreview.Draw(pillar.transform.position, pillar.Id);
            if (check.Candidate == null)
                return Localize(SeaGateWords.Unpaired);
            if (check.Ok)
                return Localize(SeaGateWords.PairOk);
            return check.Describe() + "\n" + Localize(SeaGateWords.FixHint);
        }

        private static string CycleLine(LoadedGate gate)
        {
            PortalMode mode = PortalFields.GetMode(gate.AnchorZdo);
            string label = Localize(ModeCycle.ModeLabel(mode));
            return Localize(ModeCycle.AltUseKeys) + string.Format(Localize(Words.HoverCycle), label);
        }

        /// <summary>A gate name as a player typed it, or the default name when it has none.</summary>
        private static string Shown(string name)
        {
            string plain = string.IsNullOrEmpty(name) ? "" : name.RemoveRichTextTags().Trim();
            return plain.Length > 0 ? plain : Localize(SeaGateWords.DefaultName);
        }

        private static string Localize(string text) => Localization.instance.Localize(text);
    }
}
