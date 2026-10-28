using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>A saved ship's hammer entry: its client-side prefab, the blueprint, the point its ghost is placed by, and its cost.</summary>
    public sealed class KwBpEntry
    {
        public KwBpEntry(GameObject prefab, KwBlueprint blueprint)
        {
            Prefab = prefab;
            Blueprint = blueprint;
        }

        public GameObject Prefab { get; }
        public KwBlueprint Blueprint { get; set; }

        /// <summary>In the blueprint's frame: the ship's own origin, on its keel line halfway along (<see cref="KwBpHull.Origin"/>).</summary>
        public Vector3 Anchor => Hull.Origin;

        /// <summary>The hull measured from the blueprint: its footprint and the depth it floats at.</summary>
        public KwBpHull Hull { get; set; } = new KwBpHull();
        public DateTime Written { get; set; }

        /// <summary>
        /// What its icon is kept under (<see cref="KwIconCache"/>): a hash of the file's text, so a ready-made ship, the
        /// same bytes on every machine, finds the icon the mod carries.
        /// </summary>
        public string Source { get; set; } = "";

        /// <summary>It wears its own icon (kept or rendered), not the table's.</summary>
        public bool Iconed { get; set; }

        /// <summary>Its look has been made (<see cref="KwBpGhost"/>) since its file was last read, whether anything drew or not.</summary>
        public bool Looked { get; set; }

        /// <summary>The whole cost (<see cref="KwBpCost"/>), and the part of it the piece's requirements could not show, taken at the click.</summary>
        public List<(string Item, int Amount)> Cost { get; set; } = new List<(string Item, int Amount)>();
        public List<(string Item, int Amount)> Rest { get; set; } = new List<(string Item, int Amount)>();

        /// <summary>The combined meshes its ghost wears (<see cref="KwBpGhost"/>), destroyed when it is made again.</summary>
        public List<Mesh> Meshes { get; } = new List<Mesh>();
    }

    /// <summary>
    /// The hammer entries of the saved ships (<c>KW_bp_&lt;path&gt;</c>, every ship in the ships folder and its
    /// subfolders, named "Ship: " and its path; Misc tab): client-side prefabs made at runtime from the files, kept under an
    /// inactive parent across scenes (so nothing on them ever wakes) and never registered with the network scene. Each is
    /// an empty piece placed like the game's own ships (a water piece, the game's workbench in range, see
    /// <see cref="KwBpWater"/>) that rotates and costs what the ship's parts do (<see cref="KwBpCost"/>), with a 10 cm box
    /// at its anchor (the game places a ghost by its colliders' point nearest the aim) and, once <see cref="KwBpGhost"/>
    /// has made it, the whole ship's mesh, so the ghost shows the ship. Its icon is the one kept from an earlier launch
    /// (<see cref="KwIconCache"/>); else the table's until its look is made and one rendered from it and kept. The look
    /// (about 20 to 200 ms of work, 2026-10-27) is made only when wanted: the hammer holds the ship, or it has no icon
    /// yet (<see cref="WantIcons"/>). The entries are read one a frame once the local player spawns (<see cref="Warm"/>),
    /// so taking up the hammer finds them ready. An entry is made again when its file changes; a gone file's entry goes.
    /// </summary>
    public static class KwBpEntries
    {
        private static readonly Dictionary<string, KwBpEntry> byName = new Dictionary<string, KwBpEntry>();
        private static readonly Dictionary<GameObject, KwBpEntry> byObject = new Dictionary<GameObject, KwBpEntry>();
        private static readonly Dictionary<string, DateTime> unreadable = new Dictionary<string, DateTime>();
        private static GameObject? holder;

        public static KwBpEntry? Of(Piece? piece) => piece != null && byObject.TryGetValue(piece.gameObject, out KwBpEntry entry) ? entry : null;

        public static bool IsEntry(GameObject? prefab) => prefab != null && byObject.ContainsKey(prefab);

        /// <summary>Every readable saved blueprint's entry, in name order.</summary>
        public static List<GameObject> Current()
        {
            IReadOnlyList<string> names = KwBpLibrary.Names();
            foreach (string gone in byName.Keys.Where(n => !names.Contains(n)).ToList())
                Drop(gone);
            List<GameObject> entries = new List<GameObject>();
            foreach (string name in names)
            {
                KwBpEntry? entry = Get(name);
                if (entry != null)
                    entries.Add(entry.Prefab);
            }
            return entries;
        }

        // The entry as its file is now: read again (and its ghost made again) when the file changed since.
        private static KwBpEntry? Get(string name)
        {
            DateTime written = KwBpLibrary.WrittenOf(name);
            byName.TryGetValue(name, out KwBpEntry? entry);
            if (entry != null && entry.Written == written)
                return entry;
            if (unreadable.TryGetValue(name, out DateTime bad) && bad == written)
                return null;
            KwBlueprint? blueprint = KwBpLibrary.Load(name, out string? text);
            if (blueprint == null)
            {
                unreadable[name] = written;
                Drop(name);
                return null;
            }
            entry ??= Make(name, blueprint);
            entry.Source = KwIconCache.Hash(text ?? "");
            Fill(entry, blueprint, written);
            return entry;
        }

        private static KwBpEntry Make(string name, KwBlueprint blueprint)
        {
            GameObject prefab = new GameObject(KwBlueprintStation.EntryPrefix + KwBpLibrary.SafeName(name)) { layer = Mathf.Max(0, LayerMask.NameToLayer("piece")) };
            prefab.transform.SetParent(Holder(), false);
            Piece piece = prefab.AddComponent<Piece>();
            piece.m_name = "Ship: " + name;
            piece.m_icon = KwBlueprintStation.Icon();
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_usage = Piece.UsageTagFlags.Transport;
            piece.m_resources = new Piece.Requirement[0];
            piece.m_canBeRemoved = false;
            ShipRules(piece);
            prefab.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
            KwBpEntry entry = new KwBpEntry(prefab, blueprint);
            byName[name] = entry;
            byObject[prefab] = entry;
            return entry;
        }

        private static void Fill(KwBpEntry entry, KwBlueprint blueprint, DateTime written)
        {
            entry.Blueprint = blueprint;
            entry.Written = written;
            entry.Hull = KwBpHull.Measure(blueprint);
            entry.Cost = KwBpCost.Of(blueprint);
            Piece piece = entry.Prefab.GetComponent<Piece>();
            (piece.m_resources, entry.Rest) = KwBpCost.Split(entry.Cost);
            piece.m_extraPlacementDistance = Reach(entry.Hull);
            piece.m_description =
                $"A saved ship: {blueprint.Pieces.Count} pieces, {entry.Hull.Length:0.#} m long. Built on the water like the game's ships, near a " +
                $"workbench. It costs what its parts do: {KwBpCost.Words(entry.Cost)}. Broken up with the hammer near a workbench, it gives them back in floating crates.";
            Sprite? icon = KwIcons.CanRender ? KwIconCache.Find(entry.Prefab.name, entry.Source) : null;
            entry.Iconed = icon != null;
            piece.m_icon = icon != null ? icon : KwBlueprintStation.Icon();
            // the file changed: the old look goes, and is made again from the new file when wanted
            KwBpGhost.Clear(entry);
        }

        /// <summary>Every entry without its own icon has its look made, and its icon rendered and kept (the hammer was taken up).</summary>
        public static void WantIcons()
        {
            foreach (KwBpEntry entry in byName.Values)
            {
                if (!entry.Iconed)
                    KwBpGhost.Want(entry);
            }
        }

        /// <summary>Every saved ship's entry read and priced, one a frame (from the local player's spawn, <see cref="KwBpWarm"/>).</summary>
        public static IEnumerator Warm()
        {
            foreach (string name in KwBpLibrary.Names().ToList())
            {
                Get(name);
                yield return null;
            }
        }

        // How much further than the game's placing reach a ship's ghost may stand (the game adds a piece's extra placement
        // distance to its reach): about half the ship's length and beam beyond it, so a long ship can be set on the water
        // well clear of the player instead of on top of him (a 31 m ship gets about 22 m more).
        private static int Reach(KwBpHull hull) => Mathf.CeilToInt(hull.Length * 0.6f + hull.Beam * 0.5f);

        // As the game's own ships: placed on water, a workbench in range (the Karve's station).
        private static void ShipRules(Piece piece)
        {
            piece.m_waterPiece = true;
            GameObject? karve = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("Karve") : null;
            piece.m_craftingStation = karve != null ? karve.GetComponent<Piece>().m_craftingStation : null;
        }

        private static void Drop(string name)
        {
            if (!byName.TryGetValue(name, out KwBpEntry entry))
                return;
            byName.Remove(name);
            byObject.Remove(entry.Prefab);
            KwBpGhost.Clear(entry);
            Object.Destroy(entry.Prefab);
        }

        private static Transform Holder()
        {
            if (holder == null)
            {
                holder = new GameObject("KwBlueprintEntries");
                holder.SetActive(false);
                Object.DontDestroyOnLoad(holder);
            }
            return holder.transform;
        }
    }
}
