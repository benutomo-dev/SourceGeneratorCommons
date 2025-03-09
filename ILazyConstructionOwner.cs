namespace SourceGeneratorCommons;

internal interface ILazyConstructionOwner
{
    IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor);
}
