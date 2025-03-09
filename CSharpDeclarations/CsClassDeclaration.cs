using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharpDeclarations;

sealed class CsClassDeclaration : CsGenericDefinableTypeDeclaration, IEquatable<CsClassDeclaration>
{
    public CsTypeReference? BaseType { get; private set; }

    public ClassModifier ClassModifier {get;}

    public CsClassDeclaration(ITypeContainer? container, string name, EquatableArray<GenericTypeParam> genericTypeParams = default, CsTypeReference? baseType = null, EquatableArray<CsTypeReference> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default, ClassModifier classModifier = ClassModifier.Default)
        :base(container, name, genericTypeParams, interfaces, accessibility)
    {
        BaseType = baseType;
        ClassModifier = classModifier;
    }

    public CsClassDeclaration(string name, CsAccessibility accessibility, ClassModifier classModifier, out Action<ITypeContainer?, EquatableArray<GenericTypeParam>, CsTypeReference?, EquatableArray<CsTypeReference>> complete)
        : base(name, accessibility, out var baseComplete)
    {
        ClassModifier = classModifier;

        complete = (container, genericTypeParams, baseType, interfaces) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            BaseType = baseType;

            baseComplete(container, genericTypeParams, interfaces, baseType?.GetConstructionFullCompleteFactors(RejectAlreadyCompletedFactor));
        };
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsClassDeclaration other && Equals(other);

    public sealed override bool Equals(CsGenericDefinableTypeDeclaration? other) => Equals((object?)other);


    public bool Equals(CsClassDeclaration? other)
    {
        if (!base.Equals(other))
            return false;

        if (ClassModifier != other.ClassModifier)
            return false;

        if (!EqualityComparer<CsTypeReference?>.Default.Equals(BaseType, other.BaseType))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(ClassModifier);
        hashCode.Add(BaseType);
        return hashCode.ToHashCode();
    }
    #endregion
}
