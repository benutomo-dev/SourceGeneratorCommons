#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal abstract class CsInterfaceInplementableTypeDeclaration : CsGenericDefinableTypeDeclaration, IEquatable<CsInterfaceInplementableTypeDeclaration>
{
    public EquatableArray<CsTypeRef> Interfaces { get; private set; }

    public CsInterfaceInplementableTypeDeclaration(ITypeContainer? container, string name, EquatableArray<CsTypeParameterDeclaration> genericTypeParams = default, EquatableArray<CsTypeRef> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default)
        : base(container, name, genericTypeParams, accessibility)
    {
        Interfaces = interfaces.IsDefaultOrEmpty ? EquatableArray<CsTypeRef>.Empty : interfaces;
    }

    public CsInterfaceInplementableTypeDeclaration(string name, int arity, CsAccessibility accessibility, out Action<ITypeContainer?, EquatableArray<CsTypeParameterDeclaration>, EquatableArray<CsTypeRef>, IEnumerable<IConstructionFullCompleteFactor>?> complete)
        : base(name, arity, accessibility, out var baseComplete)
    {
        complete = (container, genericTypeParams, interfaces, constructionFullCompleteFactors) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            Interfaces = interfaces;

            if (!genericTypeParams.IsDefaultOrEmpty)
            {
                if (constructionFullCompleteFactors is null)
                    constructionFullCompleteFactors = genericTypeParams.Values;
                else
                    constructionFullCompleteFactors = constructionFullCompleteFactors.Concat(genericTypeParams.Values);
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

            baseComplete(container, genericTypeParams, constructionFullCompleteFactors);
        };
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsInterfaceInplementableTypeDeclaration other && Equals(other);

    public sealed override bool Equals(CsGenericDefinableTypeDeclaration? other) => Equals((object?)other);


    public virtual bool Equals(CsInterfaceInplementableTypeDeclaration? other)
    {
        if (!base.Equals(other))
            return false;

        if (!EqualityComparer<EquatableArray<CsTypeRef>>.Default.Equals(Interfaces, other.Interfaces))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(Interfaces);
        return hashCode.ToHashCode();
    }
    #endregion
}
