using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a saved ship costs to build from the hammer: what its parts would have cost built one by one, added up item by
    /// item. A planking, deck, keel or rib part costs its biome tier's two materials by its size (<see cref="KwBiomeCost"/>);
    /// a railing by its length, in its own tier's planking materials, or (one saved without a tier, built before railings
    /// had one) in those of the planking it stands on (<see cref="RailTier"/>); a ladder by its
    /// height in wood, a mast by its height in wood and each of its sails by its area in its cloth (<see cref="KwRigCost"/>);
    /// a rudder its biome's recipe (<see cref="KwRudderTier.Recipe"/>), and its steering gear's shafts by their length
    /// (<see cref="KwRudderGear.Cost"/>); a post cap one wood on the standard post, more by its
    /// size and length (<see cref="KwCapSpec.Cost"/>); an old hammer cap, hatch, anchor windlass, name board or captain's wheel one wood;
    /// a part's doorways and windows their frames, covers and doors (<see cref="KwOpeningCost"/>). All of it is worked
    /// out from the blueprint's saved part texts, so every peer, the hammer's requirements and the click agree. Designing on
    /// the dock stays free. The game's build menu shows a piece's requirements in a few slots, one of them the workbench's,
    /// so the piece carries only as many as it can show (<see cref="Split"/>) and the rest are checked and taken at the
    /// click (<see cref="KwBpPlace"/>), however many kinds of item the ship needs. The game recovers none of it (the
    /// requirements are not recovered); a launched ship broken up with the hammer gives all of it back in floating crates
    /// (<see cref="KwShipBreakSpill"/>).
    /// </summary>
    public static class KwBpCost
    {
        private const string Wood = "Wood";
        private const string Planking = KwToolPieces.PartPrefix + "planking";

        /// <summary>The whole cost of a blueprint, item by item in the order they first come.</summary>
        public static List<(string Item, int Amount)> Of(KwBlueprint blueprint)
        {
            List<(string Item, int Amount)> total = new List<(string Item, int Amount)>();
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                foreach ((string item, int amount) in Part(blueprint, piece))
                    Add(total, item, amount);
            }
            return total;
        }

        /// <summary>Adds an amount of an item to a cost: to the item's line when it has one, else as a new line at the end.</summary>
        public static void Add(List<(string Item, int Amount)> cost, string item, int amount)
        {
            int i = cost.FindIndex(c => c.Item == item);
            if (i < 0)
                cost.Add((item, amount));
            else
                cost[i] = (item, cost[i].Amount + amount);
        }

        // One piece's cost: a generated part's by its kind, a rudder's recipe, a cap's, hatch's, windlass's, name board's or wheel's wood.
        private static IEnumerable<(string Item, int Amount)> Part(KwBlueprint blueprint, KwBpPiece piece)
        {
            string prefab = piece.Prefab;
            if (prefab.StartsWith(KwToolPieces.PartPrefix, StringComparison.Ordinal))
                return Generated(blueprint, piece, prefab.Substring(KwToolPieces.PartPrefix.Length));
            KwRudderTier? rudder = KwRudderTiers.Of(prefab);
            if (rudder != null)
                return KwRudderGear.Cost(rudder, piece);
            bool fitting = prefab.StartsWith(KwCaps.Prefix, StringComparison.Ordinal) || prefab.StartsWith(KwHatch.Prefix, StringComparison.Ordinal) || KwAnchor.Is(prefab)
                || prefab == KwNameBoard.Prefab || KwWheel.Is(prefab);
            return fitting ? new[] { (Wood, 1) } : Array.Empty<(string, int)>();
        }

        // A tool's part from its saved text: a ladder by its height, a mast by its height and its sails, a railing by its
        // length in its own tier (one with none in its planking's), a planking, deck, keel or rib part in its own tier by its size.
        private static List<(string Item, int Amount)> Generated(KwBlueprint blueprint, KwBpPiece piece, string tool)
        {
            string saved = KwShipParts.Saved(piece);
            if (tool == KwLadderTool.Tool)
                return KwRigCost.Ladder(saved);
            if (tool == KwSailTool.Tool)
                return KwRigCost.Mast(saved);
            if (tool == KwBowsprit.Tool)
                return KwRigCost.Bowsprit(blueprint, piece);
            if (tool == KwCapSpec.Tool)
                return KwCapSpec.TryParse(saved, out KwCapSpec cap) ? cap.Cost() : new List<(string Item, int Amount)> { (Wood, 1) };
            bool planked = tool == KwBiomeCost.Rail && !KwRailLooks.Tiered(piece.Data);
            KwBiomeTier tier = planked ? RailTier(blueprint, piece, saved) : KwBiomeTiers.Of(piece.Data);
            return KwOpeningCost.With(KwBiomeCost.Of(tool, saved, tier), saved, tier);
        }

        /// <summary>
        /// The tier of a railing that saves none (built before railings had one; drawn in the plank wood): the tier of the planking it stands
        /// on, the blueprint's planking laid from the same two clicks (the same place and way, the same line: shape, frames,
        /// length and ends), or a straight planking run it lies along (a railing a cabin cut at a rib, <see cref="KwRailCut.Covers"/>);
        /// Meadows (wood and resin) when no planking is under it.
        /// </summary>
        private static KwBiomeTier RailTier(KwBlueprint blueprint, KwBpPiece rail, string saved)
        {
            string line = Line(saved);
            Vector3 way = rail.Rotation * Vector3.forward;
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (piece.Prefab != Planking)
                    continue;
                string text = KwShipParts.Saved(piece);
                Vector3 along = piece.Rotation * Vector3.forward;
                if (((piece.Position - rail.Position).sqrMagnitude < 0.0025f && Vector3.Dot(along, way) > 0.999f && Line(text) == line)
                    || KwRailCut.Covers(rail.Position, way, saved, piece.Position, along, text))
                    return KwBiomeTiers.Of(piece.Data);
            }
            return KwBiomeTiers.Meadows;
        }

        // A hull part's line in its saved span: its shape, frames, length and ends, without the clicked heights or a seed.
        private static string Line(string saved) => string.Join("|", saved.Split('|').Take(5));

        /// <summary>
        /// The cost as the game's requirements (never recovered, an item the game lacks left out): the first as many as
        /// the build menu shows beside the workbench, and the rest.
        /// </summary>
        public static (Piece.Requirement[] Shown, List<(string Item, int Amount)> Left) Split(List<(string Item, int Amount)> cost)
        {
            List<(ItemDrop Drop, int Amount)> known = new List<(ItemDrop Drop, int Amount)>();
            foreach ((string item, int amount) in cost)
            {
                ItemDrop? drop = Drop(item);
                if (drop != null && amount > 0)
                    known.Add((drop, amount));
            }
            int slots = Slots();
            Piece.Requirement[] shown = known.Take(slots).Select(k => new Piece.Requirement { m_resItem = k.Drop, m_amount = k.Amount, m_recover = false }).ToArray();
            return (shown, known.Skip(slots).Select(k => (k.Drop.gameObject.name, k.Amount)).ToList());
        }

        // The build menu's requirement slots less the workbench's; four before the HUD is up.
        private static int Slots()
        {
            Hud? hud = Hud.instance;
            return hud != null && hud.m_requirementItems.Length > 1 ? hud.m_requirementItems.Length - 1 : 4;
        }

        /// <summary>What the player lacks of the cost, in words; null when they have it all.</summary>
        public static string? Missing(Player player, List<(string Item, int Amount)> cost)
        {
            Inventory inventory = player.GetInventory();
            List<string> lacking = new List<string>();
            foreach ((string item, int amount) in cost)
            {
                ItemDrop? drop = Drop(item);
                int have = drop != null ? inventory.CountItems(drop.m_itemData.m_shared.m_name) : 0;
                if (drop != null && have < amount)
                    lacking.Add($"{amount} {Shown(item)} (you have {have})");
            }
            return lacking.Count > 0 ? string.Join(", ", lacking) : null;
        }

        /// <summary>Takes the cost from the player's inventory.</summary>
        public static void Take(Player player, List<(string Item, int Amount)> cost)
        {
            foreach ((string item, int amount) in cost)
            {
                ItemDrop? drop = Drop(item);
                if (drop != null)
                    player.GetInventory().RemoveItem(drop.m_itemData.m_shared.m_name, amount);
            }
        }

        /// <summary>"12 Fine wood, 6 Bronze nails": the cost in the game's own item names.</summary>
        public static string Words(List<(string Item, int Amount)> cost) =>
            cost.Count == 0 ? "nothing" : string.Join(", ", cost.Select(c => $"{c.Amount} {Shown(c.Item)}"));

        private static ItemDrop? Drop(string item) =>
            ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(item)?.GetComponent<ItemDrop>() : null;

        private static string Shown(string item)
        {
            ItemDrop? drop = Drop(item);
            return drop != null && Localization.instance != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name) : item;
        }
    }
}
