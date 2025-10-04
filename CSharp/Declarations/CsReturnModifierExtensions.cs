#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons.CSharp.Declarations;

internal static class CsReturnModifierExtensions
{
    public static CsReturnModifier ToCsReturnModifier(this IPropertySymbol propertySymbol)
    {
        var returnModifier = ToCsReturnModifierCore(propertySymbol.ReturnsByRef, propertySymbol.ReturnsByRefReadonly);
        return returnModifier;
    }

    public static CsReturnModifier ToCsReturnModifier(this IMethodSymbol methodSymbol)
    {
        var returnModifier = ToCsReturnModifierCore(methodSymbol.ReturnsByRef, methodSymbol.ReturnsByRefReadonly);
        return returnModifier;
    }

    private static CsReturnModifier ToCsReturnModifierCore(bool returnsByRef, bool returnsByRefReadonly)
    {
        var returnModifier = (returnsByRef, returnsByRefReadonly) switch
        {
            (_, true) => CsReturnModifier.RefReadonly,
            (true, _) => CsReturnModifier.Ref,
            _ => CsReturnModifier.Default,
        };

        return returnModifier;
    }
}
