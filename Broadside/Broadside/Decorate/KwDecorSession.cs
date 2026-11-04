using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The auto-decorate tool's state on the player's own game (DECORATE.md, "How it plays"): the area's size (6 to 64 m
    /// across, 24 to start), the density (1 to 5, 3 to start), and once a click has locked an area, its reading, then its
    /// layout for the current roll (N rolls another; the seed comes from the area's place and size and the roll, so the
    /// same area and roll give the same layout). Alt + wheel or [ ] size the area before the click and set the density
    /// after it (<see cref="Turn"/>). The work runs a few milliseconds a frame from the driver (<see cref="KwDecorDriver"/>).
    /// Nothing here is sent or saved.
    /// </summary>
    public static class KwDecorSession
    {
        public const float MinHalf = 3f, MaxHalf = 32f;
        private static readonly List<KwDecorPlan> none = new List<KwDecorPlan>();
        private static int roll;

        /// <summary>The area's size, metres from its middle to a side.</summary>
        public static float Half { get; private set; } = 12f;

        public static int Density { get; private set; } = 3;
        public static KwDecorRead? Read { get; private set; }
        public static KwDecorLayout? Layout { get; private set; }

        /// <summary>Changes each time what shows changes (the previews and the lines follow it).</summary>
        public static int Version { get; private set; }

        public static bool Locked => Read != null;

        /// <summary>A layout worked out with something in it.</summary>
        public static bool Ready => Layout != null && Layout.Done && Layout.Plans.Count > 0;

        /// <summary>An area read from here, turned to a frame, its cells laid out from an anchor (so a floor's edges fall on their sides).</summary>
        public static void Lock(Vector3 centre, float yaw, Vector3 anchor)
        {
            (Read, Layout, roll) = (new KwDecorRead(Snap(centre, yaw, anchor), yaw, Half), null, 0);
            Version++;
        }

        // The centre moved to the nearest point a whole number of cells from the anchor along the frame.
        private static Vector3 Snap(Vector3 centre, float yaw, Vector3 anchor)
        {
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            Vector3 right = turn * Vector3.right, forward = turn * Vector3.forward, d = centre - anchor;
            float x = Mathf.Round(Vector3.Dot(d, right) / KwDecorGrid.Step), z = Mathf.Round(Vector3.Dot(d, forward) / KwDecorGrid.Step);
            Vector3 snapped = anchor + (right * x + forward * z) * KwDecorGrid.Step;
            return new Vector3(snapped.x, centre.y, snapped.z);
        }

        public static void Cancel()
        {
            (Read, Layout) = (null, null);
            Version++;
        }

        /// <summary>Another layout for the same area.</summary>
        public static void Reroll()
        {
            if (Read == null || !Read.Done)
                return;
            (roll, Layout) = (roll + 1, null);
            Version++;
        }

        /// <summary>Alt + wheel or [ ]: the area's size before the click, the density once an area is locked.</summary>
        public static void Turn(int notches)
        {
            if (Locked)
                Denser(notches);
            else
                Resize(notches);
        }

        // The area 2 m a notch wider or narrower.
        private static void Resize(int notches)
        {
            float half = Mathf.Clamp(Half + notches, MinHalf, MaxHalf);
            if (half != Half)
                (Half, Version) = (half, Version + 1);
        }

        private static void Denser(int notches)
        {
            int density = Mathf.Clamp(Density + notches, 1, KwDecorLayout.Densities);
            if (density != Density)
                (Density, Version) = (density, Version + 1);
        }

        /// <summary>The reading, then the layout, worked on until the time (realtime since startup, a double) runs out.</summary>
        public static void Work(double until)
        {
            if (Read == null)
                return;
            if (!Read.Done)
            {
                Read.Step(until);
                return;
            }
            KwDecorLayout layout = Layout ??= new KwDecorLayout(Read.Area, Seed());
            bool was = layout.Done;
            layout.Step(until);
            if (layout.Done && !was)
                Version++;
        }

        /// <summary>The decorations shown at the density, once the layout is worked out.</summary>
        public static List<KwDecorPlan> Shown() => Layout != null && Layout.Done ? Layout.Shown(Density) : none;

        private static int Seed()
        {
            Vector3 at = Read!.Grid.Centre;
            unchecked
            {
                return Mathf.RoundToInt(at.x * 2f) * 73856093 ^ Mathf.RoundToInt(at.z * 2f) * 19349663 ^ Mathf.RoundToInt(Read.Grid.Half) * 83492791 ^ roll * 7919;
            }
        }
    }
}
