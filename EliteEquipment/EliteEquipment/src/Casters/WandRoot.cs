using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One of the Rootbound Wand's roots (<see cref="WandRoots"/>), on every machine that has it: it comes up out of the
    /// ground (the game's root's wake-up clip, its first), lashes <see cref="LashAt"/> s after it appeared (the game's
    /// <c>attack</c> trigger, set on each machine's own animator, so every player sees it without a message), and goes
    /// <see cref="Life"/> s after it appeared (its owner shows the game's root fade to everyone and removes it). Its clips
    /// play <see cref="Speed"/> times as fast as the Elder's. On its owner, the caster's machine, which made it and handed it
    /// its cast (<see cref="Arm"/>), the strike lands when the whip does (the clip's own <c>Hit</c>, <see cref="RootHit"/>),
    /// or at <see cref="Latest"/> s should the clip not get there: the strike area (the game's <c>Aoe</c>, set up as the
    /// game sets one up from an attack) hits the caster's enemies in reach, never the caster, other players or tames, with
    /// the cast's damage, as the caster's hit (the game sends it to each target's owner); a root that hit someone raises
    /// Elemental Magic once, as the game's own areas raise their skill.
    /// </summary>
    public sealed class WandRoot : MonoBehaviour
    {
        private const float Speed = 1.5f;
        private const float LashAt = 0.4f;
        private const float Latest = 2.6f;
        private const float Life = 3.5f;
        private const string Lash = "attack";

        private static WandRoot newest;

        private ZNetView nview;
        private Aoe strike;
        private Animator animator;
        private Character caster;
        private float raise;
        private bool armed;
        private bool struck;

        /// <summary>The root made last on this machine, once: the spawner that has just made it hands it its cast.</summary>
        public static WandRoot TakeNewest()
        {
            WandRoot root = newest;
            newest = null;
            return root;
        }

        private void Awake()
        {
            nview = GetComponent<ZNetView>();
            strike = GetComponentInChildren<Aoe>(true);
            animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
                animator.speed = Speed;
            newest = this;
            Invoke(nameof(StartLash), LashAt);
            Invoke(nameof(Strike), Latest);
            InvokeRepeating(nameof(End), Life, 1f);
        }

        /// <summary>On the caster's machine, as the root is made: the hit it strikes with and the way it faces.</summary>
        public void Arm(Character by, HitData hit, Quaternion facing)
        {
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || strike == null || hit == null)
                return;
            caster = by;
            raise = hit.m_skillRaiseAmount;
            strike.Setup(by, Vector3.zero, -1f, hit, null, null);
            // The game's attack put the caster's skill on the hit already; the area would put it on a second time.
            strike.m_skill = Skills.SkillType.None;
            transform.rotation = facing;
            nview.GetZDO().SetRotation(facing);
            armed = true;
        }

        /// <summary>The whip lands: on the owner, once, the strike hits.</summary>
        public void Strike()
        {
            if (!armed || struck)
                return;
            struck = true;
            CasterSafe.Run("root strike", Land);
        }

        private void Land()
        {
            strike.Initiate();
            if (caster != null && raise > 0f && HitSomeone())
                caster.RaiseSkill(Skills.SkillType.ElementalMagic, raise);
        }

        /// <summary>The area keeps what it hit; only characters it was let hit (the caster's enemies) count.</summary>
        private bool HitSomeone()
        {
            foreach (GameObject hit in strike.m_hitList)
            {
                if (hit != null && hit.GetComponent<Character>() != null)
                    return true;
            }
            return false;
        }

        private void StartLash()
        {
            if (animator != null)
                animator.SetTrigger(Lash);
        }

        private void End()
        {
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || ZNetScene.instance == null)
                return;
            WandRoots.Fade.Create(transform.position, transform.rotation);
            ZNetScene.instance.Destroy(gameObject);
        }
    }

    /// <summary>The root clip's <c>Hit</c> (its whip landing), on the animator's object, where the clip sends it.</summary>
    public sealed class RootHit : MonoBehaviour
    {
        private WandRoot root;

        private void Awake() => root = GetComponentInParent<WandRoot>();

        public void Hit()
        {
            if (root != null)
                root.Strike();
        }
    }
}
