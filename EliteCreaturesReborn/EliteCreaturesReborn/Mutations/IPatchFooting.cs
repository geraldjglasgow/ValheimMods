using System.Collections.Generic;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// What one trail kind's patches do to this machine's own player, looked at by its <see cref="PatchLayer"/> ten times
    /// a second: slowed in ice or mud (<see cref="PatchFooting"/>), set on fire (<see cref="BurnFooting"/>) or held fast
    /// (<see cref="RootFooting"/>).
    /// </summary>
    internal interface IPatchFooting
    {
        /// <summary>One look at where <paramref name="player"/> stands among <paramref name="patches"/>.</summary>
        void Tick(Player? player, List<GroundPatch> patches);

        /// <summary>No player to hold, or this machine's patches are gone: let go of whatever the patches still do.</summary>
        void Drop();
    }
}
