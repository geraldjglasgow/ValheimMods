using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// A lead (<see cref="KwAnchorLead"/>) drawn under its windlass, on the dock (<see cref="KwAnchorDock"/>) and on a launched
    /// ship (<see cref="KwShipWindlass"/>): the fairlead's meshes (<see cref="KwAnchorLeadShapes"/>; its seat in the hull
    /// part's tier's deck wood, its metal the tier's: the plank wood in the Meadows, bronze, iron, silver, black metal,
    /// black metal, flametal), the cable a line renderer in the game's rope, and the anchor hung by its ring. No colliders:
    /// only drawn, never on a dedicated server.
    /// </summary>
    public static class KwAnchorLook
    {
        public const string FairleadName = "kw_fairlead", CableName = "kw_cable";

        private static readonly Dictionary<string, KwLook> Metals = new Dictionary<string, KwLook>
        {
            ["meadows"] = KwLook.Planks, ["blackforest"] = KwLook.Bronze, ["swamp"] = KwLook.Iron, ["mountains"] = KwLook.Silver,
            ["plains"] = KwLook.BlackMetal, ["mistlands"] = KwLook.BlackMetal, ["ashlands"] = KwLook.Flametal,
        };

        /// <summary>The fairlead under a windlass (in its frame, as the lead is), in its tier's wood and metal.</summary>
        public static GameObject Fairlead(Transform windlass, KwAnchorLead lead)
        {
            GameObject holder = new GameObject(FairleadName) { layer = windlass.gameObject.layer };
            holder.transform.SetParent(windlass, false);
            MeshBuilder wood = new MeshBuilder(Vector3.zero), metal = new MeshBuilder(Vector3.zero), dark = new MeshBuilder(Vector3.zero);
            KwAnchorLeadShapes.Draw(lead, wood, metal, dark);
            KwBiomeTier tier = KwBiomeTiers.Of(lead.Spot!.Hull.Piece.Data);
            Part(holder.transform, "wood", wood, KwBiomeLooks.For(KwPart.Deck, tier) ?? KwPrefabs.Planks);
            Part(holder.transform, "metal", metal, KwTierMaterials.Of(Metals.TryGetValue(tier.Id, out KwLook look) ? look : KwLook.Iron));
            if (lead.Spot.Pipe)
                Part(holder.transform, "throat", dark, KwTierMaterials.Of(KwLook.BlackMetal));
            return holder;
        }

        private static void Part(Transform holder, string name, MeshBuilder b, Material? material)
        {
            GameObject part = new GameObject(name) { layer = holder.gameObject.layer };
            part.transform.SetParent(holder, false);
            part.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("kw_fairlead_" + name);
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
        }

        /// <summary>The cable under a windlass: a line in the game's rope, its points set in world space.</summary>
        public static LineRenderer Cable(Transform windlass)
        {
            GameObject go = new GameObject(CableName) { layer = windlass.gameObject.layer };
            go.transform.SetParent(windlass, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = KwAnchorRoute.Width;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            if (KwSailCloth.Rope != null)
                line.sharedMaterial = KwSailCloth.Rope;
            return line;
        }

        /// <summary>The cable's points: the lead's (windlass kit) placed in the world, then, when given, a last one in the world.</summary>
        public static void Set(LineRenderer line, Transform windlass, List<Vector3> kit, Vector3? last = null)
        {
            line.positionCount = kit.Count + (last.HasValue ? 1 : 0);
            for (int i = 0; i < kit.Count; i++)
                line.SetPosition(i, windlass.TransformPoint(KwMesh.ToUnity(kit[i])));
            if (last.HasValue)
                line.SetPosition(kit.Count, last.Value);
        }

        /// <summary>The anchor (a child of the windlass) hung weighed from the lead's ring, its stock and shank as the lead lays them.</summary>
        public static void Hang(Transform anchor, KwAnchorLead lead) =>
            anchor.SetLocalPositionAndRotation(KwMesh.ToUnity(lead.Ring), Quaternion.LookRotation(KwMesh.ToUnity(lead.Stock), KwMesh.ToUnity(lead.Up)));

        /// <summary>The anchor stowed upright on the deck before the windlass, as the piece is made.</summary>
        public static void Stow(Transform anchor) => anchor.SetLocalPositionAndRotation(KwMesh.ToUnity(KwAnchor.Stowed), Quaternion.identity);
    }
}
