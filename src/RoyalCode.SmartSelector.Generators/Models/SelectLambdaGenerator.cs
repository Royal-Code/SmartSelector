using System.Text;

namespace RoyalCode.SmartSelector.Generators.Models;

internal class SelectLambdaGenerator : ValueNode
{
    private readonly MatchSelectionSnapshot match;

    public SelectLambdaGenerator(MatchSelectionSnapshot match)
    {
        this.match = match;
    }

    public override string GetValue(int indent)
    {
        var sb = new StringBuilder();
        var param = 'a';

        return GetValue(indent, sb, param);
    }

    private string GetValue(int indent, StringBuilder sb, char param)
    {
        if (match.PropertyMatches.Count is 0)
            return sb.Append(param).Append(" => new ").AppendLine(match.OriginType.Name).Append("()").ToString();

        // ......... a => new T
        // { 
        sb.Append(param).Append(" => new ").AppendLine(match.OriginType.Name)
            .Indent(indent).Append('{');

        GeneratePropertyCode(indent + 1, sb, param, match.PropertyMatches);

        sb.AppendLine().Indent(indent).Append('}');

        return sb.ToString();
    }

    private static void GeneratePropertyCode(int indent, StringBuilder sb, char param, IReadOnlyList<PropertyMatchSnapshot> properties)
    {
        foreach (var propMatch in properties)
        {
            sb.AppendLine();

            //     PropertyName =
            sb.Indent(indent).Append(propMatch.Origin.Name).Append(" = ");

            var assignDescriptor = propMatch.Assignment!;
            var assignGenerator = GetAssignGenerator(assignDescriptor);

            // política de null (DF5/DF18): o prefixo condicional envolve a atribuição;
            // o ".ToList()" textual apendado depois permanece dentro do branch não nulo do ternário
            var classification = NullAssignmentPolicy.Classify(propMatch);
            switch (classification.Kind)
            {
                case NullAssignmentKind.PropagateNull:
                    AppendNullChecks(sb, param, classification.NullCheckPaths);
                    if (IsNullableValueType(propMatch.Origin.Type))
                        sb.Append(" ? default(").Append(propMatch.Origin.Type.Name).Append(") : ");
                    else
                        sb.Append(" ? null : ");
                    break;
                case NullAssignmentKind.EmptyCollectionFallback:
                    AppendNullChecks(sb, param, classification.NullCheckPaths);
                    AppendEmptyCollection(sb, assignDescriptor.Materialization, propMatch.Origin.Type);
                    break;
            }

            var assign = new AssignProperties(
                propMatch.Origin,
                propMatch.Target!,
                assignDescriptor.InnerSelection,
                assignDescriptor.ElementAssignment);
            assignGenerator(sb, indent, param, assign);

            AppendMaterialization(sb, assignDescriptor.Materialization);

            sb.Append(',');
        }
        sb.Length--;
    }

    /// <summary>
    /// Materializa o enumerável conforme o tipo declarado no destino, como resolvido pelo matching.
    /// </summary>
    private static void AppendMaterialization(StringBuilder sb, CollectionMaterialization materialization)
    {
        switch (materialization)
        {
            case CollectionMaterialization.List:
                sb.Append(".ToList()");
                break;
            case CollectionMaterialization.Array:
                sb.Append(".ToArray()");
                break;
            case CollectionMaterialization.HashSet:
                sb.Append(".ToHashSet()");
                break;
        }
    }

    /// <summary>
    /// Coleção vazia do mesmo tipo do destino, para o fallback de origem nula.
    /// </summary>
    private static void AppendEmptyCollection(
        StringBuilder sb, CollectionMaterialization materialization, TypeSnapshot type)
    {
        var item = GenericItemName(type);
        switch (materialization)
        {
            case CollectionMaterialization.Array:
                sb.Append(" ? Array.Empty<").Append(item).Append(">() : ");
                break;
            case CollectionMaterialization.HashSet:
                sb.Append(" ? new HashSet<").Append(item).Append(">() : ");
                break;
            default:
                sb.Append(" ? new List<").Append(item).Append(">() : ");
                break;
        }
    }

    private static string GenericItemName(TypeSnapshot type)
    {
        // extrai o argumento genérico ignorando a anotação nullable do tipo externo
        var name = type.UnderlyingType;
        if (name.EndsWith("[]", StringComparison.Ordinal))
            return name.Substring(0, name.Length - 2);
        var index = name.IndexOf('<');
        return index == -1 ? name : name.Substring(index + 1, name.Length - index - 2);
    }

    private static bool IsNullableValueType(TypeSnapshot type) => type.IsNullableValueType;

