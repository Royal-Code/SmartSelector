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

            assignGenerator(sb, ident + 1, param, propMatch.Target!);

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
            _ => AssignDirect
        };
    }

    private static void AssignDirect(StringBuilder sb, int ident, char param, PropertySelection propertySelection)
    {
        sb.Append(param).Append('.').Append(propertySelection.PropertyType.Name);
    }

    private static void AssignEnumerable(StringBuilder sb, int ident, char param, PropertySelection propertySelection)
    {
        sb.Append("[.. ");
        AssignDirect(sb, ident, param, propertySelection);
        sb.Append(']');
    }

    private static void AssignSelectDirect(StringBuilder sb, int ident, char param, PropertySelection propertySelection)
    {
        // TODO
    }

    private delegate void AssignGenerator(StringBuilder sb, int ident, char param, PropertySelection propertySelection);
}
