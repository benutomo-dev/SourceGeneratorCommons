#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

interface ITypeContainer : ILazyConstructionRoot, IConstructionFullCompleteFactor
{
    string Name { get; }

    string FullName { get; }

    bool IsDefinedUnderSystemNameSpace { get; }
}
