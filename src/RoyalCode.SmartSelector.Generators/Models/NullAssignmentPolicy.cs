using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Models;

/// <summary>
/// Classificação de cada atribuição da projeção. A política só atua quando há anotação nullable
/// de referência (código oblivious mantém o comportamento anterior).
/// </summary>
internal enum NullAssignmentKind
{
    /// <summary>Sem tratamento: comportamento anterior.</summary>
    None,

    /// <summary>Origem anulável e destino anulável: condicional que propaga null (DF5).</summary>
    PropagateNull,

    /// <summary>Coleção anulável e destino não anulável: fallback de coleção vazia (DF18, Info RCSS016).</summary>
    EmptyCollectionFallback,

    /// <summary>Origem anulável e destino não anulável: comportamento mantido + warning RCSS015 (DF5).</summary>
    WarnUnsafe,
}

internal readonly struct NullAssignmentClassification(
    NullAssignmentKind kind,
    IReadOnlyList<string> nullCheckPaths,
    string sourcePath)
{
    public NullAssignmentKind Kind { get; } = kind;

    /// <summary>Caminhos parciais (sem o parâmetro) que exigem verificação de null, da raiz à folha.</summary>
    public IReadOnlyList<string> NullCheckPaths { get; } = nullCheckPaths;

    /// <summary>Caminho completo da origem, para mensagens de diagnóstico.</summary>
    public string SourcePath { get; } = sourcePath;
}

internal static class NullAssignmentPolicy
{
    /// <summary>
    /// Anulável por anotação de referência; <see cref="TypeDescriptor.IsNullable"/> (Nullable&lt;T&gt;)
    /// continua tratado pelos assign types existentes.
    /// </summary>
    internal static bool IsNullableReference(TypeDescriptor type) =>
        !type.IsNullable &&
        (type.NullableAnnotation == NullableAnnotation.Annotated ||
         type.Name.EndsWith("?", StringComparison.Ordinal));

    /// <summary>
    /// O destino aceita null quando é referência anulável ou quando foi vinculado como
    /// Nullable&lt;T&gt; (caso de 'X?' com X ainda não gerado pelo AutoDetails).
    /// </summary>
    private static bool DestinationAcceptsNull(TypeDescriptor type) =>
        type.IsNullable ||
        type.NullableAnnotation == NullableAnnotation.Annotated ||
        type.Name.EndsWith("?", StringComparison.Ordinal);

    private static bool IsNonNullableReference(TypeDescriptor type) =>
        !type.IsNullable && type.NullableAnnotation == NullableAnnotation.NotAnnotated;

    private static bool IsReferenceType(TypeDescriptor type) =>
        type.Symbol is { IsValueType: false };

    internal static NullAssignmentClassification Classify(PropertyMatch propertyMatch)
    {
        var target = propertyMatch.Target;
        var assignDescriptor = propertyMatch.AssignDescriptor;
        if (target is null || assignDescriptor is null)
            return new(NullAssignmentKind.None, [], string.Empty);

        var destination = propertyMatch.Origin.Type;
        var path = target.ToEnumerablePath().ToList();
        var leaf = path[path.Count - 1];
        var sourcePath = string.Join(".", path.Select(static node => node.PropertyType.Name));

        // caminhos parciais anuláveis; a folha só entra para objetos e coleções
        var includeLeaf = assignDescriptor.AssignType is AssignType.NewInstance or AssignType.Select;
        var nullChecks = new List<string>();
        var currentPath = string.Empty;
        foreach (var node in path)
        {
            currentPath = currentPath.Length == 0
                ? node.PropertyType.Name
                : $"{currentPath}.{node.PropertyType.Name}";

            if (!includeLeaf && ReferenceEquals(node, leaf))
                break;

            if (IsNullableReference(node.PropertyType.Type))
                nullChecks.Add(currentPath);
        }

        switch (assignDescriptor.AssignType)
        {
            case AssignType.Select:
                if (nullChecks.Count == 0)
                    return new(NullAssignmentKind.None, [], sourcePath);
                if (DestinationAcceptsNull(destination))
                    return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                if (IsNonNullableReference(destination))
                    return new(NullAssignmentKind.EmptyCollectionFallback, nullChecks, sourcePath);
                return new(NullAssignmentKind.None, [], sourcePath);

            case AssignType.NewInstance:
                if (nullChecks.Count == 0)
                    return new(NullAssignmentKind.None, [], sourcePath);
                if (DestinationAcceptsNull(destination))
                    return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                if (IsNonNullableReference(destination))
                    return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                return new(NullAssignmentKind.None, [], sourcePath);

            case AssignType.Direct:
                if (nullChecks.Count > 0)
                {
                    // navegação por caminho anulável (flattening); o condicional exige
                    // destino e folha de referência (null não é tipável para value types na expression tree)
                    if (DestinationAcceptsNull(destination) && IsReferenceType(leaf.PropertyType.Type))
                        return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                    if (IsNonNullableReference(destination) || !IsReferenceType(leaf.PropertyType.Type))
                        return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                    return new(NullAssignmentKind.None, [], sourcePath);
                }

                // folha anulável em destino não anulável: comportamento mantido + warning (DF5)
                if (IsNullableReference(leaf.PropertyType.Type) && IsNonNullableReference(destination))
                    return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                return new(NullAssignmentKind.None, [], sourcePath);

            default:
                // SimpleCast/NullableTernary(Cast): mecânica existente de value types;
                // caminho anulável não coberto pelo condicional recebe warning
                if (nullChecks.Count > 0)
                    return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                return new(NullAssignmentKind.None, [], sourcePath);
        }
    }
}
