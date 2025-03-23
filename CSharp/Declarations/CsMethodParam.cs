#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.Collections.Generic;
using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsMethodParam(
    CsTypeRefWithAnnotation Type,
    string Name,
    CsParamModifier Modifier = CsParamModifier.Default,
    bool IsScoped = false,
    EquatableArray<CsAttribute> Attributes = default
    ) : ILazyConstructionOwner
{
    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        return Type.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor);
    }

    public CsMethodParamWithDefaultValue WithDefaultValue(object? defaultValue)
    {
        return new CsMethodParamWithDefaultValue(Type, Name, defaultValue, Modifier, Attributes);
    }

    public CsMethodParam RemoveDefaultValue()
    {
        return new CsMethodParam(Type, Name, Modifier, IsScoped, Attributes);
    }
}
