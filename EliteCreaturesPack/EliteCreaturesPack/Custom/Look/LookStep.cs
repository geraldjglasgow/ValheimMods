using EliteCreaturesPack.Custom.Build;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// Puts a creature's effects and looks on its shell (features/custom-creatures.md section 3, "Effects", "Look",
    /// "Texture"). Every setting left out keeps the base's value; the base's own prefab and lists are never changed.
    /// <list type="bullet">
    /// <item>In every pass of the chain: the <c>effects:</c> lists this definition names replace the creature's own
    /// (<see cref="EffectLists"/>; a creature named there is spawned once, by the owner), and its <c>texture:</c> name is
    /// checked (<see cref="TextureFiles"/>), so each message names the definition and line that set it.</item>
    /// <item>In the last pass: the look from the whole chain (<see cref="LookPlan"/>: sizes multiply, the last tint,
    /// texture and overlay win), put on once (<see cref="LookFinish"/>): size, corpse, item tint, overlay on every peer;
    /// body tint and texture where something draws.</item>
    /// </list>
    /// Field mapping, for the export: <c>effects.hit</c> = <c>Character.m_hitEffects</c>, <c>death</c> =
    /// <c>Character.m_deathEffects</c> (corpse copies stand for <see cref="CorpseCopies.OriginalOf"/>), <c>alert</c> =
    /// <c>BaseAI.m_alertedEffects</c>, <c>idle</c> = <c>BaseAI.m_idleSound</c> plus <see cref="IdleSpawns.m_spawns"/>;
    /// <c>look.size</c> = the root's scale over the base's; <c>body tint</c> = the body materials' <c>_Color</c>;
    /// <c>item tint</c> = <see cref="ItemTint.m_tint"/>; <c>overlay</c> and its colour = <see cref="BodyOverlay"/>;
    /// <c>texture</c> = the body texture (main renderer's first material's <c>_MainTex</c>) swapped for the file.
    /// </summary>
    internal sealed class LookStep : ICreatureStep
    {
        public string Name => "look";

        public void Apply(CreatureBuild build)
        {
            EffectLists.Apply(build);
            TextureFiles.Check(build);
            if (build.IsLeaf && !build.Report.Failed)
            {
                LookFinish.Apply(build, LookPlan.Of(build.Chain));
            }
        }
    }
}
