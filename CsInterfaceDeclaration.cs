using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons;

sealed class CsInterfaceDeclaration : CsGenericDefinableTypeDeclaration, IEquatable<CsInterfaceDeclaration>
{
    public CsInterfaceDeclaration(ITypeContainer? container, string name, EquatableArray<GenericTypeParam> genericTypeParams = default, EquatableArray<CsTypeReference> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default)
        :base(container, name, genericTypeParams, interfaces, accessibility)
    {

    }


    public CsInterfaceDeclaration(string name, CsAccessibility accessibility, out Action<ITypeContainer?, EquatableArray<GenericTypeParam>, EquatableArray<CsTypeReference>> complete)
        : base(name, accessibility, out var baseComplete)
    {
        complete = (container, genericTypeParams, interfaces) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            baseComplete(container, genericTypeParams, interfaces, null);
        };
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsInterfaceDeclaration other && Equals(other);

    public sealed override bool Equals(CsGenericDefinableTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsInterfaceDeclaration? other)
    {
        if (!base.Equals(other))
            return false;

        return true;
    }

    public override int GetHashCode() => base.GetHashCode();
    #endregion
}
