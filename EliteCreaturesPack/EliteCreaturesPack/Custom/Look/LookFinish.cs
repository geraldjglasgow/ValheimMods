using System.Collections.Generic;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// Puts a creature's whole look on its prefab, once, in its chain's last pass (<see cref="LookPlan"/>). On every peer
    /// alike: its size (<see cref="BodySize"/>), its corpse copies (<see cref="CorpseCopies"/>), the item tint's component
    /// (<see cref="ItemTint"/>), the overlay (<see cref="OverlaySource"/>), and the game's remembered star materials of a
    /// prefab of its name forgotten. Then, only on a machine that draws, its body's and corpse's tint and texture
    /// (<see cref="BodyDress"/>). The drawing part is guarded on its own: whatever goes wrong there is this machine's
    /// alone and is reported without failing the creature, since a creature left out on one peer but not on the others
    /// would be an unknown object there.
    /// </summary>
    internal static class LookFinish
    {
        public static void Apply(CreatureBuild build, LookPlan plan)
        {
            if (plan.Sized)
            {
                BodySize.Apply(build.Shell, plan.Size);
            }
            List<GameObject> corpses = CorpseCopies.Make(build, plan);
            if (plan.ItemTint != null)
            {
                Items(build, plan.ItemTint.Value, corpses);
            }
            OverlaySource.Apply(build, plan);
            BodySize.ForgetStarMaterials(build.Creature.Name, build.Shell);
            if (plan.Dresses && !Drawing.Headless)
            {
                SafeCall.Run("custom creature look (drawing)", static (b, p, c) => Dress(b, p, c), build, plan, corpses);
            }
        }

        private static void Items(CreatureBuild build, Color tint, List<GameObject> corpses)
        {
            if (build.Shell.GetComponent<VisEquipment>() == null)
            {
                build.Report.Warn("it shows no carried items (its body has no equipment slots), so the item tint does nothing", "look.item tint");
                return;
            }
            build.Shell.AddComponent<ItemTint>().m_tint = tint;
            foreach (GameObject corpse in corpses)
            {
                if (corpse.GetComponent<VisEquipment>() != null)
                {
                    corpse.AddComponent<ItemTint>().m_tint = tint;
                }
            }
        }

        private static void Dress(CreatureBuild build, LookPlan plan, List<GameObject> corpses)
        {
            Texture? from = BodyDress.BodyTexture(build.Shell);
            Texture? to = plan.Texture != null ? TextureFiles.Load(plan.Texture) : null;
            if (plan.BodyTint != null && !BodyDress.BodyTakesTint(build.Shell))
            {
                build.Report.Warn(build.IsHuman
                    ? "a human's body (the player's) has no tint colour, so its body is not tinted: set its skin with human: skin tone, and its gear with item tint"
                    : "its body's material has no tint colour, so its body is not tinted", "look.body tint");
            }
            if (to != null && from == null)
            {
                build.Report.Warn("its body has no main texture to replace, so the image is not used", "texture");
            }
            BodyDress.Dress(build.Shell, plan.BodyTint, from, to);
            foreach (GameObject corpse in corpses)
            {
                BodyDress.Dress(corpse, plan.BodyTint, from, to);
            }
        }
    }
}
