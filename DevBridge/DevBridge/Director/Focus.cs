using HarmonyLib;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// Shallow focus for close shots: the game camera's own depth of field (UnityStandardAssets DepthOfField, in
    /// assembly_sunshafts, reached by name) switched on and held on a point the move names, its autofocus off; put back
    /// as it was when the move ends.
    /// </summary>
    internal static class Focus
    {
        private static Behaviour dof;
        private static CameraEffects effects;
        private static Transform target;
        private static bool saved, wasEnabled, wasAuto;
        private static float wasAperture, wasBlur;

        internal static void Hold(Vector3 point, float aperture, float blur)
        {
            if (!Find()) return;
            Save();
            if (!target) target = new GameObject("DirectorFocus").transform;
            target.position = point;
            dof.enabled = true;
            effects.m_dofAutoFocus = false;
            Traverse fields = Traverse.Create(dof);
            fields.Field("focalTransform").SetValue(target);
            fields.Field("aperture").SetValue(aperture);
            fields.Field("maxBlurSize").SetValue(blur);
        }

        internal static void Release()
        {
            if (!saved) return;
            saved = false;
            if (!dof) return;
            Traverse fields = Traverse.Create(dof);
            fields.Field("focalTransform").SetValue(null);
            fields.Field("aperture").SetValue(wasAperture);
            fields.Field("maxBlurSize").SetValue(wasBlur);
            dof.enabled = wasEnabled;
            if (effects) effects.m_dofAutoFocus = wasAuto;
        }

        private static bool Find()
        {
            if (dof && effects) return true;
            GameCamera camera = GameCamera.instance;
            if (!camera) return false;
            dof = camera.GetComponent("DepthOfField") as Behaviour;
            effects = camera.GetComponent<CameraEffects>();
            return dof && effects;
        }

        private static void Save()
        {
            if (saved) return;
            saved = true;
            Traverse fields = Traverse.Create(dof);
            (wasEnabled, wasAuto) = (dof.enabled, effects.m_dofAutoFocus);
            (wasAperture, wasBlur) = (fields.Field("aperture").GetValue<float>(), fields.Field("maxBlurSize").GetValue<float>());
        }
    }
}
