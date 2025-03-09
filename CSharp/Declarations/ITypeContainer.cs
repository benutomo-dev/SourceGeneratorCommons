using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

interface ITypeContainer : ILazyConstructionRoot, IConstructionFullCompleteFactor
{
    string Name { get; }

    string FullName { get; }
}
