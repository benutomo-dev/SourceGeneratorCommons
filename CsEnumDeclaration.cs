namespace SourceGeneratorCommons;

sealed class CsEnumDeclaration : CsUserDefinableTypeDeclaration, IEquatable<CsEnumDeclaration>
{
    public EnumUnderlyingType UnderlyingType { get; }

    public CsEnumDeclaration(ITypeContainer? container, string name, CsAccessibility accessibility = CsAccessibility.Default, EnumUnderlyingType underlyingType = EnumUnderlyingType.Int32)
        : base(container, name, accessibility)
    {
        UnderlyingType = underlyingType;
    }

    public CsEnumDeclaration(string name, CsAccessibility accessibility, EnumUnderlyingType underlyingType, out Action<ITypeContainer?> complete)
        : base(name, accessibility, out complete)
    {
        UnderlyingType = underlyingType;
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsEnumDeclaration other && Equals(other);

    public sealed override bool Equals(CsUserDefinableTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsEnumDeclaration? other)
    {
        if (!base.Equals(other))
            return false;

        if (UnderlyingType != other.UnderlyingType)
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(UnderlyingType);
        return hashCode.ToHashCode();
    }
    #endregion
}
