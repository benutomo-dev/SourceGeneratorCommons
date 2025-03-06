namespace SourceGeneratorCommons;

sealed record class CsStructDeclaration(
    ITypeContainer? Container,
    string Name,
    EquatableArray<GenericTypeParam> GenericTypeParams = default,
    EquatableArray<CsTypeReference> Interfaces = default,
    CsAccessibility Accessibility = CsAccessibility.Default,
    bool IsReadOnly = false,
    bool IsRef = false
    ) : CsGenericDefinableTypeDeclaration(Container, Name, GenericTypeParams, Accessibility)
{
}
