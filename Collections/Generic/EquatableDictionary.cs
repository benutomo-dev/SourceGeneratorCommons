#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SourceGeneratorCommons.Collections.Generic;

#pragma warning disable CA1711
public static class EquatableDictionary
#pragma warning restore CA1711
{
    public static EquatableDictionary<TKey, TValue> ToEquatableDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        where TKey : notnull
        => new EquatableDictionary<TKey, TValue>(keyValuePairs);

    public static EquatableDictionary<TKey, TValue> ToEquatableDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        where TKey : notnull
        => new EquatableDictionary<TKey, TValue>(keyValuePairs, keyComparer, valueComparer);
}

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class EquatableDictionary<TKey, TValue> : IEquatable<EquatableDictionary<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
#pragma warning disable CA1000
    public static EquatableDictionary<TKey, TValue> Empty { get; } = new EquatableDictionary<TKey, TValue>([]);
#pragma warning restore CA1000

    public IEnumerable<TKey> Keys => _dictionary.Keys;

    public IEnumerable<TValue> Values => _dictionary.Values;

    public int Count => _dictionary.Count;

    public TValue this[TKey key] => _dictionary[key];

    private Dictionary<TKey, TValue> _dictionary;

    private ImmutableArray<TKey> _keys;

    private IEqualityComparer<TValue> _valueComparer;

    private int? _hashCode;

    public EquatableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        :this(keyValuePairs, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default)
    {
    }

    public EquatableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
    {
        _valueComparer = valueComparer;

        var dictionary = new Dictionary<TKey, TValue>(keyComparer);

        var keysBuilder = keyValuePairs.TryGetNonEnumeratedCount(out var count)
            ? ImmutableArray.CreateBuilder<TKey>(count)
            : ImmutableArray.CreateBuilder<TKey>();

        foreach (var keyValuePair in keyValuePairs)
        {
            dictionary.Add(keyValuePair.Key, keyValuePair.Value);
            keysBuilder.Add(keyValuePair.Key);
        }

        _dictionary = dictionary;
        _keys = keysBuilder.MoveToImmutable();
    }

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dictionary).GetEnumerator();

    public override int GetHashCode()
    {
        if (!_hashCode.HasValue)
        {
            var hashCode = new HashCode();
            hashCode.Add(_dictionary.Count);

            foreach (var key in _keys)
            {
                var value = _dictionary[key];

                hashCode.Add(key, _dictionary.Comparer);
                hashCode.Add(value, _valueComparer);
            }

            _hashCode = hashCode.ToHashCode();
        }
        
        return _hashCode.Value;
    }

    public override bool Equals(object? obj) => obj is EquatableDictionary<TKey, TValue> other && Equals(other);

    public bool Equals(EquatableDictionary<TKey, TValue>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (_dictionary.Count != other._dictionary.Count)
            return false;

        if (_dictionary.Comparer != other._dictionary.Comparer)
            return false;

        if (_valueComparer != other._valueComparer)
            return false;

        if (_dictionary.Count == 0)
            return true;

        if (_hashCode.HasValue && other._hashCode.HasValue)
        {
            if (_hashCode.Value != other._hashCode)
                return false;
        }

        foreach (var key in _keys)
        {
            var selfValue = _dictionary[key];

            if (!other._dictionary.TryGetValue(key, out var otherValue))
                return false;

            if (!EqualityComparer<TValue>.Default.Equals(selfValue, otherValue))
                return false;
        }

        return true;
    }

    public static bool operator ==(EquatableDictionary<TKey, TValue> left, EquatableDictionary<TKey, TValue> right) => left.Equals(right);

    public static bool operator !=(EquatableDictionary<TKey, TValue> left, EquatableDictionary<TKey, TValue> right) => !left.Equals(right);

    private string GetDebuggerDisplay() => $"{nameof(Count)} = {Count}";
}
