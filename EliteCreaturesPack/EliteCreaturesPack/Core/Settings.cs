using System;
using System.Collections;
using BepInEx.Configuration;
using EliteCreaturesPack.Arsenal;
using EliteCreaturesPack.Ballista;
using EliteCreaturesPack.BearClaws;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Crossbow;
using EliteCreaturesPack.Headsman;
using EliteCreaturesPack.Kraken;
using EliteCreaturesPack.Mimic;
using EliteCreaturesPack.RimeGiant;
using EliteCreaturesPack.Slinger;
using SyncedConfig;
using UnityEngine;

namespace EliteCreaturesPack
{
    /// <summary>
    /// The General section, and the order every creature's section is bound in. Sections are numbered so the .cfg and
    /// configuration managers list them in this order. Every setting changes gameplay, so every one is synced from the
    /// server and locked while <c>Lock Configuration</c> is on there.
    /// </summary>
    public static class Settings
    {
        public const string General = "1 - General";

        public static ConfigEntry<bool> LockConfiguration { get; private set; } = null!;

        /// <summary>
        /// Once, on the main thread, the frame after one or more settings changed: an edit of the .cfg reloaded, or the
        /// server's values arriving (many at once on joining). Each creature puts its new numbers on its prefabs and on the
        /// ones already loaded, so a batch of changes costs one pass rather than one per value.
        /// </summary>
        public static event Action? Changed;

        /// <summary>Runs the next frame's <see cref="Changed"/> (the plugin, which is never destroyed).</summary>
        private static MonoBehaviour? host;

        /// <summary>The frame a <see cref="Changed"/> was put off from, or -1 when none is waiting.</summary>
        private static int waitingSince = -1;

        public static void Initialize(SyncedConfiguration config, MonoBehaviour plugin)
        {
            host = plugin;
            LockConfiguration = config.BindLocking(General, "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
            MimicSettings.Initialize(config);
            SlingerSettings.Initialize(config);
            RimeGiantSettings.Initialize(config);
            KrakenSettings.Initialize(config);
            XbowSettings.Initialize(config);
            XbowItemSettings.Initialize(config);
            ArsenalSettings.Initialize(config);
            HeadsmanSettings.Initialize(config);
            GreataxeSettings.Initialize(config);
            BearClawsSettings.Initialize(config);
            BallistaSettings.Initialize(config);
            config.Config.SettingChanged += (sender, args) => SafeCall.Run("settings changed", Coalesce);
        }

        /// <summary>
        /// A setting changed: <see cref="Changed"/> goes out next frame, once for every change until then. A wait older
        /// than a frame was lost (its coroutine stopped) and is started again; without a running plugin it goes out now.
        /// </summary>
        private static void Coalesce()
        {
            int frame = Time.frameCount;
            if (waitingSince >= 0 && frame <= waitingSince + 1)
            {
                return;
            }
            if (host == null || !host.isActiveAndEnabled)
            {
                Announce();
                return;
            }
            waitingSince = frame;
            host.StartCoroutine(NextFrame());
        }

        private static IEnumerator NextFrame()
        {
            yield return null;
            Announce();
        }

        private static void Announce()
        {
            waitingSince = -1;
            SafeCall.Run("settings changed", () => Changed?.Invoke());
        }

        /// <summary>A range from <paramref name="min"/> to <paramref name="max"/>, for a number setting.</summary>
        public static AcceptableValueRange<float> Range(float min, float max) => new AcceptableValueRange<float>(min, max);

        /// <summary>
        /// An entry whose default changed: a saved value still at the old default moves to the new one, so the change
        /// reaches .cfg files written before it; any other value stays.
        /// </summary>
        public static void Renew<T>(ConfigEntry<T> entry, T oldDefault)
        {
            if (Equals(entry.Value, oldDefault))
            {
                entry.Value = (T)entry.DefaultValue;
            }
        }
    }
}
