using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>What a look at a piece decided: combine it now, look again later (a passing state such as a highlight,
    /// snow or a burn glow), or never (its kind moves, animates, lights up or draws with a shader that can not be
    /// merged).</summary>
    internal enum Verdict
    {
        Combine,
        Later,
        Never
    }

    /// <summary>One renderer of a piece as it was when the piece was looked at: what a combined mesh copies from it and
    /// what the checks compare against to see that it has not changed since.</summary>
    internal struct Part
    {
        public MeshRenderer Renderer;
        public MeshFilter Filter;
        public Mesh Mesh;
        public Matrix4x4 Matrix;
        public int Levels;
        public int Look;
        public int MaterialStart;
        public int MaterialCount;
    }

    /// <summary>
    /// One building piece this mod knows about, from its Awake to its OnDestroy. It waits until it has been unchanged
    /// for <see cref="CombinedMeshes.SettleSeconds"/>, is looked at (<see cref="PieceInspector"/>) and joins the group of
    /// its cell; while its parts are in that group's shown build they are turned off (<see cref="Renderer.forceRenderingOff"/>,
    /// which leaves <see cref="Renderer.enabled"/> and everything else the game and other mods read as it was). Every
    /// switch of that flag goes through <see cref="SetForced"/>, so a piece is never left turned off by mistake.
    /// </summary>
    internal sealed class PieceEntry
    {
        public readonly WearNTear Piece;
        public readonly int Id;
        public readonly List<Part> Parts = new List<Part>(2);
        public readonly List<Material> Materials = new List<Material>(2);

        // The group of its cell, or null while it waits or is left out.
        public CellGroup Group;
        public int MemberIndex = -1;
        public GroupKey Key;
        public Vector3 LodPoint;
        // For a piece with its own LOD group: the reach (world size over screen height, LodLayouts.Reach) at which its
        // detailed level ends (0 when it has one level only) and at which it is culled.
        public float DetailReach;
        public float CullReach;
        public int StateBits;
        public float TouchedAt;
        public float RetryAt;
        public int BuildStamp;

        // Its parts are in Group.Build (shown or hidden).
        public bool InBuild;
        // Its parts are turned off by this mod right now.
        public bool Forced;
        public bool Queued;
        public bool Removed;
        public bool LeftOut;

        public PieceEntry(WearNTear piece)
        {
            Piece = piece;
            Id = piece.GetInstanceID();
        }

        public bool Settled(float now)
        {
            return now >= TouchedAt + CombinedMeshes.SettleSeconds && now >= RetryAt;
        }

        public void SetForced(bool off)
        {
            if (Forced == off)
            {
                return;
            }
            Forced = off;
            for (int i = 0; i < Parts.Count; i++)
            {
                MeshRenderer renderer = Parts[i].Renderer;
                if (renderer != null)
                {
                    renderer.forceRenderingOff = off;
                }
            }
        }
    }
}
