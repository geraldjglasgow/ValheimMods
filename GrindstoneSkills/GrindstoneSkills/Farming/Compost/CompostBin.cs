using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A compost bin in the world (the component <see cref="CompostPrefab"/> adds to the bin piece). Its compost points
    /// live in its ZDO (<see cref="Keys.CompostPoints"/>). Once a second, on the bin's owner while Farming and composting
    /// are on, it composts one item every Compost Time seconds (<see cref="CompostDigest"/>) and every 10 s feeds the
    /// crops around it (<see cref="CompostFeed"/>). Kitchen trash reaches it through <see cref="Keys.RpcAddCompost"/>
    /// (<see cref="CompostTrash"/>). A placement ghost (no ZDO) does nothing. Every bin ticks from one update
    /// (<see cref="Ticker"/>), all on the same frame once a second.
    /// </summary>
    public class CompostBin : MonoBehaviour
    {
        private const float FeedInterval = 10f;

        private static readonly int PointsHash = Keys.CompostPoints.GetStableHashCode();
        private static readonly List<CompostBin> all = new List<CompostBin>();
        private static float nextTick;

        private ZNetView nview;
        private float digestTimer;
        private float feedTimer;

        /// <summary>Every loaded bin on this machine.</summary>
        public static IEnumerable<CompostBin> All => all;

        public Container Container { get; private set; }

        public bool IsValid => nview != null && nview.IsValid();

        public float Points => IsValid ? nview.GetZDO().GetFloat(PointsHash) : 0f;

        public static int Capacity => Mathf.Max(1, CompostSettings.Capacity.Value);

        public static bool Working => FarmSkill.Active && CompostSettings.Enabled.Value;

        private void Awake()
        {
            nview = GetComponent<ZNetView>();
            Container = GetComponentInChildren<Container>(true);
            if (nview == null || nview.GetZDO() == null)
                return;
            all.Add(this);
            nview.Register<float>(Keys.RpcAddCompost, (sender, points) => Guard.Run(Keys.RpcAddCompost, () => AddPoints(points)));
            Ticker.Ensure();
        }

        /// <summary>Ticks every loaded bin once a second, from <see cref="Ticker"/>.</summary>
        internal static void TickAll()
        {
            float now = Time.time;
            if (all.Count == 0 || now < nextTick)
                return;
            nextTick = now + 1f;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (all[i] != null)
                    all[i].Tick();
            }
        }

        private void OnDestroy() => all.Remove(this);

        /// <summary>Adds points on the owner, up to the capacity. Elsewhere nothing happens (send with <see cref="Send"/>).</summary>
        public void AddPoints(float points)
        {
            if (IsValid && nview.IsOwner() && points > 0f)
                SetPoints(Mathf.Min(Capacity, Points + points));
        }

        public void SetPoints(float points) => nview.GetZDO().Set(PointsHash, Mathf.Max(0f, points));

        /// <summary>Gives the bin points from any machine: directly on its owner, else through its owner.</summary>
        public void Send(float points)
        {
            if (!IsValid || points <= 0f)
                return;
            if (nview.IsOwner())
                AddPoints(points);
            else
                nview.InvokeRPC(Keys.RpcAddCompost, points);
        }

        public bool IsOwner() => IsValid && nview.IsOwner();

        private void Tick()
        {
            if (!IsOwner() || !Working)
                return;
            digestTimer += 1f;
            feedTimer += 1f;
            if (digestTimer >= Mathf.Max(1f, CompostSettings.CompostTime.Value))
            {
                digestTimer = 0f;
                HookGuard.Run("Composting", static bin => CompostDigest.Step(bin), this);
            }
            if (feedTimer >= FeedInterval)
            {
                feedTimer = 0f;
                HookGuard.Run("Compost feeding", static bin => CompostFeed.Step(bin), this);
            }
        }
    }
}
