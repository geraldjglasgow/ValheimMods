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
    /// again. A look is made only when wanted (<see cref="Want"/>: the hammer holds the ship, or it has no icon yet), and
    /// its icon rendered (and kept, <see cref="KwIconCache"/>) only when it has none. Clients only (it runs from the hammer).
    /// </summary>
    public static class KwBpGhost
    {
        private static readonly Queue<KwBpEntry> waiting = new Queue<KwBpEntry>();
        private static KwBpEntry? making;
        private static bool running;

        public static void Queue(KwBpEntry entry)
        {
            if (!waiting.Contains(entry))
                waiting.Enqueue(entry);
            if (running)
                return;
            running = true;
            KwBpRunner.Run("Broadside blueprint ghost", Work(), () => (running, making) = (false, null));
        }

        /// <summary>The entry's look made, unless it is made already or on its way.</summary>
        public static void Want(KwBpEntry entry)
        {
            if (!entry.Looked && entry != making && !waiting.Contains(entry))
                Queue(entry);
        }

        /// <summary>Each frame the hammer is out (<see cref="KwBpWater"/>): the saved ship it holds has its look made.</summary>
        public static void Held(Player player)
        {
            KwBpEntry? held = KwBpEntries.Of(player.GetSelectedPiece());
            if (held != null)
                Want(held);
        }

        /// <summary>Takes the entry's look off and lets its meshes go.</summary>
        public static void Clear(KwBpEntry entry)
        {
            entry.Looked = false;
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
                KwBpEntry entry = making = waiting.Dequeue();
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
                Assemble(entry, set, blueprint);
                making = null;
            }
        }

        private static void Assemble(KwBpEntry entry, KwBpMeshSet set, KwBlueprint blueprint)
        {
            List<(Material Material, Mesh Mesh)> groups = set.Combine(KwBlueprintStation.EntryPrefix + blueprint.Name);
            if (entry.Prefab == null || entry.Blueprint != blueprint)
            {
                // the blueprint's file went, or changed, while its look was being made: made again from the new one when wanted
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
            entry.Looked = true;
            if (!entry.Iconed)
                entry.Iconed = KwIcons.RenderAll(new[] { entry.Prefab }, entry.Source) > 0;
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
