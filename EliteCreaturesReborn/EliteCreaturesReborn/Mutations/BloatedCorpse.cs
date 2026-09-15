using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Visuals;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The warning that rides a Bloated creature's corpse for the whole fuse. Spawned on every client from the death
    /// broadcast (see <see cref="EliteRpc"/>): it finds this machine's own local ragdoll near the death spot and follows
    /// it wherever it slides or rolls, so the thing that looks like it is about to explode is the thing that is. Ragdoll
    /// physics is not synchronised, so each client's corpse settles a little differently - that divergence is only
    /// cosmetic here. When the fuse ends, ONLY the owner's rider broadcasts the blast at its corpse's resting place, so
    /// the blast everyone sees and the damage the owner deals are the same spot. If no ragdoll is ever found (a creature
    /// that simply vanishes), it falls back to the place of death - better a blast in the right area than none.
    /// </summary>
    public sealed class BloatedCorpse : MonoBehaviour
    {
        /// <summary>Radius the swelling-corpse warning is scaled to - a body-sized tell, not the full blast footprint.</summary>
        private const float WarningRadius = 2f;

        /// <summary>How far from the death spot to look for the local ragdoll; a corpse cannot have rolled far this soon.</summary>
        private const float SearchRadius = 6f;

        private float _fuse;
        private bool _isOwner;
        private int _stars;
        private float _damage;
        private float _radius;
        private string _blastEffect = "";
        private Vector3 _deathPos;
        private Ragdoll? _corpse;
        private float _searchTimer;

        public static void Spawn(Vector3 deathPos, float damage, float radius, float delay, int stars,
            string warningEffect, string blastEffect, bool isOwner)
        {
            GameObject holder = new GameObject("ecr_bloated_corpse");
            holder.transform.position = deathPos;
            BloatedCorpse corpse = holder.AddComponent<BloatedCorpse>();
            corpse._fuse = delay;
            corpse._isOwner = isOwner;
            corpse._stars = stars;
            corpse._damage = damage;
            corpse._radius = radius;
            corpse._blastEffect = blastEffect;
            corpse._deathPos = deathPos;
            GameObject? warning = EffectResolver.Resolve(warningEffect, EffectResolver.Warning, "Bloated warning effect");
            LingeringVisual.Attach(holder, warning, Mathf.Max(delay, 0.01f), WarningRadius);
        }

        private void Update() => Guard.Run("BloatedCorpse.Update", Step);

        private void Step()
        {
            RideCorpse();
            _fuse -= Time.deltaTime;
            if (_fuse > 0f)
            {
                return;
            }
            EndFuse();
            Destroy(gameObject);
        }

        // Follow the local corpse once found; until then keep looking on a throttle (the ragdoll replicates a frame or
        // two after the death broadcast on a remote client), staying at the death spot in the meantime.
        private void RideCorpse()
        {
            if (_corpse != null)
            {
                transform.position = _corpse.transform.position;
                return;
            }
            _searchTimer -= Time.deltaTime;
            if (_searchTimer > 0f)
            {
                return;
            }
            _searchTimer = 0.25f;
            _corpse = NearestCorpse();
            if (_corpse != null)
            {
                transform.position = _corpse.transform.position;
            }
        }

        private Ragdoll? NearestCorpse()
        {
            Ragdoll? best = null;
            float bestSq = SearchRadius * SearchRadius;
            foreach (Ragdoll ragdoll in Object.FindObjectsOfType<Ragdoll>())
            {
                float distSq = (ragdoll.transform.position - _deathPos).sqrMagnitude;
                if (distSq < bestSq)
                {
                    bestSq = distSq;
                    best = ragdoll;
                }
            }
            return best;
        }

        // The blast position is the OWNER's corpse, sent when the fuse ENDS (not at death). Non-owners simply stop
        // showing the warning; they draw the blast when the owner's broadcast arrives.
        private void EndFuse()
        {
            if (!_isOwner)
            {
                return;
            }
            Vector3 at = _corpse != null ? _corpse.transform.position : _deathPos;
            EliteRpc.FireBlast(at, _damage, _radius, _stars, _blastEffect);
        }
    }
}
