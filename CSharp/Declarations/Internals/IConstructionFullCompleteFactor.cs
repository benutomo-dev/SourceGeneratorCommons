#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
namespace SourceGeneratorCommons.CSharp.Declarations.Internals;

internal interface IConstructionFullCompleteFactor
{
    Task SelfConstructionCompleted { get; }
}
