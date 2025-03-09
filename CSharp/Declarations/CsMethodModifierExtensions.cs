#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal static class CsMethodModifierExtensions
{
    public static CsMethodModifier ToCsMethodModifier(this IMethodSymbol methodSymbol)
    {
        var methodModifier = (methodSymbol.IsSealed, methodSymbol.IsOverride, methodSymbol.IsAbstract, methodSymbol.IsVirtual) switch
        {
            (_, _, _, true) => CsMethodModifier.Virtual,
            (_, _, true, _) => CsMethodModifier.Abstract,
            (true, true, _, _) => CsMethodModifier.SealedOverride,
            (_, true, _, _) => CsMethodModifier.Override,
            _ => CsMethodModifier.Default,
        };

        return methodModifier;
    }
}
