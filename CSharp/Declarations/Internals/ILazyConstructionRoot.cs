namespace SourceGeneratorCommons.CSharp.Declarations.Internals;

internal interface ILazyConstructionRoot
{

    Task ConstructionFullCompleted { get; }
}
