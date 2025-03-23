#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal sealed class CsDelegate : CsGenericDefinableTypeDeclaration, IEquatable<CsDelegate>
{
    public sealed override bool IsValueType => false;

    public CsTypeRefWithAnnotation ReturnType { get; private set; }

    public CsReturnModifier ReturnModifier { get; }

    public EquatableArray<CsMethodParam> MethodParams { get; private set; }

    public CsDelegate(ITypeContainer? container, string name, CsTypeRefWithAnnotation returnType, CsReturnModifier returnModifier = CsReturnModifier.Default, EquatableArray<CsMethodParam> methodParams = default, EquatableArray<CsTypeParameterDeclaration> genericTypeParams = default, CsAccessibility accessibility = CsAccessibility.Default)
        : base(container, name, genericTypeParams, accessibility)
    {
        ReturnType = returnType;
        ReturnModifier = returnModifier;
        MethodParams = methodParams.IsDefaultOrEmpty ? EquatableArray<CsMethodParam>.Empty : methodParams;
    }

    public CsDelegate(string name, int arity, CsAccessibility accessibility, CsReturnModifier returnModifier, out Action<ITypeContainer?, CsTypeRefWithAnnotation, EquatableArray<CsMethodParam>, EquatableArray<CsTypeParameterDeclaration>> complete)
        : base(name, arity, accessibility, out var baseComplete)
    {
        complete = (container, returnType, methodParams, genericTypeParams) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            ReturnType = returnType;
            MethodParams = methodParams;

            var constructionFullCompleteFactors = returnType.GetConstructionFullCompleteFactors(RejectAlreadyCompletedFactor);

            foreach (var methodParam in methodParams.Values)
            {
                if (methodParam.GetConstructionFullCompleteFactors(RejectAlreadyCompletedFactor) is { } factors)
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

    protected override CsTypeDeclaration Clone() => new CsDelegate(Container, Name, ReturnType, ReturnModifier, MethodParams, GenericTypeParams, Accessibility);

    public CsDelegate WithAccessibility(CsAccessibility accessibility)
    {
        var cloned = ((CsDelegate)Clone());
        cloned.Accessibility = accessibility;
        return cloned;
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsDelegate other && Equals(other);

    public sealed override bool Equals(CsGenericDefinableTypeDeclaration? other) => Equals((object?)other);


    public bool Equals(CsDelegate? other)
    {
        if (!base.Equals(other))
            return false;

        if (ReturnModifier != other.ReturnModifier)
            return false;

        if (!EqualityComparer<CsTypeRefWithAnnotation>.Default.Equals(ReturnType, other.ReturnType))
            return false;

        if (!EqualityComparer<EquatableArray<CsMethodParam>>.Default.Equals(MethodParams, other.MethodParams))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(ReturnModifier);
        hashCode.Add(ReturnType);
        hashCode.Add(MethodParams);
        return hashCode.ToHashCode();
    }
    #endregion
}
