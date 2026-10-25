using System;
using UnityEngine;

namespace StatusTemplates
{
    /// <summary>
    /// Makes a status effect object the way every template needs it: named, since the game takes the effect's identity
    /// (<c>NameHash</c>) from the object name, and kept loaded. Each copy the game adds to a character is a shallow clone
    /// (<c>StatusEffect.Clone</c>, a MemberwiseClone) that shares the template's native object, so the template must
    /// outlive every copy: it is never unloaded with unused assets and never destroyed.
    /// </summary>
    public static class StatusTemplate
    {
        public static T Make<T>(string name) where T : StatusEffect
        {
            T effect = ScriptableObject.CreateInstance<T>();
            effect.name = name;
            effect.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return effect;
        }
    }

    /// <summary>
    /// One of a mod's own status effects: its template, made on first use, and the copy of it on a character. The setup
    /// runs once on the new template (words, time to live, stats); the icon is asked for again on every use while the
    /// template has none, so an icon borrowed from the object database arrives once the database can answer.
    /// <see cref="SEMan.AddStatusEffect(StatusEffect, bool, int, float, short)"/> refuses a second copy of the same
    /// name, so <see cref="Add"/> returns null when the character already has one (with <c>resetTime</c> that one starts
    /// again); <see cref="Hold"/> returns the copy there or adds one.
    /// </summary>
    public sealed class StatusTemplate<T> where T : StatusEffect
    {
        private readonly Action<T>? setup;
        private readonly Func<Sprite?>? icon;
        private T? made;

        public StatusTemplate(string name, Action<T>? setup = null, Func<Sprite?>? icon = null)
        {
            Name = name;
            Hash = name.GetStableHashCode();
            this.setup = setup;
            this.icon = icon;
        }

        /// <summary>The object name; the game hashes it to find the effect.</summary>
        public string Name { get; }

        /// <summary>The name hash: the key on a character's status effects and in a hit's status field.</summary>
        public int Hash { get; }

        /// <summary>The template, made again if Unity destroyed it.</summary>
        public T Get()
        {
            T effect = Missing(made) ? Make() : made!;
            if (icon != null && effect.m_icon == null)
            {
                effect.m_icon = icon();
            }
            return effect;
        }

        public T? On(Character? character) => Seman(character)?.GetStatusEffect(Hash) as T;

        public bool Has(Character? character) => Seman(character)?.HaveStatusEffect(Hash) == true;

        public T? Add(Character? character, bool resetTime = false) =>
            Seman(character)?.AddStatusEffect(Get(), resetTime) as T;

        public T? Hold(Character? character) => On(character) ?? Add(character);

        /// <summary>Takes the copy away without its stop message.</summary>
        public bool Drop(Character? character) => Seman(character)?.RemoveStatusEffect(Hash, quiet: true) == true;

        private T Make()
        {
            T effect = StatusTemplate.Make<T>(Name);
            made = effect;
            setup?.Invoke(effect);
            return effect;
        }

        // Unity's null: a destroyed object compares equal to null only through UnityEngine.Object.
        private static bool Missing(UnityEngine.Object? effect) => effect == null;

        private static SEMan? Seman(Character? character) => character != null ? character.GetSEMan() : null;
    }
}
