namespace SourceGeneratorCommons.CSharpDeclarations;

sealed class CsTypeParameterDeclaration : CsTypeDeclaration, IEquatable<CsTypeParameterDeclaration>
{
    public CsTypeParameterDeclaration(string name) : base(container: null, name)
    {
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsTypeParameterDeclaration other && Equals(other);

    public sealed override bool Equals(CsTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsTypeParameterDeclaration? other)
    {
        if (!base.Equals(other))
            return false;

        return true;
    }

    public override int GetHashCode() => base.GetHashCode();
    #endregion
}
