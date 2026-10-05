using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Core
{
    /// <summary>
    /// The loaded containers and the section 0 rules for using them: discovery by range, the game's access checks,
    /// ownership claim before a change and the game's save path after it.
    /// </summary>
    /// <remarks>
    /// Tracking: a postfix on <c>Container.Awake</c> adds every container whose net view had a ZDO (the only case in
    /// which the game creates its inventory). <c>Container</c> has no Unity <c>OnDestroy</c>, so destroyed and
    /// unloaded containers are pruned lazily: every call of <see cref="All"/> drops the entries Unity reports as
    /// destroyed. "Inventory read at least once" is the game's own marker <c>m_lastRevision</c>: <c>Load</c> sets it
    /// on its first run whether or not the ZDO carried items, so a fresh container without items counts as loaded.
    /// Each tracked container keeps its <see cref="ContainerFacts"/> (prefab name, ship, cart, private chest), found
    /// on first use; nothing that can change at runtime (access, ward, in use, settings) is kept.
    /// </remarks>
    public static class ContainerScan
    {
        private static readonly Dictionary<Container, ContainerFacts> loaded = new Dictionary<Container, ContainerFacts>();

        /// <summary>How many containers registered so far; a list kept a while compares it to see that one woke since.</summary>
        public static int Registered { get; private set; }

        /// <summary>Every usable container within range of a position, nearest first. Never throws.</summary>
        public static List<Container> Nearby(Vector3 position, float range, ContainerUse use)
        {
            return Nearby(point => Vector3.Distance(position, point), range, container => IsUsable(container, use));
        }

        /// <summary>Every usable container whose position lies within range of a box (0 inside it), nearest first. Never throws.</summary>
        public static List<Container> Nearby(Bounds area, float range, ContainerUse use)
        {
            return Nearby(point => Mathf.Sqrt(area.SqrDistance(point)), range, container => IsUsable(container, use));
        }

        /// <summary>
        /// Every container within range of a position that the rules allow (<see cref="IsAllowed"/>), nearest first,
        /// whether or not it is ready this moment: for a list kept a short while and narrowed by
        /// <see cref="IsReady"/> each time it is used. Never throws.
        /// </summary>
        public static List<Container> Allowed(Vector3 position, float range)
        {
            return Nearby(point => Vector3.Distance(position, point), range, IsAllowed);
        }

        private static List<Container> Nearby(Func<Vector3, float> distanceTo, float range, Func<Container, bool> usable)
        {
            List<KeyValuePair<float, Container>> found = new List<KeyValuePair<float, Container>>();
            foreach (Container container in All())
            {
                try
                {
                    float distance = distanceTo(container.transform.position);
                    if (distance <= range && usable(container))
                        found.Add(new KeyValuePair<float, Container>(distance, container));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogDebug($"OpenKeep: container skipped, {e.GetType().Name}: {e.Message}");
                }
            }
            found.Sort((a, b) => a.Key.CompareTo(b.Key));
            return found.ConvertAll(pair => pair.Value);
        }

        /// <summary>
        /// Usable: inventory created and read from the ZDO at least once, valid net view, not in use by another
        /// player (the container the local client owns counts), the game's privacy
        /// check, the ward check when <see cref="CoreSettings.HonourWards"/> is on, the prefab enabled in
        /// <see cref="ContainerRules"/> and the section 0 switches for ships, carts and the private chest.
        /// A chest another player is using is never usable, in every <c>Shared Chests</c> mode: usable means
        /// changeable at once, and such a chest can only be changed through the Shared module's requests.
        /// </summary>
        public static bool IsUsable(Container container, ContainerUse use)
        {
            return IsReady(container) && PassesRules(container);
        }

        /// <summary>The part of <see cref="IsUsable"/> that changes from moment to moment and is cheap: inventory read
        /// from the ZDO, valid net view, not in use by another player.</summary>
        public static bool IsReady(Container container) => IsLoaded(container) && !InUseByAnother(container);

        /// <summary>The rest of <see cref="IsUsable"/>: inventory created, valid net view, then the switches, the prefab
        /// table and the access checks (privacy and the ward scan, the slow part).</summary>
        public static bool IsAllowed(Container container)
        {
            if (container == null || container.m_inventory == null || container.m_nview == null || !container.m_nview.IsValid())
                return false;
            return PassesRules(container);
        }

        /// <summary>
        /// A chest the Shared module may change through requests: <c>Shared Chests</c> is <c>Full</c>, another
        /// player is using it, and every other usability rule (loaded, access, ward, switches, prefab) passes.
        /// </summary>
        public static bool IsShared(Container container)
        {
            if (CoreSettings.SharedChests.Value != SharedMode.Full)
                return false;
            if (!IsLoaded(container) || !InUseByAnother(container))
                return false;
            return PassesRules(container);
        }

        /// <summary>
        /// Mirrors the game's open request: the owner may always use its own container (only it can have it open),
        /// another client is refused while the in-use flag is set in the ZDO or the cart is in use.
        /// </summary>
        public static bool InUseByAnother(Container container)
        {
            ZNetView view = container != null ? container.m_nview : null;
            if (view == null || !view.IsValid() || view.IsOwner())
                return false;
            if (container.m_wagon != null && container.m_wagon.InUse())
                return true;
            return view.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
        }

        /// <summary>Every loaded container, no usability filter. A fresh list each call; destroyed entries are dropped.</summary>
        public static IReadOnlyCollection<Container> All()
        {
            List<Container> live = new List<Container>(loaded.Count);
            List<Container> gone = null;
            foreach (Container container in loaded.Keys)
            {
                if (container != null)
                    live.Add(container);
                else
                    (gone ?? (gone = new List<Container>())).Add(container);
            }
            if (gone != null)
                gone.ForEach(container => loaded.Remove(container));
            return live;
        }

        /// <summary>
        /// Claims ownership of the container's ZDO for the local client the way the game does after an open or a
        /// take-all was granted (<c>ZNetView.ClaimOwnership</c>, then the ZDO is force sent to the previous owner),
        /// and reads the latest inventory data from the ZDO. True when the local client owns it afterwards.
        /// A container another player is using is never claimed (the game never hands such a chest over either).
        /// </summary>
        public static bool Claim(Container container)
        {
            ZNetView view = container != null ? container.m_nview : null;
            if (view == null || !view.IsValid() || container.m_inventory == null || InUseByAnother(container))
                return false;
            if (!view.IsOwner())
            {
                ZDO zdo = view.GetZDO();
                long previous = zdo.GetOwner();
                view.ClaimOwnership();
                if (previous != 0L && ZDOMan.instance != null)
                    ZDOMan.instance.ForceSendZDO(previous, zdo.m_uid);
            }
            if (!view.IsOwner())
                return false;
            container.Load();
            return true;
        }

        /// <summary>
        /// Persists a changed inventory through the game's own path: the inventory's change callback, which makes
        /// the owning container write the ZDO. Only the owner can save; call <see cref="Claim"/> first.
        /// </summary>
        public static void Save(Container container)
        {
            if (container == null || container.m_inventory == null || container.m_nview == null || !container.m_nview.IsValid())
                return;
            if (container.m_nview.IsOwner())
                container.m_inventory.Changed();
            else
                Plugin.Log.LogWarning($"OpenKeep: {PrefabName(container)} was changed without owning it; the change may not persist");
        }

        /// <summary>The prefab name of the container's net object (the ship or cart for their storage), without "(Clone)".</summary>
        public static string PrefabName(Container container) => container == null ? "" : FactsOf(container).PrefabName;

        public static bool IsShip(Container c) => c != null && FactsOf(c).IsShip;

        public static bool IsCart(Container c) => c != null && FactsOf(c).IsCart;

        public static bool IsPrivateChest(Container c) => c != null && FactsOf(c).IsPrivateChest;

        internal static void Track(Container container)
        {
            if (container == null || container.m_inventory == null || loaded.ContainsKey(container))
                return;
            loaded.Add(container, null);
            Registered++;
        }

        /// <summary>The container's fixed facts: found once and kept for a tracked container, found afresh for any other.</summary>
        private static ContainerFacts FactsOf(Container container)
        {
            if (!loaded.TryGetValue(container, out ContainerFacts facts))
                return new ContainerFacts(container);
            if (facts == null)
            {
                facts = new ContainerFacts(container);
                loaded[container] = facts;
            }
            return facts;
        }

        /// <summary>Inventory created, net view valid, inventory read from the ZDO at least once.</summary>
        private static bool IsLoaded(Container container)
        {
            if (container == null || container.m_inventory == null)
                return false;
            ZNetView view = container.m_nview;
            return view != null && view.IsValid() && container.m_lastRevision != uint.MaxValue;
        }

        /// <summary>The rules that do not depend on who is using the container: switches, prefab, access.</summary>
        private static bool PassesRules(Container container)
        {
            if (!SwitchAllows(container) || !ContainerRules.IsEnabled(PrefabName(container)))
                return false;
            return HasAccess(container);
        }

        private static bool SwitchAllows(Container container)
        {
            if (IsShip(container))
                return CoreSettings.Ships.Value;
            if (IsCart(container))
                return CoreSettings.Carts.Value;
            if (IsPrivateChest(container))
                return CoreSettings.PlayerChests.Value;
            return true;
        }

        /// <summary>The checks of <c>Container.Interact</c>: the ward, then the privacy setting for the local player.</summary>
        private static bool HasAccess(Container container)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return false;
            if (CoreSettings.HonourWards.Value && container.m_checkGuardStone
                && !PrivateArea.CheckAccess(container.transform.position, 0f, false))
                return false;
            if (container.m_privacy == Container.PrivacySetting.Private && container.m_piece == null)
                return false;
            return container.CheckAccess(player.GetPlayerID());
        }
    }

    /// <summary>Registers every container whose Awake created an inventory (net view with a ZDO).</summary>
    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    public static class ContainerAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Container __instance) => ContainerScan.Track(__instance);
    }
}
