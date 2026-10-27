using System.Collections.Generic;
using MannedWeapons;

namespace Broadside
{
    /// <summary>
    /// Where a ship cannon keeps its state and what its calls are named, in its own ZDO and on its own view; every peer
    /// names them alike. The manned weapon's (<see cref="Keys"/>, the MannedWeapons library's, all from one prefix:
    /// <c>bs_arm_user</c>, <c>bs_arm_yaw</c>, <c>bs_arm_pitch</c>, <c>bs_arm_shots</c>, <c>bs_arm_shot_at</c>, its reload
    /// <c>bs_arm_loaded</c> (1 once loaded), <c>bs_arm_step</c> and <c>bs_arm_load_at</c>; calls <c>bs_arm_request</c>,
    /// <c>bs_arm_release</c>, <c>bs_arm_answer</c>, <c>bs_arm_call</c>, <c>bs_arm_called</c>, <c>bs_arm_order</c>), and the
    /// cannon's own: which trigger fired the last shot and which a load is for, the ammunition loaded, who hauls it
    /// (<see cref="Tower"/>) with the calls to ask for that (<see cref="Tow"/>, <see cref="TowDenied"/>), and whether its
    /// breeching was left unmade where it was hauled to (<see cref="Untied"/>).
    /// </summary>
    public sealed class KwArmLink
    {
        /// <summary>The manned weapon's keys and calls, shared by every cannon.</summary>
        public static readonly WeaponKeys Keys = new WeaponKeys("bs_arm", stage: "loaded", step: "step", stepAt: "load_at");

        public KwArmLink(ZNetView view)
        {
            View = view;
            Tower = ZDO.GetHashZDOID("bs_arm_tower");
            (Kind, LoadKind, Ammo) = ("bs_arm_kind".GetStableHashCode(), "bs_arm_load_kind".GetStableHashCode(), "bs_arm_ammo".GetStableHashCode());
            Untied = "bs_arm_untied".GetStableHashCode();
        }

        public ZNetView View { get; }

        /// <summary>
        /// The trigger whose shot was fired last (0 the attack's, 1 the secondary attack's), for every peer's effects; the
        /// trigger the load is for; the ammunition loaded (an item prefab), which the shot's projectile carries.
        /// </summary>
        public int Kind { get; }
        public int LoadKind { get; }
        public int Ammo { get; }

        /// <summary>Its breeching left unmade (1) where it was last hauled to, nothing near to make it fast to (<see cref="KwBreechingTie"/>); 0 or none, made fast.</summary>
        public int Untied { get; }

        /// <summary>The character hauling it by its rear, written by the owner (the hauler, who owns its ZDO while he hauls it).</summary>
        public KeyValuePair<int, int> Tower { get; }

        /// <summary>The calls to haul it: ask the owner for it (granted by handing over ownership, as the game's cart does), and the refusal.</summary>
        public string Tow => "BS_Arm_Tow";
        public string TowDenied => "BS_Arm_TowDenied";

        /// <summary>A captain at the helm asks the owner for an empty cannon to load it from his pack (<see cref="KwBroadsideLoad"/>; granted by handing over ownership).</summary>
        public const string Load = "BS_Arm_Load";

        public bool Valid => View != null && View.IsValid();

        public ZDO Zdo => View.GetZDO();
    }
}
