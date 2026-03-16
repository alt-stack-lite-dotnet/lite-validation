using System;

namespace Lite.Validation.Rules.Inline;

/// <summary>
/// Indicates that a rule type is a candidate for automatic inline generation.
/// The type must be partial; the source generator can emit the IInlineableRule
/// implementation and the corresponding Inline method into another part.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class InlineableRuleAttribute : Attribute
{
}

