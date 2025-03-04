using System.Collections.Immutable;

namespace SourceGeneratorCommons;

class GenericTypeConstraints
{
    public GenericConstraintTypeCategory TypeCategory { get; init; }

    public bool HaveDefaultConstructor { get; init; }

    public TypeReferenceInfo? BaseType { get; init; }

    public ImmutableArray<TypeReferenceInfo> Interfaces { get; init; }

#if CODE_ANALYSYS4_12_2_OR_GREATER
    public bool AllowRefStruct { get; init; }
#endif
}
