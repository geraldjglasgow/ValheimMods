using System.Collections.Generic;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;
using HarmonyLib;

namespace EliteCreaturesPack.Custom.Saves
{
    /// <summary>
    /// Custom creatures whose definition is gone (removed, renamed, switched off, or failed to build) stay in the save
    /// untouched and come back when it returns (features/custom-creatures.md section 7). The game's server deletes every
    /// ZDO whose prefab it does not know the moment it tries to make it (<c>ZNetScene.CreateObjectsSorted</c> and
    /// <c>CreateDistantObjects</c>: "Destroyed invalid prefab ZDO", after taking it over), and a client warns about it on
    /// every pass ("Missing prefab hash"). Just before each pass, such a ZDO that carries <see cref="CustomTag.Key"/> is
    /// parked: marked as made (<c>ZDO.Created</c>, a flag kept only in memory and never saved), so the game skips it
    /// without taking it over, deleting it or warning. A warning names each such creature once per session. Nothing
    /// else is touched; the next session (with its definition back) makes it as usual.
    /// <para>
    /// The server parks always, since nothing it does not know at its first pass can become known later in the session.
    /// A client parks only once it has built from the server's files (<see cref="BuildTiming.Settled"/>): before that, an
    /// unknown prefab may be one that is about to be registered.
    /// </para>
    /// <para>
    /// Cost: one pass over the lists the game itself walks just after, 30 times a second; for a ZDO already made that is
    /// one flag test, and only a ZDO not yet made costs a prefab lookup. The ZDO's string is read only for an unknown
    /// prefab.
    /// </para>
    /// </summary>
    internal static class ParkedCreatures
    {
        private static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>A new world: each removed creature is named again, once.</summary>
        public static void Reset() => warned.Clear();

        /// <summary>Whether this peer parks now.</summary>
        public static bool Active => ZNet.instance != null && (ZNet.instance.IsServer() || BuildTiming.Settled);

        public static void Sweep(ZNetScene scene, List<ZDO> zdos)
        {
            for (int i = 0; i < zdos.Count; i++)
            {
                ZDO zdo = zdos[i];
                if (zdo.Created)
                {
                    continue;
                }
                int prefab = zdo.GetPrefab();
                if (prefab != 0 && !scene.HasPrefab(prefab))
                {
                    Consider(zdo);
                }
            }
        }

        private static void Consider(ZDO zdo)
        {
            string name = zdo.GetString(CustomTag.KeyHash);
            if (name.Length == 0)
            {
                return;
            }
            zdo.Created = true;
            if (warned.Add(name))
            {
                Log.Warn($"Custom creature '{name}' has no definition here (removed, renamed, switched off or left out): "
                    + "its creatures stay in the world, untouched and unseen, and come back when its definition does.");
            }
        }
    }

    /// <summary>Parks unknown custom creatures before the game's create pass (<see cref="ParkedCreatures"/>).</summary>
    [HarmonyPatch(typeof(ZNetScene), "CreateObjects")]
    internal static class ParkedCreaturesPatch
    {
        private static void Prefix(ZNetScene __instance, List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
        {
            if (!ParkedCreatures.Active)
            {
                return;
            }
            SafeCall.Run("custom creatures parking", static (scene, near, distant) =>
            {
                ParkedCreatures.Sweep(scene, near);
                ParkedCreatures.Sweep(scene, distant);
            }, __instance, currentNearObjects, currentDistantObjects);
        }
    }
}
