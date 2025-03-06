namespace SourceGeneratorCommons;

abstract record class CsGenericDefinableTypeDeclaration(
    ITypeContainer? Container,
    string Name,
    EquatableArray<GenericTypeParam> GenericTypeParams,
    CsAccessibility Accessibility
    ) : CsUserDefinableTypeDeclaration(Container, Name, Accessibility)
{
}
