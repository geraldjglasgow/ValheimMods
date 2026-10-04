namespace EliteCreaturesReborn.Recap.Window
{
    /// <summary>
    /// Opens and closes the death recap window, building it on first use and again after the screen it lives beside is
    /// rebuilt (a new world). While open, the game treats it like its own windows (free cursor, no attacks, Esc closes;
    /// see the WindowInput library), and nothing is recorded.
    /// </summary>
    internal static class RecapWindow
    {
        private static WindowView? _view;

        public static bool IsOpen => _view != null && _view.gameObject.activeSelf;

        public static void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>False when there is no game screen to build it on (the main menu).</summary>
        public static bool Open()
        {
            if (_view == null)
            {
                _view = WindowBuilder.Build();
            }
            if (_view == null)
            {
                return false;
            }
            _view.Show();
            return true;
        }

        public static void Close()
        {
            if (IsOpen)
            {
                _view!.Hide();
            }
        }
    }
}
