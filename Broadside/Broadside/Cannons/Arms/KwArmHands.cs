using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the gunner's fists go on a cannon and how far he leans in (the library reaches them there by two-bone IK on
    /// every peer that draws, after the animator, easing them on over his last stride up and off as he lets go): both
    /// fists on the cannon's grips (<see cref="KwArmDef.Hands"/>: the left on the breech, the right on the lanyard's
    /// handle, which goes with the pull), on its reload grips through a reload (<see cref="KwArmDef.ReloadHands"/>, the
    /// door's handle carried round to the open breech, eased in over its first 15% and out over its last); leaning in as
    /// each grip says, further the higher the barrel is tilted (its grips behind the trunnions drop) and less tilted down,
    /// never past the cannon's most (<see cref="KwArmDef.MostLean"/>). At a ship cannon he lets go and straightens up
    /// while its carriage recoils (<see cref="KwShipRecoil"/>), and takes hold again as it runs back in. All from the ZDO
    /// and the drawn barrel, so every screen shows the same.
    /// </summary>
    public static class KwArmHands
    {
        // The share of a reload the hands take to reach the reload grips and to come back; how far back the carriage rolls
        // before he has let go entirely, metres.
        private const float Edge = 0.15f, Clear = 0.15f;

        public static OperatorPose Pose(KwArm arm, in WeaponState s, OperatorRig rig)
        {
            KwArmDef def = arm.Def;
            float reload = Reloading(arm, s);
            float aimLean = def.Hands?.Lean ?? 0f, reloadLean = def.ReloadHands?.Lean ?? aimLean;
            float lean = Mathf.Lerp(aimLean, reloadLean, reload) + Mathf.Clamp(Tilt(arm.Frame!.Pitch) * def.TiltLean, -10f, 25f);
            float hold = arm.Recoil != null ? 1f - Mathf.SmoothStep(0f, 1f, arm.Recoil.Recoiled(s) / Clear) : 1f;
            Vector3? left = Fist(arm.Frame.GripLeft, arm.Motion?.ReloadLeft, reload, rig.Left);
            Vector3? right = Fist(arm.Frame.GripRight, arm.Motion?.ReloadRight, reload, rig.Right);
            return new OperatorPose(Mathf.Clamp(lean, 0f, def.MostLean), left, right, hold);
        }

        // The barrel's drawn tilt, degrees up.
        private static float Tilt(Transform pitch)
        {
            Vector3 ahead = pitch.localRotation * Vector3.forward;
            return Mathf.Atan2(ahead.y, ahead.z) * Mathf.Rad2Deg;
        }

        // How fully the hands are on the reload grips: through the middle of a reload, none outside one.
        private static float Reloading(KwArm arm, in WeaponState s)
        {
            if (arm.Def.ReloadHands == null || !s.Busy)
                return 0f;
            float p = KwArmReload.Progress(arm, s);
            return Mathf.SmoothStep(0f, 1f, p / Edge) * (1f - Mathf.SmoothStep(0f, 1f, (p - (1f - Edge)) / Edge));
        }

        // A fist's target: its grip, moved toward its reload grip by the reload's weight; a hand with neither is left as
        // the animator has it.
        private static Vector3? Fist(Transform? grip, Transform? reloadGrip, float reload, OperatorArm hand)
        {
            if (grip == null && reloadGrip == null)
                return null;
            Vector3 target = grip != null ? grip.position : hand.Grip;
            return reloadGrip != null ? Vector3.Lerp(target, reloadGrip.position, reload) : target;
        }
    }
}
