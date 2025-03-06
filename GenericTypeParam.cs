namespace SourceGeneratorCommons;

internal record struct GenericTypeParam(string Name, GenericTypeConstraints? Where = null);
