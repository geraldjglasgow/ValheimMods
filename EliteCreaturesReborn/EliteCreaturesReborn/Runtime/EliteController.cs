using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// One per creature and the single home of its resolved identity at runtime. The multiplayer rule it enforces is
    /// "rolled once by the owner, stored in the ZDO, read by everyone": the owner rolls and writes; every machine loads
    /// and applies. A machine that meets a creature its owner has not rolled yet stays pending and keeps checking, so it
    /// updates itself exactly once, cleanly, the moment the state arrives - and a creature handed to a machine before it
    /// was ever rolled is rolled by that new owner. It keeps the creature at vanilla level 1, applies the fixed scaling,
    /// and installs the per-frame behaviours (which self-gate on ownership). Patches read the traits and rules from here.
    /// </summary>
    public sealed class EliteController : MonoBehaviour
    {
        private Character _character = null!;
        private ZNetView _nview = null!;
        private BaseSpeeds _baseSpeeds;
        private bool _ready;
        private bool _pending;
        private bool _movementClamped;
        private CreatureTraits? _forced;
        private Aspect? _forcedAspect;
        private Heightmap.Biome _forcedBiome;
        private bool _isBoss;

        public CreatureTraits Traits { get; private set; } = new CreatureTraits(0, 0);
        public BiomeRules Rules { get; private set; } = RuleState.Active.Defaults;
        public bool FreshlyResolved { get; private set; }
        public float SwingSpeedFactor { get; private set; } = 1f;
        public Character Creature => _character;
        public ZNetView View => _nview;
        public bool Ready => _ready;

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
            if (_isBoss && !RuleState.Active.Boss.Enabled && !RuleState.Active.Boss.Aspects.Enabled)
            {
                enabled = false; // boss stars and aspects both off: the boss is left exactly as the game ships it
                return;
            }
            EliteRpc.EnsureRegistered();
            CreatureRpc.Register(_nview, _character, this); // on every machine, so a routed command reaches the owner
            ThievingRpc.Register(_nview, this); // same shape: the robbed player's client routes a steal to the owner
            if (!TryResolve())
            {
                _pending = true; // a non-owner meeting an unrolled creature: wait, then apply exactly once
                return;
            }
            Apply();
        }

        // Runs only while pending: it polls the ZDO until the owner's roll arrives (or until this machine becomes the
        // owner and rolls it itself), then applies once and stops. No flicker: the creature is plain until this fires.
        private void Update()
        {
            if (_pending)
            {
                Guard.Run("EliteController.Resolve", PollResolve);
            }
        }

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
            Rules = ResolveRules(TraitStore.GetBiome(zdo));
            return true;
        }

        /// <summary>A boss scales on the boss table; every other creature on its biome's.</summary>
        private BiomeRules ResolveRules(Heightmap.Biome biome)
        {
            return _isBoss ? BossView.For(RuleState.Active.Boss) : RuleState.Active.For(biome);
        }

        // OWNER ONLY: the single roll, written to the ZDO for everyone. Reached here only when this machine owns it. When
        // a console spawn has forced exact traits, those are written verbatim instead - bypassing every chance and cap.
        private void RollFresh(ZDO zdo)
        {
            Heightmap.Biome biome = _forced != null ? _forcedBiome : Heightmap.FindBiome(_character.transform.position);
            CreatureTraits fresh = _forced ?? RollFor(biome);
            TraitStore.Save(zdo, fresh);
            TraitStore.SetBiome(zdo, biome);
            _character.SetLevel(1);
            FreshlyResolved = true;
            _forced = null;
            _forcedAspect = null;
        }

        // A boss draws a star count from the boss distribution and an aspect - the altar's, when it was summoned at one,
        // else its own roll - and never a mutation, so the roll cannot be shared with the creature path.
        private CreatureTraits RollFor(Heightmap.Biome biome)
        {
            if (_isBoss)
            {
                BossRules boss = RuleState.Active.Boss;
                int stars = boss.Enabled ? TraitRoller.RollStars(BossView.For(boss)) : 0;
                Aspect aspect = boss.Aspects.Enabled
                    ? _forcedAspect ?? AspectRoller.Roll(Utils.GetPrefabName(gameObject), null) : Aspect.None;
                return new CreatureTraits(stars, aspect);
            }
            return TraitRoller.Roll(RuleState.Active.For(biome), RuleState.Active.MaxMutations, RuleState.Active);
        }

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
        /// Altar hook: the aspect locked in at the offering, handed over in the same frame the altar instantiates the
        /// boss. The stars still roll; only the aspect is fixed. Ignored once the boss has been rolled.
        /// </summary>
        public void ForceAspect(Aspect aspect) => _forcedAspect = aspect;

        private void Apply()
        {
            _baseSpeeds = BaseSpeeds.Capture(_character);
            SwingSpeedFactor = StatMath.SwingSpeedMultiplier(Rules, Traits);
            StatApplier.ApplySize(_character, Rules, Traits); // local, deterministic: every machine scales its own copy
            RefreshMovement(0f);
            if (_nview.IsOwner())
            {
                StatApplier.ApplyHealth(_character, Rules, Traits, FreshlyResolved); // owner writes s_maxHealth; others read it
            }
            if (!_isBoss)
            {
                BehaviourInstaller.Install(this); // mutation behaviours; a boss has no mutations to install
            }
            else
            {
                AspectInstaller.Install(this); // a boss's aspect behaviours, and on its first roll the twin or phantoms
            }
            _ready = true;
            enabled = false; // resolution is done; stop the poll. Other components still read this via GetComponent.
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
                Log.Info($"{name}: run speed clamped to the player's ({cap:0.#}) so it stays outrunnable");
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
