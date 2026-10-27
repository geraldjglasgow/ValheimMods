using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's patched holes (the user, 2026-10-26: "It repairs ship hulls; the whole idea is to stop the water
    /// seeping in and sinking the ship further"), on every peer. A player with a Wooden Repair Kit (<see cref="KwRepairKit"/>)
    /// asks the ship's owner to patch a hole (<see cref="Ask"/>, <see cref="PatchRpc"/>); the owner keeps it in the ship's ZDO
    /// (<see cref="KwHullPatchText"/>: each patched hole's own number, <see cref="KwHullHole.Id"/>, and its radius when
    /// patched) and tells the asker, whose game then takes one kit (<see cref="PatchedRpc"/>). A patched hole lets no sea in
    /// (<see cref="Leaking"/>, read by <see cref="KwShipFlood"/> through <see cref="KwHullHoles.Leaking"/>) and stays a hole:
    /// the ship's health is not given back, and only the hammer's repair mends it (<see cref="KwShipRepair"/>). A patch is
    /// off when its hole is mended or widened past it by a new hit: every peer stops drawing it at once (it falls,
    /// <see cref="KwHullPatchLooks"/>), and the owner takes it out of the ZDO. Never in a blueprint.
    /// </summary>
    public sealed class KwHullPatches : MonoBehaviour
    {
        public const string PatchRpc = "KW_HolePatch", PatchedRpc = "KW_HolePatched";
        private const float Poll = 0.5f, AskEvery = 1f;
        private static readonly List<KwHullPatches> loaded = new List<KwHullPatches>();

        private readonly HashSet<int> shown = new HashSet<int>();
        private readonly List<KwHullHole> leaking = new List<KwHullHole>();
        private Dictionary<int, float> patched = new Dictionary<int, float>();
        private ZNetView? view;
        private string text = "";
        private float polled, asked = -99f;
        private int read, solvedRead = -1, solvedHoles = -1, version;

        /// <summary>The launched ships loaded on this peer.</summary>
        public static IReadOnlyList<KwHullPatches> Loaded => loaded;

        public KwHullHoles Holes { get; private set; } = null!;

        /// <summary>The spot the local player's use aims at on this ship (<see cref="KwHullPatchSpot"/>).</summary>
        public KwHullPatchSpot? Spot { get; private set; }

        /// <summary>The holes with a patch on them, by number.</summary>
        public HashSet<int> Shown { get { Solve(); return shown; } }

        /// <summary>The holes that let the sea in: those with no patch on them.</summary>
        public List<KwHullHole> Leaking { get { Solve(); return leaking; } }

        /// <summary>Counts up whenever the patches on the holes change.</summary>
        public int Version { get { Solve(); return version; } }

        public static KwHullPatches Add(Transform ship, KwHullHoles holes)
        {
            KwHullPatches made = ship.gameObject.AddComponent<KwHullPatches>();
            (made.Holes, made.view) = (holes, ship.GetComponent<ZNetView>());
            holes.Patches = made;
            made.Spot = KwHullPatchSpot.Add(made);
            made.Register();
            made.Load(true);
            if (KwShipBody.Drawn)
                KwShipBody.Step("Broadside hull patches look", () => KwHullPatchLooks.Add(ship, made));
            return made;
        }

        private void Awake() => loaded.Add(this);

        private void OnDestroy()
        {
            loaded.Remove(this);
            if (view != null && view.IsValid())
            {
                view.Unregister(PatchRpc);
                view.Unregister(PatchedRpc);
            }
        }

        private void Register()
        {
            if (view == null || !view.IsValid())
                return;
            view.Register<int>(PatchRpc, (sender, id) => Guard.Run("Broadside hull patch", () => Patch(sender, id)));
            view.Register<int>(PatchedRpc, (sender, id) => Guard.Run("Broadside hull patched", () => KwHullPatchSpot.Patched(id)));
        }

        private void Update()
        {
            if (Time.time - polled < Poll)
                return;
            polled = Time.time;
            Load(false);
            Tidy();
        }

        // The ZDO's patches, read again only when their text changed (or always, forced).
        private void Load(bool force)
        {
            string now = view != null && view.IsValid() ? view.GetZDO().GetString(KwHullPatchText.Hash) : "";
            if (!force && now == text)
                return;
            (text, patched) = (now, KwHullPatchText.Read(now));
            read++;
        }

        // Which holes are patched and which leak, worked out again when the patches or the holes changed.
        private void Solve()
        {
            if (solvedRead == read && solvedHoles == Holes.Version)
                return;
            (solvedRead, solvedHoles) = (read, Holes.Version);
            KwHullPatchText.Split(Holes.Holes, patched, shown, leaking);
            version++;
        }

        /// <summary>Whether a hole (by its number) has a patch on it.</summary>
        public bool Covers(int id) => Shown.Contains(id);

        /// <summary>The radius a patched hole had when patched (what its patch covers); its own radius otherwise.</summary>
        public float Covering(KwHullHole hole) => patched.TryGetValue(hole.Id, out float radius) ? Mathf.Max(radius, hole.Radius) : hole.Radius;

        /// <summary>On the player's game: a hole (by its number) asked patched by the ship's owner; false when just asked.</summary>
        public bool Ask(int id)
        {
            if (view == null || !view.IsValid() || Time.time - asked < AskEvery)
                return false;
            asked = Time.time;
            view.InvokeRPC(PatchRpc, id);
            return true;
        }

        // On the owner: a hole patched at its radius now, unless it is gone or patched already; the asker told, who pays.
        private void Patch(long sender, int id)
        {
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            ZDO zdo = view.GetZDO();
            List<KwHullHole> holes = KwHullHoleText.Read(zdo.GetString(KwHullHoleText.HolesHash));
            int i = holes.FindIndex(h => h.Id == id);
            Dictionary<int, float> now = KwHullPatchText.Read(zdo.GetString(KwHullPatchText.Hash));
            if (i < 0 || KwHullPatchText.Holds(now, holes[i]))
                return;
            now[id] = holes[i].Radius;
            Write(now);
            view.InvokeRPC(sender, PatchedRpc, id);
        }

        // On the owner: patches whose holes are mended, or widened past them, taken out of the ZDO.
        private void Tidy()
        {
            if (patched.Count == 0 || view == null || !view.IsValid() || !view.IsOwner())
                return;
            List<KwHullHole> holes = KwHullHoleText.Read(view.GetZDO().GetString(KwHullHoleText.HolesHash));
            Dictionary<int, float> kept = KwHullPatchText.Kept(holes, patched);
            if (kept.Count < patched.Count)
                Write(kept);
        }

        private void Write(Dictionary<int, float> patches)
        {
            view!.GetZDO().Set(KwHullPatchText.Hash, KwHullPatchText.Write(patches));
            Load(true);
        }
    }
}
