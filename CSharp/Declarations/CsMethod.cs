using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsMethod(
    string Name,
    CsTypeReference ReturnType,
    CsReturnModifier ReturnModifier = CsReturnModifier.Default,
    bool IsStatic = false,
    bool IsAsync = false,
    bool IsReadOnly = false,
    EquatableArray<CsMethodParam> Params = default,
    EquatableArray<CsGenericTypeParam> GenericTypeParams = default,
    CsAccessibility Accessibility = CsAccessibility.Default,
    CsMethodModifier MethodModifier = CsMethodModifier.Default
    )
{
    public bool IsVoidMethod => ReturnType.ToString() == "void";
}
