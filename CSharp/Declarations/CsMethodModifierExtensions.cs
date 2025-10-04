#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal static class CsMethodModifierExtensions
{
    public static CsMethodModifier ToCsMethodModifier(this IPropertySymbol propertySymbol)
    {
        var methodModifier = ToCsMethodModifierCore(propertySymbol.IsSealed, propertySymbol.IsOverride, propertySymbol.IsAbstract, propertySymbol.IsVirtual);
        return methodModifier;
    }

    public static CsMethodModifier ToCsMethodModifier(this IMethodSymbol methodSymbol)
    {
        var methodModifier = ToCsMethodModifierCore(methodSymbol.IsSealed, methodSymbol.IsOverride, methodSymbol.IsAbstract, methodSymbol.IsVirtual);
        return methodModifier;
    }

    private static CsMethodModifier ToCsMethodModifierCore(bool isSealed, bool isOverride, bool isAbstract, bool isVirtual)
    {
        var methodModifier = (isSealed, isOverride, isAbstract, isVirtual) switch
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
