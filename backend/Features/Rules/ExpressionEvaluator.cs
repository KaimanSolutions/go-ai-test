using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FormBuilder.Backend;

/// <summary>
/// Evaluates arithmetic expressions containing field references and aggregate functions.
///
/// Syntax:
///   {fieldName}              — value of a form field
///   SUM({repeater.field})    — sum of a sub-field across a repeater array
///   AVG({repeater.field})    — average
///   COUNT({repeater})        — count of entries in a repeater array
///   +  -  *  /  ( )         — standard arithmetic
///
/// Examples:
///   {loanAmount} / {propertyValue} * 100   → LTV percentage
///   SUM({incomes.amount})                  → total income from repeater
///   {applicantAge}                         → direct field value
/// </summary>
public sealed class ExpressionEvaluator
{
    private static readonly Regex RxSum   = new(@"SUM\s*\(\s*\{([^}]+)\}\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxAvg   = new(@"AVG\s*\(\s*\{([^}]+)\}\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxCount = new(@"COUNT\s*\(\s*\{([^}]+)\}\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxField = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    public decimal? Evaluate(string expression, Dictionary<string, JsonElement> values)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        try
        {
            var expr = expression.Trim();

            // Step 1 — resolve aggregation functions before field substitution
            expr = RxSum.Replace(expr, m =>
            {
                var val = Aggregate(m.Groups[1].Value, values, nums => nums.Count > 0 ? nums.Sum() : 0m);
                return val?.ToString(CultureInfo.InvariantCulture) ?? "null";
            });

            expr = RxAvg.Replace(expr, m =>
            {
                var val = Aggregate(m.Groups[1].Value, values, nums => nums.Count > 0 ? nums.Average() : (decimal?)null);
                return val?.ToString(CultureInfo.InvariantCulture) ?? "null";
            });

            expr = RxCount.Replace(expr, m =>
                CountItems(m.Groups[1].Value, values).ToString(CultureInfo.InvariantCulture));

            // Step 2 — substitute {fieldName} with actual numeric values
            expr = RxField.Replace(expr, m =>
            {
                var name = m.Groups[1].Value.Trim();
                if (!values.TryGetValue(name, out var el)) return "null";
                return ToDecimalString(el) ?? "null";
            });

            // If any unresolved reference remains, the expression is indeterminate
            if (expr.Contains("null")) return null;

            // Step 3 — evaluate the resulting pure arithmetic string
            return new ArithmeticParser(expr).Parse();
        }
        catch
        {
            return null;
        }
    }

    // ── Aggregation helpers ───────────────────────────────────────────────────

    private static decimal? Aggregate(string fieldPath, Dictionary<string, JsonElement> values,
        Func<List<decimal>, decimal?> fn)
    {
        var items = GetArrayItems(fieldPath, values);
        if (items is null) return null;
        return fn(items);
    }

    private static int CountItems(string fieldPath, Dictionary<string, JsonElement> values)
    {
        var parts = fieldPath.Split('.', 2);
        var arr   = ParseArray(parts[0], values);
        return arr?.Count ?? 0;
    }

    private static List<decimal>? GetArrayItems(string fieldPath, Dictionary<string, JsonElement> values)
    {
        var parts    = fieldPath.Split('.', 2);
        var arr      = ParseArray(parts[0], values);
        if (arr is null) return null;
        var subField = parts.Length > 1 ? parts[1] : null;

        var numbers = new List<decimal>();
        foreach (var item in arr)
        {
            var node = subField is not null ? item?[subField] : item;
            if (node is null) continue;
            if (decimal.TryParse(node.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                numbers.Add(d);
        }
        return numbers;
    }

    private static JsonArray? ParseArray(string fieldName, Dictionary<string, JsonElement> values)
    {
        if (!values.TryGetValue(fieldName, out var el)) return null;
        try
        {
            var raw = el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetRawText();
            return string.IsNullOrWhiteSpace(raw) ? null : JsonNode.Parse(raw)?.AsArray();
        }
        catch { return null; }
    }

    private static string? ToDecimalString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.String => decimal.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                                    ? el.GetString() : null,
        JsonValueKind.True  => "1",
        JsonValueKind.False => "0",
        _                   => null
    };

    // ── Recursive-descent arithmetic parser ───────────────────────────────────

    private sealed class ArithmeticParser(string input)
    {
        private int _pos;

        public decimal? Parse()
        {
            var v = ParseExpr();
            return v;
        }

        // expr → term (('+' | '-') term)*
        private decimal? ParseExpr()
        {
            var left = ParseTerm();
            while (_pos < input.Length)
            {
                Skip();
                if (_pos >= input.Length) break;
                var ch = input[_pos];
                if (ch is not ('+' or '-')) break;
                _pos++;
                var right = ParseTerm();
                if (left is null || right is null) return null;
                left = ch == '+' ? left + right : left - right;
            }
            return left;
        }

        // term → factor (('*' | '/') factor)*
        private decimal? ParseTerm()
        {
            var left = ParseFactor();
            while (_pos < input.Length)
            {
                Skip();
                if (_pos >= input.Length) break;
                var ch = input[_pos];
                if (ch is not ('*' or '/')) break;
                _pos++;
                var right = ParseFactor();
                if (left is null || right is null) return null;
                if (ch == '/' && right == 0m) return null;
                left = ch == '*' ? left * right : left / right;
            }
            return left;
        }

        // factor → '-' factor | '(' expr ')' | number
        private decimal? ParseFactor()
        {
            Skip();
            if (_pos >= input.Length) return null;

            if (input[_pos] == '-') { _pos++; var v = ParseFactor(); return v is null ? null : -v; }

            if (input[_pos] == '(')
            {
                _pos++;
                var v = ParseExpr();
                Skip();
                if (_pos < input.Length && input[_pos] == ')') _pos++;
                return v;
            }

            // Number literal
            var start = _pos;
            while (_pos < input.Length && (char.IsDigit(input[_pos]) || input[_pos] == '.')) _pos++;
            if (_pos == start) { _pos++; return null; }

            return decimal.TryParse(input[start.._pos], NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        private void Skip() { while (_pos < input.Length && char.IsWhiteSpace(input[_pos])) _pos++; }
    }
}
