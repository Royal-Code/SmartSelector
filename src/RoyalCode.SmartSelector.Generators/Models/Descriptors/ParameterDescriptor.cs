﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoyalCode.SmartSelector.Generators.Models.Descriptors;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

internal sealed class ParameterDescriptor : IEquatable<ParameterDescriptor>
{
    public static ParameterDescriptor Create(ParameterSyntax syntax, SemanticModel model)
        => new(TypeDescriptor.Create(syntax.Type!, model), syntax.Identifier.Text);

    private static ParameterDescriptor? cancellationToken;
    public static ParameterDescriptor CancellationToken(SemanticModel model)
    {
        if (cancellationToken is not null)
            return cancellationToken;
        
        var type = TypeDescriptor.CancellationToken(model);
        
        cancellationToken = new(type, "ct");
        
        return cancellationToken;
    }

    public ParameterDescriptor(TypeDescriptor type, string name)
    {
        Type = type;
        Name = name;
    }

    public TypeDescriptor Type { get; }

    public string Name { get; }

    public bool Equals(ParameterDescriptor? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Name == other.Name &&
            Equals(Type, other.Type);
    }

    public override bool Equals(object? obj)
    {
        return obj is ParameterDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hashCode = -1979447941;
        hashCode = hashCode * -1521134295 + Type.GetHashCode();
        hashCode = hashCode * -1521134295 + Name.GetHashCode();
        return hashCode;
    }
}
