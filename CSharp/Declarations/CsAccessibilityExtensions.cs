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
}
