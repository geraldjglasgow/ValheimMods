using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using WindowInput;

namespace Broadside
{
    /// <summary>
    /// A small window for typing sizes, opened by the hammer tools: a title, a row per field (<see cref="KwField"/>) and OK /
    /// Cancel, in the game's own panel look a little right of the screen's centre, so the ship and a preview in the middle
    /// stay in view (<see cref="KwFormView"/>). One is open at a time; opening another reports the first one cancelled.
    /// The caller hears of every change (at open, and as soon as a typed number reads, not only when the typing ends), so
    /// it can redraw a preview, and of the end exactly once: the values on OK or Enter, null on Cancel, Esc, the player
    /// dying or teleporting, another game window opening, <see cref="Close"/>, or the game's screens going (a logout).
    /// While it is open the caller may build its window again with other fields (<see cref="Refill"/>, as a button row's
    /// press may ask: the same form, the same end) and put words on its note line (<see cref="Say"/>). A caller may hold
    /// OK back (its accept answering false: values that cannot be built), the window then staying open as it was.
    /// While it is open it is one of the game's windows (WindowInput: free cursor, no attacks, mouse look or zoom, Esc
    /// cancels) and takes the keys the way the game's own text box does (<see cref="KwFormTyping"/>): no walking, chat,
    /// hotbar, emotes or map. Tab moves between the numbers. Local to this player, nothing is sent. Outside a world it
    /// opens nothing and reports null at once.
    /// </summary>
    public static class KwForm
    {
        private sealed class Session
        {
            public Session(KwFormValues values, Action<KwFormValues>? change, Action<KwFormValues?>? done, bool noted, Func<KwFormValues, bool>? accept)
            {
                Values = values;
                Change = change;
                Done = done;
                Noted = noted;
                Accept = accept;
            }

            public KwFormValues Values { get; }
            public Action<KwFormValues>? Change { get; }
            public Action<KwFormValues?>? Done { get; }

            /// <summary>Whether OK may end the form with these values; null: always.</summary>
            public Func<KwFormValues, bool>? Accept { get; }

            /// <summary>The window has a note line (<see cref="Say"/>).</summary>
            public bool Noted { get; }
        }

        private static Session? session;
        private static int closedFrame = -1;
        private static bool registered;

        public static bool IsOpen => session != null;

        /// <summary>Open, or closed during this frame (the Enter or Esc that closed it is the form's, not the game's).</summary>
        internal static bool TakesKeys => session != null || Time.frameCount == closedFrame;

        /// <summary>
        /// Opens a form; noted, its window has a note line under its rows for the caller's words (<see cref="Say"/>); accept,
        /// when given, is asked on OK and keeps the window open by answering false (it says why on the note line).
        /// </summary>
        public static void Open(string title, IReadOnlyList<KwField> fields, Action<KwFormValues> onChange, Action<KwFormValues?> onDone, bool noted = false,
            Func<KwFormValues, bool>? accept = null)
        {
            Close();
            KwFormView.Fresh();
            IReadOnlyList<KwField> rows = fields ?? Array.Empty<KwField>();
            KwFormValues values = new KwFormValues(rows);
            bool shown = false;
            if (Player.m_localPlayer != null)
                Safe("Broadside form", () => shown = KwFormView.Show(title ?? "", rows, values, noted));
            if (!shown)
            {
                Call("Broadside form done", onDone, null);
                return;
            }
            Register();
            Session opened = new Session(values, onChange, onDone, noted, accept);
            session = opened;
            Changed();
            if (session == opened)
                KwFormView.FocusFirst();
        }

        public static void Close() => Finish(null);

