using UnityEngine;

namespace EliteCreaturesPack.Mountains
{
    /// <summary>At half health the owner latches a persistent second phase; all peers draw its paler coat.</summary>
    public sealed class FrostfangRage : MonoBehaviour
    {
        private static readonly int RageKey = "ecp_frostfang_rage".GetStableHashCode();
        private ZNetView view = null!;
        private Character character = null!;
        private MonsterAI ai = null!;
        private bool applied;
        private void Awake()
        {
            view = GetComponent<ZNetView>();
            character = GetComponent<Character>();
            ai = GetComponent<MonsterAI>();
        }
        private void Update()
        {
            if (view == null || !view.IsValid() || character.IsDead()) return;
            ZDO zdo = view.GetZDO();
            if (view.IsOwner() && !zdo.GetBool(RageKey) && character.GetHealthPercentage() <= .5f)
                zdo.Set(RageKey, true);
            if (applied || !zdo.GetBool(RageKey)) return;
            applied = true;
            character.m_runSpeed *= 1.25f;
            ai.m_circleTargetInterval = 0f;
            // Instance materials only: another Frostfang and the vanilla Wolf keep their own phase and coat.
            foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (Material material in renderer.materials)
                    if (material.HasProperty("_Color")) material.color = Color.Lerp(material.color, new Color(.86f, .92f, .95f), .45f);
        }
    }
}
