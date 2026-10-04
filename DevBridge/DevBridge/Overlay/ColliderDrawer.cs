using System.Linq;
using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// colliders: the wire shape of every collider in the area, nearest first, coloured by kind: a trigger green, a
    /// creature's or player's red, a building piece's amber, anything else (rocks, trees, items, ships) blue. The
    /// terrain's heightmap colliders are left out; layer= keeps only the named layers.
    /// </summary>
    internal static class ColliderDrawer
    {
        private static readonly Color Creature = new Color(1f, 0.3f, 0.3f, 0.9f);
        private static readonly Color Building = new Color(1f, 0.75f, 0.2f, 0.9f);
        private static readonly Color Other = new Color(0.35f, 0.65f, 1f, 0.9f);
        private static readonly Color Trigger = new Color(0.4f, 1f, 0.45f, 0.8f);

        internal static void Draw(OverlayArea area, Category into)
        {
            Collider[] found = Physics.OverlapSphere(area.Centre, area.Radius, area.Layers, QueryTriggerInteraction.Collide);
            foreach (Collider collider in found.OrderBy(collider => (collider.bounds.center - area.Centre).sqrMagnitude))
            {
                if (collider.GetComponent<Heightmap>()) continue;
                string kind = Kind(collider);
                into.Count(kind);
                if (into.Lines.Room()) into.Lines.Add(ColliderWire.Of(collider), Colour(kind), 0.03f);
            }
        }

        private static string Kind(Collider collider)
        {
            if (collider.isTrigger) return "trigger";
            if (collider.GetComponentInParent<Character>()) return "character";
            return collider.GetComponentInParent<Piece>() ? "piece" : "static";
        }

        private static Color Colour(string kind) =>
            kind == "trigger" ? Trigger : kind == "character" ? Creature : kind == "piece" ? Building : Other;
    }
}
