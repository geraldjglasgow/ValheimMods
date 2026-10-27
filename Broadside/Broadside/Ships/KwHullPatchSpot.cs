using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hole in a launched ship's hull the local player aims at, as something the game can hover and use: a child of the
    /// ship with no collider, moved onto the hole aimed at (<see cref="KwHullPatchHover"/>). Its hover says "Hole in the
    /// hull" and, with a Wooden Repair Kit in the inventory, "[E] Patch the hole (Wooden Repair Kit)"; the use key, or the
    /// kit used from the hotbar while aiming at the hole (the game's item-on-thing use), asks the ship's owner to patch it
    /// (<see cref="KwHullPatches.Ask"/>), and one kit is taken when the owner has (<see cref="Patched"/>). A hole is aimed at
    /// within the game's use reach of the eyes, through the hole itself or within <see cref="Margin"/> of its edge, from
    /// inside the hold or from outside, unless something stands more than <see cref="Clear"/> in front of it (the hull's own
    /// boxes round a hole stand within that).
    /// </summary>
    public sealed class KwHullPatchSpot : MonoBehaviour, Hoverable, Interactable
    {
        public const float Margin = 0.25f, Clear = 1f, Near = 30f;
        private const string Name = "Hole in the hull";
        private static readonly RaycastHit[] hits = new RaycastHit[16];
        private KwHullPatches patches = null!;
        private int hole;

        public static KwHullPatchSpot Add(KwHullPatches patches)
        {
            GameObject spot = new GameObject("patch_spot") { layer = patches.gameObject.layer };
            spot.transform.SetParent(patches.transform, false);
            KwHullPatchSpot made = spot.AddComponent<KwHullPatchSpot>();
            made.patches = patches;
            return made;
        }

        /// <summary>The spot of the hole the local player aims at, moved onto it; null when no hole is aimed at.</summary>
        public static KwHullPatchSpot? Aimed(Player me)
        {
            Transform eye = GameCamera.instance.transform;
            foreach (KwHullPatches ship in KwHullPatches.Loaded)
            {
                if (ship.Spot == null || ship.Holes.Holes.Count == 0 || (ship.transform.position - me.transform.position).sqrMagnitude > Near * Near)
                    continue;
                if (ship.Spot.Find(me, eye))
                    return ship.Spot;
            }
            return null;
        }

        // The hole of this ship the camera's ray meets, within the player's use reach and with nothing in front of it.
        private bool Find(Player me, Transform eye)
        {
            Transform ship = patches.transform;
            Vector3 from = ship.InverseTransformPoint(eye.position), way = ship.InverseTransformDirection(eye.forward).normalized;
            List<KwHullHole> holes = patches.Holes.Holes;
            int i = KwHoleAim.Find(holes, from, way, Margin, 50f, out float along);
            if (i < 0)
                return false;
            Vector3 at = ship.TransformPoint(from + way * along);
            if (Vector3.Distance(me.m_eye.position, at) > me.m_maxInteractDistance || Blocked(me, eye, along))
                return false;
            hole = holes[i].Id;
            transform.position = at;
            return true;
        }

        // Something other than the player more than Clear in front of the hole along the ray.
        private static bool Blocked(Player me, Transform eye, float along)
        {
            int count = Physics.RaycastNonAlloc(eye.position, eye.forward, hits, Mathf.Max(0f, along - Clear), me.m_interactMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Rigidbody? body = hits[i].collider != null ? hits[i].collider.attachedRigidbody : null;
                if (hits[i].collider != null && (body == null || body.gameObject != me.gameObject))
                    return true;
            }
            return false;
        }

        public string GetHoverText()
        {
            if (patches.Covers(hole))
                return "Patched hole\nThe hammer's repair at a workbench mends it for good";
            Player? me = Player.m_localPlayer;
            if (me == null || KwRepairKit.Count(me) == 0)
                return $"{Name}\nA {KwRepairKit.Label} patches it";
            return Localization.instance.Localize($"{Name}\n[<color=yellow><b>$KEY_Use</b></color>] Patch the hole ({KwRepairKit.Label})");
        }

        public string GetHoverName() => Name;

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt) => !hold && Patch(user);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => item.m_shared.m_name == KwRepairKit.Label && Patch(user);

        // The hole asked patched when the player has a kit; else told what it takes.
        private bool Patch(Humanoid user)
        {
            if (user != Player.m_localPlayer || patches.Covers(hole))
                return false;
            Player me = (Player)user;
            if (KwRepairKit.Count(me) == 0)
            {
                me.Message(MessageHud.MessageType.Center, $"A {KwRepairKit.Label} patches the hole (made at a workbench)");
                return false;
            }
            return patches.Ask(hole);
        }

        /// <summary>On the player's game that asked, once the ship's owner has patched the hole: one kit taken.</summary>
        public static void Patched(int id)
        {
            Player? me = Player.m_localPlayer;
            if (me != null && KwRepairKit.Take(me))
                me.Message(MessageHud.MessageType.TopLeft, "Hole patched: the sea comes in no more there, until the hammer mends it");
        }
    }

    /// <summary>
    /// The game's hover (<c>Player.FindHoverObject</c>) given the hole the local player aims at (<see cref="KwHullPatchSpot"/>)
    /// when it found nothing to hover there: nothing at all (a ray through a hole meets no collider of the ship), a launched
    /// ship as a whole, or anything else that cannot be hovered. A hatch, a door, a piece built aboard or anything else of
    /// its own keeps the hover; this runs after the other hover patches.
    /// </summary>
    [HarmonyPatch(typeof(Player), "FindHoverObject")]
    public static class KwHullPatchHover
    {
        [HarmonyPostfix, HarmonyPriority(Priority.Low)]
        private static void Postfix(Player __instance, ref GameObject hover)
        {
            if (__instance != Player.m_localPlayer || KwHullPatches.Loaded.Count == 0 || GameCamera.instance == null)
                return;
            if (hover != null && hover.GetComponentInParent<Hoverable>() != null)
                return;
            try
            {
                KwHullPatchSpot? spot = KwHullPatchSpot.Aimed(__instance);
                if (spot != null)
                    hover = spot.gameObject;
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside hull patch hover");
            }
        }
    }
}
