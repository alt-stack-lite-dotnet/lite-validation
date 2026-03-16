namespace Lite.Validation.Rules.Inline;

/// <summary>
/// Small helper methods used by inlineable rules when emitting code.
/// Lives alongside RuleInlineContext so the source generator can reuse them.
/// </summary>
public static class InlineHelpers
{
    /// <summary>
    /// Returns an expression for the details string, falling back to a default literal.
    /// </summary>
    public static string CoalesceDetails(string? detailsExpression)
        => detailsExpression ?? "\"Validation failed\"";

    /// <summary>
    /// Emits a simple predicate-based rule:
    /// if (!predicate) { result.Add(propertyPath, details); if (cascade == Stop) return; }
    /// </summary>
    public static void EmitPredicateRule(
        in RuleInlineContext ctx,
        string ruleComment,
        string failureConditionExpression)
    {
        var w = ctx.Writer;
        var result = ctx.ResultName;
        var prop = ctx.PropertyPathExpression;
        var details = CoalesceDetails(ctx.DetailsExpression);
        var cascade = ctx.RuleLevelCascadeVarName;

        w.AppendLine($"// {ruleComment}");
        w.AppendLine($"if ({failureConditionExpression})");
        w.AppendLine("{");
        w.AppendLine($"    {result}.Add({prop}, {details});");
        w.AppendLine($"    if ({cascade} == CascadeMode.Stop) return;");
        w.AppendLine("}");
        w.AppendLine();
    }
}


