namespace SourceGeneratorCommons;

internal record struct GenericTypeConstraints(
    GenericConstraintTypeCategory TypeCategory = GenericConstraintTypeCategory.Any,

    bool HaveDefaultConstructor = false,

    CsTypeReference? BaseType = null,

    EquatableArray<CsTypeReference> Interfaces = default

#if CODE_ANALYSYS4_12_2_OR_GREATER
    , bool AllowRefStruct = false
#endif
)
{

    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        IEnumerable<IConstructionFullCompleteFactor>? factors = null;

        if (BaseType?.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor) is { } baseTypeFactors)
        {
            factors = baseTypeFactors;
        }

        if (!Interfaces.IsDefaultOrEmpty)
        {
            foreach (var interfaceItem in Interfaces.Values)
            {
                if (interfaceItem.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor) is { } interfaceFactors)
                {
                    factors ??= [];
                    factors = factors.Concat(interfaceFactors);
                }
            }
        }

        return factors;
    }
}

