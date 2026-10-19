using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using Translations;

namespace EliteCreaturesReborn.Recap
{
    /// <summary>
    /// Who dealt a hit, kept as what the name is made of rather than as words, since a recap is watched later and the
    /// language may have changed by then: a player's name as it is, or a creature's own name word ("$enemy_greydwarf")
    /// with a copy of its traits and its stars as they were at the hit (the creature may be gone). <see cref="Text"/>
    /// builds the whole name ("Mad Greydwarf 2★") in the language shown, again only after the language changes.
    /// </summary>
    internal sealed class AttackerName
    {
        private readonly string? _player;
        private readonly string _creature;
        private readonly CreatureTraits? _traits;
        private readonly int _stars;
        private string _text = "";
        private int _version = -1;

        private AttackerName(string? player, string creature, CreatureTraits? traits, int stars)
        {
            _player = player;
            _creature = creature;
            _traits = traits;
            _stars = stars;
        }

        /// <summary>The name of whoever dealt a hit; null when nobody did.</summary>
        public static AttackerName? Of(Character? attacker)
        {
            if (attacker == null)
            {
                return null;
            }
            if (attacker is Player player)
            {
                return new AttackerName(player.GetPlayerName() ?? "", "", null, 0);
            }
            EliteController controller = attacker.GetComponent<EliteController>();
            if (controller == null || !controller.Ready)
            {
                return new AttackerName(null, attacker.m_name ?? "", null, attacker.GetLevel() - 1);
            }
            CreatureTraits traits = controller.Traits;
            int stars = traits.Stars > 0 ? traits.Stars : attacker.GetLevel() - 1; // a kept level's
            return new AttackerName(null, attacker.m_name ?? "", Copy(traits), stars);
        }

        /// <summary>The name's text, or empty for nobody.</summary>
        public static string TextOf(AttackerName? name) => name != null ? name.Text : "";

        /// <summary>A creature's name with its mutation and aspect words and stars ("Mad Greydwarf 2★"), or a player's name.</summary>
        public string Text
        {
            get
            {
                if (_version != Lang.Version)
                {
                    _version = Lang.Version;
                    _text = Build();
                }
                return _text;
            }
        }

        private string Build()
        {
            if (_player != null)
            {
                return _player;
            }
            string name = Localization.instance != null ? Localization.instance.Localize(_creature) : _creature;
            return (_traits != null ? Naming.Decorate(_traits, name) : name) + HitDescribe.Stars(_stars);
        }

        // The traits as they were at the hit: the creature's own may change or go with it.
        private static CreatureTraits Copy(CreatureTraits traits) => new CreatureTraits(traits.Stars, traits.Mask)
        {
            Aspect = traits.Aspect,
            ExtraAspects = traits.ExtraAspects,
            PhantomCopy = traits.PhantomCopy,
            Echo = traits.Echo,
            Decoy = traits.Decoy,
            Tier = traits.Tier,
        };
    }
}
