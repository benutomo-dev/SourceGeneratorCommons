#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons.CSharp.Declarations;

static class CsAccessibilityExtensions
{
    public static CsAccessibility ToCSharpAccessibility(this Accessibility accessibility)
    {
        var cSharpAccessibility = accessibility switch
        {
            Accessibility.Public => CsAccessibility.Public,
            Accessibility.Internal => CsAccessibility.Internal,
            Accessibility.Protected => CsAccessibility.Protected,
            Accessibility.ProtectedAndInternal => CsAccessibility.ProtectedInternal,
            Accessibility.Private => CsAccessibility.Private,
            _ => CsAccessibility.Default,
        };
        return cSharpAccessibility;
    }
    public static (CsAccessibility Property, CsAccessibility Getter, CsAccessibility Setter) GetPropertyAccessibilitySet(this IPropertySymbol propertySymbol)
    {
        var propertyAccessibility = propertySymbol.DeclaredAccessibility.ToCSharpAccessibility();

        var getterAccessibility = getMethodOverwrideAccessibility(propertyAccessibility, propertySymbol.GetMethod);
        var setterAccessibility = getMethodOverwrideAccessibility(propertyAccessibility, propertySymbol.SetMethod);

        return (propertyAccessibility, getterAccessibility, setterAccessibility);

        static CsAccessibility getMethodOverwrideAccessibility(CsAccessibility propertyAccessibility, IMethodSymbol? methodSymbol)
        {
            if (methodSymbol is null)
                return CsAccessibility.Default;

            var methodAccessibility = methodSymbol.DeclaredAccessibility.ToCSharpAccessibility();

            if (propertyAccessibility == methodAccessibility)
                return CsAccessibility.Default;
            else
                return methodAccessibility;
        }
    }
}
