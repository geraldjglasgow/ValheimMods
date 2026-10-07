using EliteCrafting.Rolling;
using EliteCrafting.Sockets;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>consecrate</c> (the Consecrated Rune, repurposed 2026-10-07, user: "make it give between 1 and 3 sockets, but 3
    /// is very rare"): a weapon, staff, armour piece or shield that has no sockets gains one to three, any rarity: one 70%,
    /// two 25%, three 5% (constants, as the rest of the socket tuning). An item with sockets already refuses it, as it
    /// refuses the Dvergr Chisel. Works only while <c>Gems and sockets</c> is on.
    /// </summary>
    internal sealed class ConsecrateVerb : IStoneVerb
    {
        private static readonly float[] CountWeights = { 70f, 25f, 5f };

        public StoneResult Run(StoneJob job)
        {
            if (!SocketSwitch.On)
            {
                return StoneResult.Refuse("gems_off");
            }
            if (GemCatalog.BaseOf(job.Class) == SocketBase.None)
            {
                return StoneResult.Refuse("no_socket_here");
            }
            if (job.State.Sockets > 0)
            {
                return StoneResult.Refuse("has_sockets");
            }
            int count = System.Math.Max(0, RollMath.PickWeighted(CountWeights, job.RollContext().Random)) + 1;
            string feedback = count == 1 ? "consecrated_one" : "consecrated_many";
            return StoneResult.Success(job.State.ToBuilder().SetSockets(count).Build(), feedback, job.ItemName, count.ToString());
        }
    }
}
