namespace SourceGeneratorCommons;

record class MethodParam(
    CsTypeReference Type,
    string Name,
    ParamModifier Modifier = ParamModifier.Default,
    bool IsScoped = false
    )
{
}
