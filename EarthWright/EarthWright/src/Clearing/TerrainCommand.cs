using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Clearing
{
    /// <summary>
    /// <c>ew terrain &lt;op&gt; [radius] [value] [options]</c> (admin): the world-edit terrain operations, around the
    /// player or at the brush. The edit is privileged, so the server checks the admin list before any owner applies it;
    /// custom menu entries (Menu module) can run it like any console command.
    /// </summary>
    public static class TerrainCommand
    {
        private static readonly string[] Help =
        {
            "ew terrain <op> [radius] [value] [options]   (admin; radius default 10 m, up to 128 m)",
            "  level [r] [height]    flat at the height (default: your feet)      raise|lower [r] [metres]   by the amount (default 1)",
            "  min [r] [height]      lift what is lower up to the height            max [r] [height]   cut what is higher down to it",
            "  band [r] <min:max>    lift or cut everything into the band            offset [r] [metres]  generated height + metres",
            "  slope [width]         from your feet to the aimed ground (width 4)    remove [r]   dig down to the dig limit",
            "  reset [r]             generated height and ground                      paint [r] paint=<kind>   ground texture only",
            "options: share=<0..1> (random share of points)  skip=pieces (not under buildings)  band=<min>:<max> (only heights in the band)",
            "  ignore=<prefab,...> / include=<prefab,...> (leave out / only near those objects, * as wildcard)  unlimited (no height limits)",
            "  paint=<dirt|paved|cultivated|grass|original|clearvegetation>  hardness=<0..1>  shape=<circle|square>  width=<m>",
            "  at=brush (the brush's centre, shape, size and target height instead of a circle around you; for custom menu entries)",
        };

        public static void Register()
        {
            Command.Add("terrain", "terrain <op> [radius] [value] [options]   admin terrain operations, 'ew terrain help' for the list", Run, adminOnly: true);
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            if (args.Length < 3 || args[2].Equals("help", System.StringComparison.OrdinalIgnoreCase))
            {
                foreach (string line in Help)
                    args.Context.AddString(line);
                return;
            }
            Player player = Player.m_localPlayer;
            TerrainRequest request = TerrainRequestParser.Parse(args.Args, 2);
            string error = player == null ? "join a world first" : request.Error;
            TerrainEdit edit = error == null ? TerrainEdits.Build(request, player, out error) : null;
            if (edit == null)
            {
                args.Context.AddString("EarthWright: " + error + ".");
                return;
            }
            bool sent = Dispatcher.Submit(edit);
            args.Context.AddString(sent ? $"EarthWright: terrain {request.Op} sent." : "EarthWright: the edit was refused (the reason is shown on screen).");
        }
    }
}
