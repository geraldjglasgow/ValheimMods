using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Where the bolt charge's white stream (<see cref="ChargeStream"/>) leaves the off hand: with a spellbook shown open on
    /// the palm (<see cref="BookOpen"/>) from the middle of its pages (the open book's top, pages up +Y; the closed book laid
    /// flat: its front cover, +Z), so the energy rises off the book and not from under it; else from the open palm's middle.
    /// The book's top is measured once per stream from its renderers, in the book's own frame.
    /// </summary>
    public static class StreamStart
    {
        /// <summary>
        /// The open palm's middle in the charge pose, metres along the left hand bone's own axes (the workshop's
        /// ee_casteranim build, 2026-10-11), so the stream leaves the palm, not the wrist.
        /// </summary>
        private static readonly Vector3 PalmOffset = new Vector3(-0.003f, 0.089f, 0.011f);

        /// <summary>
        /// The spellbook's open (or flat) model on the left hand's copy of what it holds, shown or not (only a spellbook has
        /// one, <see cref="BookOpen.Add"/>); null without one.
        /// </summary>
        public static Transform Book(Player player, out Vector3 top)
        {
            top = Vector3.zero;
            GameObject held = player.m_visEquipment != null ? player.m_visEquipment.m_leftItemInstance : null;
            if (held == null)
                return null;
            Transform book = held.transform.Find(BookOpen.Open);
            bool open = book != null;
            if (!open)
                book = held.transform.Find(BookOpen.Flat);
            if (book != null)
                top = Top(book, open ? Vector3.up : Vector3.forward);
            return book;
        }

        /// <summary>The stream's start this frame: the book's top while it is shown, else the palm's middle.</summary>
        public static Vector3 From(Transform palm, Transform book, Vector3 top) =>
            book != null && book.gameObject.activeInHierarchy ? book.TransformPoint(top) : palm.position + palm.rotation * PalmOffset;

        /// <summary>The middle of the book's face along <paramref name="up"/>, in its own frame, from every renderer's bounds.</summary>
        private static Vector3 Top(Transform book, Vector3 up)
        {
            Bounds all = default;
            bool any = false;
            foreach (Renderer renderer in book.GetComponentsInChildren<Renderer>(true))
            {
                Bounds local = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = book.InverseTransformPoint(renderer.transform.TransformPoint(local.center + Vector3.Scale(local.extents, sign)));
                    if (!any)
                        all = new Bounds(point, Vector3.zero);
                    all.Encapsulate(point);
                    any = true;
                }
            }
            return any ? all.center + Vector3.Scale(all.extents, up) : Vector3.zero;
        }
    }
}
