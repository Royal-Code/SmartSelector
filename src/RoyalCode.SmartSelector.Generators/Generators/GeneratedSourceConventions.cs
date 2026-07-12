using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class GeneratedSourceConventions
{
    internal static void ApplyRequiredNamespaces(ClassGenerator generator)
    {
        generator.Generating += static (_, builder) =>
        {
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine("using System.Collections.Generic;");
        };
    }

    internal static string FileName(
        TypeDescriptor type,
        string generatedTypeName,
        string category)
    {
        var parts = new List<string>();
        if (type.Symbol is INamedTypeSymbol symbol)
        {
            if (!symbol.ContainingNamespace.IsGlobalNamespace)
            {
                parts.Add(symbol.ContainingNamespace.ToDisplayString());
            }
            else if (symbol.TypeKind == TypeKind.Error)
            {
                var descriptorNamespace = type.Namespaces.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(descriptorNamespace))
                {
                    parts.Add(descriptorNamespace);
                }
            }

            var containingTypes = new Stack<INamedTypeSymbol>();
            for (var containingType = symbol.ContainingType;
                 containingType is not null;
                 containingType = containingType.ContainingType)
            {
                containingTypes.Push(containingType);
            }

            while (containingTypes.Count > 0)
            {
                parts.Add(containingTypes.Pop().MetadataName);
            }

            parts.Add(symbol.Name == generatedTypeName ? symbol.MetadataName : generatedTypeName);
        }
        else
        {
            var typeNamespace = type.Namespaces.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(typeNamespace))
            {
                parts.Add(typeNamespace);
            }

            parts.Add(generatedTypeName);
        }

        parts.Add(category);
        return $"{string.Join(".", parts)}.g.cs";
    }
}
