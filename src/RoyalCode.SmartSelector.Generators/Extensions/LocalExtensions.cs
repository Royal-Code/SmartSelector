using Microsoft.CodeAnalysis;

namespace System;

internal static class LocalExtensions
{
    public static TypeDescriptor CreateTypeDescriptor(this ITypeSymbol typeSymbol)
    {
        string text = typeSymbol.ToString();
        bool flag = false;
        INamedTypeSymbol? namedTypeSymbol = typeSymbol as INamedTypeSymbol;
        if (text[text.Length - 1] == '?')
        {
            flag = namedTypeSymbol != null && namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        string[] namespaces = typeSymbol.GetNamespaces().ToArray();
        if (namedTypeSymbol != null)
        {
            if (flag && namedTypeSymbol.TypeArguments[0] is INamedTypeSymbol symbol)
            {
                text = symbol.GetName() + "?";
            }
            else if (namedTypeSymbol.SpecialType == SpecialType.None)
            {
                text = namedTypeSymbol.GetName();
            }
            else
            {
                text = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
        }

        return new TypeDescriptor(text, namespaces, typeSymbol, flag);
    }
}
