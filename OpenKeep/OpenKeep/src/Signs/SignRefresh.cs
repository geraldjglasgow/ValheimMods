using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>
    /// The refresh tick, on the container's own once-a-second <c>CheckForChanges</c> (which has read the inventory
    /// from the ZDO first), on the client that owns the container's ZDO. A container that may not have a sign loses
    /// the one it has; one without a sign gets one; when the rules changed (cfg or YAML) or the container was
    /// first seen since it loaded, the sign's place is checked and it is placed anew if wrong; when the container's
    /// ZDO data revision moved since the last look (every save of its inventory) the text is rebuilt and written if
    /// the mod may. One throttle per container: after a write nothing is written again for <c>Update Seconds</c>;
    /// the next tick after the wait looks again, so a change during the wait is not lost.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.CheckForChanges))]
    public static class SignRefresh
    {
        private static int rules;

        [HarmonyPostfix]
        public static void Postfix(Container __instance) => Tick(__instance, false);

        /// <summary>A setting or the YAML changed: every container checks its sign's place and text again.</summary>
        public static void RulesChanged() => rules++;

        /// <summary>The console's rewrite: every loaded container the local game owns, throttle ignored, text written afresh.</summary>
        public static int RewriteAll()
        {
            int count = 0;
            foreach (Container container in ContainerScan.All())
            {
                if (Tick(container, true))
                    count++;
            }
            return count;
        }

        /// <summary>The owner's look at one container. True when something was placed, moved, written or removed.</summary>
        public static bool Tick(Container container, bool force)
        {
            if (!Owned(container))
                return false;
            ZDO zdo = container.m_nview.GetZDO();
            if (!SignRules.Allows(container) || SignLinks.NoSign(zdo))
                return SignLinks.SignId(zdo) != ZDOID.None && SignRemover.RemoveFrom(zdo);
            SignState state = SignStates.For(container);
            if (!force && Time.time < state.NextWrite)
                return false;
            bool wrote = Refresh(container, zdo, state, force);
            if (wrote)
                state.NextWrite = Time.time + Mathf.Clamp(SignsSettings.UpdateSeconds.Value, 0.5f, 60f);
            return wrote;
        }

        private static bool Refresh(Container container, ZDO zdo, SignState state, bool force)
        {
            bool checkPlace = state.Rules != rules;
            bool changed = force || checkPlace || zdo.DataRevision != state.Revision;
            state.Rules = rules;
            state.Revision = zdo.DataRevision;
            ZDO sign = SignLinks.LinkedSign(zdo);
            if (sign == null)
                return SignPlacer.Place(container, SignText.Build(container.GetInventory())) != null;
            if (!changed)
                return false;
            if (checkPlace)
                sign = Reposition(container, sign);
            if (!SignWriter.MayWrite(sign))
                return false;
            return SignWriter.Write(sign, SignText.Build(container.GetInventory()), force);
        }

        /// <summary>A loaded sign that is not where the rules put it now is placed anew; an unloaded one waits for its load.</summary>
        private static ZDO Reposition(Container container, ZDO sign)
        {
            ZNetView view = ZNetScene.instance.FindInstance(sign);
            if (view == null || SignPlacement.Matches(view.gameObject, SignPlacement.Compute(container)))
                return sign;
            return SignPlacer.Replace(container, sign);
        }

        /// <summary>Net view valid and owned by this client, inventory created and read from the ZDO, scene up.</summary>
        private static bool Owned(Container container)
        {
            if (container == null || container.m_inventory == null || ZNetScene.instance == null || ZDOMan.instance == null)
                return false;
            ZNetView view = container.m_nview;
            return view != null && view.IsValid() && view.IsOwner() && container.m_lastRevision != uint.MaxValue;
        }
    }
}
