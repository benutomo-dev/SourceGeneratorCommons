namespace SourceGeneratorCommons.CSharpDeclarations;

record class CsMethodParam(
    CsTypeReference Type,
    string Name,
    CsParamModifier Modifier = CsParamModifier.Default,
    bool IsScoped = false
    )
{
}
