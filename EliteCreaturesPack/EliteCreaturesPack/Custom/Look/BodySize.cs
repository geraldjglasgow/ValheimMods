using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// A creature's size, per axis, relative to its base. The whole prefab is scaled at its root, as this mod sizes the
    /// Rime Giant and as Elite Creatures Reborn sizes its stars (it multiplies the root again on every peer, so the two
    /// stack): body, colliders, the radius the game sizes status effects and hit effects by, all alike. The game's own
    /// star sizes (<c>LevelEffects</c>) set the scale of the transform they sit on outright; on a creature that is its
    /// <c>Visual</c> child and is left alone, but where they sit on the root itself (some ragdolls) their sizes are
    /// multiplied by this one's mean, so a sized creature does not shrink back when it is a star. Every peer applies the
    /// same, from the definitions, on the prefab: every creature and every peer draws the same size.
    /// </summary>
    internal static class BodySize
    {
        public static void Apply(GameObject root, Vector3 size)
        {
            root.transform.localScale = Vector3.Scale(root.transform.localScale, size);
            float mean = Mathf.Pow(Mathf.Abs(size.x * size.y * size.z), 1f / 3f);
            foreach (LevelEffects effects in root.GetComponents<LevelEffects>())
            {
                foreach (LevelEffects.LevelSetup setup in effects.m_levelSetups)
                {
                    setup.m_scale *= mean;
                }
            }
        }

        /// <summary>
        /// The game keeps one star-level material per prefab name and level for the session
        /// (<c>LevelEffects.m_materials</c>, made from the prefab's own the first time one shows that level). A custom
        /// prefab of the same name built again for another world may wear another tint or texture, so its old entries go.
        /// </summary>
        public static void ForgetStarMaterials(string prefabName, GameObject shell)
        {
            foreach (LevelEffects effects in shell.GetComponentsInChildren<LevelEffects>(true))
            {
                for (int level = 2; level <= effects.m_levelSetups.Count + 1; level++)
                {
                    LevelEffects.m_materials.Remove(prefabName + level);
                }
            }
        }
    }
}
