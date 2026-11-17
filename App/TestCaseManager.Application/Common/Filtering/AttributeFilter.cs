using System.Globalization;

namespace TestCaseManager.Application.Common.Filtering;

/// <summary>
/// Ein geparster Attributfilter, z. B. <c>tuer=2 AND nachname=Bongo</c>.
/// Wird gegen die Attribute eines Testfalls ausgewertet (rein im Speicher,
/// daher kein dynamisches SQL und keine Injection-Gefahr).
///
/// Grammatik (AND bindet stärker als OR, Klammern erlaubt):
/// <code>
///   ausdruck   := oder
///   oder       := und ( "OR"  und )*
///   und        := primär ( "AND" primär )*
///   primär     := "(" ausdruck ")" | bedingung
///   bedingung  := spalte operator wert
/// </code>
/// Operatoren: <c>= == != &lt;&gt; &lt; &gt; &lt;= &gt;= ~</c> (<c>~</c> = enthält).
/// Werte dürfen in Anführungszeichen stehen (z. B. <c>baujahr="01.01.1965"</c>).
/// </summary>
public sealed class AttributeFilter
{
    private readonly Node _root;

    private AttributeFilter(Node root) => _root = root;

    /// <summary>
    /// Versucht, den Filterausdruck zu parsen.
    /// </summary>
    /// <param name="text">Der Filterausdruck.</param>
    /// <param name="filter">Der geparste Filter (bei Erfolg).</param>
    /// <param name="error">Fehlermeldung (bei Misserfolg).</param>
    /// <returns><c>true</c>, wenn der Ausdruck gültig ist.</returns>
    public static bool TryParse(string? text, out AttributeFilter? filter, out string? error)
    {
        filter = null;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Der Filter ist leer.";
            return false;
        }

