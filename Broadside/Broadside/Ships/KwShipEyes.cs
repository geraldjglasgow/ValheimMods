using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// Inside a ship the view is the player's own eyes: with a Broadside deck over his head (a launched ship's deck, or a
    /// deck part on the dock), under a cabin's roof too low for the game's camera (the user's request, 2026-10-25: "the
    /// cabin should be in first person mode when you go into it if it's too short"; a launched ship's cabin roof or a cabin
    /// on the dock, its roof under <see cref="LowRoof"/> over his eye), or with his eye below the sea on a launched
    /// Broadside ship (in its hold), the camera stands
    /// at his eye, looking where he looks, his body drawn only as its shadow so it does not fill the view, and the camera's
    /// near plane brought in so the hull close by is not cut away. The game keeps its camera 0.3 m over the water, waves
    /// and all, so in a hold below the waterline it stood over his head and rode every wave, with the sea drawn in the
    /// hold; the water in the hull is kept off the screen by the hull's water mask (<see cref="KwShipWaterMask"/>), never
    /// by hiding the sea, which would hide it outside the ship too. Where the sea has taken over that part of the hold
    /// (flooded to it, or the ship sinking: <see cref="KwShipSeaInside"/>) the camera is held over the sea as the game's
    /// is. Out from under the deck (or
    /// seated, at the helm, dead, or with the build camera out) the game's own view is back. Runs after the game places
    /// its camera each frame. Local only.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
    public static class KwShipEyes
    {
        private const float Ceiling = 2.6f, Near = 0.05f, Rescan = 0.5f, Margin = 0.35f;

        // A cabin's roof nearer his eye than this leaves the game's camera no room: inside a low cabin (2 m) the view is his eyes, in a tall one the game's.
        private const float LowRoof = 1.2f;
        private static readonly Dictionary<Renderer, ShadowCastingMode> hidden = new Dictionary<Renderer, ShadowCastingMode>();
        private static float near = -1f, scanned;
        private static int mask;

        [HarmonyPostfix]
        private static void Postfix(GameCamera __instance) => KwShipBody.Step("Broadside ship eyes", () => Look(__instance));

        private static void Look(GameCamera game)
        {
            Player? player = Player.m_localPlayer;
            Camera? camera = game.GetComponent<Camera>();
            if (player == null || camera == null || !Inside(player))
            {
                Leave(camera);
                if (player != null)
                    KwShipSteady.Hold(game, player);
                return;
            }
            KwShipSteady.Reset();
            Eyes(game, camera, player);
        }

        // The camera at his eye (held over the sea where the sea fills that part of the hold, as the game holds it over the
        // water anywhere, so it never looks up from under a surface it cannot see), the near plane in, his body as its shadow.
        private static void Eyes(GameCamera game, Camera camera, Player player)
        {
            Vector3 eye = player.m_eye.position;
            Ship? ship = player.GetStandingOnShip();
            if (ship != null && KwShipSeaInside.SeaAt(ship, eye))
                eye.y = Mathf.Max(eye.y, Floating.GetLiquidLevel(eye) + game.m_minWaterDistance);
            game.transform.position = eye;
            if (near < 0f)
                near = camera.nearClipPlane;
            camera.nearClipPlane = Near;
            if (hidden.Count == 0 || Time.time - scanned > Rescan)
                Hide(player);
        }

        // Standing (not seated, not dead, no build camera out) under a Broadside deck or below the sea on our ship.
        private static bool Inside(Player player)
        {
            if (player.IsDead() || player.IsAttached() || player.InCutscene() || KwBpCamera.Out || player.m_eye == null)
                return false;
            return UnderDeck(player) || BelowSea(player);
        }

        // His eye under the sea's surface (and the game's camera margin over it) on a launched Broadside ship.
        private static bool BelowSea(Player player)
        {
            Ship? ship = player.GetStandingOnShip();
            if (ship == null || ship.GetComponent<KwShipBody>() == null)
                return false;
            Vector3 eye = player.m_eye.position;
            return eye.y < Floating.GetLiquidLevel(eye) + Margin;
        }

        // A Broadside deck over his head, or a cabin's roof too low for the game's camera inside (within LowRoof of his eye).
        private static bool UnderDeck(Player player)
        {
            mask = mask != 0 ? mask : LayerMask.GetMask("piece", "vehicle");
            if (!Physics.Raycast(player.m_eye.position, Vector3.up, out RaycastHit hit, Ceiling, mask, QueryTriggerInteraction.Ignore))
                return false;
            bool low = hit.distance < LowRoof;
            if (hit.collider.GetComponentInParent<KwShipBody>() != null)
                return hit.collider.name == "deck" || (low && hit.collider.name == KwShipHouses.RoofBox);
            Piece? piece = hit.collider.GetComponentInParent<Piece>();
            string name = piece != null ? Utils.GetPrefabName(piece.gameObject) : "";
            return name == KwToolPieces.PartPrefix + "deck" || (low && KwHouses.IsPart(name));
        }

        // The body as its shadow only (looked for again now and then: worn gear makes new renderers).
        private static void Hide(Player player)
        {
            scanned = Time.time;
            Transform body = player.m_visual != null ? player.m_visual.transform : player.transform;
            foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>())
            {
                if (hidden.ContainsKey(renderer) || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                    continue;
                hidden[renderer] = renderer.shadowCastingMode;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        // The game's own view again: the body drawn, the near plane as it was.
        private static void Leave(Camera? camera)
        {
            foreach (KeyValuePair<Renderer, ShadowCastingMode> was in hidden)
            {
                if (was.Key != null)
                    was.Key.shadowCastingMode = was.Value;
            }
            hidden.Clear();
            if (camera != null && near >= 0f)
                camera.nearClipPlane = near;
            near = -1f;
        }
    }
}
