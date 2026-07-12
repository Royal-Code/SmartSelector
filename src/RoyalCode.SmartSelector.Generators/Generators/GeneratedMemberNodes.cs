using System.Text;

namespace RoyalCode.SmartSelector.Generators.Generators;

/// <summary>
/// Nó que escreve linhas pré-formatadas (XML docs e atributos) com a indentação do ponto de inserção.
/// </summary>
internal sealed class RawLinesGeneratorNode : GeneratorNode
{
    private readonly string[] lines;

    public RawLinesGeneratorNode(params string[] lines)
    {
        this.lines = lines;
    }

    public override void Write(StringBuilder sb, int indent = 0)
    {
        foreach (var line in lines)
            sb.Indent(indent).AppendLine(line);
    }
}

/// <summary>
/// <see cref="PropertyGenerator"/> com linhas de prefixo (XML docs e atributos) antes da declaração.
/// O pacote externo não suporta atributos em propriedades; a escrita é replicada aqui.
/// </summary>
internal sealed class AnnotatedPropertyGenerator : PropertyGenerator
{
    private readonly string[] prefixLines;

    public AnnotatedPropertyGenerator(
        TypeDescriptor type,
        string name,
        string[] prefixLines,
        bool canGet = true,
        bool canSet = true)
        : base(type, name, canGet, canSet)
    {
        this.prefixLines = prefixLines;
    }

    public override void Write(StringBuilder sb, int indent = 0)
    {
        sb.AppendLine();

        foreach (var line in prefixLines)
            sb.Indent(indent).AppendLine(line);

        sb.Indent(indent);
        Modifiers.Write(sb);
        sb.Append(Type.Name).Append(' ').Append(Name).Append(" { ");

        if (CanGet)
            sb.Append("get; ");

        if (CanSet)
            sb.Append("set; ");

        sb.Append("}");

        if (Value is not null)
        {
            sb.Append(" = ").Append(Value.GetValue(indent));
            sb.AppendLine(";");
        }
        else
        {
            sb.AppendLine();
        }
    }
}

/// <summary>
/// <see cref="FieldGenerator"/> com linhas de prefixo (atributos) antes da declaração.
/// O pacote externo não suporta atributos em campos.
/// </summary>
internal sealed class AnnotatedFieldGenerator : FieldGenerator
{
    private readonly string[] prefixLines;

    public AnnotatedFieldGenerator(TypeDescriptor type, string name, string[] prefixLines)
        : base(type, name, privateReadonly: false)
    {
        this.prefixLines = prefixLines;
    }

    public override void Write(StringBuilder sb, int indent = 0)
    {
        foreach (var line in prefixLines)
            sb.Indent(indent).AppendLine(line);

        base.Write(sb, indent);
    }
}
