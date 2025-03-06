using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons;

static class AccessibilityExtensions
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
}
