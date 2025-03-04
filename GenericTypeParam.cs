namespace SourceGeneratorCommons;

class GenericTypeParam
{
    public required string Name { get; init; }

    public GenericTypeConstraints? Where { get; init; }
}
