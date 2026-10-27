using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One line other Broadside code shows on the ship panel (<see cref="KwShipPanel.Row"/>): a label on the left, a value
    /// on the right, the game's tooltip box on hover, and an optional bar. It shows while set or kept within the last
    /// <see cref="Linger"/> seconds of game time and the local player is aboard a launched ship; it only holds the words,
    /// the panel draws them.
    /// </summary>
    public sealed class KwPanelLine
    {
        /// <summary>Seconds a line stays after its last <see cref="Set"/> or <see cref="Keep"/>.</summary>
        public const float Linger = 2f;

        private float until = float.MinValue;

        internal KwPanelLine(string id, int index) => (Id, Index) = (id, index);

        public string Id { get; }

        /// <summary>Its place among the lines (the order first asked for).</summary>
        internal int Index { get; }

        internal string Label { get; private set; } = "";
        internal string Value { get; private set; } = "";
        internal string Topic { get; private set; } = "";
        internal string Tip { get; private set; } = "";

        /// <summary>The bar's progress, 0 to 1; shown only in between (below 0, the default: no bar).</summary>
        internal float Bar { get; private set; } = -1f;

        /// <summary>Whether it shows now.</summary>
        public bool Shown => Time.time < until;

        /// <summary>
        /// Its words, shown for <see cref="Linger"/> seconds more. The tooltip (topic over text, the game's box) shows on
        /// hover when either is given; the game localizes both, so "$" tokens translate.
        /// </summary>
        public void Set(string label, string value, string? topic = null, string? tip = null)
        {
            Label = label ?? "";
            Value = value ?? "";
            Topic = topic ?? "";
            Tip = tip ?? "";
            Keep();
        }

        /// <summary>Shown for <see cref="Linger"/> seconds more with the words it has.</summary>
        public void Keep() => until = Time.time + Linger;

        /// <summary>A thin bar along the line's bottom in the game's orange, filled to <paramref name="progress"/> (0 to 1);
        /// it shows only strictly between 0 and 1, so 1 (done) or a negative value hides it.</summary>
        public void SetBar(float progress) => Bar = progress;

        /// <summary>Hidden now, until it is set or kept again.</summary>
        public void Hide() => until = float.MinValue;
    }
}
