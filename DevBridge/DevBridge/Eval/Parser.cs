using System.Collections.Generic;
using DevBridge.Server;

namespace DevBridge.Eval
{
    internal abstract class Node
    {
    }

    internal sealed class Literal : Node
    {
        internal readonly object Value;
        internal Literal(object value) { Value = value; }
    }

    /// <summary>$player, $go("path"), ...: see Variables.</summary>
    internal sealed class Variable : Node
    {
        internal readonly string Name;
        internal readonly List<Node> Args;
        internal Variable(string name, List<Node> args) { Name = name; Args = args; }
    }

    /// <summary>A bare identifier: a type name or the start of a namespace.</summary>
    internal sealed class Name : Node
    {
        internal readonly string Ident;
        internal Name(string ident) { Ident = ident; }
    }

    /// <summary>target.Ident, or target.Ident(args) when Args is not null.</summary>
    internal sealed class Member : Node
    {
        internal readonly Node Target;
        internal readonly string Ident;
        internal readonly List<Node> Args;
        internal Member(Node target, string ident, List<Node> args) { Target = target; Ident = ident; Args = args; }
    }

    internal sealed class Index : Node
    {
        internal readonly Node Target;
        internal readonly Node Key;
        internal Index(Node target, Node key) { Target = target; Key = key; }
    }

    internal sealed class Assign : Node
    {
        internal readonly Node Target;
        internal readonly Node Value;
        internal Assign(Node target, Node value) { Target = target; Value = value; }
    }

    /// <summary>statement := expr ['=' expr];  expr := primary ('.' ident [args] | '[' expr ']')*</summary>
    internal sealed class Parser
    {
        private readonly List<Token> tokens;
        private int at;

        private Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        internal static Node Parse(string source)
        {
            var parser = new Parser(Lexer.Read(source));
            Node statement = parser.Expression();
            if (parser.Accept("=")) statement = new Assign(statement, parser.Expression());
            parser.Expect(TokenKind.End, null);
            return statement;
        }

        private Token Next() => tokens[at++];

        private bool Accept(string symbol)
        {
            Token token = tokens[at];
            if (token.Kind != TokenKind.Symbol || token.Text != symbol) return false;
            at++;
            return true;
        }

        private Token Expect(TokenKind kind, string symbol)
        {
            Token token = Next();
            if (token.Kind == kind && (symbol == null || token.Text == symbol)) return token;
            throw new BridgeException($"expected {symbol ?? kind.ToString()} at {token.Position}, found '{token.Text}'");
        }

        private Node Expression()
        {
            Node node = Primary();
            while (true)
            {
                if (Accept(".")) node = new Member(node, Expect(TokenKind.Ident, null).Text, Arguments());
                else if (Accept("[")) node = new Index(node, Closed(Expression(), "]"));
                else return node;
            }
        }

        private Node Closed(Node node, string symbol)
        {
            Expect(TokenKind.Symbol, symbol);
            return node;
        }

        private Node Primary()
        {
            Token token = Next();
            if (token.Kind == TokenKind.Number) return new Literal(Literals.Number(token.Text));
            if (token.Kind == TokenKind.String) return new Literal(Literals.Unquote(token.Text));
            if (token.Kind == TokenKind.Ident) return Word(token.Text);
            if (token.Kind == TokenKind.Symbol && token.Text == "(") return Closed(Expression(), ")");
            throw new BridgeException($"unexpected '{token.Text}' at {token.Position}");
        }

        private Node Word(string word)
        {
            if (word == "true") return new Literal(true);
            if (word == "false") return new Literal(false);
            if (word == "null") return new Literal(null);
            if (word[0] == '$') return new Variable(word.Substring(1), Arguments());
            return new Name(word);
        }

        private List<Node> Arguments()
        {
            if (!Accept("(")) return null;
            var args = new List<Node>();
            if (Accept(")")) return args;
            do args.Add(Expression()); while (Accept(","));
            Expect(TokenKind.Symbol, ")");
            return args;
        }
    }
}
