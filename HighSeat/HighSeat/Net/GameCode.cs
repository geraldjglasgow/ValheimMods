using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace HighSeat
{
    /// <summary>
    /// A game method's fingerprint: its own IL as compiled (never another mod's transpiled version: that lives in the patch,
    /// not in the method's body) reduced to what it does, in order: the methods it calls ("ZPackage.GetArray"), the fields
    /// it reads and writes ("ZSteamSocket:m_totalSent"), the types it tests or makes ("t:ZPackage"), its strings and its
    /// whole-number constants ("#8"); locals, arguments and branches are left out, so only a change to what the code does
    /// changes it. The text is hashed (FNV-1a, 32 bits). <see cref="SendChecks"/> compares these with the values taken from
    /// the game by this same code, so a send patch copies the game's code only while that code is what it was checked
    /// against. Reflection only, run once per game method per process.
    /// </summary>
    internal static class GameCode
    {
        private static readonly OpCode[] OneByte = new OpCode[256];
        private static readonly OpCode[] TwoByte = new OpCode[256];
        private static bool _tables;

        public static uint Fingerprint(MethodBase method) => Hash(Describe(method));

        /// <summary>The method's IL as text, the part of it the fingerprint keeps.</summary>
        public static string Describe(MethodBase method)
        {
            Tables();
            byte[] il = method.GetMethodBody().GetILAsByteArray();
            StringBuilder text = new StringBuilder();
            for (int at = 0; at < il.Length;)
            {
                OpCode op = il[at] == 0xFE ? TwoByte[il[at + 1]] : OneByte[il[at]];
                if (op.Size == 0)
                {
                    throw new InvalidOperationException($"unknown IL opcode at {at} in {method.Name}");
                }
                at += op.Size;
                string token = Token(method, op, il, at);
                if (token != null)
                {
                    text.Append(token).Append(',');
                }
                at += OperandSize(op, il, at);
            }
            return text.ToString();
        }

        public static uint Hash(string text)
        {
            uint hash = 2166136261;
            foreach (char c in text)
            {
                hash = (hash ^ c) * 16777619;
            }
            return hash;
        }

        // What one instruction adds to the text: a member, a type, a string or a whole number; null for the rest.
        private static string Token(MethodBase method, OpCode op, byte[] il, int at)
        {
            switch (op.OperandType)
            {
                case OperandType.InlineMethod:
                case OperandType.InlineField:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    return Member(method.Module.ResolveMember(BitConverter.ToInt32(il, at), TypeArgs(method), MethodArgs(method)));
                case OperandType.InlineString:
                    return "\"" + method.Module.ResolveString(BitConverter.ToInt32(il, at)) + "\"";
                case OperandType.ShortInlineI:
                    return "#" + (op == OpCodes.Ldc_I4_S ? (sbyte)il[at] : il[at]);
                case OperandType.InlineI:
                    return "#" + BitConverter.ToInt32(il, at);
                case OperandType.InlineI8:
                    return "#" + BitConverter.ToInt64(il, at) + "L";
                case OperandType.InlineNone:
                    return Small(op);
                default:
                    return null;
            }
        }

        // ldc.i4.m1 to ldc.i4.8, the constants that live in the opcode itself.
        private static string Small(OpCode op)
        {
            int value = op.Value - OpCodes.Ldc_I4_0.Value;
            return op.Value >= OpCodes.Ldc_I4_M1.Value && op.Value <= OpCodes.Ldc_I4_8.Value ? "#" + value : null;
        }

        private static string Member(MemberInfo member)
        {
            switch (member)
            {
                case MethodBase m:
                    return m.DeclaringType?.Name + "." + m.Name + Parameters(m);
                case FieldInfo f:
                    return f.DeclaringType?.Name + ":" + f.Name;
                case Type t:
                    return "t:" + t.Name;
                default:
                    return member?.Name;
            }
        }

        // "(Int32)", "(ZPackage&)": which overload is called counts, Write(int) and Write(long) write different bytes.
        private static string Parameters(MethodBase method)
        {
            StringBuilder text = new StringBuilder("(");
            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                text.Append(i == 0 ? "" : ";").Append(parameters[i].ParameterType.Name);
            }
            return text.Append(')').ToString();
        }

        private static int OperandSize(OpCode op, byte[] il, int at)
        {
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineSwitch:
                    return 4 + 4 * BitConverter.ToInt32(il, at);
                default:
                    return 4;
            }
        }

        private static Type[] TypeArgs(MethodBase method) =>
            method.DeclaringType != null && method.DeclaringType.IsGenericType ? method.DeclaringType.GetGenericArguments() : null;

        private static Type[] MethodArgs(MethodBase method) =>
            method.IsGenericMethod ? method.GetGenericArguments() : null;

        // Every opcode by its value, from the runtime's own list.
        private static void Tables()
        {
            if (_tables)
            {
                return;
            }
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null) is OpCode op)
                {
                    ushort value = (ushort)op.Value;
                    if (value < 0x100)
                    {
                        OneByte[value] = op;
                    }
                    else if ((value & 0xFF00) == 0xFE00)
                    {
                        TwoByte[value & 0xFF] = op;
                    }
                }
            }
            _tables = true;
        }
    }
}
