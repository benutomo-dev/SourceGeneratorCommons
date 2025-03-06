namespace SourceGeneratorCommons;

sealed record class CsClassDeclaration(
    ITypeContainer? Container,
    string Name,
    EquatableArray<GenericTypeParam> GenericTypeParams = default,
    CsTypeReference? BaseType = null,
    EquatableArray<CsTypeReference> Interfaces = default,
    CsAccessibility Accessibility = CsAccessibility.Default,
    ClassModifier ClassModifier = ClassModifier.Default
    ) : CsGenericDefinableTypeDeclaration(Container, Name, GenericTypeParams, Accessibility)
{
}
