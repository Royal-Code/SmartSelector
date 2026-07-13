using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoyalCode.SmartSelector.Generators.Models;

internal sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    private readonly string[] arguments;

    private DiagnosticInfo(
        string id,
        string[] arguments,
        string? filePath,
        TextSpan sourceSpan,
        LinePositionSpan lineSpan,
        bool hasLocation)
    {
        Id = id;
        this.arguments = arguments;
        FilePath = filePath;
        SourceSpan = sourceSpan;
        LineSpan = lineSpan;
        HasLocation = hasLocation;
    }

    internal string Id { get; }
    internal IReadOnlyList<string> Arguments => arguments;
    internal string? FilePath { get; }
    internal TextSpan SourceSpan { get; }
    internal LinePositionSpan LineSpan { get; }
    internal bool HasLocation { get; }

    internal static DiagnosticInfo Create(
        DiagnosticDescriptor descriptor,
        Location? location,
        params object?[] arguments)
    {
        var values = arguments.Select(value => value?.ToString() ?? string.Empty).ToArray();
        if (location is null || location == Location.None || !location.IsInSource)
            return new DiagnosticInfo(descriptor.Id, values, null, default, default, false);

        var lineSpan = location.GetLineSpan();
        return new DiagnosticInfo(
            descriptor.Id,
            values,
            lineSpan.Path,
            location.SourceSpan,
            lineSpan.Span,
            true);
    }

    internal Diagnostic ToDiagnostic()
    {
        var location = HasLocation
            ? Location.Create(FilePath ?? string.Empty, SourceSpan, LineSpan)
            : Location.None;
        return Diagnostic.Create(
            AnalyzerDiagnostics.Get(Id),
            location,
            arguments.Cast<object>().ToArray());
    }

    public bool Equals(DiagnosticInfo? other) => other is not null &&
        Id == other.Id && arguments.SequenceEqual(other.arguments) &&
        FilePath == other.FilePath && SourceSpan.Equals(other.SourceSpan) &&
        LineSpan.Equals(other.LineSpan) && HasLocation == other.HasLocation;

    public override bool Equals(object? obj) => obj is DiagnosticInfo other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Id.GetHashCode();
            foreach (var argument in arguments) hash = (hash * 397) ^ argument.GetHashCode();
            hash = (hash * 397) ^ (FilePath?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ SourceSpan.GetHashCode();
            return (hash * 397) ^ LineSpan.GetHashCode();
        }
    }
}
