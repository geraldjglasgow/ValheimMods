using System;
using System.Linq;
using System.Reflection;
using DevBridge.Eval;
using DevBridge.Server;

namespace DevBridge.Tune
{
    /// <summary>One step of a member path: a field or property by name, or an [index] into an array, list or dictionary.</summary>
    internal sealed class PathStep
    {
        private readonly object key;

        /// <summary>The member's name as the type spells it once a walk has found it; null for an index.</summary>
        internal string Name { get; private set; }

        internal PathStep(string name, object key)
        {
            Name = name;
            this.key = key;
        }

        private bool IsIndex => Name == null;

        internal string Text => IsIndex ? "[" + (key is string text ? "\"" + text + "\"" : key) + "]" : "." + Name;

        internal object Get(object holder)
        {
            if (IsIndex) return Indexing.Get(holder, key);
            MemberInfo member = Member(holder);
            return member is FieldInfo field ? field.GetValue(holder) : ((PropertyInfo)member).GetValue(holder, null);
        }

        /// <summary>Sets the member on the holder; on a boxed struct this changes the box, which the path then writes back.</summary>
        internal void Set(object holder, object value)
        {
            if (IsIndex)
            {
                Indexing.Set(holder, key, value);
                return;
            }
            MemberInfo member = Member(holder);
            if (member is FieldInfo field) field.SetValue(holder, Coerce.To(value, field.FieldType));
            else if (member is PropertyInfo property && property.CanWrite) property.SetValue(holder, Coerce.To(value, property.PropertyType), null);
            else throw new BridgeException($"{Describe.TypeName(holder.GetType())}{Text} is read-only");
        }

        /// <summary>The type this step holds: the member's declared type, or an array's or indexer's element type.</summary>
        internal Type Type(object holder)
        {
            if (IsIndex) return holder is Array array ? array.GetType().GetElementType() : Indexer(holder.GetType())?.PropertyType ?? typeof(object);
            MemberInfo member = Member(holder);
            return member is FieldInfo field ? field.FieldType : ((PropertyInfo)member).PropertyType;
        }

        private MemberInfo Member(object holder)
        {
            MemberInfo member = Members.Find(holder.GetType(), Name, false);
            if (!(member is FieldInfo) && !(member is PropertyInfo))
                throw new BridgeException($"{Describe.TypeName(holder.GetType())} has no field or property '{Name}' (members=1 lists them)");
            Name = member.Name;
            return member;
        }

        private static PropertyInfo Indexer(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.GetIndexParameters().Length == 1);
    }
}
