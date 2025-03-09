#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SourceGeneratorCommons.Collections.Special;

internal struct HashTable<TKey, TValue> where TKey : class where TValue : class
{
    private class HashTableEqualityComparer<T> : IEqualityComparer
    {
        private IEqualityComparer<T> _equalityComparer;

        public HashTableEqualityComparer(IEqualityComparer<T> equalityComparer)
        {
            _equalityComparer = equalityComparer;
        }

        public new bool Equals(object? x, object? y)
        {
            if (x is null && y == null)
                return true;
            else if (x is T typedX && y is T typedY)
                return _equalityComparer.Equals(typedX, typedY);
            else
                return false;
        }

        public int GetHashCode(object obj)
        {
            if (obj is T typedObj)
                return _equalityComparer.GetHashCode(typedObj);
            else
                return obj.GetHashCode();
        }
    }

    private Hashtable _hashTable;

    private Lock _lockObject;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        value = _hashTable[key] as TValue;
        return value is not null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetOrAdd<TAddKey>(TAddKey key, Func<TAddKey, TValue> createAddValue, out bool isAdded) where TAddKey : TKey
    {
        return GetOrAddInternal(key, ref createAddValue, static (TAddKey key, ref Func<TAddKey, TValue> createAddValue) =>
        {
            return createAddValue(key);
        }, out isAdded);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetOrAdd<TAddKey, TCreateArg>(TAddKey key, TCreateArg createArg, Func<TAddKey, TCreateArg, TValue> createAddValue, out bool isAdded) where TAddKey : TKey
    {
        var internalCreateArg = (createArg, createAddValue);
        return GetOrAddInternal(key, ref internalCreateArg, static (TAddKey key, ref (TCreateArg createArg, Func<TAddKey, TCreateArg, TValue> createAddValue) internalCreateArg) =>
        {
            return internalCreateArg.createAddValue(key, internalCreateArg.createArg);
        }, out isAdded);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetOrAdd<TAddKey, TCreateArg>(TAddKey key, ref TCreateArg createArg, CreateAddValue<TAddKey, TCreateArg, TValue> createAddValue, out bool isAdded) where TAddKey : TKey
    {
        return GetOrAddInternal(key, ref createArg, createAddValue, out isAdded);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TValue GetOrAddInternal<TAddKey, TCreateArg>(TAddKey key, ref TCreateArg createArg, CreateAddValue<TAddKey, TCreateArg, TValue> createAddValue, out bool isAdded) where TAddKey : TKey
    {
        if (key is null)
            throwArgumentNullException(key);

        isAdded = false;

        if (TryGetValue(key, out var value))
            return value;

        var createdValue = createAddValue(key, ref createArg);
        try
        {
            if (TryGetValue(key, out var retryBeforeLockedValue))
                return retryBeforeLockedValue;

            lock (_lockObject)
            {
                if (TryGetValue(key, out var retryAfterLockedValue))
                    return retryAfterLockedValue;

                _hashTable.Add(key, createdValue);
                isAdded = true;

                return createdValue;
            }
        }
        finally
        {
            if (!isAdded && createdValue is IDisposable)
            {
                ((IDisposable)createdValue).Dispose();
            }
        }
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void throwArgumentNullException<T>(T? arg, [CallerArgumentExpression(nameof(arg))] string? argExpression = null)
    {
        throw new ArgumentNullException(argExpression);
    }

    public HashTable(Lock lockObject, IEqualityComparer<TKey> equalityComparer)
    {
        _lockObject = lockObject;
        _hashTable = new Hashtable(new HashTableEqualityComparer<TKey>(equalityComparer));
    }
}
