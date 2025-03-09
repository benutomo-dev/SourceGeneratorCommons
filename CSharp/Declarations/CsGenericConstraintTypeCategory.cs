#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
namespace SourceGeneratorCommons.CSharp.Declarations;

enum CsGenericConstraintTypeCategory
{
    Any,
    Struct,
    Class,
    NullableClass,
    NotNull,
    Unmanaged,
}
