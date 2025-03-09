namespace SourceGeneratorCommons.Collections.Special;

internal delegate TValue CreateAddValue<TKey, TCreateArg, TValue>(TKey key, ref TCreateArg createArg);
