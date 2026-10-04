using System.Collections;
using System.Collections.Generic;
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
    /// <see cref="StarRow"/> is present, so the star display is one consistent, individually-drawn row at any count - on
    /// a boss's health bar too, which has no star badges of its own and borrows the creature bar's star sprite. A
    /// Phantom copy's boss bar gets no star row: it is gathered instead and laid out small in one row with the boss's own
    /// bar by <see cref="PhantomBars"/>; a Tethered pair's two bars are stacked by <see cref="TetherBars"/>. It also adds the icon rows on the star row's line: what a Thieving creature carries
    /// (<see cref="PouchIcons"/>) and what a Devouring creature has eaten (<see cref="MealIcons"/>).
    /// </summary>
    [HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
    public static class EnemyHudPatch
    {
        /// <summary>This frame's Phantom copy bars, reused every frame.</summary>
        private static readonly List<PhantomBars.Bar> Copies = new List<PhantomBars.Bar>();

        /// <summary>This frame's Phantom boss bars, which join their copies' row while any copy stands.</summary>
        private static readonly List<PhantomBars.Bar> Bosses = new List<PhantomBars.Bar>();

        /// <summary>This frame's Tethered pair bars, reused every frame.</summary>
        private static readonly List<PhantomBars.Bar> Pairs = new List<PhantomBars.Bar>();

        private static void Postfix(EnemyHud __instance) =>
            Guard.Run("EnemyHud.UpdateHuds stars", () => Decorate(__instance));

        private static void Decorate(EnemyHud hud)
        {
            IDictionary? huds = Traverse.Create(hud).Field("m_huds").GetValue() as IDictionary;
            if (huds == null)
            {
                return;
            }
            Copies.Clear();
            Bosses.Clear();
            Pairs.Clear();
            foreach (object data in huds.Values)
            {
                DecorateOne(Traverse.Create(data));
            }
            PhantomBars.Layout(Copies, Bosses);
            TetherBars.Layout(Pairs);
        }

        private static void DecorateOne(Traverse data)
        {
            Character character = data.Field("m_character").GetValue<Character>();
            GameObject gui = data.Field("m_gui").GetValue<GameObject>();
            if (character == null || gui == null)
            {
                return;
            }
            if (Gather(character, gui) || !IsElite(character))
            {
                return;
            }
            if (Config.Configuration.ColouredStars.Value)
            {
                HideVanillaBadges(gui);
                EnsureRow(gui, character);
            }
            EnsureIcons<PouchIcons>(gui, character, Config.Configuration.ShowStolenItems.Value, Mutation.Thieving);
            EnsureIcons<MealIcons>(gui, character, Config.Configuration.ShowDevouredCreatures.Value, Mutation.Devouring);
        }

        /// <summary>Collects the boss bars laid out after the loop: a Phantom copy's (true: it takes no star row), a
        /// Phantom boss's and a Tethered pair's (false: decorated like any boss bar, then laid out).</summary>
        private static bool Gather(Character character, GameObject gui)
        {
            if (PhantomBars.IsCopy(character, gui, out PhantomBars.Bar copy))
            {
                Copies.Add(copy);
                return true;
            }
            if (PhantomBars.IsPhantomBoss(character, gui, out PhantomBars.Bar boss))
            {
                Bosses.Add(boss);
            }
            else if (TetherBars.IsTethered(character, out ZDOID pairId))
            {
                Pairs.Add(new PhantomBars.Bar(pairId, gui));
            }
            return false;
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

        // A mutation's icon row on the nameplate - what a thief carries, what a devourer has eaten - added once, when the
        // player shows it and the creature carries the mutation.
        private static void EnsureIcons<T>(GameObject gui, Character character, bool shown, Mutation mutation)
            where T : MonoBehaviour, IPlateIcons
        {
            if (!shown || gui.GetComponent<T>() != null)
            {
                return;
            }
            EliteController controller = character.GetComponent<EliteController>();
            if (controller == null || !controller.Ready || !controller.Traits.Has(mutation))
            {
                return;
            }
            if (gui.transform.Find("Health") is RectTransform bar)
            {
                gui.AddComponent<T>().Init(character, bar);
            }
        }

        private static Sprite? FindStarSprite(GameObject gui)
        {
            Sprite? sprite = SpriteUnder(gui.transform.Find("level_3"));
            if (sprite == null)
            {
                sprite = SpriteUnder(gui.transform.Find("level_2"));
            }
            return sprite != null ? sprite : PlainHudStar();
        }

        /// <summary>
        /// The boss health bar carries no star badges of its own, so a starred boss borrows the ordinary creature bar's
        /// star - read from the HUD's template, never instantiated - and draws its row under the boss bar like any other.
        /// </summary>
        private static Sprite? PlainHudStar()
        {
            GameObject? template = EnemyHud.instance != null ? EnemyHud.instance.m_baseHud : null;
            if (template == null)
            {
                return null;
            }
            Sprite? sprite = SpriteUnder(template.transform.Find("level_3"));
            return sprite != null ? sprite : SpriteUnder(template.transform.Find("level_2"));
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
