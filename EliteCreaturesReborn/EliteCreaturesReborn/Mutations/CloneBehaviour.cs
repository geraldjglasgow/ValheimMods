using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Cloning: when it squares up to a player - its AI alerted on that player, an enemy it can sense within `range` -
    /// it leaves a decoy in its place and goes unseen. The decoy (<see cref="CloneDecoy"/>) is the one the player sees: the
    /// same name, stars, gear and health, fighting on, but its blows do no harm. The creature itself fights on unseen at
    /// full strength, and its first blow to land on a player shows it again (<see cref="CloneHits"/>); the decoy goes in a
    /// puff. Killing the decoy first shows it too, and so does the decoy's `decoy life` running out (everyone ran). It does
    /// this `times` times in its life, `cooldown` seconds apart; a tamed one never does.
    /// <para>
    /// Decided on the owner, which holds the AI: it makes the decoy (<see cref="CloneSpawner"/>) and writes the trick to
    /// the ZDO (<see cref="CloneStore"/>), and ends it there - on the struck player's word, on its decoy being gone, or on
    /// the clock. Drawn on every machine from the ZDO alone: while it says the creature hides, each one hides its body and
    /// nameplate (<see cref="CloneVeil"/>), and the moment it says otherwise each one shows it with the reveal. So a
    /// hand-over mid-trick is free, a creature whose decoy left with its owner or its zone shows itself on its next owner's
    /// first frame, and its death needs no message: its decoy, finding it gone, leaves on its own.
    /// </para>
    /// </summary>
    public sealed class CloneBehaviour : MonoBehaviour
    {
        /// <summary>From a struck player's own machine to this creature's owner: its unseen blow landed.</summary>
        public const string HitRpc = "ecr_clone_hit";

        private Character _character = null!;
        private EliteController _controller = null!;
        private BaseAI? _ai;
        private readonly CloneVeil _veil = new CloneVeil();
        private CloakBehaviour? _cloak;
        private System.Action? _step;
        private bool _headless;
        private bool _spent;
        private float _senseTimer;

        /// <summary>Seconds between looks for the player it is fighting: sensing is a raycast.</summary>
        private const float SenseInterval = 0.25f;

        /// <summary>True while its body is hidden on this machine, for the nameplate to hide with it.</summary>
        public bool Hidden => _veil.Active;

        private void Start() => Guard.Run("CloneBehaviour.Start", Setup);

        private void Setup()
        {
            _character = GetComponent<Character>();
            _controller = GetComponent<EliteController>();
            if (_character == null || _controller == null || _controller.View == null || !_controller.View.IsValid())
            {
                enabled = false;
                return;
            }
            _ai = _character.GetBaseAI();
            _cloak = GetComponent<CloakBehaviour>();
            _headless = BlinkEffects.Headless();
            _controller.View.Register(HitRpc, OnHit); // on every machine: whichever owns it then is the one that acts
        }

        private void Update() => Guard.Run("CloneBehaviour.Update", _step ??= Step);

        // The owner decides first, so on its own screen the body hides in the very frame the decoy appears.
        private void Step()
        {
            ZNetView view = _controller.View;
            if (view == null || !view.IsValid())
            {
                return;
            }
            ZDO zdo = view.GetZDO();
            if (_controller.IsOwner() && !_character.IsDead())
            {
                Decide(zdo);
            }
            if (!_headless && _veil.Tick(gameObject, CloneStore.Hiding(zdo)))
            {
                Revealed();
            }
        }

        /// <summary>Owner: end a trick that is over, or begin one when it is due and a player is in reach.</summary>
        private void Decide(ZDO zdo)
        {
            if (CloneStore.Hiding(zdo))
            {
                if (Over(zdo))
                {
                    End(zdo);
                }
                return;
            }
            Player? target = Due(zdo) ? Engaged() : null;
            if (target != null)
            {
                Begin(zdo, target);
            }
        }

        /// <summary>Its decoy is gone - killed, or left with its owner or its zone - or the decoy's time has run out.</summary>
        private bool Over(ZDO zdo)
        {
            if (ZDOMan.instance == null || ZDOMan.instance.GetZDO(CloneStore.Decoy(zdo)) == null)
            {
                return true;
            }
            float life = _controller.Rules.PowerOf(Mutation.Cloning, Fields.DecoyLife);
            return life > 0f && CloneStore.SecondsSinceMark(zdo) >= life;
        }

        /// <summary>A trick left and the cooldown past, wild, free to move, and a look for its player due. Once every
        /// trick is spent it stops asking; the cheap tests go first.</summary>
        private bool Due(ZDO zdo)
        {
            if (_spent || _character.IsTamed() || Busy() || (_senseTimer -= Time.deltaTime) > 0f)
            {
                return false;
            }
            _senseTimer = SenseInterval;
            int done = CloneStore.Done(zdo);
            _spent = done >= Times();
            return !_spent && (done == 0
                || CloneStore.SecondsSinceMark(zdo) >= _controller.Rules.PowerOf(Mutation.Cloning, Fields.Cooldown));
        }

        /// <summary>
        /// Mid-swing, staggered or latched on to something: the decoy takes over a body only in its ordinary stride, so the
        /// swap shows nothing. Nor while a cloak hides it: a decoy would appear out of nothing, seen at any distance.
        /// </summary>
        private bool Busy() =>
            _character.InAttack() || _character.IsStaggering() || _character.IsAttached() || (_cloak != null && _cloak.Hidden);

        /// <summary>Tricks in its life; a large star multiplies them.</summary>
        private int Times() =>
            Mathf.RoundToInt(Enhance.Magnitude(_controller.Rules, _controller.Traits, Mutation.Cloning, Fields.Times));

        /// <summary>
        /// The player it is fighting, when within `range`: its AI's alerted target, an enemy it can see or hear now. An
        /// animal's AI never holds a target, so an animal never does the trick.
        /// </summary>
        private Player? Engaged()
        {
            if (_ai == null || !_ai.IsAlerted() || !(_ai.GetTargetCreature() is Player player) || player.IsDead())
            {
                return null;
            }
            if (!BaseAI.IsEnemy(_character, player) || !_ai.CanSenseTarget(player))
            {
                return null;
            }
            float range = _controller.Rules.PowerOf(Mutation.Cloning, Fields.Range);
            return Vector3.Distance(player.transform.position, transform.position) <= range ? player : null;
        }

        /// <summary>Owner: the decoy takes its place and it hides. A decoy that could not be made still spends the trick.</summary>
        private void Begin(ZDO zdo, Player target)
        {
            ZDOID decoy = CloneSpawner.Make(_controller, target);
            CloneStore.Begin(zdo, decoy);
            Log.Diag(decoy != ZDOID.None
                ? $"{name} hides behind a decoy from {target.GetPlayerName()} ({CloneStore.Done(zdo)} of {Times()})"
                : $"{name}: its decoy could not be made; the trick is spent");
        }

        /// <summary>Owner: it shows itself. Its decoy, finding it no longer hides, leaves in a puff on its own owner.</summary>
        private void End(ZDO zdo)
        {
            CloneStore.End(zdo);
            Log.Diag($"{name} shows itself");
        }

        private void OnHit(long sender) => Guard.Run("CloneBehaviour.Hit", () =>
        {
            ZDO? zdo = _controller != null && _controller.IsOwner() ? _controller.View.GetZDO() : null;
            if (zdo != null && CloneStore.Hiding(zdo))
            {
                End(zdo); // its blow landed on a player: the trick is up
            }
        });

        /// <summary>Every machine, the frame the body shows again: the reveal, at the creature.</summary>
        private void Revealed()
        {
            BiomeRules rules = _controller.Rules;
            CloneEffects.Reveal(rules.PrefabOf(Mutation.Cloning, Fields.RevealEffect),
                rules.PrefabOf(Mutation.Cloning, Fields.RevealSound), _character.GetCenterPoint(),
                BlinkEffects.Radius(_character));
        }
    }
}