    private static void AppendNullChecks(StringBuilder sb, char param, IReadOnlyList<string> nullCheckPaths)
    {
        for (var i = 0; i < nullCheckPaths.Count; i++)
        {
            if (i > 0)
                sb.Append(" || ");
            sb.Append(param).Append('.').Append(nullCheckPaths[i]).Append(" == null");
        }
    }

    private static AssignGenerator GetAssignGenerator(AssignmentSnapshot assignDescriptor)
    {
        return assignDescriptor.AssignType switch
        {
            AssignType.Direct => AssignDirect,
            AssignType.SimpleCast => AssignCast,
            AssignType.NullableTernary => AssignNullableTernary,
            AssignType.NullableTernaryCast => AssignNullableTernaryCast,
            AssignType.NewInstance => AssignNewInstance,
            AssignType.Select => AssignSelect,
            _ => AssignDirect
        };
    }

    private static void AssignDirect(StringBuilder sb, int indent, char param, AssignProperties assign)
    {
        sb.Append(param).Append('.').Append(assign.Target.Path);
    }

    private static void AssignCast(StringBuilder sb, int indent, char param, AssignProperties assign)
    {
        sb.Append('(').Append(assign.Origin.Type.Name).Append(')');
        AssignDirect(sb, indent, param, assign);
    }

    private static void AssignNullableTernary(StringBuilder sb, int indent, char param, AssignProperties assign)
    {
        sb.Append(param).Append('.').Append(assign.Target.Path).Append(".HasValue ? ");
        sb.Append(param).Append('.').Append(assign.Target.Path).Append(".Value : default");
    }

    private static void AssignNullableTernaryCast(StringBuilder sb, int indent, char param, AssignProperties assign)
    {
        sb.Append(param).Append('.').Append(assign.Target.Path).Append(".HasValue ? ");
        sb.Append('(').Append(assign.Origin.Type.Name).Append(')');
        sb.Append(param).Append('.').Append(assign.Target.Path).Append(".Value : default");
    }

    private static void AssignNewInstance(StringBuilder sb, int indent, char param, AssignProperties assign)
    {
        var inner = assign.InnerSelection;
        if (inner is null)
            throw new ArgumentException("Inner selection is null.", nameof(inner));

        // o nome pode carregar a anotação nullable ('AddressDetails?'); a instância usa o tipo subjacente
        sb.Append("new ").AppendLine(assign.Origin.Type.UnderlyingType)
            .Indent(indent).Append("{");
        
        GeneratePropertyCode(indent + 1, sb, param, inner.PropertyMatches);

        sb.AppendLine().Indent(indent).Append('}');
    }

    private static void AssignSelect(StringBuilder sb, int indent, char param, AssignProperties assign)
    {
        var itemName = GenericItemName(assign.Origin.Type);
        var nextParam = (char)(param + 1);

        sb.Append(param).Append('.').Append(assign.Target.Path).Append(".Select(")
            .Append(nextParam).Append(" => ");

        // elementos mapeados: projeta um novo objeto com a seleção interna
        if (assign.InnerSelection is { } inner)
        {
            sb.Append("new ").AppendLine(itemName).Indent(indent).Append('{');
            GeneratePropertyCode(indent + 1, sb, nextParam, inner.PropertyMatches);
            sb.AppendLine().Indent(indent).Append("})");
            return;
        }

        // elementos que só precisam de conversão (enums equivalentes, por exemplo):
        // o corpo do lambda é o próprio elemento, com ou sem cast
        switch (assign.ElementAssignment?.AssignType)
        {
            case AssignType.SimpleCast:
                sb.Append('(').Append(itemName).Append(')').Append(nextParam).Append(')');
                break;
            case AssignType.Direct:
                sb.Append(nextParam).Append(')');
                break;
            default:
                throw new ArgumentException(
                    $"Cannot generate a Select for elements of '{itemName}': " +
                    "the assignment has neither an inner selection nor an element assignment.",
                    nameof(assign));
        }
    }

    private delegate void AssignGenerator(StringBuilder sb, int indent, char param, AssignProperties assign);

    private readonly ref struct AssignProperties(
        PropertySnapshot origin,
        PropertyPathSnapshot target,
        MatchSelectionSnapshot? inner,
        AssignmentSnapshot? elementAssignment)
    {
        /// <summary>
        /// The origin property type descriptor. (DTO property)
        /// </summary>
        public PropertySnapshot Origin { get; } = origin;

        /// <summary>
        /// The target property selection. (Entity property)
        /// </summary>
        public PropertyPathSnapshot Target { get; } = target;

        /// <summary>
        /// The inner selection of the target property. (Entity property)
        /// </summary>
        public MatchSelectionSnapshot? InnerSelection { get; } = inner;

        /// <summary>
        /// For a Select, how each element must be assigned when it is not a mapped object.
        /// </summary>
        public AssignmentSnapshot? ElementAssignment { get; } = elementAssignment;
    }
}
