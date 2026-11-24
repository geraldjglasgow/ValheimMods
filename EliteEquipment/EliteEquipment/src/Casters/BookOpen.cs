using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A spellbook drawn open on the palm while it is held (features/casters.md, "Held open"). The item's <c>attach</c>
    /// gets a second model beside the closed one, hidden: the workshop's open book, each book its own from its own bundle
    /// (<see cref="OpenAsset"/>: <c>ee_book_meadows_open</c>, <c>ee_book_swamp_open</c>, <c>ee_book_ashlands_open</c>; pages
    /// up +Y, spine along Z, covers along X, a <c>palm</c> point under the spine's middle), or, while the bundle has none
    /// yet, its own closed book again to lie flat on the palm (a warning from <see cref="CasterBundles.PrefabOr"/>). While
    /// the book's bundle is not in at all it has neither (the wood shield's look, the bundle's warning) and is held as the
    /// game holds a shield (<see cref="Opens"/>, read by <see cref="BookHold"/>). Every copy the game
    /// makes of the attach (the hand, the back, an item stand, the ground) has both, the closed one shown; only the left
    /// hand's copy is switched while the pose holds (<see cref="BookHold"/>), so the icon, the back, a stand and the
    /// ground stay closed.
    /// </summary>
    public static class BookOpen
    {
        /// <summary>The workshop's open book.</summary>
        public const string Open = "model_open";

        /// <summary>The closed book laid flat, while the bundle has no open book.</summary>
        public const string Flat = "model_flat";

        private const string Holder = "attach";
        private const string Closed = "model";

        /// <summary>The book's open model in its bundle.</summary>
        public static string OpenAsset(GearPiece book) => book.Asset + "_open";

        /// <summary>The held copy has a book to draw open (the workshop's open one or the closed one laid flat).</summary>
        public static bool Opens(GameObject held) => held.transform.Find(Open) != null || held.transform.Find(Flat) != null;

        /// <summary>The hidden open model under a book item's attach (after <see cref="HeldModel.Wear"/>).</summary>
        public static void Add(GameObject item, GameObject game, GearPiece book)
        {
            Transform holder = item.transform.Find(Holder);
            string asset = OpenAsset(book);
            GameObject model = CasterBundles.PrefabOr(book.Bundle, asset, book.Asset);
            if (holder == null || model == null)
                return;
            GameObject copy = Object.Instantiate(model, holder, false);
            copy.name = model.name == asset ? Open : Flat;
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = holder.gameObject.layer;
            HeldModel.Dress(copy, GameMaterials.Borrow(game, Holder));
            copy.SetActive(false);
        }

        /// <summary>A held copy shown open on the palm, or closed again as the game holds a shield.</summary>
        public static void Show(GameObject held, bool open, BookHand hand)
        {
            Transform root = held.transform;
            Transform book = root.Find(Open);
            bool lies = book == null;
            if (lies)
                book = root.Find(Flat);
            if (book == null)
                return;
            if (open)
                Place(book, hand, lies);
            book.gameObject.SetActive(open);
            Transform closed = root.Find(Closed);
            if (closed != null)
                closed.gameObject.SetActive(!open);
        }

        // The book's palm point on the palm's middle: the open book's spine along the fingers, its pages up off the palm;
        // the closed one (front +Z, top +Y) on its back cover, its top toward the fingers, by its grip. Set in the world
        // once per opening; the copy hangs from the hand, so it stays there.
        private static void Place(Transform book, BookHand hand, bool lies)
        {
            Transform palm = hand.Hand;
            Quaternion lie = lies
                ? Quaternion.LookRotation(hand.Normal, hand.Along)
                : Quaternion.LookRotation(hand.Along, hand.Normal);
            book.rotation = palm.rotation * lie;
            Transform point = GameMaterials.Find(book, lies ? "grip" : "palm");
            Vector3 at = palm.position + palm.rotation * hand.Palm;
            book.position += at - (point != null ? point.position : book.position);
        }
    }
}
