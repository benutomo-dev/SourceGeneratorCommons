namespace SourceGeneratorCommons;

internal record struct GenericTypeConstraints(
    GenericConstraintTypeCategory TypeCategory = GenericConstraintTypeCategory.Any,

    bool HaveDefaultConstructor = false,

    TypeReferenceInfo? BaseType = null,

    EquatableArray<TypeReferenceInfo> Interfaces = default

#if CODE_ANALYSYS4_12_2_OR_GREATER
    , bool AllowRefStruct = false
#endif
);

