using System.Collections.Generic;
using System.Linq;
using DevBridge.Hitbox;
using DevBridge.Overlay;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The studio's view buttons beside the time and weather switches, each one of DevBridge's own views: Free camera lets
    /// the camera go where it is and flies it like the game's fly mode (<see cref="FlyCamera"/>), and off the camera goes
    /// back to the player; Hitboxes is /hitbox's view (each melee swing flashes its reach, each hit on the player draws a line from the
    /// attacker); Colliders is /overlay's collider shapes round the player or the free camera.
    /// </summary>
    internal static class StudioViews
    {
        private const string Colliders = "colliders";

        internal static void SetCamera(bool free)
        {
            GameCamera camera = GameCamera.instance ? GameCamera.instance : throw new BridgeException("no game camera: load a world first");
            if (free) FlyCamera.Start(camera);
            else FlyCamera.Stop(camera);
        }

        internal static void SetHitboxes(bool on)
        {
            HitboxView.On = on;
            if (on) ModHits.Hook();
        }

        internal static void SetColliders(bool on)
        {
            if (!on) { OverlayView.Hide(Colliders); return; }
            if (!ZNetScene.instance) throw new BridgeException("no world loaded");
            OverlayView.Show(Colliders);
            OverlayTicker.Ensure();
            OverlayView.Redraw();
            OverlayTicker.Drawn();
        }

        internal static Dictionary<string, object> State() => new Dictionary<string, object>
        {
            ["camera"] = GameCamera.instance && GameCamera.instance.m_freeFly ? "free" : "player",
            ["hitboxes"] = HitboxView.On,
            ["colliders"] = OverlayView.All.Any(category => category.Name == Colliders && category.On),
        };
    }
}
