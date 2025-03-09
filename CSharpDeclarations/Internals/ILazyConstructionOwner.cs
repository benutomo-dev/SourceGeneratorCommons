namespace SourceGeneratorCommons.CSharpDeclarations.Internals;

internal interface ILazyConstructionOwner
{
    IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor);
}
