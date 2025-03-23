#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsEnum : CsUserDefinableTypeDeclaration, IEquatable<CsEnum>
{
    public sealed override bool IsValueType => true;

    public sealed override int Arity => 0;

    public sealed override EquatableArray<CsTypeParameterDeclaration> GenericTypeParams => EquatableArray<CsTypeParameterDeclaration>.Empty;

    public CsEnumUnderlyingType UnderlyingType { get; }

    public CsEnum(ITypeContainer? container, string name, CsAccessibility accessibility = CsAccessibility.Default, CsEnumUnderlyingType underlyingType = CsEnumUnderlyingType.Int32)
        : base(container, name, accessibility)
    {
        UnderlyingType = underlyingType;
    }

    public CsEnum(string name, CsAccessibility accessibility, CsEnumUnderlyingType underlyingType, out Action<ITypeContainer?> complete)
        : base(name, accessibility, out var baseComplete)
    {
        UnderlyingType = underlyingType;

        complete = typeContainer =>
        {
            baseComplete(typeContainer, null);
        };
    }

    protected override CsTypeDeclaration Clone() => new CsEnum(Container, Name, Accessibility, UnderlyingType);

    public CsEnum WithAccessibility(CsAccessibility accessibility)
    {
        var cloned = ((CsEnum)Clone());
        cloned.Accessibility = accessibility;
        return cloned;
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsEnum other && Equals(other);

    public sealed override bool Equals(CsUserDefinableTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsEnum? other)
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
