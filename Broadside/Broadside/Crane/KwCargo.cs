using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The cargo crane's and the cargo crate's names, keys and limits, in one place (the bench's identity table is in
    /// <c>CRANE.md</c>). The prefabs start with <c>KWC_</c>, not <c>KW_</c>: Broadside treats every <c>KW_</c> piece as
    /// a ship part (blueprints, snapping, undo), and these are land pieces.
    /// </summary>
    public static class KwCargo
    {
        public const string CranePrefab = "KWC_cargo_crane", CratePrefab = "KWC_cargo_crate";
        public const string CraneBundle = "kw_cargocrane", CrateBundle = "kw_cargocrate";

        /// <summary>RPCs on the crane's view: asking to work it and the answer; the crate's answer to a hook.</summary>
        public const string OperateRpc = "KWC_Operate", OperateReplyRpc = "KWC_OperateReply", HookedRpc = "KWC_Hooked";

        /// <summary>RPCs on the crate's view: hook it to a crane, let it go.</summary>
        public const string HookRpc = "KWC_Hook", ReleaseRpc = "KWC_Release";

        /// <summary>The crane's state (written by its owner): turret turn (degrees), boom slide (m), the head's drop under the tip (m).</summary>
        public static readonly int TurnKey = "kwc_turn".GetStableHashCode(), SlideKey = "kwc_slide".GetStableHashCode(),
            DropKey = "kwc_drop".GetStableHashCode();

        /// <summary>The crane's operator (their character) and the crate on its hook.</summary>
        public static readonly KeyValuePair<int, int> OperatorKey = ZDO.GetHashZDOID("kwc_operator"),
            CrateKey = ZDO.GetHashZDOID("kwc_crate");

        /// <summary>The crate's crane (none when it is free), its turn against the crane's turret while it hangs, and whether it floats.</summary>
        public static readonly KeyValuePair<int, int> CraneKey = ZDO.GetHashZDOID("kwc_crane");
        public static readonly int HangYawKey = "kwc_hang_yaw".GetStableHashCode(), AfloatKey = "kwc_afloat".GetStableHashCode();

        /// <summary>How far the boom slides out from rest (m), the head's drop under the tip (m), and the speeds (Run doubles them).</summary>
        public const float Slide = 8.5f, MinDrop = 0.6f, MaxDrop = 12f;
        public const float TurnSpeed = 30f, SlideSpeed = 1.2f, HoistSpeed = 1.5f, RunFactor = 2f;

        /// <summary>The surfaces a hook or a crate comes to rest on.</summary>
        public static int Solid => solid != 0 ? solid
            : solid = UnityEngine.LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");

        private static int solid;
    }
}
