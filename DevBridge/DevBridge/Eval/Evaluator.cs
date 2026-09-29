using System;
using System.Linq;
using DevBridge.Server;

namespace DevBridge.Eval
{
    /// <summary>A type used as a value: its static members are reachable through it.</summary>
    internal sealed class StaticRef
    {
        internal readonly Type Type;
        internal StaticRef(Type type) { Type = type; }
    }

    /// <summary>A dotted name that is not a type yet, like "UnityEngine" on the way to UnityEngine.Time.</summary>
    internal sealed class Pending
    {
        internal readonly string Name;
        internal Pending(string name) { Name = name; }
    }

    /// <summary>Evaluates parsed expressions with reflection: fields, properties, methods, indexers, assignment.</summary>
    internal static class Evaluator
    {
        internal static object Last;

        internal static object Run(string source)
        {
            Node node = Parser.Parse(source);
            object value = node is Assign assign ? Store(assign) : Resolve(node);
            Last = value is StaticRef ? Last : value;
            return value;
        }

        private static object Resolve(Node node)
        {
            object value = Eval(node);
            if (value is Pending pending) throw new BridgeException($"'{pending.Name}' is not a type, variable or member");
            return value;
        }

        private static object Eval(Node node)
        {
            switch (node)
            {
                case Literal literal: return literal.Value;
                case Variable variable: return Variables.Get(variable.Name, Values(variable.Args));
                case Name name: return Lookup(name.Ident);
                case Member member: return Access(member);
                case Index index: return Indexing.Get(Resolve(index.Target), Resolve(index.Key));
                default: throw new BridgeException("assignment is only allowed at the top level");
            }
        }

        private static object Lookup(string ident)
        {
            Type type = TypeIndex.Find(ident);
            return type != null ? new StaticRef(type) : (object)new Pending(ident);
        }

        private static object Access(Member member)
        {
            object target = Eval(member.Target);
            if (target is Pending pending) return Qualify(pending, member);
            object[] args = Values(member.Args);
            if (target is StaticRef type) return Members.Read(null, type.Type, member.Ident, args);
            return Members.Read(Live(target, member.Ident), target.GetType(), member.Ident, args);
        }

        private static object Qualify(Pending pending, Member member)
        {
            string full = pending.Name + "." + member.Ident;
            Type type = TypeIndex.Find(full);
            if (member.Args != null) throw new BridgeException($"'{pending.Name}' is not a type, so {member.Ident}() cannot be called on it");
            return type != null ? new StaticRef(type) : (object)new Pending(full);
        }

        private static object Live(object target, string member)
        {
            if (target == null || target is UnityEngine.Object unity && !unity)
                throw new BridgeException($"null (or destroyed) before .{member}");
            return target;
        }

        private static object[] Values(System.Collections.Generic.List<Node> args) => args?.Select(Resolve).ToArray();

        private static object Store(Assign assign)
        {
            object value = Resolve(assign.Value);
            if (assign.Target is Index index)
            {
                Indexing.Set(Resolve(index.Target), Resolve(index.Key), value);
                return value;
            }
            if (!(assign.Target is Member member) || member.Args != null)
                throw new BridgeException("only a field, property or indexed element can be assigned");
            object target = Resolve(member.Target);
            if (target is StaticRef type) Members.Write(null, type.Type, member.Ident, value);
            else Members.Write(Live(target, member.Ident), target.GetType(), member.Ident, value);
            return Resolve(member);
        }
    }
}
