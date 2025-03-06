namespace SourceGeneratorCommons;

abstract record class CsUserDefinableTypeDeclaration(
    ITypeContainer? Container,
    string Name,
    CsAccessibility Accessibility
    ) : CsTypeDeclaration(Container, Name)
{
}
