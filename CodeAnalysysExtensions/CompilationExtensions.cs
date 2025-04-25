#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons;

internal static partial class CompilationExtensions
{
    internal static INamedTypeSymbol? GetFirstTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
    {
        var typeSymbol = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);

        if (typeSymbol is not null)
            return typeSymbol;

        var typeSymbols = compilation.GetTypesByMetadataName(fullyQualifiedMetadataName);

        if (typeSymbols.Length == 0)
            return null;

        foreach (var typeSymbol2 in typeSymbols)
        {
            if (compilation.IsSymbolAccessibleWithin(typeSymbol2, compilation.Assembly))
                return typeSymbol2;
        }

        return typeSymbols[0];
    }
}
