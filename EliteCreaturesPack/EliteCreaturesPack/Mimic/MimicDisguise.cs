using System.Collections;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// The mimic's disguise and ambush. While it sleeps it is a chest on every client: no health bar (the chest's hover
    /// text comes from <see cref="MimicDisguisePatch"/>). Pressing E on it sends the opener to the creature's owner, which wakes it,
    /// sets the opener as its target and plays the ambush: the lid bursts open and snaps shut on the opener - a bite that
    /// cannot be dodged or blocked, because the opener walked into it. From then on it is an ordinary awake creature
    /// whose AI lunges with <see cref="MimicBite"/>. A copy split off by Splintering is born awake: it spawns mid-fight.
    /// </summary>
    public class MimicDisguise : MonoBehaviour, Interactable
    {
        public const string OpenRpc = "ecp_mimic_open";
        private const float SnapDelay = 10f / 30f;   // the ambush clip closes its jaws on frame 10
        private const float BiteReach = 3.2f;

        private Character _character = null!;
        private MonsterAI _ai = null!;
        private ZNetView _nview = null!;

        public bool IsDormant => _ai != null && _ai.IsSleeping();

        private void Awake()
        {
            _character = GetComponent<Character>();
            _ai = GetComponent<MonsterAI>();
            _nview = GetComponent<ZNetView>();
            if (_nview != null && _nview.IsValid())
            {
                _nview.Register<ZDOID>(OpenRpc, (sender, opener) => Guard.Run("MimicDisguise.Open", () => Opened(opener)));
                if (_nview.IsOwner())
                {
                    EliteHandOff.MarkDisguised(_nview);
                }
            }
            ApplySettings();
        }

        /// <summary>
        /// The live mimic settings (the server's, when locked), before Elite Creatures Reborn, when installed, captures
        /// its speeds in Start.
        /// </summary>
        private void ApplySettings()
        {
            if (_character is Humanoid humanoid)
            {
                humanoid.m_runSpeed = MimicSettings.RunSpeed;
                humanoid.m_walkSpeed = MimicSettings.RunSpeed * 0.5f;
            }
            MimicBite.ApplySettings();
        }

        private void Start()
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner() || !IsDormant)
            {
                return;
            }
            if (EliteHandOff.IsSplitOff(_nview))
            {
                _ai.Wakeup(); // an Elite Creatures Reborn Splintering copy: its parent was already fighting
            }
        }

        private void Update()
        {
            if (_character != null)
            {
                _character.m_hideHud = IsDormant; // no health bar or name over a "chest"
            }
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || !IsDormant || !(user is Player) || _nview == null || !_nview.IsValid())
            {
                return false;
            }
            _nview.InvokeRPC(OpenRpc, user.GetZDOID()); // to the owner, which wakes it and bites
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        // OWNER: wake, turn on the opener and bite at the snap.
        private void Opened(ZDOID opener)
        {
            if (!_nview.IsOwner() || !IsDormant)
            {
                return;
            }
            GameObject? found = ZNetScene.instance.FindInstance(opener);
            Character? victim = found != null ? found.GetComponent<Character>() : null;
            _ai.Wakeup();
            _ai.SetAlerted(true);
            if (victim != null)
            {
                _ai.SetTarget(victim);
            }
            GetComponent<ZSyncAnimation>().SetTrigger("ambush");
            StartCoroutine(Snap(victim));
        }

        private IEnumerator Snap(Character? victim)
        {
            yield return new WaitForSeconds(SnapDelay);
            if (victim == null || victim.IsDead() || _character.IsDead())
            {
                yield break;
            }
            if (Vector3.Distance(victim.transform.position, transform.position) <= BiteReach)
            {
                victim.Damage(AmbushHit(victim));
            }
        }

        /// <summary>The bite's own damage (so stars and world tiers scale it as they scale the lunge), unavoidable.</summary>
        private HitData AmbushHit(Character victim)
        {
            ItemDrop.ItemData.SharedData bite = MimicBite.Shared;
            var hit = new HitData
            {
                m_damage = bite.m_damages.Clone(),
                m_point = victim.GetCenterPoint(),
                m_dir = (victim.transform.position - transform.position).normalized,
                m_pushForce = bite.m_attackForce,
                m_dodgeable = false,
                m_blockable = false,
                m_ranged = false,
            };
            hit.SetAttacker(_character);
            return hit;
        }
    }
}
