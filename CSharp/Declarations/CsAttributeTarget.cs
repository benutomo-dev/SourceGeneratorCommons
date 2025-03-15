#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
namespace SourceGeneratorCommons.CSharp.Declarations;

internal enum CsAttributeTarget
{
    Default,
    Assembly,
    Module,
    Field,
    Event,
    Method,
    Param,
    Property,
    Return,
    Type,
}