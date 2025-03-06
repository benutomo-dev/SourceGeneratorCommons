namespace SourceGeneratorCommons;

record class CsMethodDeclaration(
    string Name,
    CsTypeReference ReturnType,
    ReturnModifier ReturnModifier = ReturnModifier.Default,
    bool IsStatic = false,
    bool IsAsync = false,
    bool IsReadOnly = false,
    EquatableArray<MethodParam> Params = default,
    EquatableArray<GenericTypeParam> GenericTypeParams = default,
    CsAccessibility Accessibility = CsAccessibility.Default,
    MethodModifier MethodModifier = MethodModifier.Default
    )
{
    public bool IsVoidMethod => ReturnType.ToString() == "void";
}
