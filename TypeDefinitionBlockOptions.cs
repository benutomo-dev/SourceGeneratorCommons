namespace SourceGeneratorCommons;

record struct TypeDefinitionBlockOptions(
    bool OmitBaseType = false,
    bool OmitInterfaces = false,
    bool OmitGenericConstraints = false,
    bool OmitPartialKeyword = false,
    string? TypeDeclarationLineTail = null
    )
{
    public static TypeDefinitionBlockOptions Simple { get; } = new TypeDefinitionBlockOptions
    {
        OmitBaseType = true,
        OmitInterfaces = true,
        OmitGenericConstraints = true,
    };
}
