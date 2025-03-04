using System.Collections.Immutable;

namespace SourceGeneratorCommons;

public readonly struct EquatableArray<T>(ImmutableArray<T> values) : IEquatable<EquatableArray<T>>
{
    public ImmutableArray<T> Values { get; } = values;

    public static implicit operator EquatableArray<T>(ImmutableArray<T> values)
    {
        return new EquatableArray<T>(values);
    }

    public static implicit operator ImmutableArray<T>(EquatableArray<T> values)
    {
        return values.Values;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        hashCode.Add(Values.Length);
        for (int i = 0; i < Values.Length && i < 4; i++)
            hashCode.Add(Values[i]);
        for (int i = 0; 0 <= Values.Length - 1 - i && i < 4; i++)
            hashCode.Add(Values[Values.Length - 1 - i]);

        return hashCode.ToHashCode();
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public bool Equals(EquatableArray<T> other)
    {
        return Values.SequenceEqual(other.Values);
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
