namespace Wayfare.SeaGates
{
    /// <summary>The keys of the sea gate words, whose English is in <c>translations/English.yml</c> and whose text comes
    /// from <see cref="Translations.Lang"/> where it is shown. Keys, never text: a pairing verdict and a refusal travel as
    /// their key (a refusal to the crew as the key's <c>$</c> token, which every crew client localizes in its own
    /// language), so nothing is worded before the player who reads it.</summary>
    public static class SeaGateWords
    {
        public const string PillarName = "wf_sg_pillar";
        public const string PillarDescription = "wf_sg_pillar_desc";
        public const string DefaultName = "wf_sg_default_name";

        public const string Unpaired = "wf_sg_unpaired";
        public const string HoverRename = "wf_sg_hover_rename";
        public const string HoverSailIn = "wf_sg_hover_sailin";
        public const string RenameTopic = "wf_sg_rename";
        public const string NotOwner = "wf_sg_notowner";

        public const string PairOk = "wf_sg_pair_ok";
        public const string PairTooFar = "wf_sg_pair_far";
        public const string PairTooClose = "wf_sg_pair_close";
        public const string PairHeight = "wf_sg_pair_height";
        public const string PairNoWater = "wf_sg_pair_nowater";
        public const string PairShallow = "wf_sg_pair_shallow";
        public const string PairTooDeep = "wf_sg_pair_deep";
        public const string Paired = "wf_sg_paired";
        public const string FixHint = "wf_sg_fix_hint";

        public const string PickerHelmHint = "wf_sg_picker_hint";
        public const string PickerCrewHint = "wf_sg_picker_crew";
        public const string Helmsman = "wf_sg_helmsman";
        public const string HelmPicks = "wf_sg_helm_picks";

        public const string DeniedClosed = "wf_sg_denied_closed";
        public const string DeniedPrivate = "wf_sg_denied_private";
        public const string DeniedAdmin = "wf_sg_denied_admin";
        public const string DeniedGone = "wf_sg_denied_gone";
        public const string DeniedCargo = "wf_sg_denied_cargo";
        public const string DeniedNotReady = "wf_sg_denied_notready";
        public const string DeniedNoHelmsman = "wf_sg_denied_nohelm";

        public const string Fallback = "wf_sg_fallback";
    }
}
