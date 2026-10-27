using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// Makes a blueprint entry's look: the whole ship as one combined mesh per material (<see cref="KwBpMeshSet"/>) under
    /// the entry (the largest as its <c>model</c>, which the hammer icon is rendered from, <see cref="KwIcons"/>), so the
    /// game's placement ghost, a copy of the entry, shows the ship, its sails' cloth too (<see cref="KwSailGhost"/>).
    /// Entries wait in a queue and are made one at a time,
    /// a few pieces a frame (<see cref="KwBpRunner"/>); a ghost already out for an entry that was just made is set up
    /// again. Clients only (it runs from the hammer).
    /// </summary>
    public static class KwBpGhost
    {
        private static readonly Queue<KwBpEntry> waiting = new Queue<KwBpEntry>();
        private static bool running;

        public static void Queue(KwBpEntry entry)
        {
            if (!waiting.Contains(entry))
                waiting.Enqueue(entry);
            if (running)
                return;
            running = true;
            KwBpRunner.Run("Broadside blueprint ghost", Work(), () => running = false);
        }

        /// <summary>Takes the entry's look off and lets its meshes go.</summary>
        public static void Clear(KwBpEntry entry)
        {
            foreach (Mesh mesh in entry.Meshes)
                Object.DestroyImmediate(mesh);
            entry.Meshes.Clear();
            Transform root = entry.Prefab.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static IEnumerator Work()
        {
            while (waiting.Count > 0)
            {
                KwBpEntry entry = waiting.Dequeue();
                KwBlueprint blueprint = entry.Blueprint;
                Vector3 anchor = entry.Anchor;
                KwBpMeshSet set = new KwBpMeshSet { Decks = KwSole.Decks(blueprint), Shut = true, Keels = KwKeelLines.Of(blueprint) };
                KwBpPace pace = new KwBpPace(int.MaxValue);
                foreach (KwBpPiece piece in blueprint.Pieces)
                {
                    set.Add(piece, anchor);
                    if (pace.Spent())
                    {
                        yield return null;
                        pace.Restart();
                    }
                }
                Assemble(entry, set);
            }
        }

        private static void Assemble(KwBpEntry entry, KwBpMeshSet set)
        {
            List<(Material Material, Mesh Mesh)> groups = set.Combine(KwBlueprintStation.EntryPrefix + entry.Blueprint.Name);
            if (entry.Prefab == null)
            {
                // the blueprint's file went while its look was being made
                groups.ForEach(g => Object.Destroy(g.Mesh));
                return;
            }
            Clear(entry);
            for (int i = 0; i < groups.Count; i++)
            {
                entry.Meshes.Add(groups[i].Mesh);
                Child(entry.Prefab, i == 0 ? "model" : "model" + i, groups[i].Mesh, new[] { groups[i].Material }, Matrix4x4.identity);
            }
            foreach ((Mesh mesh, Material[] materials, Matrix4x4 at) in set.Loose)
                Child(entry.Prefab, "part", mesh, materials, at);
            KwSailGhost.Dress(entry.Prefab, entry.Blueprint, entry.Anchor, entry.Meshes);
            KwIcons.RenderAll(new[] { entry.Prefab });
            Reghost(entry);
        }

        private static void Child(GameObject parent, string name, Mesh mesh, Material[] materials, Matrix4x4 at)
        {
            GameObject child = new GameObject(name) { layer = parent.layer };
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = at.GetColumn(3);
            child.transform.localRotation = at.rotation;
            child.transform.localScale = at.lossyScale;
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        // The local player holds this entry in the hammer: its ghost is made again with the new look.
        private static void Reghost(KwBpEntry entry)
        {
            Player player = Player.m_localPlayer;
            Piece? selected = player != null && player.InPlaceMode() ? player.GetSelectedPiece() : null;
            if (selected != null && selected.gameObject == entry.Prefab)
                player!.SetupPlacementGhost();
        }
    }
}
