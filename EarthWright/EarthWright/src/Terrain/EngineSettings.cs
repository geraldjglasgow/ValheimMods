using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The engine's settings: section "3. Operations" (the hard caps the owner of the ground puts on every stroke, and the
    /// tuning of the operations) here, section "6. Height Limits" in <see cref="LimitSettings"/> and
    /// <see cref="SlopeSettings"/>. Every key here changes the world, so every key is synced and lockable. The brush's
    /// own ranges (what a player can dial in) belong to the Brush module; these caps are what an owner accepts.
    /// </summary>
    public static class EngineSettings
    {
        /// <summary>The largest radius an approved admin edit may have (the admin terrain command's own limit).</summary>
        public const float AdminMaxRadius = 128f;

        /// <summary>The largest raise, lower or offset an approved admin edit may apply (the height limits' own maximum).</summary>
        public const float AdminMaxAmount = 512f;

        public static ConfigEntry<float> MaxRadius { get; private set; }
        public static ConfigEntry<float> MaxAmount { get; private set; }
        public static ConfigEntry<float> MaxStep { get; private set; }
        public static ConfigEntry<float> EasePower { get; private set; }
        public static ConfigEntry<float> PaintHardness { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindOperations(synced);
            LimitSettings.Bind(synced);
            SlopeSettings.Bind(synced);
            EngineCommands.Register();
        }

        private static void BindOperations(SyncedConfiguration synced)
        {
            MaxRadius = synced.Bind(Sections.Operations, "Max Radius", 100f,
                "The largest brush radius (also ring, frame and rectangle sizes) the owner of the ground accepts from a player, in metres. Larger strokes are cut down to this whatever the player's own settings say. Admin edits the server approved (admin tools, the admin terrain command) may reach 128 m.",
                acceptableValues: new AcceptableValueRange<float>(1f, 100f));
            MaxAmount = synced.Bind(Sections.Operations, "Max Amount", 50f,
                "The largest raise, lower or offset one stroke may apply, in metres. Admin edits the server approved may apply up to 512 m (the height limits still apply unless they are lifted too).",
                acceptableValues: new AcceptableValueRange<float>(0.1f, 512f));
            MaxStep = synced.Bind(Sections.Operations, "Max Step", 1000f,
                "The largest height change per point that one Ease or Step levelling stroke may make, in metres. A stroke that asks for no limit gets this one.",
                acceptableValues: new AcceptableValueRange<float>(1f, 1000f));
            EasePower = synced.Bind(Sections.Operations, "Ease Power", 1f,
                "How the Ease level style fades toward the soft edge of the brush. 1 follows the edge hardness exactly; higher values ease less near the rim, lower values more.",
                acceptableValues: new AcceptableValueRange<float>(0.25f, 4f));
            PaintHardness = synced.Bind(Sections.Operations, "Paint Edge Hardness", 0.8f,
                "The least edge hardness used for painting (0 to 1), so painted paths and fields keep a crisp edge like the game's own even when the height brush is soft. 0 paints with the brush's own hardness.",
                acceptableValues: new AcceptableValueRange<float>(0f, 1f));
        }

        public static float MaxRadiusValue => MaxRadius != null ? MaxRadius.Value : 100f;

        public static float MaxAmountValue => MaxAmount != null ? MaxAmount.Value : 50f;

        public static float MaxStepValue => MaxStep != null ? MaxStep.Value : 1000f;

        public static float EasePowerValue => EasePower != null ? EasePower.Value : 1f;

        public static float PaintHardnessValue => PaintHardness != null ? PaintHardness.Value : 0.8f;
    }
}
