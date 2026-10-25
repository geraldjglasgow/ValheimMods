using System;
using StatusTemplates;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The HUD icons of Defense's timed states, on the local player's own client: Riposte armed (seconds left), Shield
    /// Wall (sheltered), Hardened (stacks), Last Stand (invulnerable, seconds left), Last Stand recovering (its
    /// cooldown) and Desperation. Each is a <see cref="DefenseStatus"/> added to the player's SEMan while its state
    /// holds and gone as soon as it ends. They are added four times a second, and at once by the feature that just
    /// started one (<see cref="Refresh"/>), so a death that cleared them brings back those still running. Icons are item
    /// icons from the game's own items, else the Defense icon. Names are word tokens the HUD localizes as it draws them
    /// (a perk's own name where the state is a perk), so a language change applies to the icons already showing.
    /// </summary>
    public static class DefenseEffects
    {
        private const float Interval = 0.25f;

        private static readonly DefenseEffect[] Effects =
        {
            new DefenseEffect("grindstone_defense_riposte", Lang.Token("gs_perk_riposte"), "SwordIron",
                () => Riposte.ArmedFor > 0f, () => Seconds(Riposte.ArmedFor)),
            new DefenseEffect("grindstone_defense_shieldwall", Lang.Token("gs_perk_shieldwall"), "ShieldWood",
                () => ShieldWall.Sheltered(Player.m_localPlayer), null),
            new DefenseEffect("grindstone_defense_hardened", Lang.Token("gs_perk_hardened"), "ArmorIronChest",
                () => Hardened.Stacks > 0, () => Lang.Format("gs_defense_stacks", Hardened.Stacks)),
            new DefenseEffect("grindstone_defense_laststand", Lang.Token("gs_perk_laststand"), "HelmetDrake",
                () => LastStand.Invulnerable, () => Seconds(LastStand.InvulnerableFor)),
            new DefenseEffect("grindstone_defense_recovering", Lang.Token("gs_defense_laststand_recovering"), "HelmetDrake",
                () => LastStand.Unlocked && LastStand.CooldownLeft > 0f, () => StatusEffect.GetTimeString(LastStand.CooldownLeft), true),
            new DefenseEffect("grindstone_defense_desperation", Lang.Token("gs_defense_desperation"), "Bloodbag",
                () => Desperation.Holds(Player.m_localPlayer), null),
        };

        private static float timer;

        /// <summary>Every frame for the local player (<see cref="LocalPlayerTick"/>).</summary>
        public static void Tick(float dt)
        {
            timer += dt;
            if (timer < Interval)
                return;
            timer = 0f;
            HookGuard.Run("defense effects", Refresh);
        }

        /// <summary>Adds the icon of every state that holds and has none yet.</summary>
        public static void Refresh()
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead() || !DefenseSkill.Active)
                return;
            foreach (DefenseEffect effect in Effects)
            {
                if (effect.Showing() && !effect.Status.Has(player))
                    effect.Status.Add(player);
            }
        }

        private static string Seconds(float seconds) => Lang.Format("gs_defense_seconds", Mathf.CeilToInt(seconds));

        private sealed class DefenseEffect
        {
            public DefenseEffect(string id, string title, string iconItem, Func<bool> showing, Func<string> label, bool cooldown = false)
            {
                Showing = showing;
                Status = new StatusTemplate<DefenseStatus>(id, se =>
                {
                    se.m_name = title;
                    se.m_cooldownIcon = cooldown;
                    se.Showing = showing;
                    se.Label = label;
                }, () => GameIcons.Item(iconItem) ?? DefenseIcon.Find());
            }

            public Func<bool> Showing { get; }

            /// <summary>The effect SEMan clones, made once; ScriptableObjects outlive scene loads.</summary>
            public StatusTemplate<DefenseStatus> Status { get; }
        }
    }
}
