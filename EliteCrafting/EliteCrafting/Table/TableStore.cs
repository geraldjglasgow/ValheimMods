using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// What one Rune Table holds, kept in its ZDO so the game saves and replicates it (rune-table.md section 5): a count
    /// per rune (<c>ecf_rt_rune_&lt;id&gt;</c>) and one pool of pure essence (<c>ecf_rt_essence</c>) every essence draws from. Anyone reads; only the
    /// ZDO's owner writes, and the table hands its ownership to whoever opens it (as the game's chests do), so every
    /// write here happens on the client that has it open. A write without ownership changes nothing and answers false.
    /// </summary>
    internal sealed class TableStore
    {
        private static readonly Dictionary<string, int> RuneKeys = new Dictionary<string, int>();
        private static readonly int EssenceKey = "ecf_rt_essence".GetStableHashCode();

        private readonly ZNetView _view;

        public TableStore(ZNetView view)
        {
            _view = view;
        }

        public bool Valid => _view != null && _view.IsValid();

        public bool Writable => Valid && _view.IsOwner();

        public int Runes(string runeId) => Valid ? _view.GetZDO().GetInt(RuneKey(runeId)) : 0;

        /// <summary>The pure essence in the table's pool, which every chosen essence costs from.</summary>
        public int Essence => Valid ? _view.GetZDO().GetInt(EssenceKey) : 0;

        public bool AddRunes(string runeId, int count) => Change(RuneKey(runeId), count);

        public bool TakeRunes(string runeId, int count) => Runes(runeId) >= count && Change(RuneKey(runeId), -count);

        public bool AddEssence(int amount) => Change(EssenceKey, amount);

        public bool TakeEssence(int amount) => Essence >= amount && Change(EssenceKey, -amount);

        /// <summary>Every rune id the table can hold, in the economy file's order (the built-in seven).</summary>
        public static IEnumerable<string> RuneIds => StoneCatalog.BuiltInIds;

        private bool Change(int key, int delta)
        {
            if (!Writable || delta == 0)
            {
                return Writable;
            }
            ZDO zdo = _view.GetZDO();
            zdo.Set(key, System.Math.Max(0, zdo.GetInt(key) + delta));
            return true;
        }

        private static int RuneKey(string runeId) => KeyOf(RuneKeys, "ecf_rt_rune_", runeId);

        private static int KeyOf(Dictionary<string, int> cache, string prefix, string id)
        {
            if (!cache.TryGetValue(id, out int key))
            {
                key = (prefix + id).GetStableHashCode();
                cache[id] = key;
            }
            return key;
        }
    }
}
