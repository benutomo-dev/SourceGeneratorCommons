#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsTypeParameterDeclaration : CsTypeDeclaration, IEquatable<CsTypeParameterDeclaration>
{
    public sealed override bool IsValueType => Where.TypeCategory is not (CsGenericConstraintTypeCategory.Class or CsGenericConstraintTypeCategory.NullableClass);

    public sealed override bool IsReferenceType => Where.TypeCategory is not (CsGenericConstraintTypeCategory.Struct or CsGenericConstraintTypeCategory.Unmanaged);

    public sealed override int Arity => 0;

    public CsGenericTypeConstraints Where { get; private set; }

    public sealed override EquatableArray<CsTypeParameterDeclaration> GenericTypeParams => EquatableArray<CsTypeParameterDeclaration>.Empty;

    public CsTypeParameterDeclaration(string name, CsGenericTypeConstraints where) : base(container: null, name)
    {
        Where = where;
    }

    public CsTypeParameterDeclaration(string name, out Action<CsGenericTypeConstraints> complete)
        : base(name, out var baseComplete)
    {
        Where = null!;

        complete = (where) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            Where = where;

            var constructionFullCompleteFactors = Where.GetConstructionFullCompleteFactors(RejectAlreadyCompletedFactor);

            baseComplete(null, constructionFullCompleteFactors);
        };
    }

    private CsTypeParameterDeclaration(ITypeContainer? container, string name, CsGenericTypeConstraints where) : base(container, name)
    {
        Where = where;
    }

    protected override CsTypeDeclaration Clone() => new CsTypeParameterDeclaration(Container, Name, Where);

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsTypeParameterDeclaration other && Equals(other);

    public sealed override bool Equals(CsTypeDeclaration? other) => Equals((object?)other);

    public bool Equals(CsTypeParameterDeclaration? other)
    {
        if (!base.Equals(other))
            return false;

        return true;
    }

    public override int GetHashCode() => base.GetHashCode();
    #endregion
}
