namespace SourceGeneratorCommons.CSharpDeclarations.Internals;

internal interface ILazyConstructionRoot
{

    Task ConstructionFullCompleted { get; }
}
