using System;
using BundlePrefabs;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Patches;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// A Raiders Chest in the world, on every machine that holds one (features/raids.md sections 2, 4 and 5): what its
    /// hover says and what E does. E opens it like any chest; Shift + E asks the chest's owner to sound a raid
    /// (<see cref="ChestSounding"/>); while a raid is on it stays locked, and holding E for three seconds stops the raid
    /// (<see cref="ChestHold"/>, <see cref="Raid.Stop(ZNetView)"/>). It answers its own RPCs - the owner's half of the
    /// sounding, the refusal back to the player who asked, the horn - and on the owner it ends the raid as robbed when
    /// the chest is broken, before its ZDO goes. The raid itself is the <see cref="RaidRunner"/> beside it. With the
    /// setting off it is an ordinary coin chest that cannot sound a raid. The player reaches it through
    /// <see cref="ChestFace"/> on the model, so the game's container hover, used by every chest in the world, is never
    /// patched.
    /// </summary>
    internal sealed class RaidChest : MonoBehaviour
    {
        private readonly ChestHover _hover = new ChestHover();
        private Transform? _hornMouth;
        private bool _live;
        private bool _faultReported;

        public ZNetView View { get; private set; } = null!;

        public Container Container { get; private set; } = null!;

        /// <summary>The hold on E that stops a raid.</summary>
        public ChestHold Hold { get; } = new ChestHold();

        /// <summary>True on a chest in the world (not the placement ghost) whose ZDO is still there.</summary>
        public bool Live => _live && View != null && View.IsValid();

        /// <summary>Where the horn sounds from: the middle of its bell.</summary>
        public Vector3 HornAt => _hornMouth != null ? _hornMouth.position : transform.position + Vector3.up;

        private void Awake() => Guard.Run("RaidChest.Awake", Setup);

        private void Setup()
        {
            View = GetComponent<ZNetView>();
            Container = GetComponent<Container>();
            if (View == null || Container == null || !View.IsValid() || Container.GetInventory() == null)
            {
                return; // a placement ghost: no ZDO, nothing to sound
            }
            _live = true;
            _hornMouth = GameMaterials.Find(transform, ChestModel.HornMouth);
            View.Register(ChestSounding.AskRpc, OnAsk);
            View.Register<string>(ChestSounding.ReplyRpc, OnReply);
            View.Register(RaidKeys.HornRpc, OnHorn);
            ChestCoins.Remember(Container.GetInventory(), this);
            WearNTear wear = GetComponent<WearNTear>();
            if (wear != null)
            {
                wear.m_onDestroyed += OnBroken;
            }
        }

        /// <summary>The hover, every frame the player looks at the chest: a cached string (<see cref="ChestHover"/>).</summary>
        public string HoverText()
        {
            try
            {
                return Live ? _hover.Text(this) : "";
            }
            catch (Exception e)
            {
                ReportOnce(e);
                return "";
            }
        }

        /// <summary>E (and its repeats while held) and Shift + E from the local player.</summary>
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            try
            {
                return Live && Use(user, hold, alt);
            }
            catch (Exception e)
            {
                ReportOnce(e);
                return false;
            }
        }

        /// <summary>The ward test the chest's own opening makes: a player a ward shuts out cannot sound or stop a raid either.</summary>
        public bool Allowed() => !Container.m_checkGuardStone || PrivateArea.CheckAccess(transform.position);

        private bool Use(Humanoid user, bool hold, bool alt)
        {
            if (Raid.IsRunning(View))
            {
                return DuringRaid(user, hold);
            }
            if (hold)
            {
                return false;
            }
            if (alt && RaidSettings.ChestEnabled)
            {
                return ChestSounding.Ask(this);
            }
            return Container.Interact(user, false, false);
        }

        // Locked while the raid runs (opening would hand the chest, and the raid, to the opener); held, E stops it.
        private bool DuringRaid(Humanoid user, bool hold)
        {
            if (!hold)
            {
                user.Message(MessageHud.MessageType.Center, ChestText.Locked);
            }
            if (!Hold.Held(hold) || !Allowed())
            {
                return !hold;
            }
            Raid.Stop(View);
            return true;
        }

        private void OnAsk(long sender) =>
            SafeCall.Run("Raiders Chest sounding", static (chest, asker) => ChestSounding.Answer(chest, asker), this, sender);

        private void OnReply(long sender, string text) =>
            SafeCall.Run("Raiders Chest refusal", static message => ChestSounding.Show(message), text);

        private void OnHorn(long sender) =>
            SafeCall.Run("Raiders Chest horn", static chest => RaidHorn.Play(chest.HornAt), this);

        // WearNTear.Destroy calls this on the chest's owner, just before the chest and its ZDO are removed.
        private void OnBroken() => SafeCall.Run("Raiders Chest broken", static chest => chest.Robbed(), this);

        private void Robbed()
        {
            if (Live && View.IsOwner() && Raid.IsRunning(View))
            {
                Raid.Robbed(View);
            }
        }

        private void ReportOnce(Exception e)
        {
            if (!_faultReported)
            {
                _faultReported = true;
                Guard.Report(e, $"Raiders Chest at {transform.position:F0}");
            }
        }
    }
}
