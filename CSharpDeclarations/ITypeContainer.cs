using SourceGeneratorCommons.CSharpDeclarations.Internals;

namespace SourceGeneratorCommons.CSharpDeclarations;

interface ITypeContainer : ILazyConstructionRoot, IConstructionFullCompleteFactor
{
    string Name { get; }

    string FullName { get; }
}
