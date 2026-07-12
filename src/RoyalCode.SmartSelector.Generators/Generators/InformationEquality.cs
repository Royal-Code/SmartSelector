namespace RoyalCode.SmartSelector.Generators.Generators;

internal static class InformationEquality
{
    internal static bool SequenceEqual<T>(T[]? left, T[]? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        return left is not null && right is not null && left.SequenceEqual(right);
    }

    internal static int SequenceHashCode<T>(T[]? values)
    {
        if (values is null)
            return 0;

        unchecked
        {
            var hashCode = 17;
            foreach (var value in values)
                hashCode = (hashCode * 31) + (value?.GetHashCode() ?? 0);

            return hashCode;
        }
    }
}
