using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Foraging experience. The game raises the pick's skill once per pick (Player.RaiseSkill → Skills.RaiseSkill with
    /// 1, on the picker's client, not again while the pick is in flight); while <see cref="ForagePick"/> has a pick's
    /// scope open, the Foraging raise is scaled to Experience Per Pick × the item's factor × 1 + Experience Per Biome
    /// Step per step of the plant's biome (<see cref="MineXp.BiomeStep"/>, the Pickaxes steps), and the first pick of
    /// each kind of forage × Discovery Multiplier (<see cref="ForageDiscovery"/>), with a callout.
    /// </summary>
    public static class ForageXp
    {
        private static ForageEntry entry;
        private static Pickable pickable;

        public static bool Active { get; private set; }

        /// <summary>Opens the scope for a pick. False when one is already open (then the caller must not end it).</summary>
        public static bool Begin(Pickable pick, ForageEntry forage)
        {
            if (Active)
                return false;
            Active = true;
            pickable = pick;
            entry = forage;
            return true;
        }

        public static void End(bool opened)
        {
            if (!opened)
                return;
            Active = false;
            pickable = null;
            entry = null;
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class RaisePatch
        {
            [HarmonyPrefix]
            private static void Prefix(Skills __instance, Skills.SkillType skillType, ref float factor)
            {
                Player player = Player.m_localPlayer;
                if (skillType != ForagingSkill.Type || !Active || entry == null || player == null || __instance.m_player != player)
                    return;
                factor *= HookGuard.Run("Foraging experience", () => Scale(player), 1f);
            }
        }

        /// <summary>The factor for this pick's raise; records a discovery (once per scope) when it earns anything.</summary>
        private static float Scale(Player player)
        {
            ForageEntry forage = entry;
            entry = null;
            Vector3 spot = pickable != null ? pickable.transform.position : player.transform.position;
            float scale = Mathf.Max(0f, ForagingSettings.ExperiencePerPick.Value) * forage.Experience * BiomeScale(spot);
            if (scale <= 0f || !ForageDiscovery.TryRecord(player, forage.Item))
                return scale;
            ForageCallout.Show(spot, $"Discovered {Forage.ItemName(pickable)}!");
            return scale * Mathf.Max(1f, ForagingSettings.DiscoveryMultiplier.Value);
        }

        private static float BiomeScale(Vector3 spot) =>
            1f + Mathf.Max(0f, ForagingSettings.BiomeStep.Value) / 100f * MineXp.BiomeStep(Heightmap.FindBiome(spot));
    }
}
