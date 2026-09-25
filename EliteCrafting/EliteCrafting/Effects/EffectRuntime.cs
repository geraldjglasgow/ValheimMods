using System;
using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// When the aggregate is rebuilt (effects-runtime.md section 3). Triggers only set a dirty flag; the per-frame
    /// <see cref="Tick"/> (from <see cref="EffectDriver"/>) rebuilds at most once per frame. Triggers: equipment set up
    /// (equip, unequip, hide/show hand items), the local inventory's change callback, a state write to an item the local
    /// player has equipped, spawn, a rules apply, the <c>Affix effects</c> switch, the end of a teleport, and the
    /// aggregate found missing (death, anything that cleared the status effects). Local player only.
    /// </summary>
    internal static class EffectRuntime
    {
        private static bool _dirty = true;
        private static bool _enabled = true;
        private static bool _teleporting;
        private static Player? _player;
        private static Inventory? _inventory;
        private static readonly Action OnInventoryChanged = MarkDirty;

        public static void MarkDirty() => _dirty = true;

        public static void Install()
        {
            ActiveRules.RulesChanged += MarkDirty;
            ItemStateCache.Written += OnItemWritten;
            GameObject host = new GameObject("ECF_Effects");
            host.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<EffectDriver>();
        }

        /// <summary>Once per frame: a few reference and bool checks, a rebuild only when something changed.</summary>
        public static void Tick()
        {
            Player? player = Player.m_localPlayer;
            if (!ReferenceEquals(player, _player))
            {
                OnPlayerChanged(player);
            }
            if (player == null)
            {
                return;
            }
            WatchState(player);
            if (_dirty)
            {
                _dirty = false;
                Rebuild(player);
            }
            TickLocal(player);
        }

        /// <summary>
        /// The per-frame parts of the Phase 2 effects that need the local player: timed windows and the ward, the path
        /// check, a pending Reflex Draught, the durability top-up. Each returns at once when its affix is absent.
        /// </summary>
        private static void TickLocal(Player player)
        {
            CombatWindows.Tick(player);
            PathGround.Tick(player);
            Meads.Tick(player);
            Equipment.Tick(player);
        }

        /// <summary>
        /// Every peer, local player or not (a dedicated server owns creatures): stagger lengths, and the visuals every
        /// client draws for other players' affixes.
        /// </summary>
        public static void TickShared()
        {
            StaggerSpeed.Tick();
            PlayerVisuals.Tick();
        }

        private static void WatchState(Player player)
        {
            bool enabled = ItemEffects.Enabled;
            bool teleporting = player.IsTeleporting();
            if (enabled != _enabled || teleporting != _teleporting)
            {
                _enabled = enabled;
                _teleporting = teleporting;
                _dirty = true;
            }
            else if (enabled && !AggregateHost.IsPresent(player) && !player.IsDead())
            {
                _dirty = true;
            }
        }

        private static void OnPlayerChanged(Player? player)
        {
            if (_inventory != null)
            {
                _inventory.m_onChanged -= OnInventoryChanged;
            }
            _player = player;
            _inventory = player != null ? player.GetInventory() : null;
            if (_inventory != null)
            {
                _inventory.m_onChanged += OnInventoryChanged;
            }
            HealthCritical.Reset();
            CombatWindows.Reset();
            _dirty = true;
        }

        private static void OnItemWritten(ItemDrop.ItemData item)
        {
            ItemLocalCache.Forget(item);
            // Equipped: the totals may change. Anywhere in the inventory: its weight may change (Lightened), and the
            // rebuild recomputes the inventory's total weight.
            if (ItemEffects.IsEquippedByLocalPlayer(item) || (_inventory != null && _inventory.ContainsItem(item)))
            {
                MarkDirty();
            }
        }

        private static void Rebuild(Player player)
        {
            MaxPools.Snapshot before = MaxPools.Take();
            AggregateBuilder.Build(player);
            if (ItemEffects.Enabled)
            {
                AggregateHost.Apply(player);
            }
            else
            {
                AggregateHost.Remove(player);
            }
            FieldWrites.Apply(player, AggregateHost.Current);
            MaxPools.RefreshIfChanged(player, before);
            player.GetInventory().UpdateTotalWeight();
            AfterRebuild(player);
            LogRebuild();
        }

        // Phase 2 state that follows the new totals: the player's clones of Wet and Tared, the equipment top-up and
        // movement refund, the coin stacks, and the stats other peers read from this player's ZDO.
        private static void AfterRebuild(Player player)
        {
            StatusEffectTweaks.Refresh(player);
            Equipment.Refresh(player);
            AttackBonuses.RefreshCoins(player, AggregateBuilder.Critical[EffectKind.CoinDamage] > 0f);
            PlayerStats.Publish(player);
        }

        private static void LogRebuild()
        {
            if (ModSettings.LogEffectRebuilds == null || !ModSettings.LogEffectRebuilds.Value)
            {
                return;
            }
            StringBuilder text = new StringBuilder($"effects rebuilt: {AggregateBuilder.ActiveAffixCount} active affixes");
            foreach (EffectChannelTotal total in EffectTotals.Snapshot().Channels)
            {
                text.Append($"; {total.Key} {Numbers.Format(total.Applied)}");
            }
            Log.Info(text.ToString());
        }
    }

    /// <summary>Drives <see cref="EffectRuntime.Tick"/> once per frame. Lives on a hidden, persistent object.</summary>
    internal sealed class EffectDriver : MonoBehaviour
    {
        private float _nextErrorLog;

        private void Update()
        {
            try
            {
                EffectRuntime.Tick();
                EffectRuntime.TickShared();
            }
            catch (Exception e)
            {
                // At most one log line per 10 s: a failure that repeats every frame must not flood the log.
                if (Time.time >= _nextErrorLog)
                {
                    _nextErrorLog = Time.time + 10f;
                    Log.Error($"effects update failed: {e}");
                }
            }
        }
    }
}
