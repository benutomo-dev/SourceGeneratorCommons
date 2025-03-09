#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using SourceGeneratorCommons.CSharp.Declarations.Internals;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal record struct CsGenericTypeParam(string Name, CsGenericTypeConstraints? Where = null) : ILazyConstructionOwner
{
    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        return Where?.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor);
    }
}