        try
        {
            var tokens = Tokenize(text);
            var parser = new Parser(tokens);
            var node = parser.ParseExpression();
            parser.ExpectEnd();
            filter = new AttributeFilter(node);
            return true;
        }
        catch (FilterFormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Wertet den Filter gegen die Attribute eines Testfalls aus.
    /// </summary>
    /// <param name="attributes">Die (Spalte, Wert)-Paare des Testfalls.</param>
    /// <returns><c>true</c>, wenn der Testfall den Filter erfüllt.</returns>
    public bool Matches(IReadOnlyCollection<(string Column, string? Value)> attributes)
        => _root.Matches(attributes);

    // ===== Tokenizer =====

    private enum TokenType { LParen, RParen, And, Or, Operand, Operator }

    private readonly record struct Token(TokenType Type, string Text);

    private static readonly HashSet<string> ValidOperators =
        ["=", "==", "!=", "<>", "<", ">", "<=", ">=", "~"];

    private static bool IsOperatorChar(char c) => c is '=' or '!' or '<' or '>' or '~';

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c == '(')
            {
                tokens.Add(new Token(TokenType.LParen, "("));
                i++;
            }
            else if (c == ')')
            {
                tokens.Add(new Token(TokenType.RParen, ")"));
                i++;
            }
            else if (IsOperatorChar(c))
            {
                var start = i;
                while (i < text.Length && IsOperatorChar(text[i]))
                {
                    i++;
                }
                var op = text[start..i];
                if (!ValidOperators.Contains(op))
                {
                    throw new FilterFormatException($"Unbekannter Operator: „{op}“.");
                }
                tokens.Add(new Token(TokenType.Operator, op));
            }
            else if (c == '"')
            {
                var start = ++i;
                while (i < text.Length && text[i] != '"')
                {
                    i++;
                }
                if (i >= text.Length)
                {
                    throw new FilterFormatException("Fehlendes schließendes Anführungszeichen.");
                }
                tokens.Add(new Token(TokenType.Operand, text[start..i]));
                i++; // schließendes Anführungszeichen überspringen
            }
            else
            {
                var start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i])
                       && text[i] != '(' && text[i] != ')' && !IsOperatorChar(text[i]))
                {
                    i++;
                }
                var word = text[start..i];
                if (word.Equals("AND", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenType.And, word));
                }
                else if (word.Equals("OR", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenType.Or, word));
                }
                else
                {
                    tokens.Add(new Token(TokenType.Operand, word));
                }
            }
        }
        return tokens;
    }

    // ===== Parser (rekursiver Abstieg) =====

    private sealed class Parser(List<Token> tokens)
    {
        private int _position;

        private Token? Peek => _position < tokens.Count ? tokens[_position] : null;

        private Token Advance() => tokens[_position++];

        public Node ParseExpression() => ParseOr();

        private Node ParseOr()
        {
            var node = ParseAnd();
            while (Peek is { Type: TokenType.Or })
            {
                Advance();
                node = new OrNode(node, ParseAnd());
            }
            return node;
        }

        private Node ParseAnd()
        {
            var node = ParsePrimary();
            while (Peek is { Type: TokenType.And })
            {
                Advance();
                node = new AndNode(node, ParsePrimary());
            }
            return node;
        }

        private Node ParsePrimary()
        {
            var token = Peek ?? throw new FilterFormatException("Unerwartetes Ende des Filters.");

            if (token.Type == TokenType.LParen)
            {
                Advance();
                var node = ParseOr();
                if (Peek is not { Type: TokenType.RParen })
                {
                    throw new FilterFormatException("Fehlende schließende Klammer.");
                }
                Advance();
                return node;
            }

            if (token.Type != TokenType.Operand)
            {
                throw new FilterFormatException($"Spaltenname erwartet, gefunden: „{token.Text}“.");
            }
            var column = Advance().Text;

            var opToken = Peek
                ?? throw new FilterFormatException($"Operator nach „{column}“ erwartet.");
            if (opToken.Type != TokenType.Operator)
            {
                throw new FilterFormatException($"Operator nach „{column}“ erwartet, gefunden: „{opToken.Text}“.");
            }
            var op = Advance().Text;

            var valueToken = Peek
                ?? throw new FilterFormatException($"Wert nach „{column}{op}“ erwartet.");
            if (valueToken.Type != TokenType.Operand)
            {
                throw new FilterFormatException($"Wert nach „{column}{op}“ erwartet, gefunden: „{valueToken.Text}“.");
            }
            var value = Advance().Text;

            return new ConditionNode(column, op, value);
        }

        public void ExpectEnd()
        {
            if (_position != tokens.Count)
            {
                throw new FilterFormatException($"Unerwarteter Ausdruck: „{tokens[_position].Text}“.");
            }
        }
    }

    // ===== Ausdrucksbaum =====

    private abstract class Node
    {
        public abstract bool Matches(IReadOnlyCollection<(string Column, string? Value)> attributes);
    }

    private sealed class AndNode(Node left, Node right) : Node
    {
        public override bool Matches(IReadOnlyCollection<(string Column, string? Value)> attributes)
            => left.Matches(attributes) && right.Matches(attributes);
    }

    private sealed class OrNode(Node left, Node right) : Node
    {
        public override bool Matches(IReadOnlyCollection<(string Column, string? Value)> attributes)
            => left.Matches(attributes) || right.Matches(attributes);
    }

    private sealed class ConditionNode(string column, string op, string value) : Node
    {
        public override bool Matches(IReadOnlyCollection<(string Column, string? Value)> attributes)
            => attributes.Any(a =>
                string.Equals(a.Column, column, StringComparison.OrdinalIgnoreCase)
                && Compare(a.Value ?? string.Empty));

        private bool Compare(string actual) => op switch
        {
            "=" or "==" => ValuesEqual(actual, value),
            "!=" or "<>" => !ValuesEqual(actual, value),
            "~" => actual.Contains(value, StringComparison.OrdinalIgnoreCase),
            ">" => CompareOrder(actual, value) > 0,
            "<" => CompareOrder(actual, value) < 0,
            ">=" => CompareOrder(actual, value) >= 0,
            "<=" => CompareOrder(actual, value) <= 0,
            _ => false
        };

        private static bool ValuesEqual(string actual, string expected)
            => TryNumber(actual, out var a) && TryNumber(expected, out var b)
                ? a == b
                : string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

        private static int CompareOrder(string actual, string expected)
            => TryNumber(actual, out var a) && TryNumber(expected, out var b)
                ? a.CompareTo(b)
                : string.Compare(actual, expected, StringComparison.OrdinalIgnoreCase);

        private static bool TryNumber(string text, out decimal value)
            => decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value);
    }

    private sealed class FilterFormatException(string message) : Exception(message);
}
