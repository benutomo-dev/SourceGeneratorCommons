using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons;

static class AccessibilityExtensions
{
    public static CSharpAccessibility ToCSharpAccessibility(this Accessibility accessibility)
    {
        var cSharpAccessibility = accessibility switch
        {
            Accessibility.Public => CSharpAccessibility.Public,
            Accessibility.Internal => CSharpAccessibility.Internal,
            Accessibility.Protected => CSharpAccessibility.Protected,
            Accessibility.ProtectedAndInternal => CSharpAccessibility.ProtectedInternal,
            Accessibility.Private => CSharpAccessibility.Private,
            _ => CSharpAccessibility.Default,
        };
        return cSharpAccessibility;
    }
}
