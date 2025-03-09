using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharpDeclarations;

sealed class CsStructDeclaration : CsGenericDefinableTypeDeclaration, IEquatable<CsStructDeclaration>
{
    public bool IsReadOnly { get; }

    public bool IsRef { get; }

    public CsStructDeclaration(ITypeContainer? container, string name, EquatableArray<GenericTypeParam> genericTypeParams = default, EquatableArray<CsTypeReference> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default, bool isReadOnly = false, bool isRef = false)
        :base(container, name, genericTypeParams, interfaces, accessibility)
    {
        IsReadOnly = isReadOnly;
        IsRef = isRef;
    }

    public CsStructDeclaration(string name, CsAccessibility accessibility, bool isReadOnly, bool isRef, out Action<ITypeContainer?, EquatableArray<GenericTypeParam>, EquatableArray<CsTypeReference>> complete)
        : base(name, accessibility, out var baseComplete)
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

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsStructDeclaration other && Equals(other);

    public sealed override bool Equals(CsGenericDefinableTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsStructDeclaration? other)
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
