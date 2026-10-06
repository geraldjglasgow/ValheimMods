using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Stage;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Swap
{
    /// <summary>
    /// One object that draws a swapped prefab: the prefab itself, a copy of it (in the world, on the stage, the build
    /// ghost), or an item's visual made from one of the item prefab's children (attach, attach_back, attach_skin,
    /// attach_&lt;bone&gt; on a VisEquipment; attach or attach/attachobj on an item stand).
    /// </summary>
    internal sealed class Target
    {
        internal GameObject Root;

        /// <summary>The prefab's root, whose paths this object repeats below its prefix.</summary>
        internal Transform Prefab;

        /// <summary>Where this object sits in the prefab: "" for the prefab and its copies, the child's name for an item visual.</summary>
        internal string Prefix = "";

        /// <summary>Where bones are looked up by name: the object, or the character wearing the item.</summary>
        internal Transform BoneRoot;

        /// <summary>The wearer's body, for an item worn on the skin: its meshes ride the body's bones.</summary>
        internal SkinnedMeshRenderer Body;

        internal string Kind;

        internal bool IsPrefab => Kind == Targets.PrefabKind;

        /// <summary>The node at a path in the prefab, in this object; null when the path is not part of it.</summary>
        internal Transform Find(string prefabPath)
        {
            if (Prefix.Length == 0) return Paths.Find(Root.transform, prefabPath);
            if (prefabPath == Prefix) return Root.transform;
            return prefabPath.StartsWith(Prefix + "/", StringComparison.Ordinal) ? Paths.Find(Root.transform, prefabPath.Substring(Prefix.Length + 1)) : null;
        }

        /// <summary>The path in the prefab of one of this object's nodes.</summary>
        internal string PrefabPath(Transform node)
        {
            string own = Paths.Of(node, Root.transform);
            return Prefix.Length == 0 ? own : own.Length == 0 ? Prefix : Prefix + "/" + own;
        }

        /// <summary>The bones a renderer of the prefab used, in this object: the wearer's body's for a skin item, else by path, else by name.</summary>
        internal Transform[] Bones(Transform[] prefabBones) => Body ? Body.bones : (prefabBones ?? new Transform[0]).Select(Bone).ToArray();

        internal Transform RootBone(Transform prefabRoot) => Body ? Body.rootBone : Bone(prefabRoot);

        private Transform Bone(Transform prefabBone)
        {
            if (!prefabBone) return null;
            Transform here = Find(Paths.Of(prefabBone, Prefab));
            return here ? here : Paths.Named(BoneRoot, prefabBone.name);
        }
    }

    /// <summary>
    /// Everything that draws a prefab on this machine: the prefab, its live ZNetScene copies, the stage's still copies of
    /// it (/lineup, or a creature wearing a /place asset), the build ghost of a piece, and for an item the visuals worn
    /// and those shown on item stands. Each is a copy of the prefab or of one of its children.
    /// </summary>
    internal static class Targets
    {
        internal const string PrefabKind = "prefab";
        internal const string InstanceKind = "instance";
        internal const string StageKind = "stage";
        internal const string GhostKind = "ghost";
        internal const string WornKind = "worn";

        internal static List<Target> Of(SwapEntry entry)
        {
            var all = new List<Target> { Make(entry, entry.Prefab, entry.Prefab.transform, PrefabKind) };
            all.AddRange(Instances(entry));
            all.AddRange(Staged(entry));
            all.AddRange(Ghost(entry));
            if (!entry.Prefab.GetComponent<ItemDrop>()) return all;
            all.AddRange(Worn(entry));
            all.AddRange(Stands(entry));
            return all;
        }

        /// <summary>
        /// What may have started drawing the prefab since the last sweep, found without walking the world: the stage's
        /// still copies, the build ghost, and the copies <see cref="NewCopies"/> caught as the game made them.
        /// </summary>
        internal static List<Target> Fresh(SwapEntry entry)
        {
            var fresh = new List<Target>();
            fresh.AddRange(Staged(entry));
            fresh.AddRange(Ghost(entry));
            fresh.AddRange(NewCopies.Of(entry));
            return fresh;
        }

        private static Target Make(SwapEntry entry, GameObject root, Transform bones, string kind) =>
            new Target { Root = root, Prefab = entry.Prefab.transform, BoneRoot = bones, Kind = kind };

        internal static Target Instance(SwapEntry entry, ZNetView view) => Make(entry, view.gameObject, view.transform, InstanceKind);

        /// <summary>The networked copies this machine has loaded, by the prefab hash in their ZDO.</summary>
        private static IEnumerable<Target> Instances(SwapEntry entry)
        {
            if (!ZNetScene.instance) yield break;
            foreach (KeyValuePair<ZDO, ZNetView> pair in ZNetScene.instance.m_instances)
            {
                if (pair.Key != null && pair.Value && pair.Key.GetPrefab() == entry.Hash) yield return Instance(entry, pair.Value);
            }
        }

        /// <summary>The stage's still copies of the prefab: a game prefab in the row, or the creature wearing a placed asset.</summary>
        private static IEnumerable<Target> Staged(SwapEntry entry) =>
            Placements.All.Where(p => p.Alive && p.Root.name == entry.PrefabName && (p.Kind == Placement.Game || p.Spec?.On != null))
                .Select(p => Make(entry, p.Root, p.Root.transform, StageKind)).ToList();

        /// <summary>The local player's build ghost, a copy of the selected piece named after it.</summary>
        private static IEnumerable<Target> Ghost(SwapEntry entry)
        {
            GameObject ghost = Player.m_localPlayer ? Player.m_localPlayer.m_placementGhost : null;
            if (ghost && ghost.name == entry.PrefabName) yield return Make(entry, ghost, ghost.transform, GhostKind);
        }

        /// <summary>
        /// Item stands showing the item: ItemStand.SetVisualItem shows a copy of the item's attach child (of
        /// attach/attachobj when it has one), so its paths are the prefab's below that child.
        /// </summary>
        private static IEnumerable<Target> Stands(SwapEntry entry)
        {
            string prefix = StandPrefix(entry);
            if (prefix == null) yield break;
            foreach (ItemStand stand in Object.FindObjectsByType<ItemStand>(FindObjectsSortMode.None).Where(s => s.m_visualHash == entry.Hash && s.m_visualItem))
                yield return OnStand(entry, stand, prefix);
        }

        /// <summary>Where a stand's visual of the item sits in the item prefab; null when the item cannot go on a stand.</summary>
        internal static string StandPrefix(SwapEntry entry)
        {
            GameObject attach = ItemStand.GetAttachPrefab(entry.Prefab);
            return attach ? Paths.Of(ItemStand.GetAttachGameObject(attach).transform, entry.Prefab.transform) : null;
        }

        internal static Target OnStand(SwapEntry entry, ItemStand stand, string prefix)
        {
            Target target = Make(entry, stand.m_visualItem, stand.m_visualItem.transform, WornKind);
            target.Prefix = prefix;
            return target;
        }

        /// <summary>
        /// Item visuals worn by any character (players, creatures, the stage's still copies): each a copy of one of the
        /// item prefab's children, named after it, so its paths are the prefab's below that child's name.
        /// </summary>
        private static IEnumerable<Target> Worn(SwapEntry entry)
        {
            foreach (VisEquipment wearer in Object.FindObjectsByType<VisEquipment>(FindObjectsSortMode.None))
            {
                foreach (GameObject item in Items(wearer, entry.Hash).Where(i => i)) yield return WornItem(entry, wearer, item);
            }
        }

        internal static Target WornItem(SwapEntry entry, VisEquipment wearer, GameObject item)
        {
            Target target = Make(entry, item, wearer.transform, WornKind);
            target.Prefix = Utils.GetPrefabName(item.name);
            if (target.Prefix == "attach_skin") target.Body = wearer.m_bodyModel;
            return target;
        }

        /// <summary>The visuals the wearer made from the item with this hash, in every slot.</summary>
        private static IEnumerable<GameObject> Items(VisEquipment w, int hash)
        {
            var slots = new (int Hash, IEnumerable<GameObject> Items)[]
            {
                (w.m_currentRightItemHash, new[] { w.m_rightItemInstance }), (w.m_currentLeftItemHash, new[] { w.m_leftItemInstance }),
                (w.m_currentRightBackItemHash, new[] { w.m_rightBackItemInstance }), (w.m_currentLeftBackItemHash, new[] { w.m_leftBackItemInstance }),
                (w.m_currentHelmetItemHash, new[] { w.m_helmetItemInstance }), (w.m_currentBeardItemHash, new[] { w.m_beardItemInstance }),
                (w.m_currentHairItemHash, new[] { w.m_hairItemInstance }), (w.m_currentChestItemHash, w.m_chestItemInstances),
                (w.m_currentLegItemHash, w.m_legItemInstances), (w.m_currentShoulderItemHash, w.m_shoulderItemInstances),
                (w.m_currentUtilityItemHash, w.m_utilityItemInstances), (w.m_currentTrinketItemHash, w.m_trinketItemInstances),
            };
            return slots.Where(s => s.Hash == hash && s.Items != null).SelectMany(s => s.Items);
        }
    }
}
