using EliteCreaturesPack.Custom.Build;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// A creature's own copies of the items it fights with. The game's attack and weapon items are shared by every
    /// creature that carries them, so nothing of a definition is ever written into them: the creature gets a copy
    /// (<see cref="CreatureBuild.CopyPart"/>, named <c>&lt;creature&gt;_&lt;item&gt;</c>, a number added when taken) and its
    /// lists point at the copy instead. A copy has its own shared data, so its damage, attack and AI values are its own:
    /// the prefab copy is made through Unity's instantiation, which copies the item's serialized data (SharedData, its
    /// Attack objects and its damage) rather than sharing it; each creature that spawns copies it once more into its
    /// inventory, as it does for the game's own items.
    /// <para>
    /// <b>Origins.</b> Which item a copy was made from is kept in one place, the build's part origins
    /// (<see cref="CreatureBuild.OriginOf"/>), which the elite step and the export command read too: an item of the
    /// creature's lists that is one of its parts is its own copy, and its origin is the game or mod item a definition
    /// names (<c>from: troll_throw</c>, <c>portal attacks: [troll_throw]</c>).
    /// </para>
    /// <para>
    /// <b>Names.</b> Each copy's shared name (<c>m_shared.m_name</c>) is its prefab name. Creatures' attack items often
    /// share a name (a troll's punch and slam are both "slap"), and Elite Creatures Reborn finds an attack item by shared
    /// name, the first match winning; a name of its own makes each copy findable.
    /// </para>
    /// <para>
    /// <b>The item database.</b> Every copy is put in the world's ObjectDB once its creature is built
    /// (<see cref="PartItems"/>): the game finds a held item's prefab there by name to draw it on every peer and to know
    /// what it is.
    /// </para>
    /// </summary>
    internal static class OwnItems
    {
        /// <summary>Whether the item is one of this creature's own copies (free to change).</summary>
        public static bool IsOwn(CreatureBuild build, GameObject item) => build.OriginOf(item.name) != null;

        /// <summary>The game or mod item a copy was made from; an item that is no copy, its own name.</summary>
        public static string SourceOf(CreatureBuild build, GameObject item) => build.OriginOf(item.name) ?? item.name;

        /// <summary>The creature's own copy of an item it carries: the item itself when it is one already, otherwise a new
        /// copy put in its place in every list.</summary>
        public static GameObject Own(CreatureBuild build, Humanoid humanoid, GameObject item)
        {
            if (IsOwn(build, item))
            {
                return item;
            }
            GameObject own = Make(build, item);
            CarriedItems.Swap(humanoid, item, own);
            return own;
        }

        /// <summary>A new copy of an item for this creature (not yet in any of its lists).</summary>
        public static GameObject Make(CreatureBuild build, GameObject item)
        {
            GameObject copy = build.CopyPart(item, SourceOf(build, item), networked: false);
            Shared(copy).m_name = copy.name;
            return copy;
        }

        /// <summary>An item prefab's shared data.</summary>
        public static ItemDrop.ItemData.SharedData Shared(GameObject item) => item.GetComponent<ItemDrop>().m_itemData.m_shared;
    }
}