        /// <summary>
        /// The open form's window built again with these fields, the same form going on (its callers, its end); the caller
        /// hears of the new values at once. Nothing while no form is open; closed (cancelled) if the window cannot be built.
        /// </summary>
        public static void Refill(string title, IReadOnlyList<KwField> fields)
        {
            Session? open = session;
            if (open == null)
                return;
            IReadOnlyList<KwField> rows = fields ?? Array.Empty<KwField>();
            KwFormValues values = new KwFormValues(rows);
            bool shown = false;
            Safe("Broadside form", () => shown = KwFormView.Show(title ?? "", rows, values, open.Noted));
            if (!shown)
            {
                Close();
                return;
            }
            session = new Session(values, open.Change, open.Done, open.Noted, open.Accept);
            Changed();
        }

        /// <summary>Words on the open window's note line, under its rows (empty for none); nothing for a window without one.</summary>
        public static void Say(string text)
        {
            if (session != null)
                Safe("Broadside form note", () => KwFormView.Say(text ?? ""));
        }

        /// <summary>A field's value changed: the caller hears of it at once.</summary>
        internal static void Changed()
        {
            Session? open = session;
            if (open != null)
                Call("Broadside form change", open.Change, open.Values.Copy());
        }

        /// <summary>OK or Enter: every number is kept as typed (within its range, on its step), then the caller gets them (unless it holds OK back).</summary>
        internal static void Ok()
        {
            Session? open = session;
            if (open == null)
                return;
            KwFormView.CommitAll();
            bool accepted = true;
            if (session == open && open.Accept != null)
                Safe("Broadside form ok", () => accepted = open.Accept(open.Values.Copy()));
            if (session == open && accepted)
                Finish(open.Values.Copy());
        }

        /// <summary>Every frame the form is shown: it closes when it cannot stay; Enter is OK, Tab and Shift + Tab move.</summary>
        internal static void Tick()
        {
            if (session == null)
                return;
            if (!Stays())
            {
                Close();
                return;
            }
            KwFormView.Tick();
            if (ZInput.GetKeyDown(KeyCode.Return, false) || ZInput.GetKeyDown(KeyCode.KeypadEnter, false))
                Ok();
            else if (ZInput.GetKeyDown(KeyCode.Tab, false))
                KwFormView.Tab(ZInput.GetKey(KeyCode.LeftShift, false) || ZInput.GetKey(KeyCode.RightShift, false) ? -1 : 1);
        }

        /// <summary>The game's screens went with the window on them (a logout): an open form ends cancelled.</summary>
        internal static void Lost()
        {
            KwFormView.Forget();
            Finish(null);
        }

        /// <summary>
        /// Runs a step of the form. The form runs from Unity's callbacks, so a throw goes no further: Guard logs it under the
        /// mod's name, and the step reports false.
        /// </summary>
        internal static bool Safe(string what, Action action)
        {
            try
            {
                Guard.Run(what, action);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void Finish(KwFormValues? values)
        {
            Session? ended = session;
            if (ended == null)
                return;
            session = null;
            closedFrame = Time.frameCount;
            Safe("Broadside form", KwFormView.Hide);
            Call("Broadside form done", ended.Done, values);
        }

        private static void Call<T>(string what, Action<T>? callback, T value)
        {
            if (callback != null)
                Safe(what, () => callback(value));
        }

        // The player is still there and alive, not teleporting, and no game window has opened over the form.
        private static bool Stays()
        {
            Player player = Player.m_localPlayer;
            return player != null && !player.IsDead() && !player.IsTeleporting() && !OtherWindow();
        }

        private static bool OtherWindow()
        {
            TextInput box = TextInput.instance;
            bool textBox = box != null && box.m_panel != null && box.m_panel.activeSelf;
            return textBox || InventoryGui.IsVisible() || Minimap.IsOpen() || StoreGui.IsVisible() || Menu.IsVisible()
                || global::Console.IsVisible() || Hud.IsPieceSelectionVisible();
        }

        // Once: the form joins WindowInput's windows (the library's patches go in once, whoever asks first).
        private static void Register()
        {
            if (registered)
                return;
            registered = true;
            GameWindow.Install(new Harmony(Broadside.PluginGuid + ".form"));
            GameWindow.Add(() => IsOpen, Close, () => !KwFormView.PointerOver());
        }
    }
}
