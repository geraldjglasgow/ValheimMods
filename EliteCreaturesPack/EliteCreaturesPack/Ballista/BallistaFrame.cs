using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The bundle's ballista comes flat (ValheimAssets Assets/Props/BoneBallista, BRIEF.md): every moving part and every
    /// point a direct child, each part on its own pivot, unturned. This hangs them into the frame the code turns, once,
    /// on the prefab: `yaw` (the yoke, turns about Y) carries `pitch` (stock, bow mount, handles, latch; turns about X)
    /// and the feet points `stand` and `stand_load`; `pitch` carries the two arms, each a chain of vertebrae
    /// `limb_l_0`, `limb_l_1`, ... (each hung on the one before, so bending each a little bends the arm), with the
    /// string's tie point `tip_l` / `tip_r` on the last; the two string halves; the grips, nocks, seat and muzzle.
    /// </summary>
    public static class BallistaFrame
    {
        public const string Yaw = "yaw", Pitch = "pitch";
        public const string StandAim = "stand", StandLoad = "stand_load";
        public const string GripLeft = "grip_l", GripRight = "grip_r", NockRest = "nock_rest", NockDrawn = "nock_drawn";
        public const string Seat = "bolt_seat", Muzzle = "muzzle", StringLeft = "string_l", StringRight = "string_r";

        private static readonly string[] OnYaw = { StandAim, StandLoad };
        private static readonly string[] OnPitch = { GripLeft, GripRight, NockRest, NockDrawn, Seat, Muzzle, StringLeft, StringRight };

        /// <summary>The flat model hung into its frame; false (and logged) when it lacks the yaw or pitch part.</summary>
        public static bool Assemble(Transform model)
        {
            Transform? yaw = GameMaterials.Find(model, Yaw), pitch = GameMaterials.Find(model, Pitch);
            if (yaw == null || pitch == null)
            {
                Log.Error($"Bone ballista: the model has no {(yaw == null ? Yaw : Pitch)} part; it cannot turn.");
                return false;
            }
            pitch.SetParent(yaw, true);
            Hang(model, OnYaw, yaw);
            Hang(model, OnPitch, pitch);
            Chain(model, pitch, "l");
            Chain(model, pitch, "r");
            return true;
        }

        /// <summary>The name of an arm's segment: side "l" or "r", from 0 at the root.</summary>
        public static string Segment(string side, int index) => $"limb_{side}_{index}";

        public static string Tip(string side) => "tip_" + side;

        private static void Hang(Transform model, string[] names, Transform parent)
        {
            foreach (string name in names)
            {
                Transform? part = GameMaterials.Find(model, name);
                if (part == null)
                {
                    Log.Warn($"Bone ballista: the model has no {name}.");
                    continue;
                }
                part.SetParent(parent, true);
            }
        }

        /// <summary>One arm: each vertebra hung on the one before, the first on the stock, the string's tie on the last.</summary>
        private static void Chain(Transform model, Transform pitch, string side)
        {
            Transform parent = pitch;
            for (int i = 0; GameMaterials.Find(model, Segment(side, i)) is Transform segment; i++)
            {
                segment.SetParent(parent, true);
                parent = segment;
            }
            if (parent == pitch)
            {
                Log.Warn($"Bone ballista: the model has no {Segment(side, 0)}; that arm will not bend.");
            }
            Transform? tip = GameMaterials.Find(model, Tip(side));
            tip?.SetParent(parent, true);
        }
    }
}
