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
    /// destroyed, and so does a new entry once the table has doubled since the last prune (<see cref="PruneMark"/>; a
    /// dedicated server may never call <see cref="All"/>). The table is emptied when the network scene goes (the world
    /// is left). "Inventory read at least once" is the game's own marker <c>m_lastRevision</c>: <c>Load</c> sets it
    /// on its first run whether or not the ZDO carried items, so a fresh container without items counts as loaded.
    /// Each tracked container keeps its <see cref="ContainerFacts"/> (prefab name, ship, cart, private chest), found
    /// on first use; nothing that can change at runtime (access, ward, in use, settings) is kept.
    /// </remarks>
    public static class ContainerScan
    {
        private static readonly Dictionary<Container, ContainerFacts> loaded = new Dictionary<Container, ContainerFacts>();
        private static readonly List<Container> walk = new List<Container>();
        private static readonly List<KeyValuePair<float, Container>> found = new List<KeyValuePair<float, Container>>();
        private static readonly Comparison<KeyValuePair<float, Container>> Nearest = (a, b) => a.Key.CompareTo(b.Key);
        private static readonly PruneMark pruneMark = new PruneMark(256);

        /// <summary>How many containers registered so far; a list kept a while compares it to see that one woke since.</summary>
        public static int Registered { get; private set; }

        /// <summary>Every usable container within range of a position, nearest first. Never throws.</summary>
        public static List<Container> Nearby(Vector3 position, float range, ContainerUse use)
        {
            return Nearby(point => (position - point).sqrMagnitude, range, container => IsUsable(container, use));
        }

        /// <summary>The usable containers among <paramref name="among"/> within range of a position, nearest first. Never throws.</summary>
        public static List<Container> Nearby(IEnumerable<Container> among, Vector3 position, float range, ContainerUse use)
        {
            walk.Clear();
            walk.AddRange(among);
            return Closest(point => (position - point).sqrMagnitude, range, container => IsUsable(container, use));
        }

        /// <summary>Every usable container whose position lies within range of a box (0 inside it), nearest first. Never throws.</summary>
        public static List<Container> Nearby(Bounds area, float range, ContainerUse use)
        {
            return Nearby(area.SqrDistance, range, container => IsUsable(container, use));
        }

        /// <summary>
        /// Every container within range of a position that the rules allow (<see cref="IsAllowed"/>), nearest first,
        /// whether or not it is ready this moment: for a list kept a short while and narrowed by
        /// <see cref="IsReady"/> each time it is used. Never throws.
        /// </summary>
        public static List<Container> Allowed(Vector3 position, float range)
        {
            return Nearby(point => (position - point).sqrMagnitude, range, IsAllowed);
        }

        /// <summary>Walks a reused copy of the registry, since the usability test may record a container's facts in it.</summary>
        private static List<Container> Nearby(Func<Vector3, float> squaredDistanceTo, float range, Func<Container, bool> usable)
        {
            walk.Clear();
            Snapshot(walk);
            return Closest(squaredDistanceTo, range, usable);
        }

        /// <summary>The containers in the walk list within range, nearest first, by squared distance (no square root per
        /// container); empties the walk list.</summary>
        private static List<Container> Closest(Func<Vector3, float> squaredDistanceTo, float range, Func<Container, bool> usable)
        {
            float reach = range * range;
            found.Clear();
            foreach (Container container in walk)
                Consider(container, squaredDistanceTo, reach, usable);
            walk.Clear();
            found.Sort(Nearest);
            List<Container> result = new List<Container>(found.Count);
            foreach (KeyValuePair<float, Container> pair in found)
                result.Add(pair.Value);
            found.Clear();
            return result;
        }

        private static void Consider(Container container, Func<Vector3, float> squaredDistanceTo, float reach, Func<Container, bool> usable)
        {
            try
            {
                if (container == null)
                    return;
                float squared = squaredDistanceTo(container.transform.position);
                if (squared <= reach && usable(container))
                    found.Add(new KeyValuePair<float, Container>(squared, container));
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"OpenKeep: container skipped, {e.GetType().Name}: {e.Message}");
            }
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
        /// from the ZDO, valid net view, not in use by another player, and for ship and cart storage the local client
        /// owning the vehicle (only the vehicle's owner may change it).</summary>
        public static bool IsReady(Container container) =>
            IsLoaded(container) && !InUseByAnother(container) && !ContainerClaim.VehicleOfAnother(container);

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
            Snapshot(live);
            return live;
        }

        /// <summary>Fills the list with every live tracked container and drops the destroyed ones from the registry.</summary>
        private static void Snapshot(List<Container> live)
        {
            bool dead = false;
            foreach (Container container in loaded.Keys)
            {
                if (container != null)
                    live.Add(container);
                else
                    dead = true;
            }
            if (dead)
                Prune();
        }

        /// <summary>Drops the destroyed and unloaded containers.</summary>
        private static void Prune()
        {
            List<Container> gone = new List<Container>();
            foreach (Container container in loaded.Keys)
            {
                if (container == null)
                    gone.Add(container);
            }
            foreach (Container container in gone)
                loaded.Remove(container);
            pruneMark.Pruned(loaded.Count);
        }

        /// <summary>The world was left: nothing of it stays tracked.</summary>
        internal static void Forget() => loaded.Clear();

        /// <summary>
        /// Makes the local client the container's owner and reads the latest inventory data from the ZDO; true when it
        /// owns the container afterwards (<see cref="ContainerClaim"/>: one nobody owns is claimed the way the game claims
        /// a free object, one another client owns is asked for and used on a later call, ship and cart storage only
        /// while the local client owns the vehicle). A container another player is using is never claimed.
        /// </summary>
        public static bool Claim(Container container) => ContainerClaim.Claim(container, HandOver.ActionSeconds);

        /// <summary><see cref="Claim(Container)"/> for background work, which asks another client for the container at
        /// most once per <paramref name="askEvery"/> seconds.</summary>
        public static bool Claim(Container container, float askEvery) => ContainerClaim.Claim(container, askEvery);

        /// <summary>The local client owns the container or may claim it this moment, so a change can be made at once.</summary>
        public static bool CanClaimNow(Container container) => ContainerClaim.CanClaimNow(container);

        /// <summary>
        /// A container another client owns and nobody is using, every other rule passing: it is changed only through
        /// that client, by request (<c>Shared.ChestWriter</c>) or once that client hands it over (<see cref="Claim(Container)"/>).
        /// </summary>
        public static bool IsRemote(Container container)
        {
            if (!IsLoaded(container) || InUseByAnother(container) || !ContainerClaim.OwnedElsewhere(container))
                return false;
            return PassesRules(container);
        }

        /// <summary>
        /// Persists a changed inventory through the game's own path: the inventory's change callback, which makes
        /// the owning container write the ZDO. Only the owner can save; call <see cref="Claim(Container)"/> first.
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
            if (pruneMark.Due(loaded.Count))
                Prune();
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

    /// <summary>The network scene goes when the world is left (client and server): the tracked containers are forgotten.</summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OnDestroy))]
    public static class ContainerSceneGonePatch
    {
        [HarmonyPostfix]
        public static void Postfix() => ContainerScan.Forget();
    }
}
