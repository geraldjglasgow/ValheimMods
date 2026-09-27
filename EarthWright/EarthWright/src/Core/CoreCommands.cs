namespace EarthWright.Core
{
    /// <summary>The general subcommands: <c>ew on</c>, <c>ew off</c> (your own switch) and <c>ew reload</c>.</summary>
    public static class CoreCommands
    {
        public static void Register()
        {
            Command.Add("on", "on                 turns EarthWright on for you", args => SetLocal(args, true));
            Command.Add("off", "off                turns EarthWright off for you (vanilla hoe and cultivator)", args => SetLocal(args, false));
            Command.Add("reload", "reload             reloads the cfg and every EarthWright YAML file", Reload, adminOnly: true);
        }

        private static void SetLocal(Terminal.ConsoleEventArgs args, bool on)
        {
            GeneralSettings.LocalEnabled.Value = on;
            args.Context.AddString("EarthWright is now " + (on ? "on" : "off") + " for you.");
        }

        private static void Reload(Terminal.ConsoleEventArgs args)
        {
            Plugin.Synced.Config.Reload();
            Plugin.Synced.Yaml.LoadAll();
            Plugin.Synced.Yaml.ApplyAll();
            args.Context.AddString("EarthWright: configuration and YAML files reloaded.");
        }
    }
}
