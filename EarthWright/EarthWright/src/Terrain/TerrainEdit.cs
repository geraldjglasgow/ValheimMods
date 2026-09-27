using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// One terrain edit as it travels from the player who made it to the owner of every terrain compiler it touches:
    /// either a <see cref="BrushStroke"/> or a <see cref="VertexSet"/>, plus who sent it and why.
    /// </summary>
    public sealed class TerrainEdit
    {
        public EditKind Kind = EditKind.Stroke;

        public EditFlags Flags;

        /// <summary>The sending player's id (<c>Player.GetPlayerID()</c>), for ward and zone checks on the owner.</summary>
        public long SenderPlayer;

        /// <summary>The sending machine's network id (<c>ZNet.GetUID()</c>), where refusals are sent back to.</summary>
        public long SenderPeer;

        /// <summary>What made the edit: the piece prefab name, or a name such as "ramp", "undo", "command:reset".</summary>
        public string Source = "";

        public BrushStroke Stroke;

        public VertexSet Vertices;

        public bool Has(EditFlags flag) => (Flags & flag) == flag;

        public static TerrainEdit ForStroke(BrushStroke stroke, string source)
        {
            return new TerrainEdit { Kind = EditKind.Stroke, Stroke = stroke, Source = source ?? "", SenderPlayer = LocalPlayerId(), SenderPeer = ZNet.GetUID() };
        }

        public static TerrainEdit ForVertices(VertexSet set, string source)
        {
            return new TerrainEdit { Kind = EditKind.Vertices, Vertices = set, Source = source ?? "", SenderPlayer = LocalPlayerId(), SenderPeer = ZNet.GetUID() };
        }

        /// <summary>Centre and radius (XZ) of everything the edit may touch.</summary>
        public void GetArea(out Vector3 center, out float radius)
        {
            if (Kind == EditKind.Stroke)
            {
                center = Stroke.Center;
                radius = Stroke.Reach + Engine.ExtraReach(this);
                return;
            }
            Bounds bounds = Vertices.Area();
            center = bounds.center;
            // Gentle slopes may relax ground this far beyond the edit itself.
            radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + Engine.ExtraReach(this);
        }

        private static long LocalPlayerId() => Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
    }
}
