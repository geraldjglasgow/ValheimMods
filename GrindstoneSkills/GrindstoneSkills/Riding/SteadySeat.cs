using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Steady Seat, on the mount's owner, where its damage is applied: Character.RPC_Damage runs there once per hit, with
    /// the HitData freshly read from the RPC, before armour, resistances and Character.ApplyDamage. While the mount has a
    /// rider, a prefix scales every damage type of the hit by 1 - <see cref="SofterAt100"/> x the rider's Ride factor
    /// (<see cref="MountRider"/>, the level the rider's client sent in the saddle's "Controls" RPC), so any rider on any
    /// peer counts. Its own drowning (Sadle.UpdateDrown) is a hit like any other. Off, without a rider or at Ride 0 the
    /// hit is the game's.
    /// </summary>
    public static class SteadySeat
    {
        /// <summary>How much less damage a ridden mount takes at Ride 100.</summary>
        private const float SofterAt100 = 0.25f;

        /// <summary>The share of a hit a ridden mount still takes at a rider's Ride factor (0..1).</summary>
        public static float Kept(float factor) => 1f - SofterAt100 * factor;

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class Taken
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit == null || !GameSkillSettings.Riding.Value || __instance.IsPlayer() || !__instance.HaveRider())
                    return;
                HookGuard.Run("steady seat", static taken => Soften(taken.mount, taken.hit), (mount: __instance, hit));
            }
        }

        private static void Soften(Character mount, HitData hit)
        {
            ZNetView view = mount.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            float kept = Kept(MountRider.Factor(mount));
            if (kept < 1f)
                hit.m_damage.Modify(kept);
        }
    }
}
