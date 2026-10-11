using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Whether a Raiders Chest may sound a raid now, as far as the chest itself decides (features/raids.md section 2):
    /// the setting is on; it stands in a base, by the game's own base test at the chest (at least
    /// <see cref="BasePieces"/> player base pieces - workbench, fire, bed - within <see cref="BaseRange"/> m, what the
    /// game's raids ask of a player); its horn has rested an in-game day since any chest within 100 m last sounded (each
    /// chest's <see cref="RaidState.CooldownUntil"/>, read from the ZDOs, so a neighbour this machine has not loaded still
    /// counts); and no raid is on within 200 m. The hover asks on any machine, about once a second; the owner asks again as
    /// it sounds the raid, before <see cref="Raid.Check"/>. One walk of the raid hosts' ZDOs serves the cooldown and the
    /// spacing.
    /// </summary>
    internal static class ChestRules
    {
        /// <summary>Metres around the chest the base test counts, the game's own (a player's base value).</summary>
        public const float BaseRange = 20f;

        /// <summary>Player base pieces within range that make a base, the game's own threshold for its raids.</summary>
        public const int BasePieces = 3;

        private static readonly List<ZDO> Hosts = new List<ZDO>();

        /// <summary>Why this chest cannot sound a raid now; null when it can. Any machine.</summary>
        public static string? Refusal(RaidChest chest)
        {
            if (!RaidSettings.ChestEnabled)
            {
                return ChestText.Off;
            }
            Vector3 at = chest.transform.position;
            if (EffectArea.GetBaseValue(at, BaseRange) < BasePieces)
            {
                return ChestText.NotInBase;
            }
            ZDO self = chest.View.GetZDO();
            RaidZdos.Near(at, RaidTable.RaidSpacing, Hosts);
            long rest = RestUntil(self, at) - NetTime.NowMs();
            string? why = rest > 0L ? ChestText.Resting(rest) : Spacing(at, self.m_uid);
            Hosts.Clear();
            return why;
        }

        // The latest cooldown among this chest and every Raiders Chest within the shared range.
        private static long RestUntil(ZDO self, Vector3 at)
        {
            long until = new RaidState(self).CooldownUntil;
            float reach = RaidTable.CooldownShareRange * RaidTable.CooldownShareRange;
            foreach (ZDO zdo in Hosts)
            {
                if (zdo.GetPrefab() == ChestPrefab.PrefabHash && (zdo.GetPosition() - at).sqrMagnitude <= reach)
                {
                    until = Math.Max(until, new RaidState(zdo).CooldownUntil);
                }
            }
            return until;
        }

        // The game's raid near, or one of the mod's at another host within the spacing (this chest's own is checked by
        // the hover and the owner before they get here).
        private static string? Spacing(Vector3 at, ZDOID self)
        {
            if (RaidSpacing.GameRaidNear(at))
            {
                return ChestText.GameRaidNear;
            }
            foreach (ZDO zdo in Hosts)
            {
                if (zdo.m_uid != self && RaidState.IsRunning(zdo))
                {
                    return ChestText.ChestRaidNear;
                }
            }
            return null;
        }
    }
}
