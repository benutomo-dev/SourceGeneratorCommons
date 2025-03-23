#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal sealed class CsGenericTypeConstraints : ILazyConstructionOwner
{
    public CsGenericConstraintTypeCategory TypeCategory { get; }

    public bool HaveDefaultConstructor { get; }

    public CsTypeRef? BaseType { get; }

    public EquatableArray<CsTypeRef> Interfaces { get; }

    public bool AllowRefStruct { get; }

    public bool IsAny => true
        && TypeCategory == CsGenericConstraintTypeCategory.Any
        && !HaveDefaultConstructor
        && BaseType is null
        && Interfaces.IsDefaultOrEmpty;

    private static CsGenericTypeConstraints[] s_cached;

    static CsGenericTypeConstraints()
    {
        var instances = ((CsGenericConstraintTypeCategory[])Enum.GetValues(typeof(CsGenericConstraintTypeCategory)))
            .SelectMany(typeCategory =>
            {
                return new[]
                {
                    new CsGenericTypeConstraints(typeCategory, false, null, EquatableArray<CsTypeRef>.Empty, false),
                    new CsGenericTypeConstraints(typeCategory, false, null, EquatableArray<CsTypeRef>.Empty, true),
                    new CsGenericTypeConstraints(typeCategory, true, null, EquatableArray<CsTypeRef>.Empty, false),
                    new CsGenericTypeConstraints(typeCategory, true, null, EquatableArray<CsTypeRef>.Empty, true),
                };
            })
            .Select(v => (cacheKey: GetCacheKey(v.TypeCategory, v.HaveDefaultConstructor, v.AllowRefStruct), instance: v))
            .ToArray();

        var maxKeyValue = instances.Select(v => v.cacheKey).Max();

        s_cached = new CsGenericTypeConstraints[maxKeyValue + 1];
        foreach (var item in instances)
        {
            DebugSGen.Assert(s_cached[item.cacheKey] is null);
            s_cached[item.cacheKey] = item.instance;
        }
    }

    private CsGenericTypeConstraints(CsGenericConstraintTypeCategory typeCategory, bool haveDefaultConstructor, CsTypeRef? baseType, EquatableArray<CsTypeRef> interfaces, bool allowRefStruct)
    {
        TypeCategory = typeCategory;

        HaveDefaultConstructor = haveDefaultConstructor;

        BaseType = baseType;

        Interfaces = interfaces;

        AllowRefStruct = allowRefStruct;
    }

    private static int GetCacheKey(CsGenericConstraintTypeCategory typeCategory, bool haveDefaultConstructor, bool allowRefStruct)
    {
        int haveDefaultConstructorBit = (haveDefaultConstructor ? 1 : 0);

        int allowRefStructBit = (allowRefStruct ? 1 : 0) << 1;

        int typeCategoryBits = ((int)typeCategory) << 2;

        DebugSGen.Assert((typeCategoryBits & haveDefaultConstructorBit) == 0);
        DebugSGen.Assert((typeCategoryBits & allowRefStructBit) == 0);

        int cacheKey = typeCategoryBits | allowRefStructBit | haveDefaultConstructorBit;

        return cacheKey;
    }

    public static CsGenericTypeConstraints Get(CsGenericConstraintTypeCategory typeCategory, bool haveDefaultConstructor, CsTypeRef? baseType, EquatableArray<CsTypeRef> interfaces
#if CODE_ANALYSYS4_12_2_OR_GREATER
        , bool allowRefStruct
#endif
        )
    {
        if (baseType is null && interfaces.IsDefaultOrEmpty)
        {
#if CODE_ANALYSYS4_12_2_OR_GREATER
            var cacheKey = GetCacheKey(typeCategory, haveDefaultConstructor, allowRefStruct);
#else
            var cacheKey = GetCacheKey(typeCategory, haveDefaultConstructor, allowRefStruct: false);
#endif

            return s_cached[cacheKey];
        }
        else
        {
#if CODE_ANALYSYS4_12_2_OR_GREATER
            return new CsGenericTypeConstraints(typeCategory, haveDefaultConstructor, baseType, interfaces, allowRefStruct);
#else
            return new CsGenericTypeConstraints(typeCategory, haveDefaultConstructor, baseType, interfaces, allowRefStruct: false);
#endif
        }
    }

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

