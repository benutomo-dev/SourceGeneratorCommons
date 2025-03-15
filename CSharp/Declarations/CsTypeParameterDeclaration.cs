#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsTypeParameterDeclaration : CsTypeDeclaration, IEquatable<CsTypeParameterDeclaration>
{
    public sealed override bool IsValueType => false;

    public sealed override bool IsReferenceType => false;

    public sealed override bool IsGenericType => false;

    public sealed override EquatableArray<CsGenericTypeParam> GenericTypeParams => EquatableArray<CsGenericTypeParam>.Empty;

    public CsTypeParameterDeclaration(string name) : base(container: null, name)
    {
    }

    private CsTypeParameterDeclaration(ITypeContainer? container, string name) : base(container: null, name)
    {
    }

    protected override CsTypeDeclaration Clone() => new CsTypeParameterDeclaration(Container, Name);

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
