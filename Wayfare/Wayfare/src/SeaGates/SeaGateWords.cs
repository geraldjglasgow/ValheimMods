using Wayfare.Core;

namespace Wayfare.SeaGates
{
    /// <summary>The sea gate words. A module that needs one more adds it in its own file with
    /// <see cref="Language.Add"/>, so parallel work never edits this file at the same time.</summary>
    public static class SeaGateWords
    {
        public static readonly string PillarName = Language.Add("wf_sg_pillar", "Sea Gate Pillar");
        public static readonly string PillarDescription = Language.Add("wf_sg_pillar_desc",
            "Build two on the shore or in shallow water, 10 to 15 m apart with water between them: a portal for ships opens between them.");
        public static readonly string DefaultName = Language.Add("wf_sg_default_name", "Sea gate");

        public static readonly string Unpaired = Language.Add("wf_sg_unpaired", "Not paired: build a second pillar 10 to 15 m away across the water");
        public static readonly string NoDestination = Language.Add("wf_sg_nodest", "No destination");
        public static readonly string Destination = Language.Add("wf_sg_dest", "Destination: {0}");
        public static readonly string HoverSetDest = Language.Add("wf_sg_hover_setdest", "Set destination");

        public static readonly string PickerHint = Language.Add("wf_sg_picker_hint",
            "Click a sea gate to sail there from this gate. Click this gate to rename it.");
        public static readonly string RenameTopic = Language.Add("wf_sg_rename", "Name this sea gate");
        public static readonly string DestinationSet = Language.Add("wf_sg_dest_set", "Ships sailing through now go to {0}");

        public static readonly string PairOk = Language.Add("wf_sg_pair_ok", "Pairs with this pillar");
        public static readonly string PairTooFar = Language.Add("wf_sg_pair_far", "Too far from the other pillar: {0} m, at most {1} m");
        public static readonly string PairTooClose = Language.Add("wf_sg_pair_close", "Too close to the other pillar: {0} m, at least {1} m");
        public static readonly string PairHeight = Language.Add("wf_sg_pair_height", "{0} m higher or lower than the other pillar, at most {1} m");
        public static readonly string PairNoWater = Language.Add("wf_sg_pair_nowater", "No water between the pillars");
        public static readonly string PairShallow = Language.Add("wf_sg_pair_shallow", "Too shallow at the red marks: {0} m deep, a ship needs {1} m");
        public static readonly string PairTooDeep = Language.Add("wf_sg_pair_deep", "Too deep for a pillar: {0} m under water, at most {1} m");
        public static readonly string Paired = Language.Add("wf_sg_paired", "The sea gate is open");
        public static readonly string FixHint = Language.Add("wf_sg_fix_hint", "Fix it and the gate opens by itself");

        public static readonly string DeniedNoDest = Language.Add("wf_sg_denied_nodest", "This sea gate has no destination");
        public static readonly string DeniedGone = Language.Add("wf_sg_denied_gone", "The destination sea gate is gone");
        public static readonly string DeniedCargo = Language.Add("wf_sg_denied_cargo", "Ore and other restricted cargo can't pass through a sea gate");
        public static readonly string DeniedNotReady = Language.Add("wf_sg_denied_notready", "The sea gate wasn't ready: sail through again");
        public static readonly string DeniedNoHelmsman = Language.Add("wf_sg_denied_nohelm", "A ship passes through a sea gate only with someone at the helm");

        public static readonly string Fallback = Language.Add("wf_sg_fallback", "The ship didn't arrive in time: you were set ashore by the gate");

        public static void Touch() => _ = PillarName;
    }
}
