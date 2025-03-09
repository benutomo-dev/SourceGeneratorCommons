#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsMethodParam(
    CsTypeReference Type,
    string Name,
    CsParamModifier Modifier = CsParamModifier.Default,
    bool IsScoped = false
    )
{
}
