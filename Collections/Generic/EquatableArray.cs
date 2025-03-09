using System.Collections.Immutable;

namespace SourceGeneratorCommons.Collections.Generic;

public static class EquatableArray
{
    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> array) => array;

    public static EquatableArray<T> Create<T>(params ReadOnlySpan<T> values)
    {
        var builder = ImmutableArray.CreateBuilder<T>(values.Length);
#if CODE_ANALYSYS4_9_0_OR_GREATER
        builder.AddRange(values);
#else
        foreach (var value in values)
            builder.Add(value);
#endif
        return builder.MoveToImmutable();
    }
}

public readonly struct EquatableArray<T>(ImmutableArray<T> values) : IEquatable<EquatableArray<T>>
{
#pragma warning disable CA1000
    public static EquatableArray<T> Empty { get; } = ImmutableArray<T>.Empty.ToEquatableArray();
#pragma warning restore CA1000

    public ImmutableArray<T> Values { get; } = values;

    public bool IsDefaultOrEmpty => Values.IsDefaultOrEmpty;

    public int Length => Values.Length;

    public T this[int index] => Values[index];

    public static implicit operator EquatableArray<T>(ImmutableArray<T> values)
    {
        return new EquatableArray<T>(values);
    }

    public static implicit operator ImmutableArray<T>(EquatableArray<T> values)
    {
        return values.Values;
    }

    public ReadOnlySpan<T> AsSpan() => Values.AsSpan();

    public override int GetHashCode()
    {
        if (Values.IsDefault)
            return 0;

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
        return (Values.IsDefault, other.Values.IsDefault) switch
        {
            (false, false) => Values.SequenceEqual(other.Values),
            (true,  false) => other.Values.IsEmpty,
            (false, true)  => Values.IsEmpty,
            _ => true,
        };
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
