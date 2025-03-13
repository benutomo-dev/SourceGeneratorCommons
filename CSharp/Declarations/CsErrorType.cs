#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsErrorType : CsTypeDeclaration, IEquatable<CsErrorType>
{
    public sealed override bool IsValueType => false;

    public sealed override bool IsReferenceType => false;

    public sealed override bool IsGenericType => false;

    public CsErrorType(string name) : base(container: null, name)
    {
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsErrorType other && Equals(other);

    public sealed override bool Equals(CsTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsErrorType? other)
    {
        if (!base.Equals(other))
            return false;

        return true;
    }

    public override int GetHashCode() => base.GetHashCode();
    #endregion
}
