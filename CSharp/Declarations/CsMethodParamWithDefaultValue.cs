#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsMethodParamWithDefaultValue(
    CsTypeRefWithAnnotation Type,
    string Name,
    object? DefaultValue,
    CsParamModifier Modifier = CsParamModifier.Default,
    EquatableArray<CsAttribute> Attributes = default
    ) : CsMethodParam(Type, Name, Modifier, IsScoped: false, Attributes)
{
}
