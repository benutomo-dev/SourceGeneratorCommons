namespace SourceGeneratorCommons;

sealed record class CsEnumDeclaration(
    ITypeContainer? Container,
    string Name,
    CsAccessibility Accessibility = CsAccessibility.Default,
    EnumUnderlyingType UnderlyingType = EnumUnderlyingType.Int32
    ) : CsUserDefinableTypeDeclaration(Container, Name, Accessibility)
{
}
