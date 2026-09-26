using System;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using PatchGuard;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// The aspect work that must happen at the instant of a boss's death, from the death patch's prefix on its owner,
    /// because it needs the dying boss's ZDO and network view, both gone a moment later: a Twin tells its partner to fall
    /// with it, and a Phantom copy sends its vanishing puff to every client holding it (it leaves no body to mark the
    /// spot). Exception-safe and never rethrows: nothing here may stop a creature from dying.
    /// </summary>
    internal static class AspectDeath
    {
        public static void Capture(Character victim, EliteController controller, ZNetView nview)
        {
            try
            {
                Act(victim, controller.Traits, nview);
            }
            catch (Exception e)
            {
                Guard.Report(e, "boss aspect death");
            }
        }

        private static void Act(Character victim, CreatureTraits traits, ZNetView nview)
        {
            if (traits.PhantomCopy)
            {
                CreatureRpc.FireFlash(nview, victim.GetCenterPoint(), 2f, "phantom");
            }
            else if (traits.Aspect == Aspect.Twin)
            {
                TwinLink.SendFall(nview.GetZDO());
            }
        }
    }
}
