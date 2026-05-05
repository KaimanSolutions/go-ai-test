using System.Text.RegularExpressions;

namespace FormBuilder.Backend;

public sealed class RuleEngineService
{
    public FormValidationResult ValidateSubmission(FormSchema schema, Dictionary<string, object?> values)
    {
        var result = new FormValidationResult();

        foreach (var step in schema.Steps)
        {
            foreach (var field in step.Fields)
            {
                if (!IsFieldVisible(field, values)) continue;

                var value = values.GetValueOrDefault(field.Name);

                if (field.Required && IsEmpty(value))
                {
                    result.Errors[field.Name] = [$"{field.Label} is required."];
                    continue;
                }

                foreach (var validator in field.Validators)
                {
                    if (!ShouldValidate(validator, values)) continue;

                    var message = validator.Message;
                    switch (validator.RuleType.ToLowerInvariant())
                    {
                        case "min":
                            if (!IsNumber(value, out var minValue) || minValue < Convert.ToDecimal(validator.Value ?? 0))
                                AddError(field.Name, message ?? $"{field.Label} does not meet minimum value.", result);
                            break;
                        case "max":
                            if (!IsNumber(value, out var maxValue) || maxValue > Convert.ToDecimal(validator.Value ?? decimal.MaxValue))
                                AddError(field.Name, message ?? $"{field.Label} exceeds maximum value.", result);
                            break;
                        case "regex":
                            if (value is string stringValue && validator.Value is string pattern && !Regex.IsMatch(stringValue, pattern))
                                AddError(field.Name, message ?? $"{field.Label} is invalid.", result);
                            break;
                    }
                }
            }
        }

        return result;
    }

    private static bool IsFieldVisible(FormField field, Dictionary<string, object?> values) =>
        field.Conditions.All(c => EvaluateCondition(c, values));

    private static bool ShouldValidate(ValidationRule validator, Dictionary<string, object?> values) =>
        validator.Conditions.All(c => EvaluateCondition(c, values));

    private static bool EvaluateCondition(FieldCondition condition, Dictionary<string, object?> values)
    {
        values.TryGetValue(condition.FieldName, out var rawValue);
        var left  = rawValue?.ToString() ?? string.Empty;
        var right = condition.Value?.ToString() ?? string.Empty;

        return condition.Operator.ToLowerInvariant() switch
        {
            "equals"       => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "not_equals"   => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "greater_than" => TryCompare(left, right, out var gt) && gt > 0,
            "less_than"    => TryCompare(left, right, out var lt) && lt < 0,
            _              => false
        };
    }

    private static bool TryCompare(string left, string right, out int result)
    {
        if (decimal.TryParse(left, out var a) && decimal.TryParse(right, out var b))
        {
            result = a.CompareTo(b);
            return true;
        }
        result = string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static bool IsEmpty(object? value) =>
        value is null || string.IsNullOrWhiteSpace(value.ToString());

    private static bool IsNumber(object? value, out decimal number)
    {
        if (value is null) { number = 0; return false; }
        return decimal.TryParse(value.ToString(), out number);
    }

    private static void AddError(string fieldName, string message, FormValidationResult result)
    {
        if (!result.Errors.ContainsKey(fieldName))
            result.Errors[fieldName] = [];
        result.Errors[fieldName].Add(message);
    }
}
