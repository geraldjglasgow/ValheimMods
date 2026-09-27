using BepInEx.Configuration;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// One key setting of another module, found by its section and key name when a description is built, so the Menu
    /// module needs no compile-time dependency on the module that binds it. <see cref="Default"/> is shown when the
    /// setting does not exist (yet).
    /// </summary>
    public sealed class KeyRef
    {
        public KeyRef(string section, string key, KeyCode main, params KeyCode[] modifiers)
        {
            Section = section;
            Key = key;
            Default = new KeyboardShortcut(main, modifiers);
        }

        public string Section { get; }

        public string Key { get; }

        public KeyboardShortcut Default { get; }
    }

    /// <summary>
    /// The key settings the menu descriptions name, by the section and key name each module is expected to bind them
    /// under, with the defaults those modules bind. A name that matches no setting only shows the default key.
    /// </summary>
    public static class KeyNames
    {
        // Brush module, section "10. Controls".
        public static readonly KeyRef AdjustModifier = new KeyRef(Sections.Controls, "Adjust Modifier", KeyCode.LeftAlt);
        public static readonly KeyRef IncreaseValue = new KeyRef(Sections.Controls, "Increase Key", KeyCode.RightBracket);
        public static readonly KeyRef DecreaseValue = new KeyRef(Sections.Controls, "Decrease Key", KeyCode.LeftBracket);
        public static readonly KeyRef NextValue = new KeyRef(Sections.Controls, "Select Value Key", KeyCode.B);
        public static readonly KeyRef RotateLeft = new KeyRef(Sections.Controls, "Rotate Left Key", KeyCode.LeftArrow);
        public static readonly KeyRef RotateRight = new KeyRef(Sections.Controls, "Rotate Right Key", KeyCode.RightArrow);
        public static readonly KeyRef CycleShape = new KeyRef(Sections.Controls, "Shape Key", KeyCode.N);
        public static readonly KeyRef CycleStyle = new KeyRef(Sections.Controls, "Level Style Key", KeyCode.L);
        public static readonly KeyRef LockTarget = new KeyRef(Sections.Controls, "Lock Height Key", KeyCode.K);
        public static readonly KeyRef CycleTarget = new KeyRef(Sections.Controls, "Target Mode Key", KeyCode.Y);
        public static readonly KeyRef CyclePaint = new KeyRef(Sections.Controls, "Paint Key", KeyCode.P);
        public static readonly KeyRef GridMode = new KeyRef(Sections.Controls, "Grid Mode Key", KeyCode.I);
        public static readonly KeyRef AimAtEdge = new KeyRef(Sections.Controls, "Aim At Edge Key", KeyCode.O);
        public static readonly KeyRef HardLevel = new KeyRef(Sections.Controls, "Hard Level Key", KeyCode.F9);

        // History module, section "7. Undo".
        public static readonly KeyRef Undo = new KeyRef(Sections.History, "Undo Key", KeyCode.Z, KeyCode.LeftControl);
        public static readonly KeyRef Redo = new KeyRef(Sections.History, "Redo Key", KeyCode.Y, KeyCode.LeftControl);

        // Clearing module, section "5. Reset and Clearing".
        public static readonly KeyRef ResetArea = new KeyRef(Sections.Reset, "Reset Key", KeyCode.U);
        public static readonly KeyRef ResetAround = new KeyRef(Sections.Reset, "Reset Around Key", KeyCode.U, KeyCode.LeftShift);

        // Paths module, section "4. Ramps and Roads".
        // The ramp profile cycles on the Brush module's shape key while the ramp entry is selected.
        public static readonly KeyRef RampProfile = new KeyRef(Sections.Controls, "Shape Key", KeyCode.N);
        public static readonly KeyRef RemovePoint = new KeyRef(Sections.Paths, "Remove Last Point Key", KeyCode.Backspace);
        public static readonly KeyRef QuickRamp = new KeyRef(Sections.Paths, "Quick Ramp Key", KeyCode.J);
        public static readonly KeyRef CarveRoad = new KeyRef(Sections.Paths, "Carve Road Key", KeyCode.H);
        public static readonly KeyRef CarvePaved = new KeyRef(Sections.Paths, "Carve Paved Road Key", KeyCode.H, KeyCode.LeftShift);
    }
}
