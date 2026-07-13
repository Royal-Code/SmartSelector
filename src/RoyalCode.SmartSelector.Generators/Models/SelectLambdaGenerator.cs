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
                    if (propMatch.Origin.Type.UnderlyingType.EndsWith("[]", StringComparison.Ordinal))
                        sb.Append(" ? Array.Empty<").Append(GenericItemName(propMatch.Origin.Type)).Append(">() : ");
                    else
                        sb.Append(" ? new List<").Append(GenericItemName(propMatch.Origin.Type)).Append(">() : ");
                    break;
            }

            var assign = new AssignProperties(propMatch.Origin, propMatch.Target!, assignDescriptor.InnerSelection);
            assignGenerator(sb, indent, param, assign);

            // check ToList
            if (assignDescriptor.AssignType == AssignType.Select &&
                propMatch.Origin.Type.UnderlyingType.EndsWith("[]", StringComparison.Ordinal))
            {
                sb.Append('.').Append("ToArray()");
            }
            else if (assignDescriptor.RequireToList)
            {
                sb.Append('.').Append("ToList()");
            }

            sb.Append(',');
        }
        sb.Length--;
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
        var inner = assign.InnerSelection;
        if (inner is null)
            throw new ArgumentException("Inner selection is null.", nameof(inner));

        sb.Append(param).Append('.').Append(assign.Target.Path).Append(".Select(");

        var nextParam = (char)(param + 1);

        sb.Append(nextParam).Append(" => new ").AppendLine(GenericItemName(assign.Origin.Type))
            .Indent(indent).Append('{');

        GeneratePropertyCode(indent + 1, sb, nextParam, inner.PropertyMatches);

        sb.AppendLine().Indent(indent).Append("})");
    }

    private delegate void AssignGenerator(StringBuilder sb, int indent, char param, AssignProperties assign);

    private readonly ref struct AssignProperties(PropertySnapshot origin, PropertyPathSnapshot target, MatchSelectionSnapshot? inner)
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
    }
}
