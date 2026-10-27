using System.Collections.Generic;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Each cannon as a manned weapon (the MannedWeapons library), from its definition, made once a kind. Its turn: the
    /// arc each way (<see cref="KwArmDef.YawLimit"/>: 20 degrees), its elevation and its turn and tilt speeds. It holds the
    /// gunner's view to its arc and its turn speed (the Bone Ballista's feel: the view, the barrel and his feet stop
    /// together at the arc's end, and turn no faster than the gun does). The crosshair's
    /// target is looked for out to <see cref="Reach"/> and never under the sea. Its reload: one step, the shot's
    /// <see cref="KwShot.Reload"/> less its motion (<see cref="KwArmDef.FireTime"/>; the breech, the ball and the powder),
    /// begun once that motion has run (<see cref="ReloadCycle.Rest"/>), so a cannon fires every Reload seconds, and left to
    /// run on when the gunner lets go mid-reload (as it always did: letting go and taking hold again never cuts it
    /// short). Its gunner: turned <see cref="KwArmDef.Facing"/> from where it points, elbows out square to his arms (they
    /// never flip as the lanyard's fist comes back to his hip), taking it from anywhere within the library's
    /// <see cref="WeaponDesign.MostUse"/> of the stand point (brought there at once, round the gun) and counted as its
    /// gunner within <see cref="HolderRange"/> of it. The view tilts with the barrel (<see cref="WeaponArcs.ViewTiltStops"/>,
    /// on by default): no further than the barrel can lay itself on what the crosshair is on, no faster than it tilts.
    /// Unmanned it goes back to its rest lay, the middle of its arc at <see cref="RestPitch"/> (the library's
    /// <see cref="WeaponDesign.Rest"/>, on its owner, at its own turn and tilt speeds). Its keys and calls are every
    /// cannon's (<see cref="KwArmLink.Keys"/>).
    /// </summary>
    public static class KwArmDesigns
    {
        private const float Reach = 250f, HolderRange = 5f;

        /// <summary>
        /// The rest lay's elevation, degrees: unmanned, every cannon goes back to the middle of its arc (straight out of its
        /// port, the way it was placed facing) at this, so a broadside fires where the captain expects (CLAUDE.md,
        /// "Cannons"). One degree up: from a 1.5 m deck (the muzzle 2.2 to 2.5 m over a calm sea) the ball is
        /// still about 2 m over the sea 30 m out (a hull's side) and meets it 53 to 55 m out from the small iron cannons, 68
        /// to 72 m from the others (offline, the real flight).
        /// </summary>
        public const float RestPitch = 1f;

        private static readonly Dictionary<KwArmDef, WeaponDesign> Made = new Dictionary<KwArmDef, WeaponDesign>();

        /// <summary>For a cannon whose id is not known: it has no frame, so the library leaves it be.</summary>
        private static readonly WeaponDesign Unknown = new WeaponDesign("Broadside cannon", KwArmLink.Keys);

        public static WeaponDesign Of(KwArmDef? def)
        {
            if (def == null)
                return Unknown;
            if (!Made.TryGetValue(def, out WeaponDesign design))
                Made[def] = design = Make(def);
            return design;
        }

        private static WeaponDesign Make(KwArmDef def) => new WeaponDesign("Broadside " + def.Label.ToLowerInvariant(), KwArmLink.Keys)
        {
            Aim = Arcs(def),
            Reload = new ReloadCycle(Mathf.Max(0.1f, def.Primary.Reload - def.FireTime)) { Rest = def.FireTime, FinishOnLetGo = false },
            Rest = new WeaponRest(0f, RestPitch),
            Body = new OperatorBody { Facing = def.Facing, Elbows = ElbowHint.Square },
            Words = new WeaponWords { Name = def.Label, NoAmmo = "Out of ammunition." },
            UseRange = WeaponDesign.MostUse,
            HolderRange = HolderRange,
        };

        private static WeaponArcs Arcs(KwArmDef def) => new WeaponArcs
        {
            YawLimit = def.YawLimit,
            PitchDown = def.PitchDown,
            PitchUp = def.PitchUp,
            TurnRate = def.TurnSpeed,
            TiltRate = def.TiltSpeed,
            ViewRate = def.YawLimit >= 180f ? 0f : def.TurnSpeed,
            Reach = Reach,
            StopAtSea = true,
        };
    }
}
