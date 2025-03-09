﻿namespace SourceGeneratorCommons;

internal record struct GenericTypeParam(string Name, GenericTypeConstraints? Where = null) : ILazyConstructionOwner
{
    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        return Where?.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor);
    }
}
