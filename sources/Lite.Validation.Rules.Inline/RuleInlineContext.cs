using System.Text;

namespace Lite.Validation.Rules.Inline;

/// <summary>
/// Context passed to built-in rules so they can emit
/// readable, inlined validation code into the generated validator.
/// </summary>
public readonly ref struct RuleInlineContext
{
    public RuleInlineContext(
        StringBuilder writer,
        string targetName,
        string valueName,
        string resultName,
        string propertyPathExpression,
        string? detailsExpression,
        string ruleLevelCascadeVarName)
    {
        Writer = writer;
        TargetName = targetName;
        ValueName = valueName;
        ResultName = resultName;
        PropertyPathExpression = propertyPathExpression;
        DetailsExpression = detailsExpression;
        RuleLevelCascadeVarName = ruleLevelCascadeVarName;
    }

    /// <summary>StringBuilder for the generated method body.</summary>
    public StringBuilder Writer { get; }

    /// <summary>Name of the validated object variable (e.g. "order").</summary>
    public string TargetName { get; }

    /// <summary>Name of the validated property variable (e.g. "productName").</summary>
    public string ValueName { get; }

    /// <summary>Name of the ValidationResult variable (e.g. "result").</summary>
    public string ResultName { get; }

    /// <summary>Expression for the property path string (e.g. "\"ProductName\"").</summary>
    public string PropertyPathExpression { get; }

    /// <summary>Expression for the details string, or null to use a default.</summary>
    public string? DetailsExpression { get; }

    /// <summary>Name of the local variable that holds the rule-level cascade mode.</summary>
    public string RuleLevelCascadeVarName { get; }
}

