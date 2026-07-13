using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class GeneratedSourceConventions
{
    internal const string ToolName = "RoyalCode.SmartSelector.Generators";

    internal static readonly string ToolVersion =
        typeof(GeneratedSourceConventions).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    internal static readonly string GeneratedCodeAttributeLine =
        $"[global::System.CodeDom.Compiler.GeneratedCode(\"{ToolName}\", \"{ToolVersion}\")]";

    internal static void ApplyDeclaredAccessibility(
        ClassGenerator generator,
        ISymbol? symbol)
    {
        switch (symbol?.DeclaredAccessibility ?? Accessibility.Public)
        {
            case Accessibility.Public:
                generator.Modifiers.Public();
                break;
            case Accessibility.Internal:
                generator.Modifiers.Internal();
                break;
            case Accessibility.Protected:
                generator.Modifiers.Protected();
                break;
            case Accessibility.Private:
                generator.Modifiers.Private();
                break;
            case Accessibility.ProtectedOrInternal:
                generator.Modifiers.Protected();
                generator.Modifiers.Internal();
                break;
            case Accessibility.ProtectedAndInternal:
                generator.Modifiers.Private();
                generator.Modifiers.Protected();
                break;
            default:
                generator.Modifiers.Public();
                break;
        }
    }

    internal static void ApplyContainingTypes(
        ClassGenerator generator,
        INamedTypeSymbol? symbol)
    {
        if (symbol?.ContainingType is null)
            return;

        var containingTypes = new Stack<INamedTypeSymbol>();
        for (var containingType = symbol.ContainingType;
             containingType is not null;
             containingType = containingType.ContainingType)
        {
            containingTypes.Push(containingType);
        }

        while (containingTypes.Count > 0)
        {
            var containingSymbol = containingTypes.Pop();
            var containingGenerator = new ContainingTypeGenerator(
                containingSymbol.Name,
                GetTypeDeclarationKeyword(containingSymbol));
            ApplyDeclaredAccessibility(containingGenerator.Modifiers, containingSymbol);
            containingGenerator.Modifiers.Partial();
            generator.ContainingTypes.Add(containingGenerator);
        }
    }

    internal static string QualifiedTypeName(TypeDescriptor type) =>
        type.Symbol is INamedTypeSymbol symbol
            ? QualifiedTypeName(symbol)
            : type.Name;

    internal static string TypeIdentityIdentifier(TypeDescriptor type)
    {
        if (type.Symbol is not INamedTypeSymbol symbol)
            return type.Name;

        var names = new Stack<string>();
        for (var current = symbol; current is not null; current = current.ContainingType)
            names.Push(current.Name);
        return string.Join("_", names);
    }

    private static string QualifiedTypeName(INamedTypeSymbol symbol)
    {
        var names = new Stack<string>();
        for (var current = symbol; current is not null; current = current.ContainingType)
            names.Push(current.Name);
        return string.Join(".", names);
    }

    private static string GetTypeDeclarationKeyword(INamedTypeSymbol symbol) =>
        symbol switch
        {
            { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
            { IsRecord: true } => "record",
            { TypeKind: TypeKind.Struct } => "struct",
            _ => "class",
        };

    private static void ApplyDeclaredAccessibility(
        ModifiersGenerator modifiers,
        ISymbol symbol)
    {
        switch (symbol.DeclaredAccessibility)
        {
            case Accessibility.Public:
                modifiers.Public();
                break;
            case Accessibility.Internal:
                modifiers.Internal();
                break;
            case Accessibility.Protected:
                modifiers.Protected();
                break;
            case Accessibility.Private:
                modifiers.Private();
                break;
            case Accessibility.ProtectedOrInternal:
                modifiers.Protected();
                modifiers.Internal();
                break;
            case Accessibility.ProtectedAndInternal:
                modifiers.Private();
                modifiers.Protected();
                break;
        }
    }

    internal static void ApplyRequiredNamespaces(ClassGenerator generator)
    {
        generator.Generating += static (_, builder) =>
        {
            // Arquivos auto-generated têm o contexto nullable desabilitado pelo compilador;
            // "enable annotations" preserva as anotações emitidas sem produzir warnings de
            // nulabilidade dentro do código gerado.
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#nullable enable annotations");
            builder.AppendLine();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine("using System.Collections.Generic;");
        };
    }

    /// <summary>
    /// Aplica ao nome do tipo a anotação nullable modelada pelo <see cref="TypeDescriptor"/> externo,
    /// quando o nome ainda não a carrega (tipos não especiais perdem o '?' na criação por símbolo).
    /// </summary>
    internal static TypeDescriptor PreserveNullableAnnotation(TypeDescriptor type)
    {
        if (type.NullableAnnotation != NullableAnnotation.Annotated ||
            type.Name.EndsWith("?", StringComparison.Ordinal))
        {
            return type;
        }

        return new TypeDescriptor(
            type.Name + "?",
            type.Namespaces,
            type.Symbol,
            type.IsNullable,
            type.NullableAnnotation);
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
