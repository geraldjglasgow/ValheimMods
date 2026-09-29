using PatchGuard;
using UnityEngine;

namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// Turns some crypt chests into mimics. The decision is the chest owner's, once, at the moment the game would first
    /// fill the chest (when the crypt is generated): a listed chest, at the settings' `Chance`, is marked in its ZDO and
    /// left empty, and on the next frame - after the chest has finished waking - a mimic takes its place, facing the same
    /// way, and remembers which chest it was (for its loot). The chest is removed. A chest marked but not yet swapped
    /// (its owner left) is swapped by whoever owns it next.
    /// </summary>
    public static class CryptSwap
    {
        public const string PendingKey = "ecp_mimic_swap";
        public const string ChestKey = "ecp_mimic_chest";

        /// <summary>OWNER, first fill. True when the chest becomes a mimic, so it must not be filled.</summary>
        public static bool Claim(Container chest)
        {
            if (!MimicSettings.On || !IsListed(chest) || Random.value >= MimicSettings.Chance)
            {
                return false;
            }
            chest.m_nview.GetZDO().Set(PendingKey, true);
            chest.gameObject.AddComponent<CryptSwapper>();
            return true;
        }

        /// <summary>Any machine, when a chest wakes: an owner finds a swap left pending and carries it out.</summary>
        public static void Resume(Container chest)
        {
            ZNetView nview = chest.m_nview;
            if (nview != null && nview.IsValid() && nview.IsOwner() && nview.GetZDO().GetBool(PendingKey)
                && chest.GetComponent<CryptSwapper>() == null)
            {
                chest.gameObject.AddComponent<CryptSwapper>();
            }
        }

        private static bool IsListed(Container chest) =>
            MimicSettings.Listed(Utils.GetPrefabName(chest.gameObject));
    }

    /// <summary>Swaps its chest for a mimic on its first frame, while this machine still owns the chest.</summary>
    public class CryptSwapper : MonoBehaviour
    {
        private void Update()
        {
            enabled = false;
            Guard.Run("CryptSwapper.Swap", Swap);
        }

        private void Swap()
        {
            ZNetView chest = GetComponent<ZNetView>();
            GameObject prefab = ZNetScene.instance.GetPrefab(MimicPrefabs.Creature);
            if (chest == null || !chest.IsValid() || !chest.IsOwner() || prefab == null)
            {
                return;
            }
            GameObject mimic = Instantiate(prefab, transform.position, transform.rotation);
            ZNetView view = mimic.GetComponent<ZNetView>();
            if (view != null && view.GetZDO() != null)
            {
                view.GetZDO().Set(CryptSwap.ChestKey, Utils.GetPrefabName(gameObject));
            }
            ZNetScene.instance.Destroy(gameObject);
        }
    }
}
