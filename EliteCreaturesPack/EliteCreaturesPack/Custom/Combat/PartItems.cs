using System.Collections.Generic;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// Puts the item parts of the built custom creatures (their own copies of attack items and gear,
    /// <see cref="OwnItems"/>) in the world's item database, on every peer, as each world's creatures are built. The game
    /// needs a held item there: an item learns its prefab from ObjectDB by name as it wakes (<c>m_dropPrefab</c>), and
    /// <c>Humanoid.SetupVisEquipment</c> and <c>VisEquipment</c> name and draw what a creature holds and wears through
    /// it; Elite Creatures Reborn finds an attack's item through it too.
    /// <para>
    /// Only built creatures' parts go in, after every step has run, so a creature left out leaves nothing behind in the
    /// database. Each world has its own ObjectDB (made with the world's scene), so the next world starts clean; a part
    /// destroyed since is dropped from the list here all the same, as are any other dead entries.
    /// </para>
    /// </summary>
    internal static class PartItems
    {
        public static void Register(IEnumerable<CustomCreature> built)
        {
            ObjectDB? db = ObjectDB.instance;
            if (db == null)
            {
                return;
            }
            db.m_items.RemoveAll(item => item == null);
            foreach (CustomCreature creature in built)
            {
                foreach (GameObject part in creature.Parts)
                {
                    if (part != null && part.GetComponent<ItemDrop>() != null)
                    {
                        Add(db, part);
                    }
                }
            }
        }

        private static void Add(ObjectDB db, GameObject item)
        {
            int hash = item.name.GetStableHashCode();
            if (db.m_itemByHash.TryGetValue(hash, out GameObject taken) && taken != null && taken != item)
            {
                Log.Warn($"Custom creatures: the item database already has an item named '{item.name}', so the part of that name is left out of it.");
                return;
            }
            if (taken != item)
            {
                db.m_items.Add(item);
            }
            db.m_itemByHash[hash] = item;
            db.m_itemByData[item.GetComponent<ItemDrop>().m_itemData.m_shared] = item;
        }
    }
}
