using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using EliteCreaturesReborn.Visuals;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// One per creature and the single home of its resolved identity at runtime. The multiplayer rule it enforces is
    /// "rolled once by the owner, stored in the ZDO, read by everyone": the owner rolls and writes; every machine loads
    /// and applies. A machine that meets a creature its owner has not rolled yet stays pending and keeps checking, so it
    /// updates itself exactly once, cleanly, the moment the state arrives - and a creature handed to a machine before it
    /// was ever rolled is rolled by that new owner. It keeps the creature at vanilla level 1 (unless this mod's stars are
    /// off for its kind, <see cref="RuleSet.KeepsLevel"/>), applies the fixed scaling,
    /// and installs the per-frame behaviours (which self-gate on ownership). Patches read the traits and rules from here.
    /// </summary>
    public sealed class EliteController : MonoBehaviour
    {
        private Character _character = null!;
        private ZNetView _nview = null!;
        private BaseSpeeds _baseSpeeds;
        private bool _ready;
        private bool _pending;
        private bool _dressPending;
        private bool _movementClamped;
        private CreatureTraits? _forced;
        private BossDraw? _forcedDraw;
        private Heightmap.Biome _forcedBiome;
        private bool _isBoss;

        public CreatureTraits Traits { get; private set; } = new CreatureTraits(0, 0);
        public BiomeRules Rules { get; private set; } = RuleState.Active.Defaults;
        public bool FreshlyResolved { get; private set; }
        public float SwingSpeedFactor { get; private set; } = 1f;
        public Character Creature => _character;
        public ZNetView View => _nview;
        public bool Ready => _ready;

        /// <summary>The decorated name the hover name shows, kept while the name it decorates stays the same.</summary>
        public NameMemo Name { get; } = new NameMemo();

        private void Start() => Guard.Run("EliteController.Start", Setup);

        private void Setup()
        {
            _character = GetComponent<Character>();
            _nview = _character != null ? _character.GetComponent<ZNetView>() : null!;
            if (_character == null || _nview == null || !_nview.IsValid() || _character.IsPlayer())
            {
                enabled = false;
                return;
            }
            _isBoss = _character.IsBoss();
            if (LeftAsShipped())
            {
                enabled = false;
                return;
            }
            EliteRpc.EnsureRegistered();
            CreatureRpc.Register(_nview); // on every machine; the commands follow once the traits are known (Apply)
            RuleState.Changed += OnRulesChanged;
            if (!TryResolve())
            {
                _pending = true; // a non-owner meeting an unrolled creature: wait, then apply exactly once
                return;
            }
            Apply();
        }

        // Boss stars and aspects both off, and no mutations another mod fixed for it: the boss is left exactly as the game
        // ships it.
        private bool LeftAsShipped() => _isBoss && !RuleState.Active.Boss.Enabled && !RuleState.Active.Boss.Aspects.Enabled
            && Registrations.MutationsOf(PrefabName) == 0;

        // Runs only while pending: it polls the ZDO until the owner's roll arrives (or until this machine becomes the
        // owner and rolls it itself), then applies once and stops. No flicker: the creature is plain until this fires.
        // It also runs while a disguised creature (a dormant Elite Creatures Pack mimic) waits to be dressed, and dresses it once it wakes.
        private void Update()
        {
            if (_pending)
            {
                Guard.Run("EliteController.Resolve", static self => self.PollResolve(), this);
            }
            else if (_dressPending && !Disguise.Holds(_character))
            {
                _dressPending = false;
                Guard.Run("EliteController.Dress", Dress);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            RuleState.Changed -= OnRulesChanged;
            SwingRegistry.Forget(_character);
            Devourers.Forget(_character);
            ReadyElites.Forget(_character);
            AspectBearers.Forget(_character);
        }

        // An edited or newly synced rule file reaches creatures already loaded: every power read from Rules as it acts
        // (Warding's reflect and its ceiling, Leeching, knockback...) takes the new value at once. What was applied
        // from the rules when it loaded (health, size, speed) stays until the creature next loads.
        private void OnRulesChanged() => Guard.Run("EliteController.Rules", () =>
        {
            ZDO? zdo = _ready && _nview != null && _nview.IsValid() ? _nview.GetZDO() : null;
            if (zdo != null)
            {
                Rules = ResolveRules(TraitStore.GetBiome(zdo));
            }
        });

        private void PollResolve()
        {
            if (_nview == null || !_nview.IsValid() || !TryResolve())
            {
                return;
            }
            _pending = false;
            Apply();
        }

        /// <summary>Owner rolls if unrolled; every machine then loads. False when a non-owner meets it before the roll.</summary>
        private bool TryResolve()
        {
            ZDO zdo = _nview.GetZDO();
            if (zdo == null)
            {
                return false;
            }
            if (_nview.IsOwner() && !TraitStore.IsResolved(zdo))
            {
                RollFresh(zdo);
            }
            if (!TraitStore.IsResolved(zdo))
            {
                return false;
            }
            Traits = TraitStore.Load(zdo);
            Traits.Mask &= ~MutationBars.OfKind(PrefabName); // a cloaked Deathsquito saved before the bar loads plain
            Rules = ResolveRules(TraitStore.GetBiome(zdo));
            return true;
        }

        /// <summary>
        /// A boss scales on the boss table (with its biome's mutation numbers when it carries fixed mutations); every other
        /// creature on its biome's, with its own `creatures:` entry's mutation keys on top. Every machine resolves the same:
        /// the biome is in the ZDO, the prefab is the object's. A creature that keeps its game level takes no star power:
        /// the game's level scales it instead.
        /// </summary>
        private BiomeRules ResolveRules(Heightmap.Biome biome)
        {
            RuleSet set = RuleState.Active;
            bool keeps = set.KeepsLevel(Traits.Stars, _isBoss);
            if (_isBoss)
            {
                return BossView.For(set.Boss, keeps, Traits.Any ? set.For(biome, PrefabName) : null);
            }
            BiomeRules rules = set.For(biome, PrefabName);
            return keeps ? set.Unstarred(rules) : rules;
        }

        private string PrefabName => Utils.GetPrefabName(gameObject);

        // OWNER ONLY: the single roll, written to the ZDO for everyone. Reached here only when this machine owns it. When
        // a console spawn has forced exact traits, those are written verbatim instead - bypassing every chance and cap.
        // This mod's stars replace the game's level, so it goes back to 1 - unless they are off for this kind and it has
        // none, when the level the game or another mod gave it stays.
        private void RollFresh(ZDO zdo)
        {
            Heightmap.Biome biome = _forced != null ? _forcedBiome : Heightmap.FindBiome(_character.transform.position);
            CreatureTraits fresh = _forced ?? RollFor(biome);
            TraitStore.Save(zdo, fresh);
            TraitStore.SetBiome(zdo, biome);
            if (!RuleState.Active.KeepsLevel(fresh.Stars, _isBoss))
            {
                _character.SetLevel(1);
            }
            FreshlyResolved = true;
            _forced = null;
            _forcedDraw = null;
        }

        // The one roll every creature takes (CreatureRoll): a boss's stars and aspects - the altar's, locked at the
        // offering, when it was summoned at one - and a creature's stars and mutations, with what another mod registered
        // for the prefab on top. A Bountiful draw brings its extras with it.
        private CreatureTraits RollFor(Heightmap.Biome biome) =>
            _forcedDraw is BossDraw draw ? CreatureRoll.FromDraw(_character, draw) : CreatureRoll.For(_character, biome);

        /// <summary>
        /// Console-spawn hook: force exact traits and biome, bypassing every roll and cap. Called on the machine that
        /// just instantiated (and therefore owns) the creature, in the same frame, before this component's Start runs, so
        /// the forced traits are written as a fresh roll and the creature fills to its scaled health.
        /// </summary>
        public void ForceTraits(CreatureTraits traits, Heightmap.Biome biome)
        {
            _forced = traits;
            _forcedBiome = biome;
        }

        /// <summary>
        /// Altar hook: the stars and aspects locked in at the offering (a Bountiful one's extras with them), handed over in
        /// the same frame the altar instantiates the boss, so the boss is exactly what the bowl showed. Ignored once the
        /// boss has been rolled.
        /// </summary>
        public void ForceDraw(BossDraw draw) => _forcedDraw = draw;

        // The numbers apply at once, so a hit that wakes a disguised creature is already scaled and its health is not
        // refilled on waking. What can be seen - size, the star look, the mutation and aspect behaviours - waits while a
        // disguise holds (Update dresses it on waking): a dormant mimic must look exactly like the chest it copies.
        private void Apply()
        {
            _baseSpeeds = BaseSpeeds.Capture(_character);
            SwingSpeedFactor = StatMath.SwingSpeedMultiplier(Rules, Traits);
            RefreshMovement(0f);
            if (_nview.IsOwner())
            {
                StatApplier.ApplyHealth(_character, Rules, Traits, FreshlyResolved); // owner writes s_maxHealth; others read it
            }
            _ready = true;
            CreatureRpc.RegisterCommands(this, _isBoss || Traits.AnyAspect); // on every machine, so a routed command reaches the owner
            ReadyElites.Track(this); // so the nameplate finds it without a component search
            SwingRegistry.Track(this); // the swing speed is fixed from here on, so AnimSpeedPatch skips a creature at 1
            Devourers.Track(this); // so the enmity patch finds a devourer without a component search
            if (Disguise.Holds(_character))
            {
                _dressPending = true; // keep polling in Update until it wakes
                return;
            }
            Dress();
            enabled = false; // resolution is done; stop the poll. Other components still read this via GetComponent.
        }

        private void Dress()
        {
            StatApplier.ApplySize(_character, Rules, Traits); // local, deterministic: every machine scales its own copy
            StarLook.Apply(_character, Traits.Stars); // the game's tint for the stars, before any mutation reads materials
            BehaviourInstaller.Install(this); // mutation behaviours: a creature's, or the fixed ones of a registered boss
            AspectInstaller.Install(this); // aspect behaviours: a boss's or a registered creature's; on a first roll the twin or phantoms
        }

        /// <summary>Re-derives the creature's speeds from its captured base plus a live additive bonus (Devouring's slow).</summary>
        public void RefreshMovement(float devourBonus)
        {
            if (_character != null)
            {
                float factor = StatMath.MoveMultiplier(Rules, Traits, devourBonus);
                _baseSpeeds.ApplyTo(_character, ClampToPlayer(factor));
            }
        }

        /// <summary>No creature may outrun an unburdened player, however the multipliers land; clamp and log, not obey.</summary>
        private float ClampToPlayer(float factor)
        {
            float baseRun = _baseSpeeds.RunSpeed;
            float cap = PlayerSpeed.Reference();
            if (baseRun <= 0f || baseRun * factor <= cap)
            {
                return factor;
            }
            if (!_movementClamped)
            {
                _movementClamped = true;
                Log.Debug($"{name}: run speed clamped to the player's ({cap:0.#}) so it stays outrunnable");
            }
            return cap / baseRun;
        }

        /// <summary>Re-derives maximum health from the current traits and accumulated Devouring, on the owner.</summary>
        public void RefreshHealth()
        {
            if (_character != null && IsOwner())
            {
                StatApplier.ApplyHealth(_character, Rules, Traits, freshlyResolved: false);
            }
        }

        public bool IsOwner() => _nview != null && _nview.IsValid() && _nview.IsOwner();
    }
}
