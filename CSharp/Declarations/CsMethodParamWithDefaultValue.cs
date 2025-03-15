#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsMethodParamWithDefaultValue(
    CsTypeRefWithNullability Type,
    string Name,
    object? DefaultValue,
    CsParamModifier Modifier = CsParamModifier.Default,
    EquatableArray<CsAttribute> Attributes = default
    ) : CsMethodParam(Type, Name, Modifier, IsScoped: false, Attributes)
{
}
