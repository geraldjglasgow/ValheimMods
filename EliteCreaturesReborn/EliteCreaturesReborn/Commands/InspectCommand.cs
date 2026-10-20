using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Taming;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite inspect</c>: prints the resolved mark of the creature under the crosshair - its stars, its mutations, a
    /// boss's aspect (and those of a creature another mod registered aspects for), the biome it was rolled in, and the values those produced AFTER star power and any large-star enhancement (not the
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
            EliteCommands.Reply(args, $"  stars {t.Stars} ({t.LargeGlyphs} large, {t.Stars % 5} small), biome {TraitStore.GetBiome(controller.View.GetZDO())}{(t.Tier > 0 ? $", rolled at world tier {t.Tier}" : "")}");
            TameLines(args, controller);
            EliteCommands.Reply(args, $"  health x{StatMath.HealthMultiplier(r, t) * AspectMath.HealthFactor(t):0.00}, size x{StatMath.SizeMultiplier(r, t) * AspectMath.SizeFactor(t):0.00}, move x{StatMath.MoveMultiplier(r, t, 0f):0.00}, swing x{StatMath.SwingSpeedMultiplier(r, t):0.00}, attack x{DamageMath.OutgoingMultiplier(r, t, 1f):0.00}");
            EliteCommands.Reply(args, $"  max health {c.GetMaxHealth():0}");
            foreach (Mutation m in t.Active())
            {
                EliteCommands.Reply(args, "  " + MutationReport.Line(r, t, m));
            }
            HoldingLines(args, controller);
            if (c.IsBoss() || t.AnyAspect) // a creature another mod gave aspects reports them as a boss does
            {
                AspectReport.Lines(controller).ForEach(line => EliteCommands.Reply(args, "  " + line));
            }
        }

        // A creature's game stars and, once tamed, what `15 - Tamed` takes from it. Bosses keep this mod's stars when tamed.
        private static void TameLines(Terminal.ConsoleEventArgs args, EliteController controller)
        {
            if (controller.Creature.IsBoss())
            {
                return;
            }
            EliteCommands.Reply(args, "  " + GameStarsLine(controller));
            if (controller.Creature.IsTamed())
            {
                EliteCommands.Reply(args, "  " + TakenLine(controller.View.GetZDO()));
            }
        }

        // The game's own stars (a player asked to see them, 2026-10-20): its level now when it has the game's level,
        // else the level kept aside under this mod's stars, which it takes back as a tame while `15 - Tamed` Stars is false.
        private static string GameStarsLine(EliteController controller)
        {
            Character c = controller.Creature;
            if (RuleState.Active.KeepsLevel(controller.Traits, isBoss: false))
            {
                return $"game stars {c.GetLevel() - 1} (level {c.GetLevel()}), the stars it has now";
            }
            int level = Mathf.Max(1, GameLevel.Kept(controller.View.GetZDO()));
            return $"game stars {level - 1} (level {level}), kept aside: a tame has them while '{TamedSettings.Section}' Stars is false";
        }

        // What the owner has taken, from the ZDO every machine follows, and the stars it rolled, which stay stored.
        private static string TakenLine(ZDO zdo)
        {
            int strip = TameStrip.Of(zdo);
            string stars = (strip & TameStrip.Stars) != 0 ? $"taken ({TraitStore.Load(zdo).Stars} rolled, stored)" : "kept";
            string mutations = (strip & TameStrip.Mutations) != 0 ? "taken" : "kept";
            return $"tamed: this mod's stars {stars}, mutations {mutations}";
        }

        // What it holds on its ZDO: a thief's pouch, a devourer's meals.
        private static void HoldingLines(Terminal.ConsoleEventArgs args, EliteController controller)
        {
            if (controller.Traits.Has(Mutation.Thieving))
            {
                EliteCommands.Reply(args, "  " + PouchLine(controller));
            }
            if (controller.Traits.Has(Mutation.Devouring))
            {
                EliteCommands.Reply(args, "  " + MealLine(controller));
            }
        }

        // The meal list the nameplate icons are drawn from, by prefab name, against the allowance the bite checks.
        private static string MealLine(EliteController controller)
        {
            List<int> meals = MealStore.Load(controller.View.GetZDO());
            string ate = $"devoured {meals.Count} of {DevourLimits.Allowance(controller.Rules, controller.Traits)}";
            if (meals.Count == 0)
            {
                return ate + ": nothing yet";
            }
            List<string> names = new List<string>();
            foreach (int prefabHash in meals)
            {
                GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
                names.Add(prefab != null ? prefab.name : $"unknown ({prefabHash})");
            }
            return ate + ": " + string.Join(", ", names);
        }

        // The only way to check the icon on the nameplate matches what the ZDO actually holds - the gap between the
        // two is where this feature's bugs live, per the spec. The count is against the same capacity the steal checks
        // (one item per star, at least `max items`), so a full pouch reads as full here too.
        private static string PouchLine(EliteController controller)
        {
            List<PouchStore.Entry> pouch = PouchStore.Load(controller.View.GetZDO());
            string held = $"carrying {pouch.Count} of {PouchStore.ResolvedMaxItems(controller.Rules, controller.Traits)}";
            if (pouch.Count == 0)
            {
                return held + ": nothing";
            }
            List<string> items = new List<string>();
            foreach (PouchStore.Entry entry in pouch)
            {
                items.Add($"{entry.Item.m_shared.m_name} x{entry.Item.m_stack}");
            }
            return held + ": " + string.Join(", ", items);
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
