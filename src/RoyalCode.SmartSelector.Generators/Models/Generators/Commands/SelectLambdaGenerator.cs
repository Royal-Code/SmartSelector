using RoyalCode.SmartSelector.Generators.Models.Descriptors;
using System.Text;

namespace RoyalCode.SmartSelector.Generators.Models.Generators.Commands;

internal class SelectLambdaGenerator : ValueNode
{
    private readonly MatchSelection match;

    public SelectLambdaGenerator(MatchSelection match)
    {
        this.match = match;
    }

    public override string GetValue(int ident)
    {
        var sb = new StringBuilder();
        var param = 'a';

        return GetValue(ident, sb, param);
    }

    private string GetValue(int ident, StringBuilder sb, char param)
    {
        // ......... a => new T
        // { 
        sb.Append(param).Append(" => new ").AppendLine(match.OriginType.Name)
            .Ident(ident).Append('{');

        foreach (var propMatch in match.PropertyMatches)
        {
            sb.AppendLine();

            //     PropertyName = 
            sb.IdentPlus(ident).Append(propMatch.Origin.Name).Append(" = ");

            var assignDescriptor = propMatch.AssignDescriptor!;
            var assignGenerator = GetAssignGenerator(assignDescriptor);

            var assign = new AssignProperties(propMatch.Origin, propMatch.Target!);
            assignGenerator(sb, ident + 1, param, assign);

            sb.Append(',');
        }

        sb.Length--;
        sb.AppendLine();
        sb.Ident(ident).Append('}');

        return sb.ToString();
    }

    private static AssignGenerator GetAssignGenerator(AssignDescriptor assignDescriptor)
    {
        if (assignDescriptor.IsEnumerable)
        {
            return assignDescriptor.AssignType switch
            {
                AssignType.Direct => assignDescriptor.RequireSelect
                    ? AssignSelectDirect
                    : AssignEnumerable,
                _ => AssignEnumerable
            };
        }

        return assignDescriptor.AssignType switch
        {
            AssignType.Direct => AssignDirect,
            AssignType.SimpleCast => AssignCast,
            AssignType.NullableTernary => AssignNullableTernary,
            AssignType.NullableTernaryCast => AssignNullableTernaryCast,
            _ => AssignDirect
        };
    }

    private static void AssignDirect(StringBuilder sb, int ident, char param, AssignProperties assign)
    {
        sb.Append(param).Append('.').Append(assign.Target.PropertyType.Name);
    }

    private static void AssignCast(StringBuilder sb, int ident, char param, AssignProperties assign)
    {
        sb.Append('(').Append(assign.Origin.Type.Name).Append(')');
        AssignDirect(sb, ident, param, assign);
    }

    private static void AssignNullableTernary(StringBuilder sb, int ident, char param, AssignProperties assign)
    {
        sb.Append(param).Append('.').Append(assign.Target.PropertyType.Name).Append(".HasValue ? ");
        sb.Append(param).Append('.').Append(assign.Target.PropertyType.Name).Append(".Value : default");
    }

    private static void AssignNullableTernaryCast(StringBuilder sb, int ident, char param, AssignProperties assign)
    {
        sb.Append(param).Append('.').Append(assign.Target.PropertyType.Name).Append(".HasValue ? ");
        sb.Append('(').Append(assign.Origin.Type.Name).Append(')');
        sb.Append(param).Append('.').Append(assign.Target.PropertyType.Name).Append(".Value : default");
    }

    private static void AssignEnumerable(StringBuilder sb, int ident, char param, AssignProperties assign)
    {
        sb.Append("[.. ");
        AssignDirect(sb, ident, param, assign);
        sb.Append(']');
    }

    private static void AssignSelectDirect(StringBuilder sb, int ident, char param, AssignProperties assign)
    {
        // TODO
    }

    private delegate void AssignGenerator(StringBuilder sb, int ident, char param, AssignProperties assign);

    private ref struct AssignProperties(PropertyDescriptor origin, PropertySelection target)
    {
        /// <summary>
        /// The origin property type descriptor. (DTO property)
        /// </summary>
        public PropertyDescriptor Origin { get; } = origin;

        /// <summary>
        /// The target property selection. (Entity property)
        /// </summary>
        public PropertySelection Target { get; } = target;
    }
}
