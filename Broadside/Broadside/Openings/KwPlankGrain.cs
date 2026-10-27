using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The plank wood's grain laid along a board (the user's words, 2026-10-25: windows "in that same vein" as the railings,
    /// "so they don't look like the deck texture was reused"): every window, port and doorway frame, lining board, lid and
    /// shutter wears the railings' plank wood (<see cref="KwOpeningLooks.Wood"/>, the game's <c>woodwall</c> at its own
    /// texture scale), mapped board by board so the grain runs along each one at the railings' density
    /// (<see cref="PerMetre"/>, as <see cref="Solid.Planar"/> maps a rail), neither squashed nor stretched. Each board
    /// starts the wood at a place of its own (<see cref="Shift"/>), so neighbouring boards show different streaks and read
    /// as boards. Pure.
    /// </summary>
    public static class KwPlankGrain
    {
        /// <summary>Texture units a metre, as a railing's: the material's own scale makes the plank wood's grain of it.</summary>
        public const float PerMetre = 0.5f;

        /// <summary>The kinds of board a frame has, each starting the wood elsewhere.</summary>
        public const int Outside = 0, Inside = 1, Lining = 2, Leaf = 3;

        /// <summary>Where board n of a kind starts in the wood (along, across), spread over many of its repeats.</summary>
        public static Vector2 Shift(int board, int kind) =>
            new Vector2(Frac(board * 0.618f + kind * 0.37f) * 7f, Frac(board * 0.414f + kind * 0.73f) * 37f);

        /// <summary>A board's point on the wood: metres along it and across it, from where the board starts.</summary>
        public static Vector2 At(float along, float across, Vector2 shift) => new Vector2(along * PerMetre + shift.x, across * PerMetre + shift.y);

        private static float Frac(float x) => x - Mathf.Floor(x);
    }
}
