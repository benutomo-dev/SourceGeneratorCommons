namespace SourceGeneratorCommons;

sealed class CsArrayDeclaration : CsTypeDeclaration, IEquatable<CsArrayDeclaration>
{
    public CsTypeDeclaration ElementType { get; private set; }

    public int Rank { get; }

    public CsArrayDeclaration(ITypeContainer? typeContainer, string name, CsTypeDeclaration elementType, int rank = 1) : base(typeContainer, name)
    {
        Rank = rank;
        ElementType = elementType;
    }

    public CsArrayDeclaration(string name, int rank, out Action<ITypeContainer?, CsTypeDeclaration> complete) : base(name, out var baseComplete)
    {
        Rank = rank;
        ElementType = default!;

        complete = (typeContainer, elementType) =>
        {
            if (IsConstructionCompleted)
                throw new InvalidOperationException();

            ElementType = elementType;

            baseComplete(typeContainer);
        };
    }

    #region IEquatable
    public override bool Equals(object? obj) => obj is CsArrayDeclaration other && Equals(other);

    public sealed override bool Equals(CsTypeDeclaration? other) => Equals((object?)other);


    public bool Equals(CsArrayDeclaration? other)
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
