﻿#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

abstract class CsUserDefinableTypeDeclaration : CsTypeDeclaration, IEquatable<CsUserDefinableTypeDeclaration>
{
    public CsAccessibility Accessibility { get; protected set; }

    public CsUserDefinableTypeDeclaration(ITypeContainer? container, string name, CsAccessibility accessibility = CsAccessibility.Default)
        :base(container, name)
    {
        Accessibility = accessibility;
    }

    public CsUserDefinableTypeDeclaration(string name, CsAccessibility accessibility, out Action<ITypeContainer?, IEnumerable<IConstructionFullCompleteFactor>?> complete)
        : base(name, out complete)
    {
        Accessibility = accessibility;
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsUserDefinableTypeDeclaration other && Equals(other);

    public sealed override bool Equals(CsTypeDeclaration? other) => Equals((object?)other);


    public virtual bool Equals(CsUserDefinableTypeDeclaration? other)
    {
        if (!base.Equals(other))
            return false;

        if (Accessibility != other.Accessibility)
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(Accessibility);
        return hashCode.ToHashCode();
    }
    #endregion
}
