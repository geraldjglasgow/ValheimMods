using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// The bow's draw and the crossbow's reload, which the game runs only in its Player class (from the held attack
    /// button and the player's action queue), done for a human on its owner whenever its AI asks to attack
    /// (<see cref="HumanAttackPatch"/>):
    /// <list type="bullet">
    /// <item>A bow (<c>m_bowDraw</c>) is drawn first, the player's way: the weapon's draw state on in the animator
    /// (<c>bow_aim</c>, synced, so every peer sees the aim) and the draw sound, the draw time counting up to the
    /// weapon's draw duration (a human has no Bows skill, so the whole of it, 2.5 s for the game's bows), then let go
    /// through the game's own attack, which fires at full draw: full speed, full accuracy, full damage. Without it the
    /// game fires a non-player's bow at no draw at all: the slowest, widest arrow, doing nothing. If the AI stops asking
    /// for <see cref="LetDown"/> (target lost, out of view) the bow is lowered without a shot.</item>
    /// <item>A crossbow (<c>m_requiresReload</c>) is reloaded first: the weapon's reload state on for its reload time,
    /// then its "_done" trigger, then loaded (<see cref="IsLoaded"/>, which the game reads through IsWeaponLoaded, for
    /// a Humanoid always false) until the shot or an unequip unloads it (<see cref="Unload"/>).</item>
    /// </list>
    /// Disabled except while drawing or reloading, so it costs nothing otherwise. Being on every human, it is also what
    /// <see cref="HumanRegistry"/> lists them by.
    /// </summary>
    public sealed class HumanRanged : MonoBehaviour
    {
        private const float LetDown = 1f;
        private static readonly int DrawPercent = Animator.StringToHash("drawpercent");

        private Humanoid human = null!;
        private ZNetView nview = null!;
        private ZSyncAnimation zanim = null!;
        private Animator animator = null!;
        private ItemDrop.ItemData? drawing, reloading, loaded;
        private float asked, reloadTime;

        /// <summary>Whether the weapon in hand is a loaded crossbow.</summary>
        public bool IsLoaded => loaded != null && loaded == human.GetCurrentWeapon();

        private void Awake()
        {
            (human, nview, zanim) = (GetComponent<Humanoid>(), GetComponent<ZNetView>(), GetComponent<ZSyncAnimation>());
            animator = GetComponentInChildren<Animator>();
            HumanRegistry.Add(human, this);
        }

        private void OnDestroy() => HumanRegistry.Remove(human);

        /// <summary>Asked as an attack starts: true lets the game's attack go now, false holds it while drawing or reloading.</summary>
        public bool Ready()
        {
            ItemDrop.ItemData? weapon = human.GetCurrentWeapon();
            if (weapon == null)
            {
                return true;
            }
            Attack attack = weapon.m_shared.m_attack;
            if (attack.m_bowDraw)
            {
                return Drawn(weapon);
            }
            return !attack.m_requiresReload || Loaded(weapon);
        }

        /// <summary>After an attack started: a drawn bow is let go, the ammunition filled again.</summary>
        public void Fired()
        {
            ItemDrop.ItemData? weapon = human.GetCurrentWeapon();
            if (weapon == null)
            {
                return;
            }
            if (drawing == weapon)
            {
                EndDraw();
            }
            HumanAmmo.Restock(human, weapon);
        }

        /// <summary>The game unloads a crossbow as it fires and as it is unequipped.</summary>
        public void Unload() => loaded = null;

        private bool Drawn(ItemDrop.ItemData weapon)
        {
            asked = Time.time;
            if (drawing == weapon)
            {
                return human.GetAttackDrawPercentage() >= 1f;
            }
            if (drawing != null)
            {
                EndDraw();
            }
            if (!human.InAttack() && weapon.m_shared.m_attack.StartDraw(human, weapon))
            {
                BeginDraw(weapon);
            }
            return false;
        }

        private void BeginDraw(ItemDrop.ItemData weapon)
        {
            drawing = weapon;
            human.m_attackDrawTime = Time.fixedDeltaTime;
            Animate(weapon.m_shared.m_attack.m_drawAnimationState, true);
            weapon.m_shared.m_holdStartEffect.Create(transform.position, Quaternion.identity, transform);
            enabled = true;
        }

        private void EndDraw()
        {
            string state = drawing!.m_shared.m_attack.m_drawAnimationState;
            drawing = null;
            human.m_attackDrawTime = 0f;
            Animate(state, false);
        }

        private bool Loaded(ItemDrop.ItemData weapon)
        {
            if (loaded == weapon)
            {
                return true;
            }
            if (reloading != weapon && !human.InAttack())
            {
                BeginReload(weapon);
            }
            return false;
        }

        private void BeginReload(ItemDrop.ItemData weapon)
        {
            if (reloading != null)
            {
                EndReload(false);
            }
            (reloading, reloadTime) = (weapon, 0f);
            Animate(weapon.m_shared.m_attack.m_reloadAnimation, true);
            enabled = true;
        }

        private void EndReload(bool done)
        {
            string state = reloading!.m_shared.m_attack.m_reloadAnimation;
            loaded = done ? reloading : loaded;
            reloading = null;
            Animate(state, false);
            if (done && !string.IsNullOrEmpty(state))
            {
                zanim.SetTrigger(state + "_done");
            }
        }

        private void FixedUpdate() => SafeCall.Run("human bow draw and crossbow reload", static me => me.Tick(Time.fixedDeltaTime), this);

        /// <summary>Only the owner draws and reloads; anything else (no longer the owner, weapon changed, staggered) lets go.</summary>
        private void Tick(float dt)
        {
            bool owner = nview.IsValid() && nview.IsOwner();
            if (drawing != null)
            {
                Draw(owner && Holding(drawing), dt);
            }
            if (reloading != null)
            {
                Reload(owner && Holding(reloading), dt);
            }
            enabled = drawing != null || reloading != null;
        }

        private bool Holding(ItemDrop.ItemData weapon) => human.GetCurrentWeapon() == weapon && !human.IsStaggering() && !human.IsDead();

        private void Draw(bool holding, float dt)
        {
            if (!holding || Time.time - asked > LetDown)
            {
                EndDraw();
                return;
            }
            human.m_attackDrawTime += dt;
            // Straight to the animator: the player's draw blend is not among the synced floats either, and through the
            // sync it would rewrite the creature's ZDO every tick of the draw.
            animator.SetFloat(DrawPercent, human.GetAttackDrawPercentage());
        }

        private void Reload(bool holding, float dt)
        {
            reloadTime += dt;
            if (!holding || reloadTime >= reloading!.m_shared.m_attack.m_reloadTime)
            {
                EndReload(holding);
            }
        }

        private void Animate(string state, bool on)
        {
            if (!string.IsNullOrEmpty(state))
            {
                zanim.SetBool(state, on);
            }
        }
    }
}
