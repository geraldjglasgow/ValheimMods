using System.Collections.Generic;

namespace EarthWright.Costs
{
    /// <summary>One item of a bill: the item's prefab and how many.</summary>
    internal struct BillLine
    {
        public ItemDrop Item;
        public int Amount;

        /// <summary>The item's shared name ("$item_stone"), which the inventory counts and removes by.</summary>
        public string Name => Item.m_itemData.m_shared.m_name;
    }

    /// <summary>
    /// Items a terrain use costs, merged per item so a check counts an entry's stone, the extra item and the volume
    /// stone together. Checked against and paid from the local player's inventory.
    /// </summary>
    internal sealed class Bill
    {
        public readonly List<BillLine> Lines = new List<BillLine>();

        public bool IsEmpty => Lines.Count == 0;

        /// <summary>Adds an amount of an item; ignored for no item or a non-positive amount.</summary>
        public void Add(ItemDrop item, int amount)
        {
            if (item == null || amount <= 0)
                return;
            string name = item.m_itemData.m_shared.m_name;
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Name != name)
                    continue;
                Lines[i] = new BillLine { Item = Lines[i].Item, Amount = Lines[i].Amount + amount };
                return;
            }
            Lines.Add(new BillLine { Item = item, Amount = amount });
        }

        public void AddAll(Bill other)
        {
            foreach (BillLine line in other.Lines)
                Add(line.Item, line.Amount);
        }

        /// <summary>How many of the line's item the inventory holds.</summary>
        public static int Have(Inventory inventory, BillLine line) => inventory != null ? inventory.CountItems(line.Name) : 0;

        /// <summary>The index of the first line the inventory cannot pay, or -1.</summary>
        public int FirstShort(Inventory inventory)
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Have(inventory, Lines[i]) < Lines[i].Amount)
                    return i;
            }
            return -1;
        }

        public void PayFrom(Inventory inventory)
        {
            if (inventory == null)
                return;
            foreach (BillLine line in Lines)
                inventory.RemoveItem(line.Name, line.Amount);
        }

        /// <summary>The bill as the game's requirement list, so the game's own ConsumeResources pays it.</summary>
        public Piece.Requirement[] ToRequirements()
        {
            Piece.Requirement[] requirements = new Piece.Requirement[Lines.Count];
            for (int i = 0; i < Lines.Count; i++)
            {
                requirements[i] = new Piece.Requirement
                {
                    m_resItem = Lines[i].Item, m_amount = Lines[i].Amount, m_amountPerLevel = 0, m_recover = false,
                };
            }
            return requirements;
        }
    }

    /// <summary>
    /// What a terrain use costs in items, in two parts: <see cref="Items"/> (the entry's materials and the extra
    /// item, which a brush click pays through the game's ConsumeResources) and <see cref="Volume"/> (stone for the
    /// volume raised and the area paved, paid when the edit is sent), with the exact amount owed for the carry-over.
    /// </summary>
    internal sealed class UseCost
    {
        public readonly Bill Items = new Bill();
        public readonly Bill Volume = new Bill();

        /// <summary>Volume stone owed by this use before rounding (without the carried fraction).</summary>
        public float VolumeOwed;

        /// <summary>Whole volume stones charged now.</summary>
        public int VolumeDue;

        /// <summary>Both parts together, merged per item, for checking the inventory.</summary>
        public Bill Total
        {
            get
            {
                Bill total = new Bill();
                total.AddAll(Items);
                total.AddAll(Volume);
                return total;
            }
        }

        /// <summary>Takes the volume stone and books the fraction carried over to the next use.</summary>
        public void PayVolume(Inventory inventory)
        {
            Volume.PayFrom(inventory);
            VolumeCharge.Paid(VolumeOwed, VolumeDue);
        }
    }
}
