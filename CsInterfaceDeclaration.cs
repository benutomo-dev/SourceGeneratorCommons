namespace SourceGeneratorCommons;

sealed record class CsInterfaceDeclaration(
    ITypeContainer? Container,
    string Name,
    EquatableArray<GenericTypeParam> GenericTypeParams = default,
    EquatableArray<CsTypeReference> Interfaces = default,
    CsAccessibility Accessibility = CsAccessibility.Default
    ) : CsGenericDefinableTypeDeclaration(Container, Name, GenericTypeParams, Accessibility)
{
}
