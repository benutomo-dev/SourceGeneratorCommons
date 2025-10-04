#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using System.Text;

namespace SourceGeneratorCommons.CSharp.Declarations;

record struct CsPropertyGetter(CsAccessibility Accessibility);
record struct CsPropertySetter(CsAccessibility Accessibility, bool IsInitOnly);

record class CsProperty(
    string Name,
    CsTypeRefWithAnnotation Type,
    CsReturnModifier ReturnModifier = CsReturnModifier.Default,
    bool IsStatic = false,
    bool IsReqired = false,
    EquatableArray<CsMethodParam> Params = default,
    CsAccessibility Accessibility = CsAccessibility.Default,
    CsMethodModifier MethodModifier = CsMethodModifier.Default,
    CsPropertyGetter? Getter = default,
    CsPropertySetter? Setter = default
    )
{
    public bool IsIndexer => !Params.IsDefaultOrEmpty;
}
