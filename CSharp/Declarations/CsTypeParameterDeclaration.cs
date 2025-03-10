﻿#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsTypeParameterDeclaration : CsTypeDeclaration, IEquatable<CsTypeParameterDeclaration>
{
    public sealed override bool IsValueType => false;

    public sealed override bool IsReferenceType => false;

    public sealed override bool IsGenericType => false;

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
