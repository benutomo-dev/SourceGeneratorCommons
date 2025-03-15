#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal record struct CsGenericTypeConstraints(
    CsGenericConstraintTypeCategory TypeCategory = CsGenericConstraintTypeCategory.Any,

    bool HaveDefaultConstructor = false,

    CsTypeReference? BaseType = null,

    EquatableArray<CsTypeReference> Interfaces = default

#if CODE_ANALYSYS4_12_2_OR_GREATER
    , bool AllowRefStruct = false
#endif
) : ILazyConstructionOwner
{
    public bool IsAny => true
        && TypeCategory == CsGenericConstraintTypeCategory.Any
        && !HaveDefaultConstructor
        && BaseType is null
        && Interfaces.IsDefaultOrEmpty;

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

