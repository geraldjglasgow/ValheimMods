using System;
using HarmonyLib;

namespace HighSeatPlus.Crowd
{
    /// <summary>
    /// Gear put on while a player is drawn simpler: a new cape (or other cloth) is held still as it arrives instead of
    /// simulating until the next decision. The game redraws a character's gear in VisEquipment.UpdateEquipmentVisuals
    /// every frame, but calls UpdateLodgroup at its end only when something changed, after every piece (and its cloth,
    /// VisEquipment.SetupCloth) is attached and after other mods' patches on the Set...Equipped methods have added their
    /// own cloth. So this runs once per change, never per frame: a quick test for players, the record's lookup, and for a
    /// player drawn simpler one more pass over their cloth (<see cref="StillCloth.Stop"/>). On error it logs once and
    /// stops; the game's own redraw is never interrupted.
    /// </summary>
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateLodgroup))]
    internal static class GearChange
    {
        private static bool _broken;

        private static void Postfix(VisEquipment __instance)
        {
            if (_broken || !__instance.m_isPlayer)
            {
                return;
            }
            try
            {
                FarPlayer far = CrowdList.ByVis(__instance);
                if (far != null && far.Simple)
                {
                    StillCloth.Stop(far);
                }
            }
            catch (Exception e)
            {
                _broken = true;
                HighSeatPlus.Log.LogWarning($"Gear put on by far players is no longer held still this session after an error: {e}");
            }
        }
    }
}
