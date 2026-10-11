using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Saves
{
    /// <summary>
    /// On every custom creature's prefab: the first time one is in the world, its owner writes the definition's name into
    /// its ZDO (<see cref="Key"/>). The prefab hash alone cannot be turned back into a name, and a creature whose
    /// definition is gone has no prefab: the mark is how <see cref="ParkedCreatures"/> recognises it, keeps the server from
    /// deleting it and names it in the warning. Written once (a later owner finds it set), so it costs one string per
    /// creature in the save.
    /// </summary>
    public sealed class CustomTag : MonoBehaviour
    {
        /// <summary>The ZDO key: the creature's definition name (its prefab name).</summary>
        public const string Key = "ecp_custom";

        public static readonly int KeyHash = Key.GetStableHashCode();

        private void Start() => SafeCall.Run("custom creature mark", static tag => tag.Mark(), this);

        private void Mark()
        {
            ZNetView nview = GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            ZDO zdo = nview.GetZDO();
            if (zdo.GetString(KeyHash).Length == 0)
            {
                zdo.Set(KeyHash, Utils.GetPrefabName(gameObject.name));
            }
        }
    }
}
