using System.Collections.Generic;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// What our status effects take from the game's own, out of each item database as it is prepared
    /// (<see cref="CasterStatus.Prepare"/>, before the game wakes or copies it): the feather cape's slow fall into Feather
    /// Fall (<see cref="FeatherFall.Borrow"/>) and the Staff of Protection shield's hit and break effects into Ward
    /// (<see cref="WardEffect.Borrow"/>), each with its icon while ours is missing. A database without them changes nothing.
    /// </summary>
    public static class GameBorrow
    {
        private const string FeatherCape = "SlowFall";
        private const string StaffShield = "Staff_shield";

        public static void Take(List<StatusEffect> effects)
        {
            foreach (StatusEffect effect in effects)
            {
                if (effect is SE_Stats cape && effect.name == FeatherCape)
                    FeatherFall.Borrow(cape);
                else if (effect is SE_Shield shield && effect.name == StaffShield)
                    WardEffect.Template?.Borrow(shield);
            }
        }
    }
}
