﻿#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsStruct : CsInterfaceInplementableTypeDeclaration, IEquatable<CsStruct>
{
    public sealed override bool IsValueType => true;

    public bool IsReadOnly { get; }

    public bool IsRef { get; }

    public CsStruct(ITypeContainer? container, string name, EquatableArray<CsTypeParameterDeclaration> genericTypeParams = default, EquatableArray<CsTypeRef> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default, bool isReadOnly = false, bool isRef = false)
        :base(container, name, genericTypeParams, interfaces, accessibility)
    {
        IsReadOnly = isReadOnly;
        IsRef = isRef;
    }

    public CsStruct(string name, int arity, CsAccessibility accessibility, bool isReadOnly, bool isRef, out Action<ITypeContainer?, EquatableArray<CsTypeParameterDeclaration>, EquatableArray<CsTypeRef>> complete)
        : base(name, arity, accessibility, out var baseComplete)
    {
        IsReadOnly = isReadOnly;
        IsRef = isRef;

        complete = (container, genericTypeParams, interfaces) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            baseComplete(container, genericTypeParams, interfaces, null);
        };
    }

    protected override CsTypeDeclaration Clone() => new CsStruct(Container, Name, GenericTypeParams, Interfaces, Accessibility, IsReadOnly, IsRef);

    public CsStruct WithAccessibility(CsAccessibility accessibility)
    {
        var cloned = ((CsStruct)Clone());
        cloned.Accessibility = accessibility;
        return cloned;
    }

    public CsStruct WithIsReadOnly(bool isReadOnly)
    {
        if (IsReadOnly == isReadOnly)
            return this;

        return new CsStruct(Container, Name, GenericTypeParams, Interfaces, Accessibility, isReadOnly, IsRef);
    }

    public CsStruct WithIsRef(bool isRef)
    {
        if (IsRef == isRef)
            return this;

        return new CsStruct(Container, Name, GenericTypeParams, Interfaces, Accessibility, IsReadOnly, isRef);
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsStruct other && Equals(other);

    public sealed override bool Equals(CsInterfaceInplementableTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsStruct? other)
    {
        if (!base.Equals(other))
            return false;

        if (IsReadOnly != other.IsReadOnly)
            return false;

        if (IsRef != other.IsRef)
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(IsReadOnly);
        hashCode.Add(IsRef);
        return hashCode.ToHashCode();
    }
    #endregion
}
