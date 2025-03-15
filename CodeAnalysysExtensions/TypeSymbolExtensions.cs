#if !ENABLE_SOURCE_GENERATOR_COMMONS_WARNING
#pragma warning disable
#endif
using Microsoft.CodeAnalysis;

namespace SourceGeneratorCommons;

internal static partial class TypeSymbolExtensions
{
    internal static bool IsInterlockedExchangeable(this ITypeSymbol typeSymbol)
    {
        return typeSymbol switch
        {
            { IsReferenceType: true } => true,
            { SpecialType: SpecialType.System_Int32 } => true,
            { SpecialType: SpecialType.System_Int64 } => true,
            { SpecialType: SpecialType.System_IntPtr } => true,
            { SpecialType: SpecialType.System_Single } => true,
            { SpecialType: SpecialType.System_Double } => true,
            _ => false,
        };
    }

    internal static bool IsXSymbolImpl(ITypeSymbol? typeSymbol, string ns1, string typeName)
    {
        DebugSGen.Assert(!ns1.Contains('.'));
        DebugSGen.Assert(!typeName.Contains('.'));

        if (typeSymbol is null) return false;

        if (typeSymbol.Name != typeName) return false;

        var containingNamespaceSymbol = typeSymbol.ContainingNamespace;

        if (containingNamespaceSymbol is null) return false;

        if (containingNamespaceSymbol.Name != ns1) return false;

        if (containingNamespaceSymbol.ContainingNamespace is null) return false;

        if (!containingNamespaceSymbol.ContainingNamespace.IsGlobalNamespace) return false;

        return true;
    }


    internal static bool IsAssignableToIXImpl(ITypeSymbol? typeSymbol, Func<ITypeSymbol, bool> isXTypeFunc)
    {
        if (typeSymbol is null) return false;

        if (isXTypeFunc(typeSymbol)) return true;

        if (typeSymbol.AllInterfaces.Any((Func<INamedTypeSymbol, bool>)isXTypeFunc)) return true;

        // ジェネリック型の型パラメータの場合は型パラメータの制約を再帰的に確認
        if (typeSymbol is ITypeParameterSymbol typeParameterSymbol && typeParameterSymbol.ConstraintTypes.Any(constraintType => IsAssignableToIXImpl(constraintType, isXTypeFunc)))
        {
            return true;
        }

        return false;
    }

    internal static bool IsXAttributedMemberImpl(ISymbol? symbol, Func<INamedTypeSymbol, bool> isXAttributeSymbol)
    {
        if (symbol is null) return false;

        foreach (var attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass is not null && isXAttributeSymbol(attributeData.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

}
