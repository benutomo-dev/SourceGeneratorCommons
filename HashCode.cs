namespace System;

internal class HashCode
{
    private const int InitialValue = 871247934;

    private int _value;
    private bool _isAdded;

    public void Add<T>(T value)
    {
        if (!_isAdded)
        {
            _value = InitialValue;
            _isAdded = true;
        }

        _value = _value * -1521134295 + EqualityComparer<T>.Default.GetHashCode(value);
    }

    public void Add<T>(T value, IEqualityComparer<T> comparer)
    {
        if (!_isAdded)
        {
            _value = InitialValue;
            _isAdded = true;
        }

        _value = _value * -1521134295 + comparer.GetHashCode(value);
    }

    public int ToHashCode()
    {
        if (!_isAdded)
        {
            _value = InitialValue;
            _isAdded = true;
        }

        return _value;
    }

    public static int Combine<T1>(T1 value1)
    {
        HashCode hashCode = new HashCode();
        hashCode.Add(value1);
        return hashCode.ToHashCode();
    }

#pragma warning disable CS0809 // 旧形式のメンバーが、旧形式でないメンバーをオーバーライドします
    [Obsolete($"計算済みハッシュ値を得るためには{nameof(ToHashCode)}()メソッドを使用する", error: true)]
    public override int GetHashCode() => base.GetHashCode();
#pragma warning restore CS0809 // 旧形式のメンバーが、旧形式でないメンバーをオーバーライドします
}
