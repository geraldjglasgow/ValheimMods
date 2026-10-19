using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Raids;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Helgrind, Hel's gate (features/arena.md section 2): the piece <c>ECR_Helgrind</c>, a copy of the game's stone portal
    /// (<c>portal_stone</c>: its placement, health, stone wear, hit and destroy effects, sounds, player-base area, guide point
    /// and its teleport trigger's box, resized to the gate's opening) wearing the workshop's gate (<see cref="GateModel"/>). The game's pairing goes:
    /// its <c>TeleportWorld</c> (tag, links, hover, model colour) is removed and its trigger's caller swapped for
    /// <see cref="GateTrigger"/>; the copy's name is in none of the game's portal lists, so no tag or link ever touches it.
    /// The swirl's particles and light go too (<see cref="GateLook"/> draws ours on clients); its looping sound stays under
    /// its <c>EffectFade</c>. Built once per process on every peer, dedicated server included, under BundlePrefabs' inactive
    /// bench, and put in the scene's prefab list in a <c>ZNetScene.Awake</c> prefix whether Arena is on or not, so gates
    /// already built always load. The prefab's name is hashed into saves: it never changes.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class GatePrefab
    {
        public const string PrefabName = "ECR_Helgrind";

        private const string GamePortal = "portal_stone";
        private const string Trigger = "TELEPORT";
        private const string Fade = "_target_found_red";
        private const string Sound = "SFX";

        /// <summary>The gate prefab; null until the scene first woke (or when the game's stone portal is missing).</summary>
        public static GameObject? Prefab { get; private set; }

        [HarmonyPrefix]
        private static void Prefix(ZNetScene __instance)
        {
            try
            {
                AddTo(__instance.m_prefabs);
            }
            catch (Exception e)
            {
                // Never out of ZNetScene.Awake: a throw there leaves the world loading for ever.
                Log.Error($"Helgrind could not be made: {e}");
            }
        }

        private static void AddTo(List<GameObject> prefabs)
        {
            if (Prefab == null)
            {
                Prefab = Build(prefabs, ChestPrefab.Find(prefabs, GamePortal));
            }
            if (Prefab != null && !prefabs.Contains(Prefab))
            {
                prefabs.Add(Prefab);
            }
        }

        private static GameObject? Build(List<GameObject> prefabs, GameObject? portal)
        {
            if (portal == null || portal.GetComponent<Piece>() == null || portal.GetComponent<TeleportWorld>() == null)
            {
                Log.Warn($"the game's {GamePortal} was not found: there is no Helgrind.");
                return null;
            }
            GameObject copy = PrefabBench.Copy(portal, PrefabName);
            Unpair(copy);
            GateModel.Wear(copy, prefabs);
            FitTrigger(copy.transform.Find(Trigger), GateModel.PointIn(copy, GateModel.Center, GateModel.CenterAsBuilt));
            Describe(copy.GetComponent<Piece>());
            copy.AddComponent<GateLook>();
            copy.AddComponent<GateHover>();
            return copy;
        }

        private static void Unpair(GameObject copy)
        {
            Object.DestroyImmediate(copy.GetComponent<TeleportWorld>());
            Transform? trigger = copy.transform.Find(Trigger);
            if (trigger == null)
            {
                Log.Warn($"the game's {GamePortal} has no {Trigger}: Helgrind cannot be walked through.");
            }
            else
            {
                foreach (TeleportWorldTrigger old in trigger.GetComponents<TeleportWorldTrigger>())
                {
                    Object.DestroyImmediate(old);
                }
                trigger.gameObject.AddComponent<GateTrigger>();
            }
            KeepSound(copy.transform.Find(Fade));
        }

        // The stone portal's box filled its 3.8 m opening (1.74 times its size, 2.32 m up); the gate's arched opening is
        // 2.44 x 3.22 m round portal_center, so the box is moved there and given the opening's size (GateModel.Opening).
        private static void FitTrigger(Transform? trigger, Vector3 center)
        {
            BoxCollider? box = trigger != null ? trigger.GetComponent<BoxCollider>() : null;
            if (trigger == null || box == null)
            {
                return;
            }
            trigger.localPosition = center;
            trigger.localRotation = Quaternion.identity;
            trigger.localScale = Vector3.one;
            box.center = Vector3.zero;
            box.size = GateModel.Opening;
            box.isTrigger = true;
        }

        // The game's swirl without its particles and light: only its looping sound, faded in and out by its EffectFade.
        private static void KeepSound(Transform? fade)
        {
            if (fade == null)
            {
                return;
            }
            for (int i = fade.childCount - 1; i >= 0; i--)
            {
                Transform child = fade.GetChild(i);
                if (child.name != Sound)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        // The hammer's Misc tab; the cost and the Black Forge are set with the hammer (GateHammer), once items exist.
        private static void Describe(Piece piece)
        {
            piece.m_name = "$" + GateWords.Piece;
            piece.m_description = "$" + GateWords.Description;
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_enabled = true;
            if (GateModel.Icon != null)
            {
                piece.m_icon = GateModel.Icon;
            }
        }
    }
}
