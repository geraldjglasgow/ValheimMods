using System;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The HUD icons of Defense's timed states, on the local player's own client: Riposte armed (seconds left), Shield
    /// Wall (sheltered), Hardened (stacks), Last Stand (invulnerable, seconds left), Last Stand recovering (its
    /// cooldown) and Desperation. Each is a <see cref="DefenseStatus"/> added to the player's SEMan while its state
    /// holds and gone as soon as it ends. They are added four times a second, and at once by the feature that just
    /// started one (<see cref="Refresh"/>), so a death that cleared them brings back those still running. Icons are item
    /// icons from the game's own items, else the Defense icon.
    /// </summary>
    public static class DefenseEffects
    {
        private const float Interval = 0.25f;

        private static readonly DefenseEffect[] Effects =
        {
            new DefenseEffect("grindstone_defense_riposte", "Riposte", "SwordIron",
                () => Riposte.ArmedFor > 0f, () => Seconds(Riposte.ArmedFor)),
            new DefenseEffect("grindstone_defense_shieldwall", "Shield Wall", "ShieldWood",
                () => ShieldWall.Sheltered(Player.m_localPlayer), null),
            new DefenseEffect("grindstone_defense_hardened", "Hardened", "ArmorIronChest",
                () => Hardened.Stacks > 0, () => $"{Hardened.Stacks}x"),
            new DefenseEffect("grindstone_defense_laststand", "Last Stand", "HelmetDrake",
                () => LastStand.Invulnerable, () => Seconds(LastStand.InvulnerableFor)),
            new DefenseEffect("grindstone_defense_recovering", "Last Stand recovering", "HelmetDrake",
                () => LastStand.Unlocked && LastStand.CooldownLeft > 0f, () => StatusEffect.GetTimeString(LastStand.CooldownLeft), true),
            new DefenseEffect("grindstone_defense_desperation", "Desperation", "Bloodbag",
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
            SEMan seman = player.GetSEMan();
            foreach (DefenseEffect effect in Effects)
            {
                if (effect.Showing() && !seman.HaveStatusEffect(effect.Hash))
                    seman.AddStatusEffect(effect.Template());
            }
        }

        private static string Seconds(float seconds) => $"{Mathf.CeilToInt(seconds)}s";

        private sealed class DefenseEffect
        {
            private readonly string id;
            private readonly string title;
            private readonly string iconItem;
            private readonly Func<string> label;
            private readonly bool cooldown;
            private DefenseStatus template;

            public DefenseEffect(string id, string title, string iconItem, Func<bool> showing, Func<string> label, bool cooldown = false)
            {
                this.id = id;
                this.title = title;
                this.iconItem = iconItem;
                Showing = showing;
                this.label = label;
                this.cooldown = cooldown;
                Hash = id.GetStableHashCode();
            }

            public Func<bool> Showing { get; }
            public int Hash { get; }

            /// <summary>The effect SEMan clones, made once; ScriptableObjects outlive scene loads.</summary>
            public DefenseStatus Template()
            {
                if (template != null)
                    return template;
                template = ScriptableObject.CreateInstance<DefenseStatus>();
                template.name = id;
                template.m_name = title;
                template.m_icon = Icon();
                template.m_cooldownIcon = cooldown;
                template.Showing = Showing;
                template.Label = label;
                return template;
            }

            private Sprite Icon()
            {
                GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(iconItem) : null;
                Sprite[] icons = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_icons : null;
                return icons != null && icons.Length > 0 && icons[0] != null ? icons[0] : DefenseIcon.Find();
            }
        }
    }
}
