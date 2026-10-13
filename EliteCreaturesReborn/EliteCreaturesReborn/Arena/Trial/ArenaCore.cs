using System;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The trial's engine, on the hidden, persistent arena core at the hall's origin (features/arena.md section 4). It
    /// keeps nothing of the trial itself: <see cref="ArenaState"/> reads it from the core's ZDO. On the core's owner it
    /// steps the trial twice a second (<see cref="ArenaRun"/>); on every other machine it only answers the reads of the
    /// doors, the runes and the HUD line, and the moment ownership moves here the next step carries the trial on from the
    /// ZDO alone. The live core is kept in <see cref="Current"/>, so nothing ever searches the scene for it. Its RPCs are
    /// registered on every machine, since each goes to whoever owns it.
    /// </summary>
    public sealed class ArenaCore : MonoBehaviour
    {
        private ZNetView _nview = null!;
        private bool _live;
        private float _next;
        private bool _faultReported;

        /// <summary>The core this machine holds; null where the world's centre is not loaded.</summary>
        public static ArenaCore? Current { get; private set; }

        public ZNetView View => _nview;

        /// <summary>The core's ZDO; null on a core not yet live or already gone.</summary>
        public ZDO? Zdo => _live && _nview != null && _nview.IsValid() ? _nview.GetZDO() : null;

        public bool IsOwner => Zdo != null && _nview.IsOwner();

        /// <summary>The trial in the core's ZDO. Only while <see cref="Zdo"/> is not null.</summary>
        public ArenaState State => new ArenaState(_nview.GetZDO());

        /// <summary>The owner's own memory of the step, never in the ZDO: when the boss wave last had a boss alive.</summary>
        internal float BossSeenAt { get; set; }

        private void Awake() => Guard.Run("ArenaCore.Awake", Setup);

        private void Setup()
        {
            _nview = GetComponent<ZNetView>();
            if (_nview == null || !_nview.IsValid())
            {
                return; // the inactive prefab itself, or a ZDO-less copy: no trial
            }
            _live = true;
            _nview.Register(ArenaKeys.StartRpc, OnStart);
            _nview.Register(ArenaKeys.StopRpc, OnStop);
            _nview.Register<long, int, bool>(ArenaKeys.KillRpc, OnKill);
            Current = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Current, this))
            {
                Current = null;
            }
        }

        private void Update()
        {
            float now = Time.time;
            if (now < _next || !_live)
            {
                return;
            }
            _next = now + ArenaTable.TickSeconds;
            if (!IsOwner)
            {
                return;
            }
            try
            {
                ArenaRun.Step(this, NetTime.NowMs());
            }
            catch (Exception e)
            {
                ReportOnce(e);
            }
        }

        private void OnStart(long sender) => Guard.Run("ArenaCore.OnStart", () =>
        {
            if (IsOwner)
            {
                ArenaStart.Begin(this, sender);
            }
        });

        private void OnStop(long sender) => Guard.Run("ArenaCore.OnStop", () =>
        {
            if (IsOwner)
            {
                ArenaEnd.End(this, stopped: true);
            }
        });

        private void OnKill(long sender, long trial, int points, bool kill) => Guard.Run("ArenaCore.OnKill", () =>
        {
            if (IsOwner)
            {
                ArenaScore.Add(this, trial, points, kill);
            }
        });

        private void ReportOnce(Exception e)
        {
            if (!_faultReported)
            {
                _faultReported = true;
                Guard.Report(e, "Hel's Trial");
            }
        }
    }
}
