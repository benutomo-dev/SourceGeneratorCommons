﻿using Microsoft.CodeAnalysis;
using SourceGeneratorCommons.CSharpDeclarations;

namespace SourceGeneratorCommons;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    public static IncrementalValueProvider<CsDeclarationProvider> CreateCsDeclarationProvider(this IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider
            .Select(selector);

        static CsDeclarationProvider selector(Compilation compilation, CancellationToken cancellationToken)
        {
            return new CsDeclarationProvider(cancellationToken);
        }
    }
}
