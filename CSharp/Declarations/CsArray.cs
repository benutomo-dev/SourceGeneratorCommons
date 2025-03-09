namespace SourceGeneratorCommons.CSharp.Declarations;

sealed class CsArray : CsTypeDeclaration, IEquatable<CsArray>
{
    public CsTypeDeclaration ElementType { get; private set; }

    public int Rank { get; }

    public CsArray(ITypeContainer? typeContainer, string name, CsTypeDeclaration elementType, int rank = 1) : base(typeContainer, name)
    {
        Rank = rank;
        ElementType = elementType;
    }

    public CsArray(string name, int rank, out Action<ITypeContainer?, CsTypeDeclaration> complete) : base(name, out var baseComplete)
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
                [elementType]
                );
        };
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsArray other && Equals(other);

    public sealed override bool Equals(CsTypeDeclaration? other) => Equals((object?)other);


    public bool Equals(CsArray? other)
    {
        if (!base.Equals(other))
            return false;

        if (Rank != other.Rank)
            return false;

        if (!EqualityComparer<CsTypeDeclaration>.Default.Equals(ElementType, other.ElementType))
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
