using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace System;

internal static class AttributeSyntaxExtensions
{
    public static IEnumerable<string> GetConstructorStrings(this AttributeSyntax attr)
    {
        if (attr.ArgumentList is not { Arguments.Count: > 0 }) yield break;
        foreach (var arg in attr.ArgumentList.Arguments)
        {
            if (arg.NameEquals is not null) continue; // skip named
            if (TryReadSingle(arg.Expression, out var value))
                yield return value;
            else if (arg.Expression is ArrayCreationExpressionSyntax acs && acs.Initializer is not null)
            {
                foreach (var expr in acs.Initializer.Expressions)
                    if (TryReadSingle(expr, out var v)) yield return v;
            }
            else if (arg.Expression is ImplicitArrayCreationExpressionSyntax iacs && iacs.Initializer is not null)
            {
                foreach (var expr in iacs.Initializer.Expressions)
                    if (TryReadSingle(expr, out var v)) yield return v;
            }
            else if (arg.Expression is InitializerExpressionSyntax init)
            {
                foreach (var expr in init.Expressions)
                    if (TryReadSingle(expr, out var v)) yield return v;
            }
        }
    }

    public static IEnumerable<string> GetConstructorStringSet(this AttributeSyntax attr)
        => attr.GetConstructorStrings().Distinct(StringComparer.Ordinal);

    public static IEnumerable<string> GetNamedArgumentStrings(this AttributeSyntax attr, string name)
    {
        if (attr.ArgumentList is not { Arguments.Count: > 0 }) yield break;
        foreach (var arg in attr.ArgumentList.Arguments)
        {
            if (arg.NameEquals?.Name.Identifier.Text != name) continue;
            var expr = arg.Expression;
            if (TryReadSingle(expr, out var value))
            {
                yield return value;
                continue;
            }
            if (expr is ArrayCreationExpressionSyntax acs && acs.Initializer is not null)
            {
                foreach (var item in acs.Initializer.Expressions)
                    if (TryReadSingle(item, out var v)) yield return v;
            }
            else if (expr is ImplicitArrayCreationExpressionSyntax iacs && iacs.Initializer is not null)
            {
                foreach (var item in iacs.Initializer.Expressions)
                    if (TryReadSingle(item, out var v)) yield return v;
            }
            else if (expr is InitializerExpressionSyntax init)
            {
                foreach (var item in init.Expressions)
                    if (TryReadSingle(item, out var v)) yield return v;
            }
        }
    }

    public static IEnumerable<string> GetNamedArgumentStringSet(this AttributeSyntax attr, string name)
        => attr.GetNamedArgumentStrings(name).Distinct(StringComparer.Ordinal);

    private static bool TryReadSingle(ExpressionSyntax expr, out string value)
    {
        switch (expr)
        {
            case LiteralExpressionSyntax { Token.Value: string s }:
                value = s; return true;
            case InvocationExpressionSyntax { Expression: IdentifierNameSyntax id } inv when id.Identifier.Text == "nameof":
                if (inv.ArgumentList.Arguments.Count == 1)
                {
                    var inner = inv.ArgumentList.Arguments[0].Expression;
                    if (inner is IdentifierNameSyntax ins)
                    { value = ins.Identifier.Text; return true; }
                    if (inner is MemberAccessExpressionSyntax ma)
                    { value = ma.Name.Identifier.Text; return true; }
                }
                break;
        }
        value = string.Empty; return false;
    }
}
