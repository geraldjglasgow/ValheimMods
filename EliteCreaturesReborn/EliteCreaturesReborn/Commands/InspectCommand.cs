using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite inspect</c>: prints the resolved mark of the creature under the crosshair - its stars, its mutations, the
    /// biome it was rolled in, and the values those produced AFTER star power and any large-star enhancement (not the
    /// configured numbers). Read-only; runs on whichever machine the admin is looking from, reading the creature's
    /// already-synced traits, so it needs no ownership.
    /// </summary>
    public static class InspectCommand
    {
        public static void Run(Terminal.ConsoleEventArgs args)
        {
            Character? target = UnderCrosshair();
            EliteController? controller = target != null ? target.GetComponent<EliteController>() : null;
            if (target == null)
            {
                EliteCommands.Reply(args, "elite inspect: no creature under the crosshair.");
                return;
            }
            if (controller == null || !controller.Ready)
            {
                EliteCommands.Reply(args, $"elite inspect: {target.GetHoverName()} is plain (not yet resolved).");
                return;
            }
            Report(args, controller);
        }

        private static void Report(Terminal.ConsoleEventArgs args, EliteController controller)
        {
            Character c = controller.Creature;
            CreatureTraits t = controller.Traits;
            BiomeRules r = controller.Rules;
            EliteCommands.Reply(args, $"elite inspect: {c.GetHoverName()}");
            EliteCommands.Reply(args, $"  stars {t.Stars} ({t.LargeGlyphs} large, {t.Stars % 5} small), biome {TraitStore.GetBiome(controller.View.GetZDO())}");
            EliteCommands.Reply(args, $"  health x{StatMath.HealthMultiplier(r, t):0.00}, size x{StatMath.SizeMultiplier(r, t):0.00}, move x{StatMath.MoveMultiplier(r, t, 0f):0.00}, swing x{StatMath.SwingSpeedMultiplier(r, t):0.00}, attack x{DamageMath.OutgoingMultiplier(r, t, 1f):0.00}");
            EliteCommands.Reply(args, $"  max health {c.GetMaxHealth():0}");
            foreach (Mutation m in t.Active())
            {
                EliteCommands.Reply(args, "  " + MutationReport.Line(r, t, m));
            }
        }

        // Reads from wherever the admin is looking: a ray straight down the camera, first collider that belongs to a
        // Character. A pure query, so it runs the same on host or client.
        private static Character? UnderCrosshair()
        {
            Camera cam = Camera.main;
            if (cam == null || !Physics.Raycast(cam.transform.position, cam.transform.forward,
                    out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                return null;
            }
            return hit.collider != null ? hit.collider.GetComponentInParent<Character>() : null;
        }
    }
}
