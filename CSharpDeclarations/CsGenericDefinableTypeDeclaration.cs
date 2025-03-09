using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharpDeclarations.Internals;

namespace SourceGeneratorCommons.CSharpDeclarations;

abstract class CsGenericDefinableTypeDeclaration : CsUserDefinableTypeDeclaration, IEquatable<CsGenericDefinableTypeDeclaration>
{
    public EquatableArray<GenericTypeParam> GenericTypeParams { get; private set; }

    public EquatableArray<CsTypeReference> Interfaces { get; private set; }


    public CsGenericDefinableTypeDeclaration(ITypeContainer? container, string name, EquatableArray<GenericTypeParam> genericTypeParams = default, EquatableArray<CsTypeReference> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default)
        : base(container, name, accessibility)
    {
        GenericTypeParams = genericTypeParams.IsDefaultOrEmpty ? EquatableArray<GenericTypeParam>.Empty : genericTypeParams;
        Interfaces = interfaces.IsDefaultOrEmpty ? EquatableArray<CsTypeReference>.Empty : interfaces;
    }

    public CsGenericDefinableTypeDeclaration(string name, CsAccessibility accessibility, out Action<ITypeContainer?, EquatableArray<GenericTypeParam>, EquatableArray<CsTypeReference>, IEnumerable<IConstructionFullCompleteFactor>?> complete)
        : base(name, accessibility, out var baseComplete)
    {
        complete = (container, genericTypeParams, interfaces, constructionFullCompleteFactors) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            GenericTypeParams = genericTypeParams;
            Interfaces = interfaces;

            foreach (var genericTypeParam in genericTypeParams.Values)
            {
                if (genericTypeParam.GetConstructionFullCompleteFactors(RejectAlreadyCompletedFactor) is { } factors)
                {
                    if (constructionFullCompleteFactors is null)
                        constructionFullCompleteFactors = factors;
                    else
                        constructionFullCompleteFactors = constructionFullCompleteFactors.Concat(factors);
                }
            }

            foreach (var interfaceItem in interfaces.Values)
            {
                if (interfaceItem.GetConstructionFullCompleteFactors(RejectAlreadyCompletedFactor) is { } factors)
                {
                    if (constructionFullCompleteFactors is null)
                        constructionFullCompleteFactors = factors;
                    else
                        constructionFullCompleteFactors = constructionFullCompleteFactors.Concat(factors);
                }
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

        if (!EqualityComparer<EquatableArray<GenericTypeParam>>.Default.Equals(GenericTypeParams, other.GenericTypeParams))
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
