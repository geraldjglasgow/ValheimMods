using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the crane's hook does, on the operator's game (the crane's owner). Pick up (Attack): a crate (the cargo crate or
    /// any storage crate, <see cref="KwCrate"/>) whose top the hook has been lowered onto, anywhere over it, is asked to hang
    /// from this crane: the crane names it in its ZDO
    /// (<see cref="KwCargo.CrateKey"/>) and calls the crate's owner (<see cref="KwCargo.HookRpc"/>), who agrees unless the
    /// crate is open or on another crane and names the crane in the crate's ZDO; only with both names does the crate hang
    /// (<see cref="KwCrate"/>). Hooked, the head is drawn up to the sling and lifts the crate a little. Set down (Block),
    /// the operator leaving or the crane changing owner: the crane forgets the crate and tells it to let go
    /// (<see cref="KwCargo.ReleaseRpc"/>); the crate's owner sets it down under where it hung, and either name missing for a
    /// few seconds lets it go too. Block over the game's cart with no room for the crate (or a crate too big, or a cart not
    /// ready) is refused with a message (<see cref="KwCartLoad.Refusal"/>). The crate's items never leave it but when it
    /// falls more than 2 m and breaks (<see cref="KwCrateBreak"/>). Lowering stops where the hook, or the crate's bottom,
    /// meets what is under it, a cart's load space too (<see cref="Lowest"/>).
    /// </summary>
    public sealed class KwCraneLift
    {
        private const float Answer = 4f, LiftAbove = 0.3f, Margin = 0.25f, Below = 0.45f, Above = 1.1f;
        private readonly KwCrane crane;
        private readonly ZNetView view;
        private readonly KwCraneRig rig;
        private float asked = -99f, raise;

        public KwCraneLift(KwCrane crane, ZNetView view, KwCraneRig rig) => (this.crane, this.view, this.rig) = (crane, view, rig);

        /// <summary>Whether a crate has been asked and has not answered yet.</summary>
        public bool Waiting => asked > 0f && Time.time - asked < Answer;

        private ZDOID Crate => view.GetZDO().GetZDOID(KwCargo.CrateKey);

        /// <summary>The most the head may drop now: the hook, or the crate hanging from it, resting on what is under it.</summary>
        public float Lowest(KwCranePose pose)
        {
            KwCrate? crate = KwCrate.Find(Crate);
            float free = crate != null && crate.HangsFrom(crane) ? crate.FreeBelow() : KwCargoCast.FreeBelow(rig.Hook!.position, crane.transform);
            return pose.Drop + free;
        }

        /// <summary>How far to raise the head this frame on its own (after a hook, a little lift).</summary>
        public float TakeRaise(float dt)
        {
            if (raise <= 0f)
                return 0f;
            float step = Mathf.Min(raise, KwCargo.HoistSpeed * dt);
            raise -= step;
            return step / Mathf.Max(1e-4f, KwCargo.HoistSpeed * dt);
        }

        public void PickUp(Player me)
        {
            if (!view.IsOwner())
                me.Message(MessageHud.MessageType.Center, KwCraneText.NotReady);
            else if (!Crate.IsNone())
                me.Message(MessageHud.MessageType.Center, KwCraneText.Holding);
            else if (KwCrate.Under(rig.Hook!.position, Margin, Below, Above) is KwCrate crate)
                Ask(crate);
            else
                me.Message(MessageHud.MessageType.Center, KwCraneText.NoCrate);
        }

        private void Ask(KwCrate crate)
        {
            view.GetZDO().Set(KwCargo.CrateKey, crate.Id);
            asked = Time.time;
            crate.AskHook(crane.Id);
        }

        /// <summary>The crate's answer, on the crane's owner: hooked (the head drawn up to its sling), or refused.</summary>
        public void Hooked(ZDOID crate, bool ok)
        {
            if (!view.IsOwner() || Crate != crate)
                return;
            asked = -99f;
            if (!ok)
            {
                view.GetZDO().Set(KwCargo.CrateKey, ZDOID.None);
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, KwCraneText.InUse);
                return;
            }
            KwCrate? hung = KwCrate.Find(crate);
            if (hung != null)
                crane.Jump(Mathf.Max(0f, hung.LiftPoint.y - rig.Hook!.position.y));
            raise = LiftAbove;
        }

        public void SetDown(Player me)
        {
            if (Crate.IsNone())
                me.Message(MessageHud.MessageType.Center, KwCraneText.Empty);
            else if (KwCartLoad.Refusal(KwCrate.Find(Crate)) is string full)
                me.Message(MessageHud.MessageType.Center, full);
            else
                Release();
        }

        /// <summary>Lets go of the crate on the hook, if any: forgotten here (on the owner) and told to settle under where it hangs.</summary>
        public void Release()
        {
            ZDOID crate = Crate;
            if (crate.IsNone())
                return;
            (asked, raise) = (-99f, 0f);
            if (view.IsOwner())
                view.GetZDO().Set(KwCargo.CrateKey, ZDOID.None);
            ZDO? zdo = ZDOMan.instance.GetZDO(crate);
            if (zdo != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), crate, KwCargo.ReleaseRpc, crane.Id);
        }

        /// <summary>A crate that never answered is forgotten.</summary>
        public void Tick()
        {
            if (asked < 0f || Time.time - asked < Answer)
                return;
            asked = -99f;
            if (view.IsOwner())
                view.GetZDO().Set(KwCargo.CrateKey, ZDOID.None);
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, KwCraneText.NoAnswer);
        }
    }
}
