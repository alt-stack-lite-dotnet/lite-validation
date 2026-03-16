using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lite.Validation.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class ValidatorSourceGenerator : IIncrementalGenerator
{
    private const string FluentValidatorOpenName = "Lite.Validation.Fluent.FluentValidator<T>";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var validators = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax cds
                    && cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
                    && cds.BaseList is not null,
                transform: static (ctx, ct) => TryGetValidatorInfo(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        context.RegisterSourceOutput(validators, static (spc, info) => Emit(spc, info));
    }

    // -------------------------------------------------------------------------
    // Phase 1: collect info
    // -------------------------------------------------------------------------

    private static FluentValidatorInfo? TryGetValidatorInfo(
        GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct) is not INamedTypeSymbol symbol)
            return null;

        var baseType = symbol.BaseType;
        while (baseType is not null)
        {
            if (baseType.IsGenericType &&
                baseType.OriginalDefinition.ToDisplayString() == FluentValidatorOpenName)
            {
                var targetType = baseType.TypeArguments[0];
                return BuildInfo(classDecl, symbol, targetType);
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    private static FluentValidatorInfo? BuildInfo(
        ClassDeclarationSyntax classDecl,
        INamedTypeSymbol symbol,
        ITypeSymbol targetType)
    {
        var rulesMethod = FindRulesMethod(classDecl);
        if (rulesMethod is null) return null;

        var builderParam = GetBuilderParamName(rulesMethod);
        if (builderParam is null) return null;

        var ownDeps = ExtractDependencies(rulesMethod);
        var baseDeps = GetBaseConstructorDependencies(symbol.BaseType);
        var allDeps = baseDeps.Concat(ownDeps.Where(o => !baseDeps.Any(b => b.ParamName == o.ParamName))).ToList();

        bool ruleLevelStop = false, classLevelStop = false;
        var (propRules, abort) = ParseBody(rulesMethod, builderParam, allDeps, ref ruleLevelStop, ref classLevelStop);
        if (abort) return null;

        bool hasAsync = propRules.Any(p => p.Rules.Any(r => r.IsAsync));

        return new FluentValidatorInfo(
            ns: symbol.ContainingNamespace?.ToDisplayString() ?? "",
            className: symbol.Name,
            targetType: targetType.ToDisplayString(),
            baseDeps: baseDeps,
            ownDeps: ownDeps,
            allDeps: allDeps,
            propRules: propRules,
            hasAsync: hasAsync,
            ruleLevelStop: ruleLevelStop,
            classLevelStop: classLevelStop);
    }

    private static bool IsFluentValidator(INamedTypeSymbol? type)
    {
        while (type is not null)
        {
            if (type.IsGenericType && type.OriginalDefinition.ToDisplayString() == FluentValidatorOpenName)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    private static IMethodSymbol? GetConfigureMethod(INamedTypeSymbol type)
    {
        foreach (var m in type.GetMembers().OfType<IMethodSymbol>())
        {
            if (!m.IsStatic || m.Parameters.Length < 1) continue;
            var first = m.Parameters[0].Type as INamedTypeSymbol;
            if (first?.OriginalDefinition?.Name == "ValidationBuilder")
                return m;
        }
        return null;
    }

    private static List<DependencyInfo> GetDependenciesFromMethod(IMethodSymbol method)
    {
        return method.Parameters
            .Skip(1)
            .Select(p => new DependencyInfo(p.Type.ToDisplayString(), p.Name))
            .ToList();
    }

    private static List<DependencyInfo> GetBaseConstructorDependencies(INamedTypeSymbol? type)
    {
        if (type is null || !IsFluentValidator(type))
            return new List<DependencyInfo>();

        var configure = GetConfigureMethod(type);
        if (configure is null)
            return new List<DependencyInfo>();

        var ownOfBase = GetDependenciesFromMethod(configure);
        var baseOfBase = GetBaseConstructorDependencies(type.BaseType);
        return baseOfBase.Concat(ownOfBase.Where(o => !baseOfBase.Any(b => b.ParamName == o.ParamName))).ToList();
    }

    // Static method (any name) whose parameter list includes one ValidationBuilder<T> param
    private static MethodDeclarationSyntax? FindRulesMethod(ClassDeclarationSyntax cls)
        => cls.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m =>
            m.Modifiers.Any(SyntaxKind.StaticKeyword)
            && m.ParameterList.Parameters.Any(p =>
                p.Type?.ToString().Contains("ValidationBuilder") == true));

    private static string? GetBuilderParamName(MethodDeclarationSyntax method)
        => method.ParameterList.Parameters
            .FirstOrDefault(p => p.Type?.ToString().Contains("ValidationBuilder") == true)
            ?.Identifier.Text;

    private static List<DependencyInfo> ExtractDependencies(MethodDeclarationSyntax method)
        => method.ParameterList.Parameters
            .Where(p => p.Type?.ToString().Contains("ValidationBuilder") != true)
            .Select(p => new DependencyInfo(p.Type?.ToString() ?? "object", p.Identifier.Text))
            .ToList();

    // -------------------------------------------------------------------------
    // Phase 2: parse body
    // -------------------------------------------------------------------------

    private static (List<PropertyRuleInfo> Rules, bool Abort) ParseBody(
        MethodDeclarationSyntax method,
        string builderParam,
        List<DependencyInfo> deps,
        ref bool ruleLevelStop,
        ref bool classLevelStop)
    {
        var propRules = new List<PropertyRuleInfo>();
        if (method.Body is null) return (propRules, false);

        foreach (var stmt in method.Body.Statements)
        {
            // CascadeMode assignments: b.RuleLevelCascadeMode = CascadeMode.Stop
            if (stmt is ExpressionStatementSyntax
                {
                    Expression: AssignmentExpressionSyntax
                    {
                        Left: MemberAccessExpressionSyntax la,
                        Right: var ra
                    }
                })
            {
                var mn = la.Name.Identifier.Text;
                var isStop = ra.ToString().EndsWith("Stop");
                if (mn == "RuleLevelCascadeMode") ruleLevelStop = isStop;
                else if (mn == "ClassLevelCascadeMode") classLevelStop = isStop;
                continue;
            }

            if (stmt is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax inv })
                continue;

            var root = GetRootMethodName(inv);
            if (root is "RuleForEach" or "Transform") return (propRules, true);

            if (root == "When" && GetRootArgCount(inv) == 2) return (propRules, true);

            var prop = ParseChain(inv, builderParam, deps);
            if (prop is not null) propRules.Add(prop);
        }

        return (propRules, false);
    }

    private static string? GetRootMethodName(InvocationExpressionSyntax inv)
    {
        var cur = inv;
        while (cur.Expression is MemberAccessExpressionSyntax ma)
        {
            if (ma.Expression is not InvocationExpressionSyntax inner) return ma.Name.Identifier.Text;
            cur = inner;
        }
        return null;
    }

    private static int GetRootArgCount(InvocationExpressionSyntax inv)
    {
        var cur = inv;
        while (cur.Expression is MemberAccessExpressionSyntax ma)
        {
            if (ma.Expression is not InvocationExpressionSyntax inner)
                return cur.ArgumentList.Arguments.Count;
            cur = inner;
        }
        return 0;
    }

    // Walk chain from tail to head, buffering WithDetails / When / Unless
    private static PropertyRuleInfo? ParseChain(
        InvocationExpressionSyntax tail,
        string builderParam,
        List<DependencyInfo> deps)
    {
        string? propertyName = null;
        var rules = new List<RuleCallInfo>();
        string? pendingDetails = null;
        string? pendingCond = null;
        bool pendingUnless = false;

        var cur = tail;
        while (cur is not null)
        {
            if (cur.Expression is not MemberAccessExpressionSyntax ma) break;
            var method = ma.Name.Identifier.Text;

            if (method == "RuleFor")
            {
                // Extract property name from x => x.PropName
                if (cur.ArgumentList.Arguments.Count > 0
                    && cur.ArgumentList.Arguments[0].Expression is SimpleLambdaExpressionSyntax sl
                    && sl.ExpressionBody is MemberAccessExpressionSyntax spa)
                {
                    propertyName = spa.Name.Identifier.Text;
                }
                break;
            }

            if (method == "WithDetails" && cur.ArgumentList.Arguments.Count > 0)
            {
                var raw = cur.ArgumentList.Arguments[0].Expression.ToString();
                // Strip surrounding quotes from string literal
                pendingDetails = raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"'
                    ? raw.Substring(1, raw.Length - 2)
                    : raw;
                goto MoveNext;
            }

            if (method is "When" or "Unless" && cur.ArgumentList.Arguments.Count == 1)
            {
                pendingCond = ExtractLambdaBody(cur.ArgumentList.Arguments[0].Expression, "target");
                pendingUnless = method == "Unless";
                goto MoveNext;
            }

            if (method == "DependentRules") return null; // abort class

            if (method is "Must" or "MustAsync")
            {
                if (cur.ArgumentList.Arguments.Count > 0)
                {
                    var arg = cur.ArgumentList.Arguments[0].Expression;
                    if (HasBlockBody(arg)) return null; // abort class
                    var body = InlineMust(arg, deps);
                    if (body is null) return null;
                    rules.Insert(0, new RuleCallInfo(method, new[] { body },
                        Pop(ref pendingDetails), Pop(ref pendingCond), PopBool(ref pendingUnless),
                        isAsync: method == "MustAsync"));
                }
                goto MoveNext;
            }

            // Standard rule
            {
                var args = cur.ArgumentList.Arguments.Select(a => a.ToString()).ToArray();
                rules.Insert(0, new RuleCallInfo(method, args,
                    Pop(ref pendingDetails), Pop(ref pendingCond), PopBool(ref pendingUnless),
                    isAsync: false));
            }

        MoveNext:
            cur = ma.Expression as InvocationExpressionSyntax;
        }

        return propertyName is not null && rules.Count > 0
            ? new PropertyRuleInfo(propertyName, rules)
            : null;
    }

    private static string? Pop(ref string? s) { var v = s; s = null; return v; }
    private static bool PopBool(ref bool b) { var v = b; b = false; return v; }

    private static string? ExtractLambdaBody(ExpressionSyntax expr, string replaceWith)
    {
        if (expr is SimpleLambdaExpressionSyntax s && s.ExpressionBody is not null)
            return ReplaceWord(s.ExpressionBody.ToString(), s.Parameter.Identifier.Text, replaceWith);

        if (expr is ParenthesizedLambdaExpressionSyntax p && p.ExpressionBody is not null)
        {
            var last = p.ParameterList.Parameters.LastOrDefault()?.Identifier.Text;
            var body = p.ExpressionBody.ToString();
            return last is not null ? ReplaceWord(body, last, replaceWith) : body;
        }
        return null;
    }

    private static bool HasBlockBody(ExpressionSyntax expr)
        => expr is SimpleLambdaExpressionSyntax { Block: not null }
        or ParenthesizedLambdaExpressionSyntax { Block: not null };

    private static string? InlineMust(ExpressionSyntax expr, List<DependencyInfo> deps)
    {
        string? p1 = null, p2 = null, body = null;

        if (expr is SimpleLambdaExpressionSyntax sl && sl.ExpressionBody is not null)
        {
            p1 = sl.Parameter.Identifier.Text;
            body = sl.ExpressionBody.ToString();
        }
        else if (expr is ParenthesizedLambdaExpressionSyntax pl && pl.ExpressionBody is not null)
        {
            var ps = pl.ParameterList.Parameters;
            if (ps.Count >= 1) p1 = ps[0].Identifier.Text;
            if (ps.Count >= 2) p2 = ps[1].Identifier.Text;
            body = pl.ExpressionBody.ToString();
        }

        if (body is null) return null;

        if (p1 is not null && p1 != "_") body = ReplaceWord(body, p1, "target");
        if (p2 is not null && p2 != "_") body = ReplaceWord(body, p2, "value");

        // Replace dependency param names → _fieldName
        foreach (var dep in deps)
            body = ReplaceWord(body, dep.ParamName, "_" + dep.ParamName);

        return body;
    }

    private static string ReplaceWord(string text, string word, string replacement)
        => Regex.Replace(text, $@"\b{Regex.Escape(word)}\b", replacement);

    // -------------------------------------------------------------------------
    // Phase 3: emit
    // -------------------------------------------------------------------------

    private static void Emit(SourceProductionContext ctx, FluentValidatorInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Lite.Validation;");
        sb.AppendLine();

        bool hasNs = !string.IsNullOrEmpty(info.Namespace);
        if (hasNs) { sb.AppendLine($"namespace {info.Namespace}"); sb.AppendLine("{"); }

        var i = hasNs ? "    " : "";

        sb.AppendLine($"{i}partial class {info.ClassName}");
        sb.AppendLine($"{i}{{");

        // Fields (own deps; derived may repeat base param names so its Validate() can reference them)
        foreach (var dep in info.OwnDependencies)
            sb.AppendLine($"{i}    private readonly {dep.TypeName} _{dep.ParamName};");
        if (info.OwnDependencies.Count > 0) sb.AppendLine();

        // Public ctor (for DI); chain to base when applicable
        var ctorArgs = string.Join(", ", info.AllDependencies.Select(d => $"{d.TypeName} {d.ParamName}"));
        var baseArgs = info.BaseDependencies.Count > 0
            ? " : base(" + string.Join(", ", info.BaseDependencies.Select(d => d.ParamName)) + ")"
            : "";
        sb.AppendLine($"{i}    public {info.ClassName}({ctorArgs}){baseArgs}");
        sb.AppendLine($"{i}    {{");
        foreach (var dep in info.OwnDependencies)
            sb.AppendLine($"{i}        _{dep.ParamName} = {dep.ParamName};");
        sb.AppendLine($"{i}    }}");
        sb.AppendLine();

        // Do not use sealed so validators can be inherited (e.g. BaseOrderValidator -> DerivedOrderValidator).
        // IsAsync
        sb.AppendLine($"{i}    public override bool IsAsync => {(info.HasAsyncRules ? "true" : "false")};");
        sb.AppendLine();

        // Validate()
        sb.AppendLine($"{i}    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{i}    public override ValidationResult Validate({info.TargetTypeFullName} target)");
        sb.AppendLine($"{i}    {{");
        sb.AppendLine($"{i}        var result = new ValidationResult();");
        EmitBody(sb, info, $"{i}        ", async: false);
        sb.AppendLine($"{i}        return result;");
        sb.AppendLine($"{i}    }}");
        sb.AppendLine();

        // ValidateAsync()
        if (info.HasAsyncRules)
        {
            sb.AppendLine($"{i}    public override async global::System.Threading.Tasks.ValueTask<ValidationResult> ValidateAsync(");
            sb.AppendLine($"{i}        {info.TargetTypeFullName} target, global::System.Threading.CancellationToken cancellationToken = default)");
            sb.AppendLine($"{i}    {{");
            sb.AppendLine($"{i}        var result = new ValidationResult();");
            EmitBody(sb, info, $"{i}        ", async: true);
            sb.AppendLine($"{i}        return result;");
            sb.AppendLine($"{i}    }}");
        }
        else
        {
            sb.AppendLine($"{i}    public override global::System.Threading.Tasks.ValueTask<ValidationResult> ValidateAsync(");
            sb.AppendLine($"{i}        {info.TargetTypeFullName} target, global::System.Threading.CancellationToken cancellationToken = default)");
            sb.AppendLine($"{i}        => new global::System.Threading.Tasks.ValueTask<ValidationResult>(Validate(target));");
        }

        // Email helper
        if (info.PropertyRules.Any(p => p.Rules.Any(r => r.RuleType == "EmailAddress")))
        {
            sb.AppendLine();
            sb.AppendLine($"{i}    private static bool IsValidEmail_Gen(string? v)");
            sb.AppendLine($"{i}    {{");
            sb.AppendLine($"{i}        if (string.IsNullOrEmpty(v)) return false;");
            sb.AppendLine($"{i}        var at = v!.IndexOf('@');");
            sb.AppendLine($"{i}        return at > 0 && at < v.Length - 1 && v.IndexOf('.', at) > at + 1;");
            sb.AppendLine($"{i}    }}");
        }

        sb.AppendLine($"{i}}}");
        if (hasNs) sb.AppendLine("}");

        ctx.AddSource($"{info.ClassName}.g.cs", sb.ToString());
    }

    private static void EmitBody(StringBuilder sb, FluentValidatorInfo info, string ind, bool async)
    {
        foreach (var prop in info.PropertyRules)
        {
            var label = $"_end_{prop.PropertyName}";
            // Only emit the label if CascadeStop is active (otherwise goto is never emitted)
            bool needsLabel = info.RuleLevelCascadeStop && prop.Rules.Count > 1;

            sb.AppendLine($"{ind}// Validation for {prop.PropertyName}");
            sb.AppendLine($"{ind}#region Property {prop.PropertyName}");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    var value = target.{prop.PropertyName};");

            foreach (var rule in prop.Rules)
            {
                var ri = ind + "    ";
                if (rule.ConditionBody is not null)
                {
                    var cond = rule.IsUnless ? $"!({rule.ConditionBody})" : rule.ConditionBody;
                    sb.AppendLine($"{ind}    if ({cond})");
                    sb.AppendLine($"{ind}    {{");
                    ri = ind + "        ";
                }

                var gotoStr = info.RuleLevelCascadeStop ? $" goto {label};" : "";
                EmitRule(sb, rule, prop.PropertyName, "value", ri, gotoStr, async);

                if (rule.ConditionBody is not null)
                    sb.AppendLine($"{ind}    }}");
            }

            if (needsLabel) sb.AppendLine($"{ind}    {label}:;");
            sb.AppendLine($"{ind}}}");
            sb.AppendLine($"{ind}#endregion");

            if (info.ClassLevelCascadeStop)
                sb.AppendLine($"{ind}if (!result.IsSuccess) return result;");
        }
    }

    private static void EmitRule(StringBuilder sb, RuleCallInfo r,
        string prop, string valueVar, string ind, string gotoStr, bool async)
    {
        var d = r.Details;
        switch (r.RuleType)
        {
            case "NotNull":
                sb.AppendLine($"{ind}if ({valueVar} is null) {{ result.Add(\"{prop}\", \"{d ?? "Must not be null"}\");{gotoStr} }}");
                break;
            case "Null":
                sb.AppendLine($"{ind}if ({valueVar} is not null) {{ result.Add(\"{prop}\", \"{d ?? "Must be null"}\"); }}");
                break;
            case "NotEmpty":
                // V1: assumes string property (generates string.IsNullOrWhiteSpace)
                sb.AppendLine($"{ind}if (string.IsNullOrWhiteSpace({valueVar})) {{ result.Add(\"{prop}\", \"{d ?? "Must not be empty"}\"); }}");
                break;
            case "GreaterThan":
                sb.AppendLine($"{ind}if (!({valueVar} > {A(r, 0)})) {{ result.Add(\"{prop}\", \"{d ?? $"Must be > {A(r, 0)}"}\"); }}");
                break;
            case "GreaterThanOrEqualTo":
                sb.AppendLine($"{ind}if (!({valueVar} >= {A(r, 0)})) {{ result.Add(\"{prop}\", \"{d ?? $"Must be >= {A(r, 0)}"}\"); }}");
                break;
            case "LessThan":
                sb.AppendLine($"{ind}if (!({valueVar} < {A(r, 0)})) {{ result.Add(\"{prop}\", \"{d ?? $"Must be < {A(r, 0)}"}\"); }}");
                break;
            case "LessThanOrEqualTo":
                sb.AppendLine($"{ind}if (!({valueVar} <= {A(r, 0)})) {{ result.Add(\"{prop}\", \"{d ?? $"Must be <= {A(r, 0)}"}\"); }}");
                break;
            case "InclusiveBetween":
                sb.AppendLine($"{ind}if (!({valueVar} >= {A(r, 0)} && {valueVar} <= {A(r, 1)})) {{ result.Add(\"{prop}\", \"{d ?? $"Must be in [{A(r, 0)}, {A(r, 1)}]"}\"); }}");
                break;
            case "ExclusiveBetween":
                sb.AppendLine($"{ind}if (!({valueVar} > {A(r, 0)} && {valueVar} < {A(r, 1)})) {{ result.Add(\"{prop}\", \"{d ?? $"Must be in ({A(r, 0)}, {A(r, 1)})"}\"); }}");
                break;
            case "Length":
                sb.AppendLine($"{ind}if ({valueVar} is null || {valueVar}.Length < {A(r, 0)} || {valueVar}.Length > {A(r, 1)}) {{ result.Add(\"{prop}\", \"{d ?? $"Length must be [{A(r, 0)}, {A(r, 1)}]"}\"); }}");
                break;
            case "MinimumLength":
                sb.AppendLine($"{ind}if ({valueVar} is null || {valueVar}.Length < {A(r, 0)}) {{ result.Add(\"{prop}\", \"{d ?? $"Min length: {A(r, 0)}"}\"); }}");
                break;
            case "MaximumLength":
                sb.AppendLine($"{ind}if ({valueVar} is not null && {valueVar}.Length > {A(r, 0)}) {{ result.Add(\"{prop}\", \"{d ?? $"Max length: {A(r, 0)}"}\"); }}");
                break;
            case "EmailAddress":
                sb.AppendLine($"{ind}if (!IsValidEmail_Gen({valueVar})) {{ result.Add(\"{prop}\", \"{d ?? "Must be a valid email"}\"); }}");
                break;
            case "Must":
                sb.AppendLine($"{ind}if (!({A(r, 0)})) {{ result.Add(\"{prop}\", \"{d ?? "Validation failed"}\"); }}");
                break;
            case "MustAsync":
                if (async)
                    sb.AppendLine($"{ind}if (!(await ({A(r, 0)}).ConfigureAwait(false))) {{ result.Add(\"{prop}\", \"{d ?? "Validation failed"}\"); }}");
                // else: skip in sync Validate() — IsAsync=true ensures ValidateAsync() is always called
                break;
        }
    }

    private static string A(RuleCallInfo r, int i)
        => i < r.Arguments.Count ? r.Arguments[i] : "default";
}

// -------------------------------------------------------------------------
// Data models
// -------------------------------------------------------------------------

internal sealed class FluentValidatorInfo
{
    public string Namespace { get; }
    public string ClassName { get; }
    public string TargetTypeFullName { get; }
    public IReadOnlyList<DependencyInfo> BaseDependencies { get; }
    public IReadOnlyList<DependencyInfo> OwnDependencies { get; }
    public IReadOnlyList<DependencyInfo> AllDependencies { get; }
    public IReadOnlyList<PropertyRuleInfo> PropertyRules { get; }
    public bool HasAsyncRules { get; }
    public bool RuleLevelCascadeStop { get; }
    public bool ClassLevelCascadeStop { get; }

    public FluentValidatorInfo(
        string ns, string className, string targetType,
        IReadOnlyList<DependencyInfo> baseDeps, IReadOnlyList<DependencyInfo> ownDeps, IReadOnlyList<DependencyInfo> allDeps,
        IReadOnlyList<PropertyRuleInfo> propRules,
        bool hasAsync, bool ruleLevelStop, bool classLevelStop)
    {
        Namespace = ns; ClassName = className; TargetTypeFullName = targetType;
        BaseDependencies = baseDeps; OwnDependencies = ownDeps; AllDependencies = allDeps;
        PropertyRules = propRules;
        HasAsyncRules = hasAsync; RuleLevelCascadeStop = ruleLevelStop; ClassLevelCascadeStop = classLevelStop;
    }
}

internal sealed class DependencyInfo
{
    public string TypeName { get; }
    public string ParamName { get; }
    public DependencyInfo(string typeName, string paramName) { TypeName = typeName; ParamName = paramName; }
}

internal sealed class PropertyRuleInfo
{
    public string PropertyName { get; }
    public IReadOnlyList<RuleCallInfo> Rules { get; }
    public PropertyRuleInfo(string name, IReadOnlyList<RuleCallInfo> rules) { PropertyName = name; Rules = rules; }
}

internal sealed class RuleCallInfo
{
    public string RuleType { get; }
    public IReadOnlyList<string> Arguments { get; }
    public string? Details { get; }
    public string? ConditionBody { get; }
    public bool IsUnless { get; }
    public bool IsAsync { get; }

    public RuleCallInfo(string ruleType, IReadOnlyList<string> args,
        string? details, string? conditionBody, bool isUnless, bool isAsync)
    {
        RuleType = ruleType; Arguments = args; Details = details;
        ConditionBody = conditionBody; IsUnless = isUnless; IsAsync = isAsync;
    }
}
