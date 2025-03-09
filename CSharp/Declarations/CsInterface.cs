#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsInterface : CsInterfaceInplementableTypeDeclaration, IEquatable<CsInterface>
{
    public sealed override bool IsValueType => false;

    public sealed override bool CanInherit => true;

    public CsInterface(ITypeContainer? container, string name, EquatableArray<CsGenericTypeParam> genericTypeParams = default, EquatableArray<CsTypeReference> interfaces = default, CsAccessibility accessibility = CsAccessibility.Default)
        :base(container, name, genericTypeParams, interfaces, accessibility)
    {

    }


    public CsInterface(string name, CsAccessibility accessibility, out Action<ITypeContainer?, EquatableArray<CsGenericTypeParam>, EquatableArray<CsTypeReference>> complete)
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
    public override bool Equals(object? obj) => obj is CsInterface other && Equals(other);

    public sealed override bool Equals(CsInterfaceInplementableTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsInterface? other)
    {
        if (!base.Equals(other))
            return false;

        return true;
    }

    public override int GetHashCode() => base.GetHashCode();
    #endregion
}
