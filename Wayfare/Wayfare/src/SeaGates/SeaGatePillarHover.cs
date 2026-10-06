using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.SeaGates
{
    /// <summary>A pillar's hover text. Unpaired: the pillar's name and how to pair it. Paired: the gate's name, how a
    /// ship uses it, then the E and Alt+E prompts in the look of the portals' own (<see cref="TeleportWorldHoverPatch"/>).
    /// Names players typed are shown without rich text and never run through the localizer. A hover text is asked for
    /// every frame the player looks at the pillar, so a paired gate's text is built again only when its name, its mode,
    /// the language or the input device changes.</summary>
    public static class SeaGatePillarHover
    {
        private const string UseKey = "[<color=yellow><b>$KEY_Use</b></color>] ";

        private static readonly LocalWord pillarName = new LocalWord(SeaGateWords.PillarName);

        // The paired text last built, and what it was built from.
        private static string builtName;
        private static string builtCycle;
        private static string built;

        // The unpaired text last built, and what it was built from: the pillar, the verdict's kind (0 no candidate,
        // 1 would pair, 2 why not), its reason as described (the same string until it changes) and the language.
        private static SeaGatePillar unpairedFor;
        private static int unpairedKind = -1;
        private static string unpairedReason;
        private static int unpairedRevision = -1;
        private static string unpaired;

        public static string Text(SeaGatePillar pillar, bool seaGatesOn)
        {
            if (Localization.instance == null)
                return "";
            if (!seaGatesOn || pillar == null || !pillar.IsValid)
                return pillarName.Text;
            LoadedGate gate = SeaGateRegistry.GateOf(pillar);
            if (gate == null)
                return UnpairedText(pillar);
            return PairedText(gate.Name, CycleHover.Line(PortalFields.GetMode(gate.AnchorZdo)));
        }

        /// <summary>The cycle line comes from <see cref="CycleHover"/>, the same string until the mode, the language or the
        /// device changes, so comparing it covers all three.</summary>
        private static string PairedText(string name, string cycle)
        {
            if (built != null && name == builtName && ReferenceEquals(cycle, builtCycle))
                return built;
            builtName = name;
            builtCycle = cycle;
            built = Shown(name) + "\n" + Localize(SeaGateWords.HoverSailIn) + "\n" + Localize(UseKey + SeaGateWords.HoverRename) +
                    "\n" + cycle;
            return built;
        }

        /// <summary>Why this pillar isn't in a gate, drawn on the water too (<see cref="SeaGatePreview"/>): the pillar it
        /// would pair with and what is wrong, with the numbers; pairing retries by itself once it is fixed. Built again
        /// only when the pillar, the verdict, its reason or the language changes.</summary>
        private static string UnpairedText(SeaGatePillar pillar)
        {
            PairCheck check = SeaGatePreview.Draw(pillar.transform.position, pillar.Id);
            int kind = check.Candidate == null ? 0 : check.Ok ? 1 : 2;
            string reason = kind == 2 ? SeaGatePreview.Reason : null;
            if (unpaired != null && pillar == unpairedFor && kind == unpairedKind && ReferenceEquals(reason, unpairedReason) &&
                Language.Revision == unpairedRevision)
                return unpaired;
            unpairedFor = pillar;
            unpairedKind = kind;
            unpairedReason = reason;
            unpairedRevision = Language.Revision;
            unpaired = pillarName.Text + "\n" + UnpairedLines(kind, reason);
            return unpaired;
        }

        private static string UnpairedLines(int kind, string reason)
        {
            if (kind == 0)
                return Localize(SeaGateWords.Unpaired);
            if (kind == 1)
                return Localize(SeaGateWords.PairOk);
            return reason + "\n" + Localize(SeaGateWords.FixHint);
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
