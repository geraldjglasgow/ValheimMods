using EliteCrafting.Affixes;
using EliteCrafting.Display;
using EliteCrafting.Rules;
using EliteCrafting.Sockets;
using EliteCrafting.Text;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>socket</c> (the Dvergr Chisel, sockets.md section 5): one socket into a weapon, staff, armour piece or shield
    /// that has none, any rarity. An item with sockets already (one or more, dropped or cut) refuses it.
    /// </summary>
    internal sealed class SocketVerb : IStoneVerb
    {
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
            return StoneResult.Success(job.State.ToBuilder().SetSockets(1).Build(), "socket_cut", job.ItemName);
        }
    }

    /// <summary>
    /// <c>gem</c> (the gems, sockets.md sections 3-4): the gem's stat for the item's base, its tier rolled now on the
    /// item's own ladder (<see cref="GemRolls"/>), into the next empty socket. With every socket full the player picks the
    /// socket whose gem it replaces (<see cref="StoneResult.ChooseSocket"/>, <see cref="GemChooser"/>); the old gem is lost.
    /// </summary>
    internal sealed class GemVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (!SocketSwitch.On)
            {
                return StoneResult.Refuse("gems_off");
            }
            if (job.State.Sockets == 0)
            {
                return StoneResult.Refuse("gem_no_socket");
            }
            AffixDef? stat = StatOf(job);
            if (stat == null)
            {
                return StoneResult.Refuse("gem_wrong_item", job.StoneName);
            }
            int socket = job.Socket >= 0 ? job.Socket : job.State.FreeSockets > 0 ? job.State.FilledSockets : -1;
            return socket < 0 ? StoneResult.ChooseSocket() : Place(job, stat, socket);
        }

        /// <summary>The live, enabled inscription this gem gives on the item's base; null when it does not fit.</summary>
        private static AffixDef? StatOf(StoneJob job)
        {
            string? id = GemCatalog.StatFor(job.Def!.Id, GemCatalog.BaseOf(job.Class));
            AffixDef? def = id != null ? job.Rules.Affixes.Get(id) : null;
            return def != null && def.Enabled ? def : null;
        }

        private static StoneResult Place(StoneJob job, AffixDef stat, int socket)
        {
            AffixRoll? roll = GemRolls.Roll(stat, job.RollContext());
            if (roll == null)
            {
                return StoneResult.Refuse("gem_wrong_item", job.StoneName);
            }
            ItemStateBuilder builder = job.State.ToBuilder();
            bool replaces = socket < job.State.FilledSockets;
            if (!builder.SetGem(socket, new GemRoll(job.Def!.Id, roll.Value)))
            {
                return StoneResult.Refuse("sockets_full");
            }
            string line = AffixLines.Sentence(stat.Id, roll.Value.Value, stat) + "  "
                + Words.Localize("$ecf_ui_tier", stat.ShownTier(roll.Value.Tier).ToString());
            return replaces
                ? StoneResult.Success(builder.Build(), "gem_replaced", job.StoneName, (socket + 1).ToString(), line)
                : StoneResult.Success(builder.Build(), "gem_socketed", job.StoneName, job.ItemName, line);
        }
    }
}
