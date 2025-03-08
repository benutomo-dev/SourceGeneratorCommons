using System.Collections.Generic;

namespace SourceGeneratorCommons;

internal record struct GenericTypeParam(string Name, GenericTypeConstraints? Where = null)
{
    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        return Where?.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor);
    }
}
