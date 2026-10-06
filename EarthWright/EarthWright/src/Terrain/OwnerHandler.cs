using EarthWright.Core;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Owner side: every terrain compiler registers <see cref="RpcName"/> on its network view. The machine that owns the
    /// compiler (whoever the game made its owner, as for the game's own terrain ops) checks the edit with the owner
    /// guards, applies it through the <see cref="Engine"/>, rebuilds the local heightmap at once and saves the compiler
    /// to its ZDO (which the game replicates to every client) through <see cref="SaveThrottle"/>, at most a few times a
    /// second while a click is held. Privileged flags are dropped unless the server relayed the edit. The sender always
    /// gets an answer (<see cref="EditAnswers"/>): applied, refused with the reason, or retry when this machine does not
    /// own the compiler (ownership moved while the edit travelled) or has not loaded it yet.
    /// </summary>
    public static class OwnerHandler
    {
        public const string RpcName = "EW_TerrainEdit";

        internal static void Register(TerrainComp comp)
        {
            if (comp.m_nview == null || !comp.m_nview.IsValid() || comp.m_hmap == null)
                return;
            comp.m_nview.Register<ZPackage>(RpcName, (sender, pkg) => Safe.Run("EarthWright terrain edit", () => Receive(comp, sender, pkg)));
        }

        private static void Receive(TerrainComp comp, long sender, ZPackage pkg)
        {
            if (comp == null || comp.m_nview == null)
                return;
            TerrainEdit edit = EditWire.Read(pkg, sender);
            if (edit == null)
                return;
            if (!comp.m_nview.IsValid() || !comp.m_nview.IsOwner() || !comp.m_initialized)
            {
                EditAnswers.Send(edit, EditAnswer.Retry);
                return;
            }
            bool approved = edit.Has(EditFlags.Privileged) && Side.IsServerPeer(sender);
            if (!approved)
                edit.Flags &= ~(EditFlags.Privileged | EditFlags.IgnoreLimits);
            GuardContext context = new GuardContext { Edit = edit, SenderPeer = sender, Comp = comp, ServerApproved = approved };
            string reason = EditGuards.CheckOwner(context);
            if (!string.IsNullOrEmpty(reason))
            {
                EditAnswers.Send(edit, EditAnswer.Refused, reason);
                return;
            }
            Apply(comp, edit);
            EditAnswers.Send(edit, EditAnswer.Applied);
        }

        /// <summary>Applies an already checked edit to a compiler this machine owns, rebuilds and saves (paced).</summary>
        public static void Apply(TerrainComp comp, TerrainEdit edit)
        {
            EditResult result = Engine.Apply(comp, edit);
            if (!result.Changed)
                return;
            edit.GetArea(out Vector3 center, out float radius);
            SaveThrottle.Request(comp, edit, center, radius);
            comp.m_hmap.Poke(1, !result.HeightChanged);
            if (ClutterSystem.instance != null)
                ClutterSystem.instance.ResetGrass(center, radius);
            if (GeneralSettings.DebugLog.Value)
                Plugin.Log.LogInfo($"Applied {edit.Source} at {center} r{radius:0.0}: height {result.HeightChanged}, paint {result.PaintChanged}");
            EditEvents.RaiseApplied(comp, edit);
        }
    }

    [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.Awake))]
    public static class TerrainCompAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(TerrainComp __instance) => OwnerHandler.Register(__instance);
    }
}
