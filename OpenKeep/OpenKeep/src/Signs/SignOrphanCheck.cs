using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>
    /// A loading automatic sign whose container is gone, or whose container points at another sign, is destroyed by
    /// whoever owns the sign. The check waits ten seconds after the sign's <c>Awake</c> and repeats every five while
    /// the sign is not owned here, because a client receives the ZDOs of an area over several frames and the
    /// container's may arrive after the sign's; a dedicated server keeps every ZDO but instantiates only objects
    /// near the world origin, so the check cannot be left to it. The same <c>Sign.Awake</c> postfix switches the
    /// loaded sign's wear off (<see cref="SignWear"/>).
    /// </summary>
    public sealed class SignOrphanCheck : MonoBehaviour
    {
        private const float FirstCheck = 10f;
        private const float Repeat = 5f;
        private ZNetView view;

        private void Start()
        {
            view = GetComponent<ZNetView>();
            InvokeRepeating(nameof(Check), FirstCheck, Repeat);
        }

        private void Check()
        {
            if (view == null || !view.IsValid() || ZDOMan.instance == null || ZNetScene.instance == null)
            {
                Destroy(this);
                return;
            }
            ZDO zdo = view.GetZDO();
            ZDOID containerId = SignLinks.ContainerId(zdo);
            ZDO container = containerId != ZDOID.None ? ZDOMan.instance.GetZDO(containerId) : null;
            if (containerId == ZDOID.None || (container != null && SignLinks.SignId(container) == zdo.m_uid))
            {
                Destroy(this);
                return;
            }
            if (!view.IsOwner())
                return;
            Plugin.Log.LogInfo($"OpenKeep: sign {zdo.m_uid} has no container any more ({containerId}); removed");
            ZNetScene.instance.Destroy(gameObject);
        }

        [HarmonyPatch(typeof(Sign), nameof(Sign.Awake))]
        private static class AwakePatch
        {
            [HarmonyPostfix]
            private static void Postfix(Sign __instance)
            {
                ZNetView view = __instance.GetComponent<ZNetView>();
                if (view == null || !view.IsValid() || !SignLinks.IsAutomatic(view.GetZDO()))
                    return;
                SignWear.Protect(__instance.gameObject);
                if (__instance.GetComponent<SignOrphanCheck>() == null)
                    __instance.gameObject.AddComponent<SignOrphanCheck>();
            }
        }
    }
}
