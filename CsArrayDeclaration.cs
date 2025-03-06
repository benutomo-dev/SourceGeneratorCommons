namespace SourceGeneratorCommons;

sealed record class CsArrayDeclaration(
    ITypeContainer? Container,
    string Name,
    CsTypeDeclaration ElementType,
    int Rank = 1
    ) : CsTypeDeclaration(Container, Name)
{
}
