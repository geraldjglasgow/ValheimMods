using System;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The hall's hold on a creature, beside the game's own mind: on every arena creature, and on every creature that
    /// starts inside the hall, on every machine that loads it (<see cref="ArenaAttachPatch"/>, or <see cref="ArenaSpawn"/>
    /// as it makes one). It lists the creature in <see cref="ArenaFighters"/>, so the core's owner counts the living
    /// without a search. Once tagged, on every machine, it is made one of Hel's (<see cref="Refresh"/>). Every second, on
    /// the creature's owner only, it judges (<see cref="ArenaOrders"/>): a creature that appeared in the hall during a
    /// trial joins it, a tagged one whose trial is over vanishes, a tamed one is the players' now, and one left with no
    /// target is set on the nearest player. Ownership may move between machines; the next owner's copy picks up from the
    /// tag in the ZDO.
    /// </summary>
    internal sealed class ArenaFighter : MonoBehaviour
    {
        private const float TickSeconds = 1f;

        /// <summary>The group every arena creature shares: the game never sets two of one group on each other, so Draugr
        /// and Greydwarves of one wave fight the players, not each other.</summary>
        public const string Group = "ecr_arena";

        private ZNetView _nview = null!;
        private float _next;
        private bool _dressed;
        private string _group = "";
        private string _defeatKey = "";
        private string _dream = "";
        private bool _faultReported;

        public Character Body { get; private set; } = null!;

        public MonsterAI? Ai { get; private set; }

        public ZNetView View => _nview;

        /// <summary>The creature's ZDO; null once it is gone.</summary>
        public ZDO? Zdo => _nview != null && _nview.IsValid() ? _nview.GetZDO() : null;

        /// <summary>The owner's memory: since when it has had no arena core to read its trial from; -1 while it has.</summary>
        internal float NoCoreSince { get; set; } = -1f;

        private void Awake() => Guard.Run("ArenaFighter.Awake", Setup);

        private void Setup()
        {
            _nview = GetComponent<ZNetView>();
            Body = GetComponent<Character>();
            Ai = GetComponent<MonsterAI>();
            if (Zdo == null || Body == null || Body.IsPlayer())
            {
                enabled = false;
                return;
            }
            ArenaFighters.Add(this);
            Refresh();
        }

        private void OnDestroy() => ArenaFighters.Remove(this);

        private void Update()
        {
            float now = Time.time;
            if (now < _next)
            {
                return;
            }
            _next = now + TickSeconds;
            try
            {
                Refresh();
                ArenaOrders.Judge(this, now);
            }
            catch (Exception e)
            {
                ReportOnce(e);
            }
        }

        /// <summary>
        /// Follows the tag, on every machine and in memory only. Tagged: the arena's group, and none of a boss's world
        /// progress - an arena boss sets no "defeated" key and plays no dream, whoever owns it when it dies. Untagged
        /// again (tamed): the creature is the game's own once more.
        /// </summary>
        public void Refresh()
        {
            bool tagged = ArenaTag.IsArena(Zdo);
            if (tagged == _dressed)
            {
                return;
            }
            if (tagged)
            {
                Dress();
            }
            else
            {
                Undress();
            }
        }

        private void Dress()
        {
            _dressed = true;
            _group = Body.m_group;
            _defeatKey = Body.m_defeatSetGlobalKey;
            _dream = Body.m_dreamCinematic;
            Body.m_group = Group;
            Body.m_defeatSetGlobalKey = "";
            Body.m_dreamCinematic = "";
        }

        private void Undress()
        {
            _dressed = false;
            Body.m_group = _group;
            Body.m_defeatSetGlobalKey = _defeatKey;
            Body.m_dreamCinematic = _dream;
        }

        private void ReportOnce(Exception e)
        {
            if (!_faultReported)
            {
                _faultReported = true;
                Guard.Report(e, $"arena creature at {transform.position:F0}");
            }
        }
    }
}
