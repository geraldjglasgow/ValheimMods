using System.Collections.Generic;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Gilded, the loot goblin: it pays several times over (<see cref="Loot.GildedLoot"/>), so it never fights a player -
    /// it runs from any it sees. This class decides; two patches act on the decision. While a player stands within
    /// <c>flee distance</c> and the game's own sight check says the creature can see them - line of sight, its view cone
    /// until alerted, the player's stealth, mist - it runs from the nearest such player with the game's own flee
    /// (<see cref="GildedFleePatch"/>), and keeps running a moment after losing sight, or after a player's hit from any
    /// range, so one tree trunk does not stop it dead. Players are never its enemies (<see cref="GildedEnemyPatch"/>), so
    /// it never targets, chases or strikes one; with nobody in sight it goes about its life. Only a player the game
    /// itself would call its enemy counts, so a tamed Gilded creature flees no friend. An animal (AnimalAI) takes no part
    /// in any of this: it already runs from whatever threatens it and never attacks. Attached on every machine and
    /// registered so the per-step patches find it with one lookup, and look at all only while a Gilded creature is
    /// loaded; the decision runs where the AI runs, the owner, so a hand-over simply moves it. It also puts on the
    /// glitter. No health, damage or speed cost: the movement clamp already keeps every creature outrunnable.
    /// </summary>
    public sealed class GildedBehaviour : MonoBehaviour
    {
        /// <summary>Seconds it keeps running from where it last saw (or was hit by) a player. A judgement call: long
        /// enough to carry it past a tree or a rock, short enough that a player who backs off is soon forgotten.</summary>
        private const float FleeMemory = 3f;

        private static readonly Dictionary<BaseAI, GildedBehaviour> ByAI = new Dictionary<BaseAI, GildedBehaviour>();
        private static readonly HashSet<Character> Bodies = new HashSet<Character>();

        private Character _character = null!;
        private EliteController _controller = null!;
        private MonsterAI? _ai;
        private float _fleeDistance = 30f;
        private Vector3 _fleeFrom;
        private float _fleeUntil;

        /// <summary>True while any Gilded monster is loaded here; when false, it is the per-step patches' whole cost.</summary>
        public static bool Any => ByAI.Count > 0;

        /// <summary>The Gilded behaviour steering this AI, or null for every other creature.</summary>
        public static GildedBehaviour? For(BaseAI ai) => ByAI.TryGetValue(ai, out GildedBehaviour gilded) ? gilded : null;

        /// <summary>True for a loaded Gilded creature whose MonsterAI would otherwise count players as its enemies.</summary>
        public static bool ShunsPlayers(Character character) => Bodies.Contains(character);

        private void Start() => Guard.Run("GildedBehaviour.Start", Setup);

        private void Setup()
        {
            gameObject.AddComponent<GildedGlitter>(); // every Gilded creature glitters, animal or monster
            _character = GetComponent<Character>();
            _controller = GetComponent<EliteController>();
            _ai = GetComponent<MonsterAI>();
            if (_character == null || _controller == null || _ai == null)
            {
                return; // no MonsterAI: an animal already flees whatever threatens it
            }
            _fleeDistance = _controller.Rules.PowerOf(Mutation.Gilded, Fields.FleeDistance); // never large-star enhanced
            ByAI[_ai] = this;
            Bodies.Add(_character);
            _character.m_onDamaged += OnDamaged;
        }

        // `is not null` rather than Unity's ==: the siblings may already read as destroyed here, and the entries must go.
        private void OnDestroy()
        {
            if (_ai is not null)
            {
                ByAI.Remove(_ai);
            }
            if (_character is not null)
            {
                Bodies.Remove(_character);
                _character.m_onDamaged -= OnDamaged;
            }
        }

        /// <summary>
        /// Owner only, once per AI step: the point to run from while a player is in sight (or was a moment ago), or
        /// false to let the creature be itself. A sleeping creature sees nothing; the game wakes it by its own rules.
        /// </summary>
        internal bool FleeFrom(out Vector3 from)
        {
            from = _fleeFrom;
            if (_ai == null || _character.IsDead() || _ai.IsSleeping() || !_controller.IsOwner())
            {
                return false;
            }
            Player? seen = NearestSeen();
            if (seen != null)
            {
                Remember(seen.transform.position);
            }
            from = _fleeFrom;
            return Time.time < _fleeUntil;
        }

        private Player? NearestSeen()
        {
            Player? nearest = null;
            float best = _fleeDistance;
            foreach (Player player in Player.GetAllPlayers())
            {
                float distance = Vector3.Distance(player.transform.position, transform.position);
                if (distance <= best && Sees(player))
                {
                    nearest = player;
                    best = distance;
                }
            }
            return nearest;
        }

        /// <summary>
        /// The game's own sight check with the flee distance as its range, so the setting means the same on every
        /// creature: line of sight, the view cone until alerted, the player's stealth and mist all still count. Only a
        /// player the game itself would call this creature's enemy is a threat - never a tamed creature's friend.
        /// </summary>
        private bool Sees(Player player)
        {
            if (_ai == null || player.IsDead() || !GildedEnemyPatch.Vanilla(_character, player))
            {
                return false;
            }
            Vector3 eye = _character.m_eye != null ? _character.m_eye.position : _character.GetCenterPoint();
            return BaseAI.CanSeeTarget(transform, eye, _fleeDistance, _ai.m_viewAngle, _ai.IsAlerted(),
                _ai.m_mistVision, player);
        }

        /// <summary>A player's hit sends it running from them, seen or not; damage lands on the owner, as the flee runs.</summary>
        private void OnDamaged(float damage, Character attacker) =>
            SafeCall.Run("Gilded flee on hit", () => Startle(attacker));

        private void Startle(Character attacker)
        {
            if (attacker != null && attacker.IsPlayer() && _controller.IsOwner()
                && GildedEnemyPatch.Vanilla(_character, attacker))
            {
                Remember(attacker.transform.position);
            }
        }

        private void Remember(Vector3 from)
        {
            _fleeFrom = from;
            _fleeUntil = Time.time + FleeMemory;
        }
    }
}
