#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;
using SourceGeneratorCommons.CSharp.Declarations;

namespace SourceGeneratorCommons;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    public static IncrementalValueProvider<CsDeclarationProvider> CreateCsDeclarationProvider(this IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider
            .Select(selector);

        static CsDeclarationProvider selector(Compilation compilation, CancellationToken cancellationToken)
        {
            return new CsDeclarationProvider(compilation, cancellationToken);
        }
    }
}
