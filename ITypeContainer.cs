namespace SourceGeneratorCommons;

interface ITypeContainer : ILazyConstructionRoot, IConstructionFullCompleteFactor
{
    string Name { get; }

    string FullName { get; }
}
