#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

abstract class CsGenericDefinableTypeDeclaration : CsUserDefinableTypeDeclaration, IEquatable<CsGenericDefinableTypeDeclaration>
{
    public sealed override bool IsGenericType => !GenericTypeParams.IsDefaultOrEmpty;

    public EquatableArray<CsGenericTypeParam> GenericTypeParams { get; private set; }


    public CsGenericDefinableTypeDeclaration(ITypeContainer? container, string name, EquatableArray<CsGenericTypeParam> genericTypeParams = default, CsAccessibility accessibility = CsAccessibility.Default)
        : base(container, name, accessibility)
    {
        GenericTypeParams = genericTypeParams.IsDefaultOrEmpty ? EquatableArray<CsGenericTypeParam>.Empty : genericTypeParams;
    }

    public CsGenericDefinableTypeDeclaration(string name, CsAccessibility accessibility, out Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, IEnumerable<IConstructionFullCompleteFactor>?> complete)
        : base(name, accessibility, out var baseComplete)
    {
        complete = (container, genericTypeParams, constructionFullCompleteFactors) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            GenericTypeParams = genericTypeParams;

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

        if (!EqualityComparer<EquatableArray<CsGenericTypeParam>>.Default.Equals(GenericTypeParams, other.GenericTypeParams))
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
