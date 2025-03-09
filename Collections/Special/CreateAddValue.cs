#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
namespace SourceGeneratorCommons.Collections.Special;

internal delegate TValue CreateAddValue<TKey, TCreateArg, TValue>(TKey key, ref TCreateArg createArg);
