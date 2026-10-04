using UnityEngine;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// A light worn on the head shines from the camera too while it is out (Circlet Light): a light of our own on the
    /// game camera, pointing where it looks, given the worn light's settings every frame (so a light another mod
    /// recolours, dims or switches off at runtime follows), except where Circlet Intensity, Range or Spot Angle set
    /// their own. Off with the worn light, with the camera back, or with Circlet Light off. Local only: never
    /// networked; the circlet on the player stays lit as before.
    /// </summary>
    public static class CircletLight
    {
        private static Light copy;

        public static void Update(GameCamera camera)
        {
            Light source = CameraState.Active && CameraPrefs.CircletLight.Value ? CircletSource.Find(Player.m_localPlayer) : null;
            if (source == null)
            {
                if (copy != null)
                    copy.enabled = false;
                return;
            }
            Light light = Get(camera);
            Copy(light, source);
            light.enabled = true;
        }

        private static Light Get(GameCamera camera)
        {
            if (copy != null && copy.transform.parent == camera.transform)
                return copy;
            GameObject holder = new GameObject("OpenKeep_CameraCirclet");
            holder.transform.SetParent(camera.transform, worldPositionStays: false);
            copy = holder.AddComponent<Light>();
            return copy;
        }

        private static void Copy(Light light, Light source)
        {
            light.type = source.type;
            light.color = source.color;
            light.cookie = source.cookie;
            light.renderMode = source.renderMode;
            light.cullingMask = source.cullingMask;
            light.shadows = source.shadows;
            light.shadowStrength = source.shadowStrength;
            light.intensity = Own(CameraPrefs.CircletIntensity.Value, source.intensity);
            light.range = Own(CameraPrefs.CircletRange.Value, CircletSource.BaseRange(source));
            light.spotAngle = Own(CameraPrefs.CircletSpotAngle.Value, source.spotAngle);
            light.innerSpotAngle = Mathf.Min(source.innerSpotAngle, light.spotAngle);
        }

        /// <summary>The setting when it is above 0, else the worn light's own value.</summary>
        private static float Own(float setting, float own) => setting > 0f ? setting : own;
    }
}
