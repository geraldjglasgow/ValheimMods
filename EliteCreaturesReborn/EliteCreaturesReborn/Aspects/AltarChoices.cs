using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Traits;
using PatchGuard;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// The choices a player makes in the altar window, sent from any machine to the altar's owner, which checks them
    /// against its own rules (<see cref="AltarPicks"/>), keeps them in the altar's ZDO and shows them on the bowl at once:
    /// the star count and the aspect as one message, and the extras beside a chosen Bountiful as another. Nothing can be
    /// chosen once the offering is made.
    /// </summary>
    internal static class AltarChoices
    {
        /// <summary>The star count and the aspect value, each a value or <see cref="AltarPicks.Random"/>.</summary>
        private const string PickRpc = "ECR_AltarPick";

        /// <summary>The extras beside a chosen Bountiful, one bit per aspect value; 0 lets them all roll.</summary>
        private const string ExtrasRpc = "ECR_AltarExtras";

        /// <summary>On every machine, as the altar starts.</summary>
        public static void Register(AltarAspect altar)
        {
            ZNetView? view = altar.Bowl != null ? altar.Bowl.m_nview : null;
            if (view != null && view.IsValid())
            {
                view.Register<int, int>(PickRpc, (_, stars, aspect) => Guard.Run("altar choice", () => Pick(altar, stars, aspect)));
                view.Register<int>(ExtrasRpc, (_, extras) => Guard.Run("altar extras", () => PickExtras(altar, extras)));
            }
        }

        /// <summary>From the altar window: asks the altar's owner to keep these choices.</summary>
        public static void Choose(AltarAspect altar, int stars, int aspect) => Send(altar, PickRpc, stars, aspect);

        /// <summary>From the altar window: asks the altar's owner to keep these extras beside its chosen Bountiful.</summary>
        public static void ChooseExtras(AltarAspect altar, int extras) => Send(altar, ExtrasRpc, extras);

        private static void Send(AltarAspect altar, string rpc, params object[] values)
        {
            ZNetView? view = altar.Bowl != null ? altar.Bowl.m_nview : null;
            if (view != null && view.IsValid())
            {
                view.InvokeRPC(rpc, values);
            }
        }

        // Owner only. A choice the rules do not allow is kept as Random; a part set back to Random rolls at once, so the
        // bowl stops showing the old choice. Extras belong to a chosen Bountiful and go with it.
        private static void Pick(AltarAspect altar, int stars, int aspect)
        {
            ZDO? zdo = OpenZdo(altar);
            if (zdo == null)
            {
                return;
            }
            int keepStars = AltarPicks.AllowsStars(stars) ? stars : AltarPicks.Random;
            int keepAspect = AltarPicks.AllowsAspect(altar.Boss, aspect) ? aspect : AltarPicks.Random;
            bool freshStars = keepStars == AltarPicks.Random && AspectStore.GetPickedStars(zdo) != AltarPicks.Random;
            bool freshAspect = keepAspect == AltarPicks.Random && AspectStore.GetPickedAspect(zdo) != AltarPicks.Random;
            AspectStore.SetPicks(zdo, keepStars, keepAspect);
            if (keepAspect != (int)Aspect.Bountiful)
            {
                AspectStore.SetPickedExtras(zdo, 0);
            }
            AltarRoller.Draw(zdo, altar.Boss, freshStars, freshAspect);
        }

        // Owner only, and only beside a chosen Bountiful: the extras the rules allow are kept and the rest roll again at
        // once, so the bowl shows the new choice.
        private static void PickExtras(AltarAspect altar, int extras)
        {
            ZDO? zdo = OpenZdo(altar);
            if (zdo == null || AltarPicks.AspectOf(zdo, altar.Boss) != Aspect.Bountiful)
            {
                return;
            }
            int keep = AltarPicks.TrimExtras(altar.Boss, extras);
            AspectStore.SetPickedExtras(zdo, keep);
            AltarRoller.RedrawExtras(zdo, altar.Boss, keep);
        }

        // The altar's ZDO while this machine owns it and a choice can still be made there.
        private static ZDO? OpenZdo(AltarAspect altar)
        {
            ZDO? zdo = altar.OwnedZdo();
            return zdo != null && AltarSettings.Enabled && !altar.Offered ? zdo : null;
        }
    }
}
