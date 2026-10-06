using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HaloMenu.API;
using HaloMenu.Config;

namespace HaloMenu.Api
{
    /// <summary>The plugin's implementation of the API contract, handed to <see cref="HaloMenuAPI.Provide"/>.
    /// Owns every ring: the default one plus whatever dependent mods create.</summary>
    public sealed class HaloMenuServiceImpl : HaloMenuService
    {
        public const string DefaultRingId = "halomenu.default";

        private readonly ConfigFile config;
        private readonly Dictionary<string, RingRuntime> rings = new Dictionary<string, RingRuntime>();

        public RingRuntime DefaultRing { get; }

        /// <summary>Rebuilt only when a ring is created (rare), so the per-frame driver never allocates walking it.</summary>
        public RingRuntime[] AllRings { get; private set; }

        public HaloMenuServiceImpl(ConfigFile config, RingSettings defaultRingSettings)
        {
            this.config = config;
            DefaultRing = new RingRuntime(DefaultRingId, defaultRingSettings);
            AddRing(DefaultRing);
        }

        public override void Register(RingEntry entry) => DefaultRing.Add(entry);

        public override Ring CreateRing(string ringId)
        {
            if (rings.TryGetValue(ringId, out RingRuntime existing))
                return existing;
            RingRuntime ring = new RingRuntime(ringId, BindQuietly(ringId));
            AddRing(ring);
            return ring;
        }

        /// <summary>A ring's 18 entries bound with saving held, then the .cfg written once, not once per Bind.</summary>
        private RingSettings BindQuietly(string ringId)
        {
            bool saving = config.SaveOnConfigSet;
            config.SaveOnConfigSet = false;
            try
            {
                return RingSettings.Bind(config, ringId, RingDefaults.Default());
            }
            finally
            {
                config.SaveOnConfigSet = saving;
                if (saving)
                    config.Save();
            }
        }

        private void AddRing(RingRuntime ring)
        {
            rings[ring.Id] = ring;
            AllRings = rings.Values.ToArray();
        }
    }
}
