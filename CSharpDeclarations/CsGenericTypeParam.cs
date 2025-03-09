using SourceGeneratorCommons.CSharpDeclarations.Internals;

namespace SourceGeneratorCommons.CSharpDeclarations;

internal record struct CsGenericTypeParam(string Name, CsGenericTypeConstraints? Where = null) : ILazyConstructionOwner
{
    public IEnumerable<IConstructionFullCompleteFactor>? GetConstructionFullCompleteFactors(bool rejectAlreadyCompletedFactor)
    {
        return Where?.GetConstructionFullCompleteFactors(rejectAlreadyCompletedFactor);
    }
}
