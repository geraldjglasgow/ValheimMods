using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// wards: each ward's (PrivateArea's) protected radius, green while it is on and grey while off. The game measures it
    /// flat (PrivateArea.IsInside uses the distance across the ground), so it is a column of any height: a ring at the
    /// ward's base, and a thinner one at the height the overlay is drawn round when that is more than a metre away.
    /// </summary>
    internal static class WardDrawer
    {
        private static readonly Color Enabled = new Color(0.3f, 1f, 0.4f, 0.9f);
        private static readonly Color Disabled = new Color(0.6f, 0.6f, 0.6f, 0.8f);

        internal static void Draw(OverlayArea area, Category into)
        {
            foreach (PrivateArea ward in PrivateArea.m_allAreas)
            {
                if (!ward || Utils.DistanceXZ(ward.transform.position, area.Centre) > area.Radius + ward.m_radius) continue;
                bool on = ward.IsEnabled();
                into.Count(on ? "enabled" : "disabled");
                Color colour = on ? Enabled : Disabled;
                Vector3 at = ward.transform.position + Vector3.up * 0.1f;
                into.Lines.Add(Shapes.Ring(at, ward.m_radius), colour, 0.06f);
                if (Mathf.Abs(area.Centre.y - at.y) > 1f) into.Lines.Add(Shapes.Ring(new Vector3(at.x, area.Centre.y, at.z), ward.m_radius), colour, 0.03f);
            }
        }
    }
}
