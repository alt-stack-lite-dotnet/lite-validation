namespace Lite.Validation.Rules.Inline;

public interface ICustomRuleInline
{
    void Inline(in RuleInlineContext ctx);
}

