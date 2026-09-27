using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Cooking experience for tapping a fermenter, which the game gives nothing for. Fermenter.Interact runs on the
    /// tapping player's client: with ward access (PrivateArea.CheckAccess) and the barrel Ready it sends RPC_Tap to
    /// the owner, who empties the barrel and spawns the meads, and returns true. The prefix reads the ready barrel
    /// before the tap empties it (at once when the tapper owns it); the postfix credits the tapper when Interact
    /// reports success. Each brew (the barrel's ZDO and its start time) is credited once: a remote owner's emptying
    /// reaches the tapper a moment later, and a second press in between would otherwise count again.
    /// </summary>
    public static class FermenterXp
    {
        /// <summary>A ready barrel as the prefix saw it.</summary>
        public sealed class Tap
        {
            public ZDOID Zdo;
            public long Start;
            public ItemDrop Mead;
        }

        private static ZDOID lastZdo = ZDOID.None;
        private static long lastStart;

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.Interact))]
        private static class InteractPatch
        {
            [HarmonyPrefix]
            private static void Prefix(Fermenter __instance, Humanoid user, bool hold, out Tap __state) =>
                __state = hold ? null : ReadyTap(__instance, user);

            [HarmonyPostfix]
            private static void Postfix(bool __result, Tap __state)
            {
                if (__result && __state != null)
                    Credit(__state);
            }
        }

        private static Tap ReadyTap(Fermenter fermenter, Humanoid user)
        {
            if (ExperienceSettings.FermenterTap.Value <= 0f || user == null || user != Player.m_localPlayer)
                return null;
            if (fermenter.m_nview == null || !fermenter.m_nview.IsValid() || fermenter.GetStatus() != Fermenter.Status.Ready)
                return null;
            if (!PrivateArea.CheckAccess(fermenter.transform.position, 0f, flash: false))
                return null;
            Fermenter.ItemConversion conversion = fermenter.GetItemConversion(fermenter.GetContent());
            ZDO zdo = fermenter.m_nview.GetZDO();
            Tap tap = new Tap { Zdo = zdo.m_uid, Start = zdo.GetLong(ZDOVars.s_startTime), Mead = conversion?.m_to };
            return tap.Mead == null || (tap.Zdo == lastZdo && tap.Start == lastStart) ? null : tap;
        }

        /// <summary>Fermenter Tap Experience times the Experience Multiplier and the mead's tier.</summary>
        private static void Credit(Tap tap)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            lastZdo = tap.Zdo;
            lastStart = tap.Start;
            float tier = XpScaling.Tier(tap.Mead.m_itemData);
            XpScaling.RaiseScaled(player, ExperienceSettings.FermenterTap.Value * XpScaling.Multiplier * tier);
        }
    }
}
