using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wayfare.SeaGates
{
    /// <summary>The measured water of a gate being placed or of an unpaired pillar being looked at, client only: a post
    /// at each of the nine spots the rules measure (<see cref="SeaGateDepth.Survey"/>), running from the seabed up out
    /// of the water, so its length under the surface is the depth, green when deep enough and red when not; and a lane
    /// on the water from the gate's middle out to each side, green where ships may come out. Shows the player exactly
    /// where to dig or which spot to move away from. Hides itself when nothing has refreshed it for a frame.</summary>
    internal sealed class SeaGatePreviewMarks : MonoBehaviour
    {
        private const float PostWidth = 0.15f;
        private const float LaneWidth = 0.1f;
        private const float PostAbove = 0.8f;      // how far a post stands out of the water
        private const float MaxPostDepth = 8f;     // deep water: the post stops this far down
        private const float DryPost = 1f;          // on land: a short red post
        private const float LaneLift = 0.05f;
        private static readonly Color OkColour = new Color(0.35f, 1f, 0.4f, 0.9f);
        private static readonly Color BadColour = new Color(1f, 0.3f, 0.25f, 0.9f);

        private static SeaGatePreviewMarks instance;

        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private int used;
        private int refreshedFrame = -10;

        /// <summary>Draws the posts and lanes for these spots; call every frame while they should stay.</summary>
        public static void Show(List<DepthSample> samples)
        {
            SeaGatePreviewMarks marks = Ensure();
            marks.refreshedFrame = Time.frameCount;
            marks.used = 0;
            foreach (DepthSample sample in samples)
                marks.DrawPost(sample);
            Vector3? middle = MiddleOf(samples);
            if (middle.HasValue)
            {
                marks.DrawLane(samples, middle.Value, SeaGateFields.SideFront);
                marks.DrawLane(samples, middle.Value, SeaGateFields.SideBack);
            }
            marks.HideFrom(marks.used);
        }

        public static void Hide()
        {
            if (instance != null)
                instance.HideFrom(0);
        }

        private static SeaGatePreviewMarks Ensure()
        {
            if (instance != null)
                return instance;
            instance = new GameObject("Wayfare.SeaGatePreviewMarks").AddComponent<SeaGatePreviewMarks>();
            return instance;
        }

        /// <summary>Under water: from the seabed (at most 8 m down) to a little above the surface. On land or where the
        /// ground is unknown: a short red post on the ground.</summary>
        private void DrawPost(DepthSample sample)
        {
            if (sample.Depth <= 0f)
            {
                Vector3 ground = sample.Surface - Vector3.up * sample.Depth;
                Draw(ground, ground + Vector3.up * DryPost, PostWidth, false);
                return;
            }
            Vector3 bottom = sample.Surface - Vector3.up * Mathf.Min(sample.Depth, MaxPostDepth);
            Draw(bottom, sample.Surface + Vector3.up * PostAbove, PostWidth, sample.Ok);
        }

        /// <summary>On the water, from the gate's middle to the farthest spot on that side.</summary>
        private void DrawLane(List<DepthSample> samples, Vector3 middle, int side)
        {
            Vector3? far = null;
            foreach (DepthSample sample in samples)
            {
                if (sample.Side == side)
                    far = sample.Surface;
            }
            if (far.HasValue)
                Draw(middle + Vector3.up * LaneLift, far.Value + Vector3.up * LaneLift, LaneWidth, SeaGateDepth.SideOk(samples, side));
        }

        /// <summary>The middle of the three spots between the pillars (the survey lists them first, in order).</summary>
        private static Vector3? MiddleOf(List<DepthSample> samples)
        {
            int seen = 0;
            foreach (DepthSample sample in samples)
            {
                if (sample.Side == 0 && ++seen == 2)
                    return sample.Surface;
            }
            return null;
        }

        private void Draw(Vector3 from, Vector3 to, float width, bool ok)
        {
            LineRenderer line = Next(width);
            if (line == null)
                return;
            Color colour = ok ? OkColour : BadColour;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startColor = colour;
            line.endColor = colour;
            line.enabled = true;
        }

        private LineRenderer Next(float width)
        {
            if (used == lines.Count)
            {
                LineRenderer made = Build();
                if (made == null)
                    return null;
                lines.Add(made);
            }
            LineRenderer line = lines[used++];
            line.startWidth = width;
            line.endWidth = width;
            return line;
        }

        /// <summary>A line renderer on its own child, so there can be many; null when the game has no usable material.</summary>
        private LineRenderer Build()
        {
            Material material = SeaGatePreviewMaterial.Get();
            if (material == null)
                return null;
            GameObject child = new GameObject("Mark");
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            line.enabled = false;
            return line;
        }

        private void HideFrom(int index)
        {
            for (int i = index; i < lines.Count; i++)
                lines[i].enabled = false;
        }

        private void LateUpdate()
        {
            if (Time.frameCount - refreshedFrame > 1)
                HideFrom(0);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
