using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cannon's triggers, on the gunner's own game (the library hands his attack buttons to <see cref="KwArm"/>): the
    /// attack button pulls trigger 0 (the cannon's shot), the secondary attack trigger 1 (its other shot, when it has one).
    /// A pull fires only a loaded cannon whose load fits the trigger (<see cref="KwArmReload"/>; the ball and powder were
    /// taken as it was loaded): the library's trigger then shoots (<see cref="Shoot"/>: the lanyard pulled, the shot
    /// leaving partway into it, <see cref="KwArmLaunch"/>) and writes it empty for every peer; the reload follows by
    /// itself. A press that cannot fire says why (reloading, out of ammunition or powder, loaded for the other trigger);
    /// holding the button fires each new load only for a shot that repeats.
    /// </summary>
    public static class KwArmTrigger
    {
        /// <summary>A trigger's shot: 1 the other shot when the cannon has one, else its shot.</summary>
        public static KwShot Of(KwArmDef def, int trigger) => trigger == 1 && def.Alternate != null ? def.Alternate : def.Primary;

        /// <summary>The gunner's attack buttons this frame: each button down or held pulls its trigger.</summary>
        public static void Buttons(KwArm arm, Player gunner, bool attack, bool attackHold, bool secondary, bool secondaryHold)
        {
            if (attack || attackHold)
                Pull(arm, gunner, 0, attack);
            if (secondary || secondaryHold)
                Pull(arm, gunner, 1, secondary);
        }

        /// <summary>On the gunner's game, the loaded cannon fires the trigger pulled: false when its shot was not made, and it stays loaded.</summary>
        public static bool Shoot(KwArm arm, Player gunner)
        {
            KwShot shot = Of(arm.Def, arm.Pulled);
            if (KwShots.Prefab(shot) == null)
                return false;
            arm.Link.Zdo.Set(arm.Link.Kind, arm.Pulled);
            arm.Launch.Hold(shot, gunner, KwArmReload.Item(arm.Link.Zdo.GetString(arm.Link.Ammo)));
            return true;
        }

        private static void Pull(KwArm arm, Player gunner, int trigger, bool pressed)
        {
            if (trigger == 1 && arm.Def.Alternate == null)
                return;
            KwShot shot = Of(arm.Def, trigger);
            if (!pressed && !shot.Repeats)
                return;
            arm.Want = trigger;
            WeaponState s = arm.State;
            if (!pressed && !s.Loaded)
                return;
            string? why = Refused(arm, gunner, trigger, shot, s);
            if (why == null)
            {
                arm.Pulled = trigger;
                arm.Fire(gunner);
            }
            else if (pressed)
                gunner.Message(MessageHud.MessageType.Center, Localization.instance.Localize(why));
        }

        // Why the shot cannot fire now, or null.
        private static string? Refused(KwArm arm, Player gunner, int trigger, KwShot shot, in WeaponState s)
        {
            if (KwShots.Prefab(shot) == null)
                return $"{arm.Def.Label} cannot fire: its shot was not made (see the log).";
            if (s.Busy)
                return $"Reloading: {KwArmReload.Left(arm, s):0.0} s";
            if (!s.Loaded)
                return KwArmReload.Lacks(gunner, shot, out _) ?? "Reloading";
            int holds = arm.Link.Zdo.GetInt(arm.Link.LoadKind);
            return KwArmReload.Fits(arm.Def, holds, trigger) ? null : KwArmWords.LoadedFor(arm.Def, holds);
        }
    }
}
