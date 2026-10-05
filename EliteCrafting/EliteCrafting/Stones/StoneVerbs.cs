using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// One rune verb: its own precondition (pipeline step 11) and its dry run on a copy (step 12). Returns a refusal
    /// or the new state with its feedback; never writes.
    /// </summary>
    internal interface IStoneVerb
    {
        StoneResult Run(StoneJob job);
    }

    /// <summary>
    /// The fixed verb set of the seven runes, dispatched by the rune's <c>verb</c>: <c>promote</c> (Awakening, Ascension),
    /// <c>add</c> (Shaping, Consecrated), <c>reroll</c> (Recasting), <c>strip</c> (Cleansing), <c>corrupt</c> (Serpent).
    /// A new verb is one class and one line in <see cref="Implemented"/>. A stone whose verb this build cannot perform
    /// refuses as disabled (<c>stone_disabled</c>), with one log warning for the server owner, rather than failing (IMP-60).
    /// </summary>
    internal static class StoneVerbs
    {
        private static readonly Dictionary<StoneVerb, IStoneVerb> Implemented = new Dictionary<StoneVerb, IStoneVerb>
        {
            { StoneVerb.Promote, new PromoteVerb() },
            { StoneVerb.Add, new AddVerb() },
            { StoneVerb.Strip, new StripVerb() },
            { StoneVerb.Corrupt, new CorruptVerb() },
            { StoneVerb.Reroll, new RerollVerb() },
        };

        private static readonly HashSet<string> Warned = new HashSet<string>();

        /// <summary>Whether this build performs the verb; the economy loader disables stones whose verb it does not.</summary>
        public static bool IsImplemented(StoneVerb verb) => Implemented.ContainsKey(verb);

        /// <summary>A live, enabled definition whose verb this build performs (pipeline step 4).</summary>
        public static bool IsUsable(StoneDef? def)
        {
            if (def == null || !def.Enabled)
            {
                return false;
            }
            if (Implemented.ContainsKey(def.Verb))
            {
                return true;
            }
            if (Warned.Add(def.Id))
            {
                Log.Warn($"rune '{def.Id}' is enabled, but its verb '{def.Verb}' is not in this version; it refuses as disabled");
            }
            return false;
        }

        public static StoneResult Run(StoneJob job)
        {
            StoneVerb verb = job.Def!.Verb;
            return Implemented[verb].Run(job);
        }
    }
}
