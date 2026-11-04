using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A crate dropped from a crane from too high breaks as it lands (2026-11-03, the user's words: "if a crate is dropped from
    /// a crane from higher than 2 meters, then it breaks on impact and the contents go all over. Stackable items should show
    /// as stacks"). When a crane lets a crate go (Block, the operator leaving, the crane gone, the crate not named for three
    /// seconds), its owner measures how far its bottom falls to where it comes to rest (the ground, a floor, a deck, a cart's
    /// bed); more than <see cref="Height"/> and it breaks there: the landing written to its ZDO with <see cref="BreakHash"/>,
    /// so every screen lets it fall there under gravity (<see cref="KwCrateLook"/>) and nobody opens, hooks or removes it
    /// meanwhile (<see cref="KwCratePatches"/>), and on its owner, once its drawn crate has landed (or the fall's time and a
    /// half second more have passed), its items are dropped whole round the landing (<see cref="KwCrateSpill"/>) and then
    /// the crate is broken the game's way (<c>WearNTear.Remove</c>: its destroyed effects, sound and fragments on every
    /// screen, its build resources dropped as any broken piece's). Falling onto water it never breaks: it floats, as crates
    /// do. Done once: a crate already broken or removed has no component left to break it again, and the owner's flag stops a
    /// second run; a crate whose owner changed mid-fall is broken by its new owner's check (<see cref="Keep"/>).
    /// </summary>
    public sealed class KwCrateBreak : MonoBehaviour
    {
        /// <summary>The highest fall a crate takes whole (m, its bottom to where it lands).</summary>
        public const float Height = 2f;

        /// <summary>On a crate: it is falling to break where its ZDO has it.</summary>
        public static readonly int BreakHash = "kwc_break".GetStableHashCode();

        private const float Gravity = 9.81f, Late = 0.5f, Landed = 0.02f;
        private Vector3 at;
        private float due;
        private bool done;

        /// <summary>Whether the crate is falling to break (its ZDO says so).</summary>
        public static bool Pending(KwCrate crate) => crate.View != null && crate.View.IsValid() && crate.View.GetZDO().GetBool(BreakHash);

        /// <summary>
        /// On the owner, let go from a crane: a fall of more than <see cref="Height"/> from where it is drawn to <paramref name="land"/>
        /// breaks it there as it lands (the landing written, the break armed). Whether it breaks.
        /// </summary>
        public static bool Begin(KwCrate crate, Vector3 land, Quaternion turn)
        {
            float fall = crate.transform.position.y - land.y;
            if (fall <= Height)
                return false;
            ZDO zdo = crate.View!.GetZDO();
            zdo.Set(BreakHash, true);
            zdo.SetPosition(land);
            zdo.SetRotation(turn);
            Arm(crate, land, Mathf.Sqrt(2f * fall / Gravity));
            return true;
        }

        /// <summary>On the owner's check: a crate falling to break with no break armed here (its owner changed mid-fall) breaks where its ZDO has it.</summary>
        public static void Keep(KwCrate crate)
        {
            if (Pending(crate) && !crate.TryGetComponent(out KwCrateBreak _))
                Arm(crate, crate.View!.GetZDO().GetPosition(), 0f);
        }

        private static void Arm(KwCrate crate, Vector3 land, float fallTime)
        {
            KwCrateBreak armed = crate.gameObject.AddComponent<KwCrateBreak>();
            (armed.at, armed.due) = (land, Time.time + fallTime + Late);
        }

        private void Update()
        {
            if (!done && (transform.position.y <= at.y + Landed || Time.time >= due))
                Guard.Run("Broadside crate break", static b => b.Break(), this);
        }

        // On the owner, once: the items out whole round the landing first, then the crate broken the game's way.
        private void Break()
        {
            ZNetView? view = GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner())
            {
                Destroy(this);
                return;
            }
            done = true;
            foreach (Collider part in GetComponentsInChildren<Collider>())
                part.enabled = false;
            Container? box = GetComponent<Container>();
            if (box != null)
                KwCrateSpill.Out(box, at, (int)view.GetZDO().m_uid.ID);
            if (TryGetComponent(out WearNTear wear))
                wear.Remove(false);
            else
                ZNetScene.instance.Destroy(gameObject);
        }
    }
}
