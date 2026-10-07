using System.Collections.Generic;
using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Installs every <see cref="ClonedPiece"/> after ZNetScene.Awake and after ObjectDB.Awake, so whichever wakes second
    /// finds both the scene and the hammer. Features add their piece in a static constructor or field initializer that runs
    /// before the first scene wakes (<see cref="Add"/> from the feature's own ZNetScene.Awake prefix is enough).
    /// </summary>
    public static class ClonedPieces
    {
        private static readonly List<ClonedPiece> pieces = new List<ClonedPiece>();

        public static ClonedPiece Add(ClonedPiece piece)
        {
            if (piece != null && !pieces.Contains(piece))
                pieces.Add(piece);
            return piece;
        }

        private static void InstallAll()
        {
            foreach (ClonedPiece piece in pieces)
                HookGuard.Run("piece " + piece.PrefabName, piece.Install);
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => InstallAll();
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => InstallAll();
        }
    }
}
