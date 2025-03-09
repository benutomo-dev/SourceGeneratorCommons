#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.CSharp.Declarations.Internals;
using System.Collections.Generic;

namespace SourceGeneratorCommons.CSharp.Declarations;

record class CsMethodParam(
    CsTypeReference Type,
    string Name,
    CsParamModifier Modifier = CsParamModifier.Default,
    bool IsScoped = false
    ) : ILazyConstructionOwner
{
    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        return Type?.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor);
    }
}
