#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal static class CsReturnModifierExtensions
{
    public static CsReturnModifier ToCsReturnModifier(this IMethodSymbol methodSymbol)
    {
        var returnModifier = (methodSymbol.ReturnsByRef, methodSymbol.ReturnsByRefReadonly) switch
        {
            (_, true) => CsReturnModifier.RefReadonly,
            (true, _) => CsReturnModifier.Ref,
            _ => CsReturnModifier.Default,
        };

        return returnModifier;
    }
}
