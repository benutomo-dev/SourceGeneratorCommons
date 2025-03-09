#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace SourceGeneratorCommons;

internal static partial class PropertySymbolExtensions
{
    internal static bool IsAutoImplementedProperty(this IPropertySymbol propertySymbol, CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in propertySymbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(cancellationToken);

            if (syntax is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                if (propertyDeclarationSyntax.ExpressionBody is not null)
                {
                    return false;
                }

                if (propertyDeclarationSyntax.AccessorList is null)
                {
                    continue;
                }

                foreach (var accessorDeclarationSyntax in propertyDeclarationSyntax.AccessorList.Accessors)
                {
                    if (accessorDeclarationSyntax.Body is not null)
                    {
                        return false;
                    }

                    if (accessorDeclarationSyntax.ExpressionBody is not null)
                    {
                        return false;
                    }
                }
            }
            else if (syntax is IndexerDeclarationSyntax indexerDeclarationSyntax)
            {
                if (indexerDeclarationSyntax.ExpressionBody is not null)
                {
                    return false;
                }

                if (indexerDeclarationSyntax.AccessorList is null)
                {
                    continue;
                }

                foreach (var accessorDeclarationSyntax in indexerDeclarationSyntax.AccessorList.Accessors)
                {
                    if (accessorDeclarationSyntax.Body is not null)
                    {
                        return false;
                    }

                    if (accessorDeclarationSyntax.ExpressionBody is not null)
                    {
                        return false;
                    }
                }
            }
            else
            {
                DebugSGen.Fail();
                return false;
            }
        }

        return true;
    }
}
