using PatchGuard;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// The helm key list: while the local player steers a launched Broadside ship, the helm's keys are listed low at the
    /// bottom right of the screen, in the strip where the game shows its own key hints (which it hides at a helm), in their
    /// look: each line a copy of the game's own attack hint (<see cref="Template"/>: its label beside its key cap), in a row
    /// laid out as the game lays its hints (right-aligned, the same spacing). H pushes the ship off
    /// (<see cref="KwShipNudge"/>), B bails the flooded hold (<see cref="KwShipBail"/>). Hidden at once off the helm, and
    /// while the game's own hints or a window would show there (the inventory, the map, the radial or game menu, chat).
    /// Other mods add their own lines without a reference either way: the list is published as a GameObject named
    /// <see cref="Container"/> under the game's key hints, each child a copy of the game's hint ("Text" beside
    /// "key_bkg"/"Key"), shown in order after Broadside's; a mod copies the first line, renames it and sets its words, and
    /// shows or hides it (the ship cannons' lines, <see cref="KwHelmRows"/>: U fires a broadside, J the shot arcs). Nothing on a dedicated server.
    /// </summary>
    public static class KwHelmKeys
    {
        public const string Container = "Broadside_HelmKeys";

        /// <summary>The game's hint each line copies, under its key hints.</summary>
        public const string Template = "CombatHints/Keyboard/PrimaryAttack";

        private static readonly (string Name, string Key, string Words)[] Lines =
        {
            ("Broadside_Nudge", "H", "Push off"),
            ("Broadside_Bail", "B", "Bail (in the flooded hold)"),
        };

        private static GameObject? list;
        private static KeyHints? missing;
        private static Ship? steered;
        private static bool ours;

        /// <summary>Once, on a game that draws: the list's ticker.</summary>
        public static void Install()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            GameObject ticker = new GameObject("Broadside_HelmKeysTicker");
            Object.DontDestroyOnLoad(ticker);
            ticker.AddComponent<KwHelmKeysTicker>();
        }

        /// <summary>Each frame: the list shown while a Broadside ship is steered and nothing else shows there, else hidden.</summary>
        public static void Tick()
        {
            bool show = Steering() && Clear();
            GameObject? keys = show ? List() : list;
            if (keys != null && keys.activeSelf != show)
                keys.SetActive(show);
        }

        // Whether the local player steers a launched Broadside ship (the ship he steers looked at again only when it changes).
        private static bool Steering()
        {
            Player? me = Player.m_localPlayer;
            Ship? ship = me != null && !me.IsDead() ? me.GetControlledShip() : null;
            if (ship != steered)
                (steered, ours) = (ship, ship != null && ship.GetComponent<KwShipBody>() != null);
            return ship != null && ours;
        }

        // Nothing else shows in the strip or over it: the inventory, the map, the radial or game menu, chat, a pause.
        private static bool Clear() =>
            !InventoryGui.IsVisible() && !Minimap.IsOpen() && !Menu.IsVisible() && !Hud.InRadial() && !Game.IsPaused()
            && (Chat.instance == null || !Chat.instance.IsChatDialogWindowVisible());

        // The list, made under the game's key hints the first time it is shown (again after a new scene); null without them.
        private static GameObject? List()
        {
            KeyHints? hints = KeyHints.instance;
            if (list != null || hints == null || hints == missing)
                return list;
            Transform? template = hints.transform.Find(Template);
            if (template == null)
            {
                missing = hints;
                Log.Warn($"Helm key list off: the game's key hints have no {Template}.");
                return null;
            }
            list = Make(hints.transform, template);
            return list;
        }

        // The row over the hints' strip, laid out as the game's own row, and Broadside's lines in it.
        private static GameObject Make(Transform hints, Transform template)
        {
            GameObject row = new GameObject(Container, typeof(RectTransform));
            RectTransform rect = (RectTransform)row.transform;
            rect.SetParent(hints, false);
            (rect.anchorMin, rect.anchorMax, rect.pivot) = (Vector2.zero, Vector2.one, new Vector2(1f, 0f));
            (rect.offsetMin, rect.offsetMax) = (Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            HorizontalLayoutGroup? game = template.parent != null ? template.parent.GetComponent<HorizontalLayoutGroup>() : null;
            (layout.spacing, layout.childAlignment) = (game != null ? game.spacing : 40f, game != null ? game.childAlignment : TextAnchor.MiddleRight);
            (layout.childControlWidth, layout.childControlHeight, layout.childForceExpandWidth, layout.childForceExpandHeight) = (true, true, false, false);
            foreach ((string name, string key, string words) in Lines)
                Line(row.transform, template.gameObject, name, key, words);
            row.SetActive(false);
            return row;
        }

        // One line: a copy of the game's hint, its label and its key cap given the line's words.
        private static void Line(Transform row, GameObject template, string name, string key, string words)
        {
            GameObject line = Object.Instantiate(template, row, false);
            line.name = name;
            line.SetActive(true);
            Set(line.transform, "Text", words);
            Set(line.transform, "key_bkg/Key", key);
        }

        private static void Set(Transform line, string path, string text)
        {
            Transform? part = line.Find(path);
            TMP_Text? field = part != null ? part.GetComponent<TMP_Text>() : null;
            if (field != null)
                field.text = text;
        }
    }

    /// <summary>The helm key list's frame (<see cref="KwHelmKeys.Install"/>); a throw is reported and never stops the game's.</summary>
    public sealed class KwHelmKeysTicker : MonoBehaviour
    {
        private void Update()
        {
            try
            {
                KwHelmKeys.Tick();
            }
            catch (System.Exception e)
            {
                Guard.Report(e, "Broadside helm keys");
            }
        }
    }
}
