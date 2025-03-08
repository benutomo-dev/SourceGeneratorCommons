namespace SourceGeneratorCommons;

internal interface ILazyConstructionRoot
{

    Task ConstructionFullCompleted { get; }
}
