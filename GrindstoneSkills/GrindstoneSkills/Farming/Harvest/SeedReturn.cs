using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Seed return: picking a crop sometimes gives back the seed its plant was grown from (carrot seeds for a carrot, a
    /// carrot for seed carrots, barley for barley), with the crop's stars. The chance is the picker's share of Seed Return
    /// Chance At Level 100. The picker's client makes the item at the crop, as the game's own drops pop out; it owns the
    /// new item's ZDO, so every client sees it.
    /// </summary>
    public static class SeedReturn
    {
        public static void Roll(Player player, Pickable pickable, CropPlant crop, int stars)
        {
            if (crop.Seed == null || Random.value >= FarmSkill.Share(FarmingPerkSettings.SeedReturnAt100.Value, FarmSkill.Level(player)))
                return;
            Vector3 position = pickable.transform.position + Vector3.up * 0.5f;
            Spawn(crop.Seed, position, stars);
            string name = Localization.instance.Localize(crop.Seed.m_itemData.m_shared.m_name);
            FarmCallout.Show(pickable.transform.position, "+1 " + name);
        }

        private static void Spawn(ItemDrop seed, Vector3 position, int stars)
        {
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject spawned = Object.Instantiate(seed.gameObject, position, rotation);
            ItemDrop item = spawned.GetComponent<ItemDrop>();
            if (item == null)
                return;
            item.SetStack(1);
            ItemDrop.OnCreateNew(item);
            CropSpawn.Apply(item, stars);
            Rigidbody body = spawned.GetComponent<Rigidbody>();
            if (body != null)
                body.linearVelocity = Vector3.up * 4f;
        }
    }
}
