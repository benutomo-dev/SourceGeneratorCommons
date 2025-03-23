#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using System.ComponentModel;

namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsClass : CsInterfaceInplementableTypeDeclaration, IEquatable<CsClass>
{
    public sealed override bool IsValueType => false;

    public sealed override bool CanInherit => ClassModifier is not (CsClassModifier.Sealed or CsClassModifier.Static);

    public CsTypeRef? BaseType { get; private set; }

    public CsClassModifier ClassModifier {get;}

    public CsClass(ITypeContainer? container, string name, EquatableArray<CsGenericTypeParam> genericTypeParams = default, CsTypeRef? baseType = null, EquatableArray<CsTypeRef> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default, CsClassModifier classModifier = CsClassModifier.Default)
        :base(container, name, genericTypeParams, interfaces, accessibility)
    {
        BaseType = baseType;
        ClassModifier = classModifier;
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public CsClass(string name, CsAccessibility accessibility, CsClassModifier classModifier, out Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, CsTypeRef?, EquatableArray<CsTypeRef>> complete)
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

    protected override CsTypeDeclaration Clone() => new CsClass(Container, Name, GenericTypeParams, BaseType, Interfaces, Accessibility, ClassModifier);

    public CsClass WithAccessibility(CsAccessibility accessibility)
    {
        var cloned = ((CsClass)Clone());
        cloned.Accessibility = accessibility;
        return cloned;
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsClass other && Equals(other);

    public sealed override bool Equals(CsInterfaceInplementableTypeDeclaration? other) => Equals((object?)other);


    public bool Equals(CsClass? other)
    {
        if (!base.Equals(other))
            return false;

        if (ClassModifier != other.ClassModifier)
            return false;

        if (!EqualityComparer<CsTypeRef?>.Default.Equals(BaseType, other.BaseType))
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
