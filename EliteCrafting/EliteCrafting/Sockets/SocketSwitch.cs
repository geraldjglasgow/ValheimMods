using EliteCrafting.Config;
using EliteCrafting.Rules;

namespace EliteCrafting.Sockets
{
    /// <summary>The <c>Gems and sockets</c> switch (synced): off, no sockets on drops and the chisel and gems neither drop nor work.</summary>
    internal static class SocketSwitch
    {
        public static bool On => ModSettings.GemsAndSockets?.Value ?? true;

        /// <summary>Whether this stone may drop: every other rune, and the chisel, the gems and the Consecrated Rune (it
        /// gives sockets) while the switch is on.</summary>
        public static bool Drops(StoneDef stone) =>
            On || (!StoneCatalog.IsSocketStone(stone.Id) && stone.Verb != StoneVerb.Consecrate);
    }
}
