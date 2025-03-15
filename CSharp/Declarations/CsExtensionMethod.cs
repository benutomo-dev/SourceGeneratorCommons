#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;
using SourceGeneratorCommons.Collections.Generic;
using System.Text;

namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsExtensionMethod(
    string Name,
    CsTypeRefWithNullability ReturnType,
    CsReturnModifier ReturnModifier = CsReturnModifier.Default,
    bool IsAsync = false,
    bool IsReadOnly = false,
    EquatableArray<CsMethodParam> Params = default,
    EquatableArray<CsGenericTypeParam> GenericTypeParams = default,
    CsAccessibility Accessibility = CsAccessibility.Default
    ) : CsMethod(Name, ReturnType, ReturnModifier, IsStatic: true, IsAsync, IsReadOnly, Params, GenericTypeParams, Accessibility, MethodModifier: CsMethodModifier.Default)
{
    public bool IsVoidMethod => ReturnType.ToString() == "void";
}
