using System;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock's material (DOCKS.md): the one item it costs, how strong its piece is and the sizes it is made to. Wood: plain
    /// plank boards on square timbers and posts in the game's wood floor planks; core wood: wide split-pine boards on
    /// round-log joists, cap beams, pilings and curbs in the core wood log pieces' look, heavier and harder; fine wood:
    /// narrow, tight boards of planed fine wood on square timbers in the fine wood stack's look. Plain data; sizes in metres,
    /// the dock's own frame (x across, y up from the deck's top, z along).
    /// </summary>
    public sealed class KwPierKind
    {
        public KwPierKind(string id, string label, string item, float health, bool round)
        {
            (Id, Label, Item, Health, Round) = (id, label, item, health, round);
        }

        /// <summary>Its id, the end of its prefab's name: <c>wood</c>, <c>corewood</c>, <c>finewood</c>.</summary>
        public string Id { get; }

        public string Label { get; }

        /// <summary>The one item it costs, by the game's prefab name.</summary>
        public string Item { get; }

        public float Health { get; }

        /// <summary>Round-log joists, cap beams, pilings and curbs (core wood); square timbers otherwise.</summary>
        public bool Round { get; }

        /// <summary>The game's support rules for it: core wood is hard wood, as the game's core wood pieces.</summary>
        public WearNTear.MaterialType Wear => Round ? WearNTear.MaterialType.HardWood : WearNTear.MaterialType.Wood;

        public string Prefab => KwPierPrefabs.Prefix + Id;

        /// <summary>Deck boards: width along the dock, the gap between two, thickness, and the longest length before a butt joint.</summary>
        public float Board { get; set; } = 0.25f;
        public float Gap { get; set; } = 0.012f;
        public float Thick { get; set; } = 0.06f;
        public float BoardRun { get; set; } = 3f;

        /// <summary>The joists along the dock under the boards and the cap beam across each pair of pilings (width, height; a log's diameter both).</summary>
        public Vector2 Joist { get; set; } = new Vector2(0.1f, 0.18f);
        public Vector2 Cap { get; set; } = new Vector2(0.16f, 0.18f);

        /// <summary>The low edge timber along each side, on the deck (width, height; a log's diameter both).</summary>
        public Vector2 Curb { get; set; } = new Vector2(0.12f, 0.1f);

        /// <summary>A piling's side, or a round piling's diameter.</summary>
        public float Post { get; set; } = 0.2f;

        /// <summary>The joists' underside (the cap beams' top), and the cap beams' underside, where the pilings start.</summary>
        public float JoistFoot => -Thick - Joist.y;
        public float CapFoot => JoistFoot - Cap.y;

        /// <summary>How far in from a deck edge a piling's centre stands (under the edge, inside the deck's width).</summary>
        public float PostIn => Post * 0.5f + 0.02f;
    }

    /// <summary>The three dock materials, in the order the wheel steps through them (Meadows and Black Forest only).</summary>
    public static class KwPierKinds
    {
        public static readonly KwPierKind Wood = new KwPierKind("wood", "Wood", "Wood", 1000f, false);

        public static readonly KwPierKind CoreWood = new KwPierKind("corewood", "Core wood", "RoundLog", 1500f, true)
        {
            Board = 0.3f, Gap = 0.015f, Thick = 0.08f, BoardRun = 2f, Joist = new Vector2(0.2f, 0.2f), Cap = new Vector2(0.24f, 0.24f),
            Curb = new Vector2(0.16f, 0.16f), Post = 0.3f,
        };

        public static readonly KwPierKind FineWood = new KwPierKind("finewood", "Fine wood", "FineWood", 1200f, false)
        {
            Board = 0.18f, Gap = 0.006f, Thick = 0.05f, BoardRun = 2f, Joist = new Vector2(0.1f, 0.16f), Cap = new Vector2(0.14f, 0.16f),
            Curb = new Vector2(0.1f, 0.09f), Post = 0.2f,
        };

        public static readonly KwPierKind[] All = { Wood, CoreWood, FineWood };

        /// <summary>A material by its id; wood for one unknown.</summary>
        public static KwPierKind Of(string id) => Array.Find(All, k => k.Id == id) ?? Wood;

        /// <summary>A material by its dock prefab's name; null for any other prefab.</summary>
        public static KwPierKind? OfPrefab(string prefab) => Array.Find(All, k => k.Prefab == prefab);

        /// <summary>The material a number of wheel notches on from this one, round the three.</summary>
        public static KwPierKind Step(KwPierKind kind, int steps)
        {
            int i = Array.IndexOf(All, kind) + steps;
            return All[(i % All.Length + All.Length) % All.Length];
        }
    }
}
