using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using LocalEffects;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The casters' effects, seen by every player near: the caster's machine sends one routed RPC
    /// (<see cref="Rpc"/>, through <see cref="NearbyCast"/>) and each machine draws a local copy of the workshop effect
    /// (<c>ee_casterfx_meadows</c>, dressed by <see cref="BundleEffects"/>) and plays a game sound with it. Nothing is
    /// networked but the message; a dedicated server draws nothing. The cast flash and the chant ring are authored in
    /// Hearthsong's gold and turned to each spell's colour once (<see cref="Tinted"/>).
    /// </summary>
    public static class CasterFx
    {
        public const string Cast = "ee_fx_cast";
        public const string Mend = "ee_fx_mend";
        public const string Chant = "ee_fx_chant";
        public const string Bolt = "ee_fx_bolt";
        public const string BoltHit = "ee_fx_bolt_hit";
        public const string HealMarker = "ee_fx_heal_target";
        public const string MendBolt = "ee_fx_mendbolt";
        public const string Charge = "ee_fx_charge";
        public const string ChargeFull = "ee_fx_charge_full";
        public const string ChargeReady = "ee_fx_charge_ready";
        public const string ChargeStream = "ee_fx_charge_stream";

        private const string Rpc = "EliteEquipment_CasterFx";
        private const float Range = 60f;
        // LocalEffect sizes a flash from this radius; at it, an effect is drawn as authored.
        private const float AsAuthored = 4f;

        private static readonly Dictionary<string, GameObject> prepared = new Dictionary<string, GameObject>();

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<string, Vector3, ZDOID, string>(Rpc, Receive);
        }

        /// <summary>Shows an effect to every player near <paramref name="at"/>, this one included unless told otherwise.</summary>
        public static void Send(string effect, Vector3 at, ZDOID target, Spell spell, bool self = true) =>
            NearbyCast.Send(at, Range, self, Rpc, effect, at, target, spell != null ? spell.Key : "");

        /// <summary>The heal's motes on a healed character, and its sound, on this machine only.</summary>
        public static void MendOn(Character healed)
        {
            if (healed == null || Headless)
                return;
            LocalEffect.Attach(Prepared(Mend), healed.transform, healed.transform.position, endless: false);
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(CasterSounds.For(Mend)), healed.transform.position);
        }

        /// <summary>A one-shot workshop effect at a point, as authored, and its game sound, on this machine only.</summary>
        public static void Here(string effect, Vector3 at)
        {
            if (Headless)
                return;
            LocalEffect.Flash(Prepared(effect), at, AsAuthored);
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(CasterSounds.For(effect)), at);
        }

        /// <summary>A dressed, inactive copy of a workshop effect; null where nothing is drawn or the bundle is missing.</summary>
        public static GameObject Prepared(string name)
        {
            if (prepared.TryGetValue(name, out GameObject effect))
                return effect;
            AssetBundle bundle = Headless ? null : CasterBundles.Get(CasterBundles.Effects);
            effect = bundle != null ? CasterSafe.Run("caster effect " + name, () => BundleEffects.Prepare(bundle, name)) : null;
            prepared[name] = effect;
            return effect;
        }

        private static bool Headless => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        /// <summary>
        /// An effect in a spell's colour: the gold effect itself for a gold spell (or none), else a copy turned once by the
        /// spell's hue and saturation (<see cref="Spell.HueShift"/>).
        /// </summary>
        private static GameObject Tinted(string name, Spell spell)
        {
            if (spell == null || spell.Gold)
                return Prepared(name);
            string tinted = name + "_" + spell.Key;
            if (prepared.TryGetValue(tinted, out GameObject effect))
                return effect;
            AssetBundle bundle = Headless ? null : CasterBundles.Get(CasterBundles.Effects);
            effect = bundle != null ? CasterSafe.Run("caster effect " + tinted, () => BundleEffects.Prepare(bundle, name, tinted)) : null;
            if (effect != null)
                EffectTint.Shift(effect, spell.HueShift, spell.Saturation);
            prepared[tinted] = effect;
            return effect;
        }

        private static void Receive(long sender, string effect, Vector3 at, ZDOID target, string spell) =>
            CasterSafe.Run("caster effect", () => Play(effect, at, target, Spells.Find(spell)));

        private static void Play(string effect, Vector3 at, ZDOID target, Spell spell)
        {
            if (Headless || Player.m_localPlayer == null)
                return;
            Character character = Find(target);
            if (effect == Mend && character != null)
            {
                MendOn(character);
                return;
            }
            if (effect == MendBolt)
            {
                if (character != null)
                    HealFlight.Launch(at, character, MendOn);
                return;
            }
            LocalEffect.Flash(Tinted(effect, spell), at, AsAuthored);
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(CasterSounds.For(effect)), at);
        }

        /// <summary>The character with this id on this machine, or null (none, or not loaded here).</summary>
        private static Character Find(ZDOID id)
        {
            GameObject found = id.IsNone() || ZNetScene.instance == null ? null : ZNetScene.instance.FindInstance(id);
            return found != null ? found.GetComponent<Character>() : null;
        }
    }
}
