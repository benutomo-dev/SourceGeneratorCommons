namespace SourceGeneratorCommons;

record class MethodParam(
    TypeReferenceInfo Type,
    string Name,
    ParamModifier Modifier = ParamModifier.Default,
    bool IsScoped = false
    )
{
}
