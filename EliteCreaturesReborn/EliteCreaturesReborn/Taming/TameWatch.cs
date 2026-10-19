using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Taming
{
    /// <summary>
    /// Every loaded creature of a kind the game can tame, checked once a second (<see cref="TameTicker"/>): its owner
    /// settles what `15 - Tamed` takes from it (<see cref="TameSettle"/>) the moment it is tamed or a setting changes,
    /// and every machine whose copy no longer matches its ZDO - other stars or mutations, or another level - loads it
    /// again (<see cref="LocalReload"/>). A copy has to be made again: its stars' look, size, health and mutation
    /// behaviours are set up once as it loads, and the game reads a level only as it loads. A ridden creature waits
    /// until its rider gets off. Kinds without a Tameable are never tamed by the game and never watched.
    /// </summary>
    internal static class TameWatch
    {
        /// <summary>Each watched creature, with what its owner had taken when its copy here was set up.</summary>
        private static readonly Dictionary<EliteController, int> Applied = new Dictionary<EliteController, int>();

        private static readonly List<EliteController> Scratch = new List<EliteController>();

        /// <summary>Every machine, as a creature's traits are applied.</summary>
        public static void Track(EliteController controller)
        {
            Character creature = controller.Creature;
            if (!creature.IsBoss() && creature.GetComponent<Tameable>() != null)
            {
                Applied[controller] = TameStrip.Of(controller.View.GetZDO());
            }
        }

        public static void Forget(EliteController controller) => Applied.Remove(controller);

        public static void Tick()
        {
            Scratch.Clear();
            Scratch.AddRange(Applied.Keys);
            foreach (EliteController controller in Scratch)
            {
                Check(controller);
            }
        }

        private static void Check(EliteController controller)
        {
            ZNetView? nview = controller != null ? controller.View : null;
            ZDO? zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            if (zdo == null || !Applied.TryGetValue(controller!, out int applied))
            {
                return;
            }
            if (nview!.IsOwner() && ServerLock.RulesArrived)
            {
                TameSettle.Settle(controller!.Creature, zdo);
            }
            int strip = TameStrip.Of(zdo);
            if (strip != applied)
            {
                CatchUp(controller!, zdo, strip);
            }
        }

        // The ZDO has moved on from this copy: noted when nothing here would change, else the copy is made again.
        private static void CatchUp(EliteController controller, ZDO zdo, int strip)
        {
            if (!Differs(controller, zdo, strip))
            {
                Applied[controller] = strip; // a plain creature losing mutations it never had: nothing to load again
            }
            else if (!Ridden(controller.Creature))
            {
                Applied.Remove(controller);
                LocalReload.Run(controller.View);
            }
        }

        // The copy here against what the ZDO now makes of it.
        private static bool Differs(EliteController controller, ZDO zdo, int strip)
        {
            CreatureTraits now = TameStrip.View(TraitStore.Load(zdo), strip);
            CreatureTraits had = controller.Traits;
            return now.Stars != had.Stars || now.Mask != had.Mask || now.GameStars != had.GameStars
                || zdo.GetInt(ZDOVars.s_level, 1) != controller.Creature.GetLevel();
        }

        private static bool Ridden(Character creature)
        {
            Tameable tameable = creature.GetComponent<Tameable>();
            return tameable != null && tameable.m_saddle != null && tameable.m_saddle.HaveValidUser();
        }
    }
}
