using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// The screen heights of a combined group's LOD levels. Unity draws a LOD level while size × lodBias ÷ (distance ×
    /// 2 tan(fov/2)) is at least its height, the distance taken from the camera to the LOD group's reference point. A
    /// group has one LOD group for all its pieces, with two levels (each piece's detailed level, then its simple one; a
    /// piece with one level is in both) or one when no piece has two. The detailed level ends at the largest detail
    /// reach of its pieces and the group is culled at their largest cull reach (<see cref="LodLayouts.Reach"/>), each
    /// plus the group's radius (how far its reference point is from its pieces' own) over K, where K is lodBias ÷
    /// (2 tan(fov/2)) for the main camera. So no piece ever drops detail or disappears earlier than the game would draw
    /// it; the cost is drawing some pieces in more detail or further than the game. K is read every
    /// <see cref="CheckSeconds"/>; when the graphics settings or the field of view move it by more than
    /// <see cref="ChangeShare"/>, every group's heights are set again (no mesh is rebuilt).
    /// </summary>
    internal static class GroupLod
    {
        private const float CheckSeconds = 1f;
        private const float ChangeShare = 0.02f;
        private const float DefaultFov = 65f;
        private const float MinHeight = 0.00001f;

        private static float _nextCheck;

        public static float K { get; private set; }

        /// <summary>True when K changed and the groups' heights need setting again.</summary>
        public static bool Refresh(float now)
        {
            if (K > 0f && now < _nextCheck)
            {
                return false;
            }
            _nextCheck = now + CheckSeconds;
            Camera camera = Utils.GetMainCamera();
            float fov = camera != null ? camera.fieldOfView : DefaultFov;
            float k = QualitySettings.lodBias / (2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
            if (K > 0f && Mathf.Abs(k - K) <= K * ChangeShare)
            {
                return false;
            }
            bool changed = K > 0f;
            K = Mathf.Max(k, 0.01f);
            return changed;
        }

        /// <summary>Sets the heights of a group of size 1: one level culled at <paramref name="cull"/>, or two
        /// switching at <paramref name="detail"/>.</summary>
        public static void Fill(LOD[] lods, float detail, float cull, float radius)
        {
            float margin = radius / Mathf.Max(K, 0.01f);
            if (lods.Length == 1)
            {
                lods[0].screenRelativeTransitionHeight = Height(cull + margin, 1f);
                return;
            }
            float first = Height(detail + margin, 1f);
            lods[0].screenRelativeTransitionHeight = first;
            lods[1].screenRelativeTransitionHeight = Height(cull + margin, first);
        }

        public static void Reset()
        {
            K = 0f;
            _nextCheck = 0f;
        }

        // A level reaching this far, kept below the level before it as Unity requires.
        private static float Height(float reach, float below)
        {
            float height = Mathf.Clamp(1f / Mathf.Max(reach, 1f), MinHeight, 1f);
            return height >= below ? below * 0.999f : height;
        }
    }
}
