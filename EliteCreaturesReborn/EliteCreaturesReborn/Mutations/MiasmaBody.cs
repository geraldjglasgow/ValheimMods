using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Visuals;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The poison visual a Miasmic creature wears on its body at all times - the same wisp a poisoned character trails,
    /// so you can see one coming and know what it is before the nameplate resolves. It is worn, not suffered: the
    /// creature is not poisoned and takes no harm. Purely cosmetic and on every machine; it rides the creature, so it
    /// moves and is destroyed with it for free. Cloned from the configured <c>body effect</c> prefab, looped so it never
    /// stops, and scaled small (to the body, not the full cloud radius) so it reads as an aura rather than a hazard.
    /// </summary>
    public sealed class MiasmaBody : MonoBehaviour
    {
        /// <summary>Body-aura scale relative to the creature's own radius; a wisp on the body, not a ground-cloud.</summary>
        private const float BodyScale = 0.75f;

        private void Start() => Guard.Run("MiasmaBody.Start", Build);

        private void Build()
        {
            EliteController controller = GetComponent<EliteController>();
            Character character = GetComponent<Character>();
            if (controller == null || character == null)
            {
                return;
            }
            string effect = controller.Rules.PrefabOf(Mutation.Miasmic, Fields.BodyEffect);
            GameObject? prefab = EffectResolver.Resolve(effect, EffectResolver.Cloud, "Miasmic body effect");
            GameObject? clone = CosmeticClone.Spawn(prefab, transform, transform.position, endless: true);
            Dress(clone, Mathf.Max(0.5f, character.GetRadius() * 2f) * BodyScale);
        }

        private void Dress(GameObject? clone, float size)
        {
            if (clone == null)
            {
                return;
            }
            clone.transform.localScale *= Mathf.Max(size, 0.01f) / 4f; // 4 = the authored radius of a vanilla ground effect
            foreach (ParticleSystem system in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.loop = true;
                system.Play(withChildren: true);
            }
        }
    }
}
