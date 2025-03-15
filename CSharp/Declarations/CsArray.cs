#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsArray : CsTypeDeclaration, IEquatable<CsArray>
{
    public sealed override bool IsValueType => false;

    public sealed override bool IsGenericType => false;

    public sealed override EquatableArray<CsGenericTypeParam> GenericTypeParams => EquatableArray<CsGenericTypeParam>.Empty;

    public CsTypeRefWithNullability ElementType { get; private set; }

    public int Rank { get; }

    public CsArray(ITypeContainer? typeContainer, string name, CsTypeRefWithNullability elementType, int rank = 1) : base(typeContainer, name)
    {
        Rank = rank;
        ElementType = elementType;
    }

    public CsArray(string name, int rank, out Action<ITypeContainer?, CsTypeRefWithNullability> complete) : base(name, out var baseComplete)
    {
        Rank = rank;
        ElementType = default!;

        complete = (typeContainer, elementType) =>
        {
            if (SelfConstructionCompleted.IsCompleted)
                throw new InvalidOperationException();

            ElementType = elementType;

            baseComplete(
                typeContainer,
                elementType.GetConstructionFullCompleteFactors(RejectAlreadyCompletedFactor)
                );
        };
    }

    protected override CsTypeDeclaration Clone() => new CsArray(Container, Name, ElementType, Rank);

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsArray other && Equals(other);

    public sealed override bool Equals(CsTypeDeclaration? other) => Equals((object?)other);


    public bool Equals(CsArray? other)
    {
        if (!base.Equals(other))
            return false;

        if (Rank != other.Rank)
            return false;

        if (!EqualityComparer<CsTypeRefWithNullability>.Default.Equals(ElementType, other.ElementType))
            return false;


        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(Rank);
        hashCode.Add(ElementType);
        return hashCode.ToHashCode();
    }
    #endregion
}
