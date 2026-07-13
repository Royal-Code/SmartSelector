using Microsoft.CodeAnalysis;

namespace RoyalCode.SmartSelector.Generators.Models;

/// <summary>
/// Classificação de cada atribuição da projeção. A política considera anotações nullable
/// de referência e <see cref="Nullable{T}"/>; código oblivious mantém o comportamento anterior.
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

    private static bool IsNonNullableValueType(TypeDescriptor type) =>
        !type.IsNullable && type.Symbol is { IsValueType: true };

    private static bool IsNonNullableDestination(TypeDescriptor type) =>
        IsNonNullableReference(type) || IsNonNullableValueType(type);

    private static bool IsNullableSource(TypeDescriptor type) =>
        type.IsNullable || IsNullableReference(type);

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
                    // navegação por caminho anulável (flattening): destinos nullable propagam null;
                    // para Nullable<T>, o emissor usa default(T?) no branch nulo.
                    if (DestinationAcceptsNull(destination))
                        return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                    if (IsNonNullableDestination(destination))
                        return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                    return new(NullAssignmentKind.None, [], sourcePath);
                }

                // folha anulável em destino não anulável: comportamento mantido + warning (DF5)
                if (IsNullableSource(leaf.PropertyType.Type) && IsNonNullableDestination(destination))
                    return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                return new(NullAssignmentKind.None, [], sourcePath);

            default:
                // SimpleCast/NullableTernary(Cast): preserva a mecânica de conversão existente,
                // mas aplica a mesma política direcional ao caminho e à folha nullable.
                if (nullChecks.Count > 0)
                {
                    if (DestinationAcceptsNull(destination))
                        return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                    if (IsNonNullableDestination(destination))
                        return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                }
                if (IsNullableSource(leaf.PropertyType.Type) && IsNonNullableDestination(destination))
                    return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                return new(NullAssignmentKind.None, [], sourcePath);
        }
    }

    internal static NullAssignmentClassification Classify(PropertyMatchSnapshot propertyMatch)
    {
        var target = propertyMatch.Target;
        var assignment = propertyMatch.Assignment;
        if (target is null || assignment is null)
            return new(NullAssignmentKind.None, [], string.Empty);

        var destination = propertyMatch.Origin.Type;
        var path = target.Properties;
        var leaf = path[path.Count - 1];
        var sourcePath = target.Path;
        var includeLeaf = assignment.AssignType is AssignType.NewInstance or AssignType.Select;
        var nullChecks = new List<string>();
        var currentPath = string.Empty;
        for (var index = 0; index < path.Count; index++)
        {
            var node = path[index];
            currentPath = currentPath.Length == 0 ? node.Name : $"{currentPath}.{node.Name}";
            if (!includeLeaf && index == path.Count - 1)
                break;
            if (node.Type.IsNullableReference)
                nullChecks.Add(currentPath);
        }

        var destinationAcceptsNull = destination.MayBeNull;
        var nonNullableReference = destination.IsNonNullableReference;
        var nonNullableDestination = nonNullableReference || (!destination.IsNullable && destination.IsValueType);
        var nullableSource = leaf.Type.MayBeNull;

        switch (assignment.AssignType)
        {
            case AssignType.Select:
                if (nullChecks.Count == 0) return new(NullAssignmentKind.None, [], sourcePath);
                if (destinationAcceptsNull) return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                if (nonNullableReference) return new(NullAssignmentKind.EmptyCollectionFallback, nullChecks, sourcePath);
                return new(NullAssignmentKind.None, [], sourcePath);
            case AssignType.NewInstance:
                if (nullChecks.Count == 0) return new(NullAssignmentKind.None, [], sourcePath);
                if (destinationAcceptsNull) return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                if (nonNullableReference) return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                return new(NullAssignmentKind.None, [], sourcePath);
            case AssignType.Direct:
                if (nullChecks.Count > 0)
                {
                    if (destinationAcceptsNull) return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                    if (nonNullableDestination) return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                    return new(NullAssignmentKind.None, [], sourcePath);
                }
                return nullableSource && nonNullableDestination
                    ? new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath)
                    : new(NullAssignmentKind.None, [], sourcePath);
            default:
                if (nullChecks.Count > 0)
                {
                    if (destinationAcceptsNull) return new(NullAssignmentKind.PropagateNull, nullChecks, sourcePath);
                    if (nonNullableDestination) return new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath);
                }
                return nullableSource && nonNullableDestination
                    ? new(NullAssignmentKind.WarnUnsafe, nullChecks, sourcePath)
                    : new(NullAssignmentKind.None, [], sourcePath);
        }
    }
}
