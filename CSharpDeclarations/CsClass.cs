using SourceGeneratorCommons.Collections.Generic;
using System.ComponentModel;

namespace SourceGeneratorCommons.CSharpDeclarations;

sealed class CsClass : CsGenericDefinableTypeDeclaration, IEquatable<CsClass>
{
    public CsTypeReference? BaseType { get; private set; }

    public CsClassModifier ClassModifier {get;}

    public CsClass(ITypeContainer? container, string name, EquatableArray<CsGenericTypeParam> genericTypeParams = default, CsTypeReference? baseType = null, EquatableArray<CsTypeReference> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default, CsClassModifier classModifier = CsClassModifier.Default)
        :base(container, name, genericTypeParams, interfaces, accessibility)
    {
        BaseType = baseType;
        ClassModifier = classModifier;
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public CsClass(string name, CsAccessibility accessibility, CsClassModifier classModifier, out Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, CsTypeReference?, EquatableArray<CsTypeReference>> complete)
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
    public override bool Equals(object? obj) => obj is CsClass other && Equals(other);

    public sealed override bool Equals(CsGenericDefinableTypeDeclaration? other) => Equals((object?)other);


    public bool Equals(CsClass? other)
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
