using System;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// One creature's decorated name (<see cref="Naming"/>), kept. The game asks for a creature's name every frame its
    /// nameplate or hover text shows, so the words are put together again only when what they decorate changes: the
    /// name the game hands in (a tamed creature renamed, the language switched) or the traits it carries, which a
    /// creature resolves once and keeps.
    /// </summary>
    public sealed class NameMemo
    {
        private string? _from;
        private CreatureTraits? _traits;
        private string _made = "";

        public string Decorate(CreatureTraits traits, string baseName)
        {
            if (!ReferenceEquals(traits, _traits) || !string.Equals(baseName, _from, StringComparison.Ordinal))
            {
                _made = Naming.Decorate(traits, baseName);
                _from = baseName;
                _traits = traits;
            }
            return _made;
        }
    }
}
