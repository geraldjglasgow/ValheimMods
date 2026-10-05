using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// What a build or a ground fix costs this player: every piece's building materials added up (pieces the world's
    /// "no build cost" key makes free left out), the stone the ground work needs less the stone it gives back (a
    /// surplus is <see cref="StoneBack"/>), the pieces the player has not learned yet, and the prefabs this game does
    /// not have. Free with "Build Without Materials" and in no-cost mode; no-cost mode also needs no learned pieces.
    /// </summary>
    public sealed class MaterialBill
    {
        private const string StoneItem = "Stone";

        /// <summary>Amounts by the item's name token ("$item_wood").</summary>
        public readonly Dictionary<string, int> Amounts = new Dictionary<string, int>();

        /// <summary>The display names (pieces' "$piece_..." tokens) of pieces the player has not learned.</summary>
        public readonly HashSet<string> Unlearned = new HashSet<string>();

        /// <summary>Prefab names the game does not have (a mod that is not installed here).</summary>
        public readonly HashSet<string> Missing = new HashSet<string>();

        /// <summary>No materials are taken ("Build Without Materials" or no-cost mode).</summary>
        public bool Free;

        /// <summary>The player is in no-cost mode: pieces need not be learned and are marked cheated, as the game does.</summary>
        public bool Cheated;

        /// <summary>Stone the ground work gives back beyond what it needs, handed to the player when the work starts.</summary>
        public int StoneBack;

        /// <summary>The ground work's stone: raised ground needs it, lowered ground gives it back.</summary>
        public int GroundNeeded;
        public int GroundRemoved;

        public static MaterialBill For(Blueprint bp, Player player)
        {
            MaterialBill bill = ForGround(player);
            foreach (BlueprintPiece p in bp.Pieces)
            {
                Piece piece = PieceOf(p.Prefab);
                if (piece == null)
                    bill.Missing.Add(p.Prefab);
                else
                    bill.Add(piece, player);
            }
            return bill;
        }

        /// <summary>A bill without pieces (a ground fix); <see cref="AddGround"/> adds its stone.</summary>
        public static MaterialBill ForGround(Player player)
        {
            bool cheated = player.NoCostCheat();
            return new MaterialBill { Cheated = cheated, Free = cheated || BlueprintSettings.FreeMaterials };
        }

        public static Piece PieceOf(string prefab)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            return go != null ? go.GetComponent<Piece>() : null;
        }

        private void Add(Piece piece, Player player)
        {
            if (!Cheated && !player.IsRecipeKnown(piece.m_name))
                Unlearned.Add(piece.m_name);
            if (Free || ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
                return;
            foreach (Piece.Requirement r in piece.m_resources)
            {
                if (r.m_resItem != null && r.m_amount > 0)
                    Put(r.m_resItem.m_itemData.m_shared.m_name, r.m_amount);
            }
        }

        /// <summary>The ground work's stone: the difference of needed and removed is paid, or handed back.</summary>
        public void AddGround(GroundWork work)
        {
            GroundNeeded = work.StoneNeeded;
            GroundRemoved = work.StoneRemoved;
            if (Free)
                return;
            int net = GroundNeeded - GroundRemoved;
            string stone = StoneName();
            if (net > 0 && stone != null)
                Put(stone, net);
            else if (net < 0)
                StoneBack = -net;
        }

        private void Put(string name, int amount) => Amounts[name] = (Amounts.TryGetValue(name, out int had) ? had : 0) + amount;

        private static string StoneName()
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(StoneItem) : null;
            return prefab != null ? prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name : null;
        }

        /// <summary>The materials the player lacks as "Wood 120/400, Stone 3/90" (localized), or null when they have everything.</summary>
        public string Shortfall(Player player)
        {
            if (Free)
                return null;
            Inventory inventory = player.GetInventory();
            List<string> lacking = Amounts.Where(a => inventory.CountItems(a.Key) < a.Value)
                .Select(a => $"{Localization.instance.Localize(a.Key)} {inventory.CountItems(a.Key)}/{a.Value}").ToList();
            return lacking.Count == 0 ? null : string.Join(", ", lacking);
        }

        /// <summary>The bill as "Wood 400, Stone 90" (localized), biggest first, at most <paramref name="most"/> materials and "+N more"; null when free or empty.</summary>
        public string Summary(int most = 6)
        {
            if (Free || Amounts.Count == 0)
                return null;
            List<string> parts = Amounts.OrderByDescending(a => a.Value).Take(most)
                .Select(a => $"{Localization.instance.Localize(a.Key)} {a.Value}").ToList();
            if (Amounts.Count > most)
                parts.Add($"+{Amounts.Count - most}");
            return string.Join(", ", parts);
        }

        /// <summary>Takes the materials from the player's inventory (check <see cref="Shortfall"/> first) and hands back surplus stone.</summary>
        public void Charge(Player player)
        {
            if (Free)
                return;
            foreach (KeyValuePair<string, int> a in Amounts)
                player.GetInventory().RemoveItem(a.Key, a.Value);
            GiveStone(player, StoneBack);
        }

        /// <summary>Stone into the inventory, a stack at a time; what does not fit drops at the player's feet.</summary>
        private static void GiveStone(Player player, int amount)
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(StoneItem) : null;
            if (prefab == null || amount <= 0)
                return;
            int stack = Mathf.Max(1, prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize);
            for (int left = amount; left > 0; left -= stack)
            {
                int n = Mathf.Min(stack, left);
                if (player.GetInventory().AddItem(prefab, n))
                    continue;
                ItemDrop drop = Object.Instantiate(prefab, player.transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
                drop.SetStack(n);
            }
        }
    }
}
