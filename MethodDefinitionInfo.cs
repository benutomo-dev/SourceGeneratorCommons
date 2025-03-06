namespace SourceGeneratorCommons;

record class MethodDefinitionInfo(
    string Name,
    TypeReferenceInfo ReturnType,
    ReturnModifier ReturnModifier = ReturnModifier.Default,
    bool IsStatic = false,
    bool IsAsync = false,
    bool IsReadOnly = false,
    EquatableArray<MethodParam> Params = default,
    EquatableArray<GenericTypeParam> GenericTypeParams = default,
    CSharpAccessibility Accessibility = CSharpAccessibility.Default,
    MethodModifier MethodModifier = MethodModifier.Default
    )
{
    public bool IsVoidMethod => ReturnType.ToString() == "void";
}
