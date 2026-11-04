using System.Collections.Generic;
using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The tool's frame on the player's own game (one hidden object, made the first time the tool is taken in hand, so
    /// never on a dedicated server): the placing queue (<see cref="KwDecorPlacer"/>, which runs on with the tool put
    /// away); with the tool in hand the keys (Alt + wheel and [ ] the size before the click and the density after, N,
    /// Ctrl + Z; never while typing, with a menu or the build menu open; the plain wheel left to the camera), the reading
    /// and layout worked on for at most 4 ms a frame, the aim, the area's square, the previews when the layout or density
    /// changes (a few made a frame), and the lines (four times a second). With the tool put away everything is hidden and
    /// a locked area let go.
    /// </summary>
    public sealed class KwDecorDriver : MonoBehaviour
    {
        private const double Budget = 0.004;
        private const float Lines = 0.25f;
        private static KwDecorDriver? instance;
        private List<KwDecorPlan> shown = new List<KwDecorPlan>();
        private int version = -1;
        private float linesAt;
        private bool held;

        /// <summary>Where the tool is aimed this frame; null for nowhere within reach.</summary>
        public static Vector3? Aim { get; private set; }

        /// <summary>The piece aimed at, if any.</summary>
        public static Piece? AimPiece { get; private set; }

        public static void Ensure()
        {
            if (instance != null)
                return;
            GameObject holder = new GameObject("BS_DecorateDriver");
            DontDestroyOnLoad(holder);
            instance = holder.AddComponent<KwDecorDriver>();
        }

        private void Update() => Guard.Run("Broadside auto-decorate", Tick);

        private void Tick()
        {
            KwDecorPlacer.Step();
            Player? me = Player.m_localPlayer;
            if (!KwDecorTool.Held(me))
            {
                Away();
                return;
            }
            held = true;
            Keys(me!);
            KwDecorSession.Work(Time.realtimeSinceStartupAsDouble + Budget);
            if (KwDecorSession.Read is KwDecorRead read)
                Locked(me!, read);
            else
                Aiming(me!);
            KwDecorGhosts.Step();
        }

        private void Away()
        {
            KwToolWheel.Own(false);
            if (!held)
                return;
            held = false;
            KwDecorSession.Cancel();
            KwDecorGhosts.Hide();
            KwDecorOutline.Hide();
            KwDecorHud.Hide();
            (version, Aim, AimPiece) = (-1, null, null);
        }

        private static void Keys(Player me)
        {
            bool free = me.TakeInput() && !Typing.Active && !Hud.IsPieceSelectionVisible(), alt = KwToolWheel.Alt;
            KwToolWheel.Own(free && alt);
            int notches = free && alt ? KwToolWheel.Notches() : 0;
            if (!free)
                return;
            if (notches != 0)
                KwDecorSession.Turn(notches);
            if (Hotkey.Pressed(KeyCode.LeftBracket) || Hotkey.Pressed(KeyCode.RightBracket))
                KwDecorSession.Turn(Hotkey.Pressed(KeyCode.RightBracket) ? 1 : -1);
            if (Hotkey.Pressed(KeyCode.N))
                KwDecorSession.Reroll();
            if (Input.GetKeyDown(KeyCode.Z) && Ctrl() && !KwDecorPlacer.Busy)
                KwDecorUndo.Undo(me);
        }

        // Either Ctrl held, no Shift or Alt.
        private static bool Ctrl() =>
            (Hotkey.KeyHeld(KeyCode.LeftControl) || Hotkey.KeyHeld(KeyCode.RightControl))
            && !Hotkey.KeyHeld(KeyCode.LeftShift) && !Hotkey.KeyHeld(KeyCode.RightShift) && !Hotkey.KeyHeld(KeyCode.LeftAlt) && !Hotkey.KeyHeld(KeyCode.RightAlt);

        private void Aiming(Player me)
        {
            Look(me);
            if (version != -1)
                (version, shown) = (-1, new List<KwDecorPlan>());
            KwDecorGhosts.Hide();
            if (Aim is Vector3 at)
                KwDecorOutline.Show(at, KwDecorAim.Quick(AimPiece), KwDecorSession.Half, KwDecorOutline.Aiming);
            else
                KwDecorOutline.Hide();
            Say(me, false);
        }

        private void Locked(Player me, KwDecorRead read)
        {
            Look(me);
            bool ready = KwDecorSession.Ready, done = KwDecorSession.Layout?.Done ?? false;
            Color colour = ready ? KwDecorOutline.Showing : done ? KwDecorOutline.Refused : KwDecorOutline.Working;
            KwDecorOutline.Show(read.Grid.Centre, read.Grid.Yaw, read.Grid.Half, colour);
            bool changed = version != KwDecorSession.Version;
            if (changed)
            {
                version = KwDecorSession.Version;
                shown = KwDecorSession.Shown();
                KwDecorGhosts.Show(shown);
            }
            Say(me, changed);
        }

        private static void Look(Player me)
        {
            bool found = KwDecorAim.Find(me, KwDecorSession.Read?.Grid.Half ?? KwDecorSession.Half, out Vector3 at, out Piece? piece);
            Aim = found ? at : (Vector3?)null;
            AimPiece = found ? piece : null;
        }

        // The lines, four times a second or when what shows changed.
        private void Say(Player me, bool now)
        {
            if (!now && Time.time < linesAt)
                return;
            linesAt = Time.time + Lines;
            KwDecorRead? read = KwDecorSession.Read;
            KwDecorLayout? layout = KwDecorSession.Layout;
            if (read == null)
                KwDecorHud.Show(KwDecorWords.Aiming(KwDecorSession.Half));
            else if (KwDecorSession.Ready)
                KwDecorHud.Show(KwDecorWords.Ready(me, shown, layout!));
            else if (layout != null && layout.Done)
                KwDecorHud.Show(KwDecorWords.NothingFits + "\nEsc lets the area go");
            else
                KwDecorHud.Show(KwDecorWords.Working(read.Done ? -1f : read.Progress));
        }
    }
}
