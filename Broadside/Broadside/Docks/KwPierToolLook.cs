using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Dock tool's prefab (<c>BS_tool_pier</c>): a copy of the wood floor wearing a short wood dock of its own (a real
    /// section's shape, 1.4 m by 1.6 m on short pilings, <see cref="KwPierShape"/>) as its ghost and its rendered hammer
    /// icon (<see cref="KwIcons"/>, a screen only), with the kit's placing box (<see cref="KwPrefabs.Anchor"/>). It never
    /// turns (the wheel is the tool's, <see cref="KwToolWheel"/>), reaches a little farther than a piece for its first
    /// click, and needs a workbench in range as any piece built there.
    /// </summary>
    public static class KwPierToolLook
    {
        private const int Reach = 5;

        public static GameObject Make(GameObject source, CraftingStation? bench)
        {
            GameObject copy = PrefabBench.Copy(source, KwPierPrefabs.Tool);
            KwPrefabs.Strip(copy);
            GameObject model = KwPierPrefabs.Model(copy);
            KwPierMesh mesh = new KwPierMesh();
            KwPierShape.Build(mesh, Sample(), KwPierKinds.Wood);
            model.GetComponent<MeshFilter>().sharedMesh = mesh.ToMesh("bs_tool_pier");
            Material? look = KwPierLooks.Of(KwPierKinds.Wood);
            if (look != null)
                model.GetComponent<MeshRenderer>().sharedMaterial = look;
            KwPrefabs.Anchor(model);
            Piece piece = copy.GetComponent<Piece>();
            KwPierPrefabs.Configure(piece, KwPierWords.Name, bench);
            (piece.m_description, piece.m_canRotate, piece.m_extraPlacementDistance) = (KwPierWords.Description, false, Reach);
            WearNTear wear = copy.GetComponent<WearNTear>();
            (wear.m_new, wear.m_worn, wear.m_broken, wear.m_wet, wear.m_fragmentRoots) = (model, model, model, null, null);
            KwIcons.RenderAll(new[] { copy });
            return copy;
        }

        // A short section on short pilings, the same every time.
        private static KwPierSpec Sample() =>
            new KwPierSpec(1.4f, 1.6f, false, 7, new[] { new Vector2(1.2f, 1.2f), new Vector2(1.2f, 1.2f) });
    }
}
