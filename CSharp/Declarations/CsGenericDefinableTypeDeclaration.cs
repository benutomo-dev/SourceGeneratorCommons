#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System;

namespace SourceGeneratorCommons.CSharp.Declarations;

abstract class CsGenericDefinableTypeDeclaration : CsUserDefinableTypeDeclaration, IEquatable<CsGenericDefinableTypeDeclaration>
{
    public sealed override int Arity { get; }

    public sealed override EquatableArray<CsTypeParameterDeclaration> GenericTypeParams => _genericTypeParams;

    private EquatableArray<CsTypeParameterDeclaration> _genericTypeParams;

    public CsGenericDefinableTypeDeclaration(ITypeContainer? container, string name, EquatableArray<CsTypeParameterDeclaration> genericTypeParams = default, CsAccessibility accessibility = CsAccessibility.Default)
        : base(container, name, accessibility)
    {
        _genericTypeParams = genericTypeParams.IsDefaultOrEmpty ? EquatableArray<CsTypeParameterDeclaration>.Empty : genericTypeParams;
        Arity = _genericTypeParams.Length;
    }

    public CsGenericDefinableTypeDeclaration(string name, int arity, CsAccessibility accessibility, out Action<ITypeContainer?, EquatableArray<CsTypeParameterDeclaration>, IEnumerable<IConstructionFullCompleteFactor>?> complete)
        : base(name, accessibility, out var baseComplete)
    {
        Arity = arity;
        complete = (container, genericTypeParams, constructionFullCompleteFactors) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            _genericTypeParams = genericTypeParams.IsDefaultOrEmpty ? EquatableArray<CsTypeParameterDeclaration>.Empty : genericTypeParams;

            if (_genericTypeParams.Length != Arity)
                throw new InvalidOperationException();

            if (!genericTypeParams.IsDefaultOrEmpty)
            {
                if (constructionFullCompleteFactors is null)
                    constructionFullCompleteFactors = genericTypeParams.Values;
                else
                    constructionFullCompleteFactors = constructionFullCompleteFactors.Concat(genericTypeParams.Values);
            }

            baseComplete(container, constructionFullCompleteFactors);
        };
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsGenericDefinableTypeDeclaration other && Equals(other);

    public sealed override bool Equals(CsUserDefinableTypeDeclaration? other) => Equals((object?)other);


    public virtual bool Equals(CsGenericDefinableTypeDeclaration? other)
    {
        if (!base.Equals(other))
            return false;

        if (!EqualityComparer<EquatableArray<CsTypeParameterDeclaration>>.Default.Equals(GenericTypeParams, other.GenericTypeParams))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(GenericTypeParams);
        return hashCode.ToHashCode();
    }
    #endregion
}
