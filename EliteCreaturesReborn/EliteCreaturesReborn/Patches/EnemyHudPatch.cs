using System.Collections;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Replaces the nameplate's stars with the mod's coloured row. Each frame, for every elite creature's nameplate it
    /// hides the vanilla two- and three-star badges (which only ever cover those two counts) and ensures the coloured
    /// <see cref="StarRow"/> is present, so the star display is one consistent, individually-drawn row at any count.
    /// </summary>
    [HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
    public static class EnemyHudPatch
    {
        private static void Postfix(EnemyHud __instance) =>
            Guard.Run("EnemyHud.UpdateHuds stars", () => Decorate(__instance));

        private static void Decorate(EnemyHud hud)
        {
            IDictionary? huds = Traverse.Create(hud).Field("m_huds").GetValue() as IDictionary;
            if (huds == null)
            {
                return;
            }
            foreach (object data in huds.Values)
            {
                DecorateOne(Traverse.Create(data));
            }
        }

        private static void DecorateOne(Traverse data)
        {
            Character character = data.Field("m_character").GetValue<Character>();
            GameObject gui = data.Field("m_gui").GetValue<GameObject>();
            if (character == null || gui == null || !IsElite(character))
            {
                return;
            }
            if (Config.Configuration.ColouredStars.Value)
            {
                HideVanillaBadges(gui);
                EnsureRow(gui, character);
            }
            EnsurePouchIcons(gui, character);
        }

        private static bool IsElite(Character character)
        {
            EliteController controller = character.GetComponent<EliteController>();
            return controller != null && controller.Ready;
        }

        private static void HideVanillaBadges(GameObject gui)
        {
            SetInactive(gui.transform.Find("level_2"));
            SetInactive(gui.transform.Find("level_3"));
        }

        private static void SetInactive(Transform badge)
        {
            if (badge != null && badge.gameObject.activeSelf)
            {
                badge.gameObject.SetActive(false);
            }
        }

        private static void EnsureRow(GameObject gui, Character character)
        {
            if (gui.GetComponent<StarRow>() != null)
            {
                return;
            }
            Sprite? sprite = FindStarSprite(gui);
            RectTransform? bar = gui.transform.Find("Health") as RectTransform;
            if (sprite == null || bar == null)
            {
                return;
            }
            gui.AddComponent<StarRow>().Init(character, sprite, bar);
        }

        private static void EnsurePouchIcons(GameObject gui, Character character)
        {
            if (!Config.Configuration.ShowStolenItems.Value || gui.GetComponent<PouchIcons>() != null)
            {
                return;
            }
            EliteController controller = character.GetComponent<EliteController>();
            if (controller == null || !controller.Ready || !controller.Traits.Has(Mutation.Thieving))
            {
                return;
            }
            if (gui.transform.Find("Health") is RectTransform bar)
            {
                gui.AddComponent<PouchIcons>().Init(character, bar);
            }
        }

        private static Sprite? FindStarSprite(GameObject gui)
        {
            Sprite? sprite = SpriteUnder(gui.transform.Find("level_3"));
            return sprite != null ? sprite : SpriteUnder(gui.transform.Find("level_2"));
        }

        private static Sprite? SpriteUnder(Transform badge)
        {
            if (badge == null)
            {
                return null;
            }
            Image image = badge.GetComponentInChildren<Image>(true);
            return image != null ? image.sprite : null;
        }
    }
}
