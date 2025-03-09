namespace SourceGeneratorCommons.CSharp.Declarations.Internals;

internal interface ILazyConstructionOwner
{
    IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor);
}
