using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// On each hammock (and only there: its hover is named for it by this, <see cref="KwHammockHover"/>), on every peer.
    /// Its shape is read from its ZDO when it wakes (<see cref="KwHammockSpec.Key"/>, written by the game that hung it in
    /// the frame it placed it, <see cref="Take"/>) and its look and colliders made from it (<see cref="KwHammockModel"/>);
    /// one with no shape saved keeps the prefab's, two posts. On its owner, once a second, its hooks are held by what they
    /// are fixed into (<see cref="KwHammockHold"/>), or it comes down. On a screen, while it rides a launched ship
    /// (<see cref="KwShipRider"/>), its sling (cloth, bars, ropes and the sleeper's place) hangs plumb as the ship heels,
    /// swung about the line between its two rope points (each end fixed to the ship: a post on its deck, a hook in its
    /// beams, walls or masts), the one way a hammock hung from two points can swing: the world's up read in the sling's
    /// rest axes, the sling turned about the line by the heel across it (at most 40 degrees), eased so it lags a little like
    /// a weight on ropes. A sleeper lies on the spawn point under the sling, so he swings with it. Every frame within 40 m of
    /// the camera; farther away it stays as it was; on land, or once it no longer rides, it hangs at rest. Nothing kept or
    /// sent for the swing: each screen swings it from the ship as it draws it. Off on a placement ghost.
    /// </summary>
    public sealed class KwHammock : MonoBehaviour
    {
        private const float Most = 40f, Ease = 0.35f, Near = 40f, Look = 1f;
        private readonly List<Object> made = new List<Object>();
        private readonly KwHammockHold hold = new KwHammockHold();
        private System.Action? slow;
        private ZNetView? view;
        private Transform? sling;
        private Quaternion rest = Quaternion.identity;
        private float angle, speed, next;

        /// <summary>The shape it is built to.</summary>
        public KwHammockSpec Spec { get; private set; } = KwHammockSpec.Standard;

        /// <summary>Its rider record while it was built on a launched ship (looked for once a second), else null.</summary>
        public KwShipRider? Rider { get; private set; }

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            sling = transform.Find(KwHammockModel.SlingPath);
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null)
            {
                enabled = false;
                return;
            }
            if (KwHammockSpec.TryParse(zdo.GetString(KwHammockSpec.Key), out KwHammockSpec spec))
                KwShipBody.Step("Broadside hammock", () => Dress(spec));
        }

        /// <summary>A hammock just placed on this game: its shape written to its new ZDO and built at once, in the frame it was placed.</summary>
        public static void Take(GameObject piece, KwHammockSpec spec)
        {
            piece.GetComponent<ZNetView>()?.GetZDO()?.Set(KwHammockSpec.Key, spec.ToString());
            KwHammock? hammock = piece.GetComponent<KwHammock>();
            if (hammock != null)
                hammock.Dress(spec);
        }

        // Built to a shape: the parts it wore let go, its highlight and the game's material blocks told of the new ones.
        private void Dress(KwHammockSpec spec)
        {
            Free();
            (sling, rest) = KwHammockModel.Dress(gameObject, spec, made);
            (Spec, angle, speed) = (spec, 0f, 0f);
            WearNTear? wear = GetComponent<WearNTear>();
            if (wear != null && wear.m_nview != null && wear.m_nview.GetZDO() != null)
                wear.m_renderers = wear.GetHighlightRenderers();
            MaterialMan? blocks = MaterialMan.instance;
            if (blocks != null && blocks.m_blocks.ContainsKey(gameObject.GetInstanceID()))
                blocks.RegisterRenderers(gameObject, true);
        }

        private void Free()
        {
            foreach (Object mesh in made)
            {
                if (mesh != null)
                    Destroy(mesh);
            }
            made.Clear();
        }

        private void OnDestroy() => Free();

        private void Update()
        {
            if (Time.time >= next)
                KwShipBody.Step("Broadside hammock", slow ??= Slow);
            if (sling != null && KwShipBody.Drawn && Rider != null && Rider.On != null && Close())
                Swing();
        }

        // Once a second: its rider looked for (one is added when it is built on a ship or loads riding one), the sling at
        // rest when it rides none, and on its owner its hooks held.
        private void Slow()
        {
            next = Time.time + Look;
            Rider = GetComponent<KwShipRider>();
            if (Rider == null || Rider.On == null)
                Settle();
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo != null)
                hold.Tick(this, zdo);
        }

        private void Settle()
        {
            if (angle == 0f && speed == 0f || sling == null)
                return;
            (angle, speed) = (0f, 0f);
            sling.localRotation = rest;
        }

        private bool Close() => GameCamera.instance != null && (GameCamera.instance.transform.position - transform.position).sqrMagnitude < Near * Near;

        // The sling turned about the rope line (its rest z) until its up is the world's, as far as a turn about the line can make it.
        private void Swing()
        {
            Vector3 up = Quaternion.Inverse(transform.rotation * rest) * Vector3.up;
            float target = Mathf.Clamp(Mathf.Atan2(-up.x, up.y) * Mathf.Rad2Deg, -Most, Most);
            angle = Mathf.SmoothDampAngle(angle, target, ref speed, Ease);
            sling!.localRotation = rest * Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}
